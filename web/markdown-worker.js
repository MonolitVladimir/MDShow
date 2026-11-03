// SPDX-License-Identifier: MIT
'use strict';
importScripts('vendor/marked.js');
self.onmessage = event => {
  try {
    const text = event.data;
    if (typeof text !== 'string' || text.length > 1024 * 1024) throw new Error('previewInput');
    const html = marked.parse(text, { gfm: true, breaks: false });
    if (html.length > 2 * 1024 * 1024 || (html.match(/</g) || []).length > 20000) throw new Error('previewComplex');
    self.postMessage({ html });
  } catch (error) { self.postMessage({ error: error.message }); }
};
