// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MdShow
{
    public sealed class Document
    {
        public const int MaxBytes = 16 * 1024 * 1024;
        public string Id = Guid.NewGuid().ToString("N");
        public string Path = "";
        public string Text = "";
        public string OriginalText = "";
        public string DiskHash = "";
        public string EncodingName = "utf-8";
        public bool Bom;
        public string Newline = "\r\n";
        public bool Dirty { get { return Text != OriginalText; } }
        public string Name { get { return Path.Length == 0 ? L.T("untitled") : System.IO.Path.GetFileName(Path); } }

        public static string Normalize(string text) { return text.Replace("\r\n", "\n").Replace("\r", "\n"); }
        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        public static Document Read(string path)
        {
            path = System.IO.Path.GetFullPath(path);
            byte[] bytes = ReadBytes(path);
            Encoding encoding = new UTF8Encoding(false, true);
            int skip = 0;
            if (bytes.Take(4).SequenceEqual(new byte[] { 0xff, 0xfe, 0, 0 })) { encoding = new UTF32Encoding(false, true, true); skip = 4; }
            else if (bytes.Take(4).SequenceEqual(new byte[] { 0, 0, 0xfe, 0xff })) { encoding = new UTF32Encoding(true, true, true); skip = 4; }
            else if (bytes.Take(3).SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf })) skip = 3;
            else if (bytes.Take(2).SequenceEqual(new byte[] { 0xff, 0xfe })) { encoding = new UnicodeEncoding(false, true, true); skip = 2; }
            else if (bytes.Take(2).SequenceEqual(new byte[] { 0xfe, 0xff })) { encoding = new UnicodeEncoding(true, true, true); skip = 2; }
            string raw;
            try { raw = encoding.GetString(bytes, skip, bytes.Length - skip); }
            catch (DecoderFallbackException)
            {
                if (skip != 0) throw new IOException(L.T("invalidUnicode"));
                encoding = Encoding.GetEncoding(1251, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                raw = encoding.GetString(bytes);
            }
            if (raw.IndexOf('\0') >= 0) throw new IOException(L.T("binaryFile"));
            return new Document { Path = path, Text = Normalize(raw), OriginalText = Normalize(raw), DiskHash = Hash(bytes),
                EncodingName = encoding.WebName, Bom = skip > 0,
                Newline = raw.Contains("\r\n") ? "\r\n" : raw.Contains("\r") ? "\r" : "\n" };
        }

        // Check the opened file before allocation, rather than a potentially stale path length.
        public static byte[] ReadBytes(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > MaxBytes) throw new IOException(L.T("fileTooLarge"));
                using (var reader = new BinaryReader(stream)) return reader.ReadBytes((int)stream.Length);
            }
        }

        public bool ChangedOnDisk()
        {
            return Path.Length > 0 && (!File.Exists(Path) || new FileInfo(Path).Length > MaxBytes || Hash(ReadBytes(Path)) != DiskHash);
        }

        public void Save(string target, bool utf8)
        {
            target = System.IO.Path.GetFullPath(target);
            string encodingName = utf8 ? "utf-8" : EncodingName;
            bool bom = utf8 ? false : Bom;
            Encoding encoding = Encoding.GetEncoding(encodingName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            byte[] content = encoding.GetBytes(Text.Replace("\n", Newline));
            byte[] prefix = bom ? encoding.GetPreamble() : new byte[0];
            byte[] bytes = prefix.Concat(content).ToArray();
            AtomicWrite(target, bytes);
            Path = target;
            EncodingName = encodingName;
            Bom = bom;
            OriginalText = Text;
            DiskHash = Hash(bytes);
        }

        // The temporary file is on the same volume. A failed replace leaves the original intact.
        public static void AtomicWrite(string target, byte[] bytes)
        {
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(target)) File.Replace(temp, target, null);
                else File.Move(temp, target);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static string ResolveLocal(string documentPath, string relative)
        {
            if (String.IsNullOrEmpty(documentPath) || String.IsNullOrWhiteSpace(relative)) return null;
            relative = Uri.UnescapeDataString(relative).Replace('/', System.IO.Path.DirectorySeparatorChar);
            if (System.IO.Path.IsPathRooted(relative) || relative.Contains(":")) return null;
            string root = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(documentPath)) + System.IO.Path.DirectorySeparatorChar;
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
            // Do not follow directory junctions or symbolic links outside the document folder.
            for (string part = path; part != null && part.Length >= root.Length; part = System.IO.Path.GetDirectoryName(part))
                if ((File.Exists(part) || Directory.Exists(part)) && (File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0) return null;
            return path;
        }
    }
}
