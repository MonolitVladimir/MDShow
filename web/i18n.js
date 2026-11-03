// SPDX-License-Identifier: MIT
/* Shared catalogs arrive from the native host. Translations are always plain text. */
(function (root) {
  'use strict';
  const languages = ['en-US', 'ru-RU', 'de-DE', 'ar', 'zh-CN'];
  let catalogs = {}, locale = 'en-US';
  const diagnostics = [];
  const placeholders = value => (value.match(/\{\d+\}/g) || []).sort().join(',');
  function resolve(preference, system) {
    if (languages.length === 1) return 'en-US';
    if (languages.includes(preference)) return preference;
    if (preference !== 'auto') return 'en-US';
    system = String(system || '').toLowerCase();
    for (const [prefix, language] of [['ru', 'ru-RU'], ['de', 'de-DE'], ['ar', 'ar'], ['en', 'en-US']])
      if (system === prefix || system.startsWith(prefix + '-')) return language;
    return ['zh-cn', 'zh-sg', 'zh-hans'].includes(system) || system.startsWith('zh-hans-') ? 'zh-CN' : 'en-US';
  }
  function t(key, ...args) {
    const fallback = catalogs['en-US']?.[key];
    if (typeof fallback !== 'string') { diagnostics.push('Unknown key: ' + key); return key; }
    let value = catalogs[locale]?.[key];
    if (typeof value !== 'string' || !value.trim() || placeholders(value) !== placeholders(fallback)) {
      diagnostics.push('Fallback: ' + locale + '/' + key); value = fallback;
    }
    return value.replace(/\{(\d+)\}/g, (match, index) => index < args.length ? String(args[index]) : match);
  }
  function translate(container = document) {
    for (const attribute of ['text', 'title', 'aria-label', 'placeholder']) {
      const data = attribute === 'text' ? 'data-i18n' : 'data-i18n-' + attribute;
      container.querySelectorAll('[' + data + ']').forEach(element => {
        let value = t(element.getAttribute(data));
        if (attribute === 'title' && element.dataset.shortcut) value += ' · \u2066' + element.dataset.shortcut + '\u2069';
        if (attribute === 'text') element.textContent = value; else element.setAttribute(attribute, value);
      });
    }
  }
  const api = { languages, diagnostics, resolve, placeholders, t, translate,
    configure: data => { catalogs = data; languages.splice(0, languages.length, ...Object.keys(data)); },
    select: (preference, system) => { locale = resolve(preference, system); return locale; },
    get locale() { return locale; }
  };
  if (typeof module !== 'undefined') module.exports = api; else root.MDI18n = api;
})(typeof window !== 'undefined' ? window : globalThis);
