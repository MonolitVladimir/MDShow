// SPDX-License-Identifier: MIT
'use strict';
const $ = id => document.getElementById(id);
const docs = new Map();
let activeId = null;
let settings = { Theme: 'system', Mode: 'split', ZoomPercent: 100, Wrap: true, Language: 'auto', EditorWidth: 48 };
let systemLanguage = 'en-US';
const t = (...args) => MDI18n.t(...args);
const displayName = d => d.path === '' ? t('untitled') : d.name;
function applyLanguage() {
  const language = MDI18n.select(settings.Language, systemLanguage);
  document.documentElement.lang = language; document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
  MDI18n.translate(); $('language').value = MDI18n.languages.length === 1 ? language : settings.Language; document.body.dataset.dropLabel = t('drop');
  for (const d of docs.values()) { d.editor.setAttribute('aria-label', t('editorLabel')); d.editor.placeholder = t('editorPlaceholder'); }
}
let renderTimer, toastTimer, findIndex = -1;
let renderVersion = 0, cancelParse = null;
let startupTrace = false;
function startupStage(stage) { if (startupTrace) requestAnimationFrame(() => requestAnimationFrame(() => send('startupStage', { stage }))); }
function startupCheckpoint(stage) { if (startupTrace) send('startupStage', { stage }); }
const systemTheme = matchMedia('(prefers-color-scheme: dark)');
const active = () => docs.get(activeId);
const send = (action, extra = {}) => window.chrome.webview.postMessage({ action, id: activeId, ...extra });
function notice(message) { $('toast').textContent = message; $('toast').hidden = false; clearTimeout(toastTimer); toastTimer = setTimeout(() => $('toast').hidden = true, 3500); }
function persist() { send('settings', { theme: settings.Theme, mode: settings.Mode, zoomPercent: settings.ZoomPercent, wrap: settings.Wrap, language: settings.Language, editorWidth: settings.EditorWidth }); }
function applySettings() {
  document.body.classList.toggle('dark', settings.Theme === 'dark' || (settings.Theme === 'system' && systemTheme.matches));
  $('theme-label').textContent = t(settings.Theme);
  $('wrap-label').textContent = t(settings.Wrap ? 'on' : 'off');
  document.documentElement.style.setProperty('--editor-size', 15 * settings.ZoomPercent / 100 + 'px');
  document.documentElement.style.setProperty('--preview-size', 16 * settings.ZoomPercent / 100 + 'px');
  $('zoom').textContent = settings.ZoomPercent + '%';
  $('zoom').setAttribute('aria-label', t('zoomReset', settings.ZoomPercent));
  $('zoom').title = t('zoomReset', settings.ZoomPercent) + ' · Ctrl+0';
  document.documentElement.style.setProperty('--editor-width', settings.EditorWidth + '%');
  $('splitter').setAttribute('aria-valuenow', settings.EditorWidth);
  $('splitter').setAttribute('aria-valuemin', 20); $('splitter').setAttribute('aria-valuemax', 80);
  $('zoom-out').disabled = settings.ZoomPercent <= 50;
  $('zoom-in').disabled = settings.ZoomPercent >= 300;
  $('panes').className = 'panes mode-' + settings.Mode;
  $('formatbar').hidden = settings.Mode === 'read';
  document.querySelectorAll('[data-mode]').forEach(b => { b.classList.toggle('selected', b.dataset.mode === settings.Mode); b.setAttribute('aria-pressed', b.dataset.mode === settings.Mode); });
  for (const d of docs.values()) d.editor.classList.toggle('no-wrap', !settings.Wrap);
}
systemTheme.addEventListener('change', applySettings);
function setMode(mode) { settings.Mode = mode; applySettings(); persist(); }
function selectAllText() {
  const field = document.activeElement, selection = window.getSelection();
  selection.removeAllRanges();
  if (field.matches('textarea, input[type="text"]')) { field.select(); return; }
  const target = $('help').open ? $('help') :
    settings.Mode === 'read' || $('preview-scroll').contains(field) ? $('preview') : null;
  if (target) {
    const range = document.createRange(); range.selectNodeContents(target); selection.addRange(range);
  } else if (active()) { active().editor.focus(); active().editor.select(); }
}
function setZoom(percent) { settings.ZoomPercent = Math.max(50, Math.min(300, Math.round(percent))); applySettings(); persist(); }
function renderTabs() {
  const focused = document.activeElement.closest?.('.tab');
  const focusId = focused?.dataset.id, focusClose = document.activeElement.classList.contains('tab-close');
  $('tabs').replaceChildren();
  for (const d of [...docs.values()].reverse()) {
    const tab = document.createElement('div'); tab.className = 'tab' + (d.id === activeId ? ' active' : '');
    tab.dataset.id = d.id; tab.setAttribute('role', 'tab'); tab.setAttribute('aria-selected', d.id === activeId); tab.tabIndex = d.id === activeId ? 0 : -1; tab.title = d.path || d.name;
    const title = document.createElement('span'); title.className = 'tab-title'; title.textContent = displayName(d);
    const dot = document.createElement('span'); dot.className = 'tab-dot'; dot.textContent = d.text !== d.original ? '●' : '';
    const close = document.createElement('button'); close.className = 'tab-close'; close.textContent = '×'; close.setAttribute('aria-label', t('closeTab', displayName(d)));
    close.onclick = e => { e.stopPropagation(); send('close', { id: d.id }); };
    tab.append(title, dot, close); tab.onclick = () => activate(d.id);
    tab.onkeydown = e => { if (e.key === 'Enter' || e.key === ' ') activate(d.id); if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') { e.preventDefault(); cycleTab((e.key === 'ArrowLeft' ? -1 : 1) * (MDI18n.locale === 'ar' ? -1 : 1)); $('tabs').querySelector('.active')?.focus(); } };
    $('tabs').append(tab);
    if (focusId === d.id) (focusClose ? close : tab).focus();
  }
  $('tabs').querySelector('.active')?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
}
function updateStatus() {
  const d = active();
  const before = d ? d.editor.value.slice(0, d.editor.selectionStart) : '';
  $('position').textContent = t('position', before.split('\n').length, before.length - before.lastIndexOf('\n'));
  $('words').textContent = t('stats', d ? MDCore.words(d.text).toLocaleString(MDI18n.locale) : 0, d ? d.text.length.toLocaleString(MDI18n.locale) : 0);
  $('encoding').textContent = (d?.encoding || 'UTF-8').toUpperCase(); $('newline').textContent = d?.newline || 'CRLF';
}
function changed(d) {
  d.text = d.editor.value;
  send('change', { id: d.id, text: d.text });
  renderTabs(); updateStatus(); clearTimeout(renderTimer); renderTimer = setTimeout(render, d.text.length > 200000 ? 600 : 140);
}
function receiveDocument(data, saved) {
  startupCheckpoint('documentReceived');
  if (data.original === null) data = { ...data, original: data.text };
  let d = docs.get(data.id);
  if (d && saved) { Object.assign(d, data); renderTabs(); updateStatus(); return; }
  if (!d) {
    const editor = document.createElement('textarea'); editor.className = 'editor'; editor.spellcheck = false; editor.dir = 'auto'; editor.setAttribute('aria-label', t('editorLabel')); editor.placeholder = t('editorPlaceholder');
    d = { ...data, editor, previewScroll: 0 }; docs.set(d.id, d); $('editors').append(editor);
    editor.addEventListener('input', () => changed(d));
    editor.addEventListener('click', updateStatus); editor.addEventListener('keyup', updateStatus); editor.addEventListener('select', updateStatus);
    editor.addEventListener('keydown', e => {
      if (e.isComposing || e.keyCode === 229) return;
      if (e.key === 'Tab' && !e.ctrlKey && !e.altKey && !e.shiftKey) { e.preventDefault(); insert(d, '    '); }
      if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); $('tabs').querySelector('.active')?.focus(); }
      if (e.key === 'Enter' && !e.ctrlKey && !e.shiftKey && editor.selectionStart === editor.selectionEnd) {
        const line = editor.value.slice(0, editor.selectionStart).split('\n').pop();
        const match = line.match(/^(\s*)([-*+] |\d+\. )(\[[ xX]\] )?(.*)$/);
        if (match) {
          e.preventDefault();
          if (!match[4].trim()) insert(d, '', editor.selectionStart - line.length, editor.selectionStart);
          else { const prefix = /^\d/.test(match[2]) ? (parseInt(match[2], 10) + 1) + '. ' : match[2]; insert(d, '\n' + match[1] + prefix + (match[3] ? '[ ] ' : '')); }
        }
      }
    });
  } else Object.assign(d, data);
  startupCheckpoint('editorValueBegin');
  // Avoid rescanning auto direction for every internal text node while a large
  // value is assigned. Restore native first-strong direction before display/input.
  d.editor.dir = 'ltr';
  d.editor.value = data.text;
  d.editor.dir = 'auto';
  startupCheckpoint('editorValueEnd');
  d.editor.setSelectionRange(0, 0);
  startupCheckpoint('editorSelectionSet');
  d.editor.scrollTop = 0;
  startupCheckpoint('editorScrollSet');
  activate(d.id);
}
function activate(id) {
  if (!docs.has(id)) return;
  if (active()) active().previewScroll = $('preview-scroll').scrollTop;
  activeId = id;
  for (const d of docs.values()) d.editor.hidden = d.id !== id;
  findIndex = -1; applySettings(); startupCheckpoint('activationSettings'); renderTabs(); startupCheckpoint('activationTabs'); updateStatus(); startupCheckpoint('activationStatus');
  const previewScroll = active().previewScroll;
  const rendered = render().then(ok => { if (activeId === id) { if (ok) $('preview-scroll').scrollTop = previewScroll; startupStage('documentReady'); } return ok; });
  startupCheckpoint('activationRender');
  send('active');
  if (settings.Mode !== 'read') active().editor.focus();
  startupCheckpoint('activationFocus');
  return rendered;
}
function cycleTab(direction = 1) { const ids = [...docs.keys()].reverse(); if (ids.length) activate(ids[(ids.indexOf(activeId) + direction + ids.length) % ids.length]); }
function previewError(code) { const error = new Error(t(code)); error.code = code; return error; }
function parseMarkdown(text) {
  if (cancelParse) cancelParse();
  if (text.length > 1024 * 1024) return Promise.reject(previewError('previewInput'));
  return new Promise((resolve, reject) => {
    const worker = new Worker('markdown-worker.js');
    const finish = (error, html) => {
      clearTimeout(timer); worker.terminate();
      if (cancelParse === cancel) cancelParse = null;
      if (error) reject(error); else resolve(html);
    };
    const cancel = () => finish(previewError('previewCancelled'));
    const timer = setTimeout(() => finish(previewError('previewTimeout')), 1500);
    cancelParse = cancel;
    worker.onmessage = e => finish(e.data.error ? previewError(['previewInput', 'previewComplex'].includes(e.data.error) ? e.data.error : 'previewFailed') : null, e.data.html);
    worker.onerror = e => { e.preventDefault(); finish(previewError('previewFailed')); };
    worker.postMessage(text);
  });
}
function safeMarkdown(html, id) {
  const fragment = DOMPurify.sanitize(html, {
    RETURN_DOM_FRAGMENT: true, USE_PROFILES: { html: true },
    FORBID_TAGS: ['style', 'script', 'iframe', 'form', 'button', 'textarea', 'select', 'video', 'audio', 'object', 'embed', 'link', 'meta', 'base'],
    FORBID_ATTR: ['style', 'srcset', 'id', 'name', 'autofocus', 'tabindex'], ALLOW_DATA_ATTR: false
  });
  fragment.querySelectorAll('input').forEach(el => { if (el.type !== 'checkbox') el.remove(); else el.disabled = true; });
  fragment.querySelectorAll('a').forEach(a => {
    const href = a.getAttribute('href') || '';
    if (/^(https?:|mailto:)/i.test(href)) { a.dataset.external = href; a.title = href; }
    else if (href.startsWith('#')) a.dataset.anchor = href.slice(1);
    else if (href && !/^(?:[a-z][a-z\d+.-]*:|[\\/]{1,2})/i.test(href)) a.dataset.local = href;
    a.setAttribute('href', '#'); a.removeAttribute('target'); a.removeAttribute('download');
  });
  fragment.querySelectorAll('img').forEach(img => {
    const src = img.getAttribute('src') || '';
    if (/^data:image\/(png|jpeg|gif|webp|bmp);base64,/i.test(src)) return;
    if (src && !/^(?:[a-z][a-z\d+.-]*:|[\\/]{1,2})/i.test(src)) {
      img.src = 'https://mdshow.invalid/media/' + id + '/' + src;
      img.addEventListener('error', () => { img.alt = t('imageMissing', img.alt || src); }, { once: true });
    } else { const label = document.createElement('span'); label.className = 'blocked-image'; label.textContent = t('imageBlocked', img.alt || t('externalImages')); img.replaceWith(label); }
  });
  fragment.querySelectorAll('pre > code').forEach(code => {
    const language = [...code.classList].find(c => c.startsWith('language-'));
    if (language) { const label = document.createElement('span'); label.className = 'code-label'; label.textContent = language.slice(9); code.parentElement.prepend(label); }
  });
  return fragment;
}
function showPreview(fragment) {
  // Resolve the first strong character once. dir=auto on the large live tree
  // repeatedly rescans descendants while they are inserted into WebView2.
  const direction = document.createElement('span');
  direction.dir = 'auto'; direction.textContent = fragment.textContent;
  $('preview').dir = direction.matches(':dir(rtl)') ? 'rtl' : 'ltr';
  $('preview').replaceChildren(fragment);
}
async function render() {
  clearTimeout(renderTimer);
  const version = ++renderVersion;
  const d = active(); $('outline').replaceChildren();
  if (!d || !d.text.trim()) {
    if (cancelParse) cancelParse();
    $('preview').dir = MDI18n.locale === 'ar' ? 'rtl' : 'ltr';
    $('preview').innerHTML = '<div class="empty-preview"><img class="empty-logo" src="logo.png" width="64" height="64" alt=""><h1 data-i18n="emptyTitle"></h1><p data-i18n="emptyText"></p><button class="primary" data-action="open" data-i18n="open"></button></div>';
    MDI18n.translate($('preview'));
    $('heading-count').textContent = '0'; sideEmpty('outline', 'emptyOutline'); return true;
  }
  try {
    const text = d.text;
    const html = await parseMarkdown(text);
    if (version !== renderVersion || activeId !== d.id || d.text !== text) return false;
    showPreview(safeMarkdown(html, d.id));
  }
  catch (error) {
    if (version === renderVersion) { $('preview').textContent = error.message; $('heading-count').textContent = '0'; }
    return false;
  }
  const headings = $('preview').querySelectorAll('h1,h2,h3,h4,h5,h6');
  const slugs = new Map();
  headings.forEach((h, index) => {
    const base = h.textContent.toLowerCase().trim().replace(/[^\p{L}\p{N}\s_-]/gu, '').replace(/\s/g, '-');
    const count = slugs.get(base) || 0; slugs.set(base, count + 1); h.id = base + (count ? '-' + count : '');
    const button = document.createElement('button'); button.textContent = h.textContent; button.title = h.textContent;
    button.dir = 'auto'; button.style.paddingInlineStart = (10 + (Number(h.tagName[1]) - 1) * 10) + 'px';
    button.onclick = () => { if (settings.Mode === 'edit') setMode('split'); h.scrollIntoView({ block: 'start', behavior: 'smooth' }); };
    $('outline').append(button);
  });
  $('heading-count').textContent = headings.length;
  if (!headings.length) sideEmpty('outline', 'addHeadings');
  updateFindCount();
  return true;
}
function insert(d, text, start = d.editor.selectionStart, end = d.editor.selectionEnd) {
  if (!d) return;
  d.editor.focus(); d.editor.setSelectionRange(start, end);
  // Chromium's edit command keeps formatting and replacement in the native undo stack.
  if (!document.execCommand('insertText', false, text)) { d.editor.setRangeText(text, start, end, 'end'); changed(d); }
  updateStatus();
}
function format(kind) {
  const d = active(); if (!d) return;
  if (settings.Mode === 'read') setMode('split');
  const edit = MDCore.format(d.text, d.editor.selectionStart, d.editor.selectionEnd, kind, t);
  insert(d, edit.insert, edit.start, edit.end); d.editor.setSelectionRange(edit.selectionStart, edit.selectionEnd);
}
function found() { return MDCore.matches(active()?.text || '', $('find').value, $('match-case').checked); }
function updateFindCount() { const items = found(); $('find-count').textContent = items.length ? `${findIndex >= 0 ? findIndex + 1 : '—'}/${items.length}` : $('find').value ? '0' : ''; }
function findNext(direction = 1) {
  const d = active(), items = found(); if (!d || !items.length) { updateFindCount(); return; }
  findIndex = (findIndex + direction + items.length) % items.length;
  if (settings.Mode === 'read') setMode('split');
  const item = items[findIndex]; d.editor.focus(); d.editor.setSelectionRange(item.start, item.end);
  const line = d.text.slice(0, item.start).split('\n').length - 1;
  d.editor.scrollTop = Math.max(0, line * 15 * settings.ZoomPercent / 100 * 1.8 - d.editor.clientHeight / 3);
  updateFindCount(); updateStatus();
}
function toggleFind() { $('findbar').hidden = false; $('find').focus(); $('find').select(); updateFindCount(); }
async function exportHtml() {
  if (!await render()) return;
  const clone = $('preview').cloneNode(true);
  clone.querySelectorAll('a').forEach(a => { a.href = a.dataset.external || a.dataset.local || '#' + (a.dataset.anchor || ''); for (const key of Object.keys(a.dataset)) delete a.dataset[key]; });
  const originalImages = $('preview').querySelectorAll('img');
  clone.querySelectorAll('img').forEach((img, i) => {
    const source = originalImages[i];
    try { if (!source.complete || !source.naturalWidth) throw new Error(); const canvas = document.createElement('canvas'); canvas.width = source.naturalWidth; canvas.height = source.naturalHeight; canvas.getContext('2d').drawImage(source, 0, 0); img.src = canvas.toDataURL('image/png'); }
    catch { img.replaceWith(document.createTextNode(img.alt || t('imageUnavailable'))); }
  });
  const title = document.createElement('span'); title.textContent = active()?.name || 'Markdown';
  const css = 'body{max-width:860px;margin:40px auto;padding:0 24px;color:#202735;font:16px/1.75 Segoe UI,sans-serif;overflow-wrap:anywhere}h1,h2,h3{line-height:1.35}h1{border-bottom:1px solid #ddd;padding-bottom:16px}a{color:#4162dd}pre,code{background:#f2f4f8;border-radius:5px;font-family:Consolas,monospace}pre{padding:20px;overflow:auto}code{padding:2px 4px}pre code{padding:0}blockquote{border-inline-start:3px solid #4162dd;padding:1px 20px;background:#f5f7ff}table{border-collapse:collapse}th,td{border:1px solid #ddd;padding:8px 14px}img{max-width:100%}pre,code{direction:ltr;unicode-bidi:isolate;text-align:left}a{unicode-bidi:isolate}body{font-family:Segoe UI,Microsoft YaHei UI,sans-serif}.code-label{display:block;font-size:11px;color:#888}';
  send('exportHtml', { html: '<!doctype html><html lang="' + MDI18n.locale + '"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><meta http-equiv="Content-Security-Policy" content="default-src &#39;none&#39;; img-src data:; style-src &#39;unsafe-inline&#39;; base-uri &#39;none&#39;; form-action &#39;none&#39;"><title>' + title.innerHTML + '</title><style>' + css + '</style></head><body dir="' + $('preview').dir + '">' + clone.innerHTML + '</body></html>' });
}
async function action(name) {
  $('menu').hidden = true; $('menu-toggle').setAttribute('aria-expanded', 'false');
  const d = active();
  switch (name) {
    case 'find': toggleFind(); break;
    case 'closeFind': $('findbar').hidden = true; d?.editor.focus(); break;
    case 'next': findNext(); break;
    case 'previous': findNext(-1); break;
    case 'replace': { if (!d) break; const item = found().find(m => m.start === d.editor.selectionStart && m.end === d.editor.selectionEnd); if (item) insert(d, $('replacement').value, item.start, item.end); findIndex = -1; findNext(); break; }
    case 'replaceAll': { if (!d) break; const result = MDCore.replaceAll(d.text, $('find').value, $('replacement').value, $('match-case').checked); if (result.count) insert(d, result.text, 0, d.text.length); findIndex = -1; updateFindCount(); notice(t('replacements', result.count)); break; }
    case 'theme': settings.Theme = ({ system: 'light', light: 'dark', dark: 'system' })[settings.Theme]; applySettings(); persist(); break;
    case 'wrap': settings.Wrap = !settings.Wrap; applySettings(); persist(); break;
    case 'sidebar': $('sidebar').hidden = !$('sidebar').hidden; break;
    case 'zoomIn': setZoom(Math.floor(settings.ZoomPercent / 10) * 10 + 10); break;
    case 'zoomOut': setZoom(Math.ceil(settings.ZoomPercent / 10) * 10 - 10); break;
    case 'zoomReset': setZoom(100); break;
    case 'undo': case 'redo': if (d) { d.editor.focus(); document.execCommand(name); } break;
    case 'help': $('help').showModal(); break;
    case 'exportHtml': if (d) await exportHtml(); break;
    case 'exportPdf': case 'print': if (d && await render()) send(name); break;
    default: send(name);
  }
}
document.addEventListener('click', e => {
  const button = e.target.closest('[data-action]'); if (button) action(button.dataset.action);
  const formatButton = e.target.closest('[data-format]'); if (formatButton) format(formatButton.dataset.format);
  const modeButton = e.target.closest('[data-mode]'); if (modeButton) setMode(modeButton.dataset.mode);
  if (!e.target.closest('.menu-anchor')) { $('menu').hidden = true; $('menu-toggle').setAttribute('aria-expanded', 'false'); }
});
function toggleMenu() { $('menu').hidden = !$('menu').hidden; $('menu-toggle').setAttribute('aria-expanded', !$('menu').hidden); if (!$('menu').hidden) $('menu').querySelector('button').focus(); }
$('menu-toggle').onclick = toggleMenu;
$('menu-toggle').onkeydown = e => { if (e.key === 'ArrowDown' && $('menu').hidden) { e.preventDefault(); toggleMenu(); } };
$('menu').onkeydown = e => {
  if (e.target.tagName === 'SELECT' || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key)) return;
  const items = [...$('menu').querySelectorAll('button, summary, select')].filter(el => el.getClientRects().length && !el.disabled);
  const index = items.indexOf(document.activeElement);
  const next = e.key === 'Home' ? 0 : e.key === 'End' ? items.length - 1 : (index + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
  e.preventDefault(); items[next]?.focus();
};
$('menu').onfocusout = () => { setTimeout(() => { if (!$('menu').contains(document.activeElement) && document.activeElement !== $('menu-toggle')) { $('menu').hidden = true; $('menu-toggle').setAttribute('aria-expanded', 'false'); } }, 0); };
$('language').onchange = async () => {
  const scroll = $('preview-scroll').scrollTop;
  settings.Language = $('language').value; applyLanguage(); applySettings(); renderTabs(); updateStatus(); recent(settings.Recent || []); persist();
  await render(); $('preview-scroll').scrollTop = scroll;
};
$('preview').onclick = e => {
  const a = e.target.closest('a'); if (!a) return; e.preventDefault();
  if (a.dataset.external) send('external', { url: a.dataset.external });
  else if (a.dataset.local) send('localLink', { url: a.dataset.local.split('#')[0] });
  else if (a.dataset.anchor) { try { const target = [...$('preview').querySelectorAll('[id]')].find(h => h.id === decodeURIComponent(a.dataset.anchor)); target?.scrollIntoView({ block: 'start', behavior: 'smooth' }); } catch {} }
};
$('find').oninput = () => { findIndex = -1; updateFindCount(); };
$('match-case').onchange = () => { findIndex = -1; updateFindCount(); };
$('findbar').onkeydown = e => { if (e.key === 'Enter') { e.preventDefault(); findNext(e.shiftKey ? -1 : 1); } };
document.addEventListener('keydown', e => {
  if (e.isComposing || e.keyCode === 229) return;
  if (e.altKey && !e.ctrlKey && MDCore.shortcutCode(e) === 'KeyF') { e.preventDefault(); toggleMenu(); return; }
  if (e.key === 'F1') { e.preventDefault(); action('help'); return; }
  if (e.key === 'Escape') {
    if (!$('menu').hidden) { $('menu').hidden = true; $('menu-toggle').setAttribute('aria-expanded', 'false'); $('menu-toggle').focus(); }
    else if (!$('findbar').hidden) { $('findbar').hidden = true; active()?.editor.focus(); }
    return;
  }
  if (!e.ctrlKey || e.altKey) return;
  const key = MDCore.shortcutCode(e);
  if (key === 'KeyA' && !e.shiftKey) { e.preventDefault(); selectAllText(); return; }
  const actions = { KeyN: 'new', KeyO: 'open', KeyS: e.shiftKey ? 'saveAs' : 'save', KeyW: 'close', KeyF: 'find', KeyH: 'find', KeyP: 'print' };
  if (actions[key]) { e.preventDefault(); action(actions[key]); return; }
  if (['Digit1', 'Digit2', 'Digit3'].includes(key)) { e.preventDefault(); setMode({ Digit1: 'edit', Digit2: 'split', Digit3: 'read' }[key]); return; }
  if (key === 'Tab') { e.preventDefault(); cycleTab(e.shiftKey ? -1 : 1); return; }
  if (['KeyB', 'KeyI', 'KeyK'].includes(key)) { e.preventDefault(); format({ KeyB: 'bold', KeyI: 'italic', KeyK: 'link' }[key]); return; }
  if (['Equal', 'NumpadAdd', 'Minus', 'NumpadSubtract'].includes(key)) { e.preventDefault(); action(key === 'Minus' || key === 'NumpadSubtract' ? 'zoomOut' : 'zoomIn'); }
  if (key === 'Digit0') { e.preventDefault(); action('zoomReset'); }
});
$('panes').addEventListener('wheel', e => {
  if (!e.ctrlKey || e.deltaY === 0) return;
  e.preventDefault(); action(e.deltaY < 0 ? 'zoomIn' : 'zoomOut');
}, { passive: false });
let dragDepth = 0;
document.addEventListener('dragenter', e => { if (e.dataTransfer.types.includes('Files')) { e.preventDefault(); dragDepth++; document.body.classList.add('drop-active'); } });
document.addEventListener('dragover', e => { if (e.dataTransfer.types.includes('Files')) e.preventDefault(); });
document.addEventListener('dragleave', () => { if (--dragDepth <= 0) document.body.classList.remove('drop-active'); });
document.addEventListener('drop', e => { e.preventDefault(); dragDepth = 0; document.body.classList.remove('drop-active'); if (e.dataTransfer.files.length) window.chrome.webview.postMessageWithAdditionalObjects({ action: 'drop' }, Array.from(e.dataTransfer.files)); });
function setPanelWidth(value) { settings.EditorWidth = Math.max(20, Math.min(80, Math.round(value))); applySettings(); }
$('splitter').onpointerdown = e => { $('splitter').setPointerCapture(e.pointerId); };
$('splitter').onpointermove = e => { if (!$('splitter').hasPointerCapture(e.pointerId)) return; const r = $('panes').getBoundingClientRect(); setPanelWidth((MDI18n.locale === 'ar' ? r.right - e.clientX : e.clientX - r.left) / r.width * 100); };
$('splitter').onpointerup = () => persist();
$('splitter').onkeydown = e => { if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return; e.preventDefault(); setPanelWidth(settings.EditorWidth + (e.key === 'ArrowLeft' ? -3 : 3) * (MDI18n.locale === 'ar' ? -1 : 1)); persist(); };
function sideEmpty(id, key) { const p = document.createElement('p'); p.className = 'side-empty'; p.textContent = t(key); $(id).replaceChildren(p); }
function recent(paths) { $('recent').replaceChildren(); for (const path of paths.slice(0, 8)) { const b = document.createElement('button'); b.textContent = '↗  ' + path.split(/[\\/]/).pop(); b.title = path; b.onclick = () => send('recent', { path }); $('recent').append(b); } if (!paths.length) sideEmpty('recent', 'emptyRecent'); }
window.chrome.webview.addEventListener('message', event => {
  const m = event.data;
  switch (m.type) {
    case 'frame': document.body.classList.add('unified-titlebar'); document.documentElement.style.setProperty('--caption-width', m.captionWidth + 'px'); break;
    case 'settings': if (m.catalogs) MDI18n.configure(m.catalogs); systemLanguage = m.systemLanguage || systemLanguage; settings = { ...settings, ...m.settings }; applyLanguage(); applySettings(); recent(settings.Recent || []); updateStatus(); startupTrace = !!m.startupTrace; startupStage('uiReady'); break;
    case 'document': receiveDocument(m.document, false); break;
    case 'saved': receiveDocument(m.document, true); break;
    case 'activate': activate(m.id); break;
    case 'recent': settings.Recent = m.paths; recent(m.paths); break;
    case 'notice': notice(m.message); break;
    case 'closed': { const d = docs.get(m.id); d?.editor.remove(); docs.delete(m.id); if (m.id === activeId) { activeId = null; if (docs.size) activate([...docs.keys()].pop()); else send('new'); } renderTabs(); break; }
  }
});
window.mdShow = { snapshot: () => [...docs.values()].map(d => ({ id: d.id, text: d.editor.value })) };
send('ready');
