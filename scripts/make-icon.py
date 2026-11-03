# SPDX-License-Identifier: MIT
"""Derive Windows resources from the approved artwork; never redraw the mark.

Requires Pillow 12.2.0 only when regenerating the checked-in assets. The regular
application build consumes the checked-in ICO and PNG without Python or Pillow.
"""
import hashlib
import io
from pathlib import Path
import struct

from PIL import Image, ImageFilter

root = Path(__file__).resolve().parents[1]
source = root / 'assets/logo-source.png'
expected = '51cae42be07e1a0846c741aac1753f1c10d6253d9a29de9b2495f844f89ef128'
if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
    raise SystemExit('Approved logo checksum changed; review the artwork before regenerating.')
original = Image.open(source).convert('RGBA')
# The supplied cutout has low-alpha coloured debris outside its solid silhouette.
# Close tiny alpha holes, then remove narrow protrusions; interior RGB is untouched.
mask = original.getchannel('A').point(lambda a: 255 if a >= 250 else 0)
mask = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(9))
mask = mask.filter(ImageFilter.MinFilter(17)).filter(ImageFilter.MaxFilter(17))
box = mask.getbbox()
mask = mask.filter(ImageFilter.GaussianBlur(0.65))
clean = original.copy()
clean.putalpha(mask)
clean = clean.crop((box[0]-3, box[1]-3, box[2]+3, box[3]+3))
side = max(clean.size)
square = Image.new('RGBA', (side+32, side+32))
square.alpha_composite(clean, ((side+32-clean.width)//2, (side+32-clean.height)//2))

def resize(size):
    # Premultiplied alpha prevents dark/coloured fringes when reducing the icon.
    return square.convert('RGBa').resize((size,size), Image.Resampling.LANCZOS).convert('RGBA')

resize(512).save(root/'assets/app-master.png', optimize=True)
resize(64).save(root/'web/logo.png', optimize=True)
frames = []
for size in (16,24,32,48,64,128,256):
    image = resize(size)
    stream = io.BytesIO()
    # Palette PNG frames keep the ICO small; alpha remains part of the palette.
    image.quantize(colors=256, method=Image.Quantize.FASTOCTREE).save(stream,format='PNG',optimize=True)
    frames.append((size,stream.getvalue()))
offset = 6 + 16*len(frames)
directory = bytearray(struct.pack('<HHH',0,1,len(frames)))
for size,data in frames:
    directory.extend(struct.pack('<BBBBHHII',size%256,size%256,0,0,1,32,len(data),offset))
    offset += len(data)
(root/'assets/app.ico').write_bytes(directory + b''.join(data for _,data in frames))
print('Generated transparent master, web logo and ICO: 16/24/32/48/64/128/256.')
