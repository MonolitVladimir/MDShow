// SPDX-License-Identifier: MIT
/* Pure text operations shared by the editor and Node tests. */
(function (root) {
  'use strict';
  const normalize = text => text.replace(/\r\n?/g, '\n');
  function matches(text, query, matchCase) {
    if (!query) return [];
    const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const re = new RegExp(escaped, matchCase ? 'gu' : 'giu');
    return Array.from(text.matchAll(re), m => ({ start: m.index, end: m.index + m[0].length }));
  }
  function replaceAll(text, query, replacement, matchCase) {
    const found = matches(text, query, matchCase);
    for (let i = found.length - 1; i >= 0; i--) text = text.slice(0, found[i].start) + replacement + text.slice(found[i].end);
    return { text, count: found.length };
  }
  function format(text, start, end, kind, translate) {
    const label = (key, fallback) => translate ? translate(key) : fallback;
    const selected = text.slice(start, end);
    const wrappers = { bold: ['**', '**', label('bold', 'Bold')], italic: ['*', '*', label('italic', 'Italic')], strike: ['~~', '~~', label('strike', 'Text')], code: ['`', '`', label('code', 'Code')], link: ['[', '](https://example.com)', label('link', 'Link')] };
    if (wrappers[kind]) {
      let [before, after, placeholder] = wrappers[kind];
      if (kind === 'code' && selected.includes('\n')) { before = '```\n'; after = '\n```'; }
      const value = selected || placeholder;
      return { start, end, insert: before + value + after, selectionStart: start + before.length, selectionEnd: start + before.length + value.length };
    }
    const prefixes = { heading: '## ', quote: '> ', list: '- ', task: '- [ ] ' };
    if (prefixes[kind]) {
      start = text.lastIndexOf('\n', start - 1) + 1;
      const value = text.slice(start, end);
      const insert = value.split('\n').map(line => prefixes[kind] + line).join('\n');
      return { start, end, insert, selectionStart: start + insert.length, selectionEnd: start + insert.length };
    }
    const column = label('column', 'Column'), value = label('value', 'Value');
    const insert = `\n| ${column} 1 | ${column} 2 |\n| --- | --- |\n| ${value} | ${value} |\n`;
    return { start, end, insert, selectionStart: start + insert.length, selectionEnd: start + insert.length };
  }
  function shortcutCode(event) {
    if (event.code) return event.code;
    // Windows accessibility/virtual-key input can omit KeyboardEvent.code.
    // keyCode remains layout-independent, unlike event.key on a Russian keyboard.
    if (event.keyCode >= 65 && event.keyCode <= 90) return 'Key' + String.fromCharCode(event.keyCode);
    if (event.keyCode >= 48 && event.keyCode <= 57) return 'Digit' + (event.keyCode - 48);
    return ({ 9: 'Tab', 107: 'NumpadAdd', 109: 'NumpadSubtract', 187: 'Equal', 189: 'Minus' })[event.keyCode] || '';
  }
  const api = { normalize, matches, replaceAll, format, shortcutCode, words: text => (text.match(/[\p{L}\p{N}]+(?:[-’'][\p{L}\p{N}]+)*/gu) || []).length };
  if (typeof module !== 'undefined') module.exports = api;
  else root.MDCore = api;
})(typeof window !== 'undefined' ? window : globalThis);
