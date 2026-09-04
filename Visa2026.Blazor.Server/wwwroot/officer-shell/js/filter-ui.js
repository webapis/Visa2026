/** Family / action filter chips — PNG parity (staged, in-process, templates). */

import { TPL_KEYS } from './mock-data.js';

const STAGED_FAMILY_ORDER = ['reg', 'inv', 'ext', 'wp'];

const TEMPLATE_ACTION_ORDER = [
  { key: 'Issuance', label: 'Issuance' },
  { key: 'Registration', label: 'Registration' },
  { key: 'Cancellation', label: 'Cancellation' },
  { key: 'Business trip', label: 'Business trip' },
];

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

export function countByTplKey(items, keyField = 'tplKey') {
  const counts = { all: items.length };
  for (const item of items) {
    const k = item[keyField];
    if (k) counts[k] = (counts[k] || 0) + 1;
  }
  return counts;
}

export function countByAction(templates) {
  const counts = { all: templates.length };
  for (const t of templates) {
    let bucket = t.action;
    if (t.action.includes('Business trip')) bucket = 'Business trip';
    counts[bucket] = (counts[bucket] || 0) + 1;
  }
  return counts;
}

export function matchesTplFilter(item, filterKey, keyField = 'tplKey') {
  if (!filterKey || filterKey === 'all') return true;
  return item[keyField] === filterKey;
}

export function matchesActionFilter(template, filterKey) {
  if (!filterKey || filterKey === 'all') return true;
  if (filterKey === 'Business trip') return template.action.includes('Business trip');
  return template.action === filterKey;
}

export function matchesSearch(text, query) {
  if (!query) return true;
  return text.toLowerCase().includes(query.toLowerCase());
}

/** @param {'staged'|'inProcess'|'templates'} page */
export function renderTplFamilyChips(page, items, activeKey, { showLegend = false } = {}) {
  const counts = countByTplKey(items);
  const chips = [`<button type="button" class="os-chip${activeKey === 'all' ? ' is-active' : ''}" data-family-filter="${page}" data-filter-key="all">All (${counts.all})</button>`];
  for (const key of STAGED_FAMILY_ORDER) {
    const n = counts[key] || 0;
    if (n === 0 && activeKey !== key) continue;
    const meta = TPL_KEYS[key];
    chips.push(`<button type="button" class="os-chip os-chip--${key}${activeKey === key ? ' is-active' : ''}" data-family-filter="${page}" data-filter-key="${key}">${esc(meta.label)} (${n})</button>`);
  }
  let html = `<div class="os-chip-row os-chip-row--family">${chips.join('')}</div>`;
  if (showLegend) {
    html += `<div class="os-legend">${STAGED_FAMILY_ORDER.map(key =>
      `<span class="os-legend__item"><span class="os-dot" style="background:${TPL_KEYS[key].color}"></span>${esc(TPL_KEYS[key].label)}</span>`).join('')}</div>`;
  }
  return html;
}

export function renderActionFamilyChips(templates, activeKey) {
  const counts = countByAction(templates);
  const chips = [`<button type="button" class="os-chip${activeKey === 'all' ? ' is-active' : ''}" data-family-filter="templates" data-filter-key="all">All (${counts.all})</button>`];
  for (const { key, label } of TEMPLATE_ACTION_ORDER) {
    const n = counts[key] || 0;
    if (n === 0 && activeKey !== key) continue;
    chips.push(`<button type="button" class="os-chip os-chip--action${activeKey === key ? ' is-active' : ''}" data-family-filter="templates" data-filter-key="${esc(key)}">${esc(label)} (${n})</button>`);
  }
  return `<div class="os-chip-row os-chip-row--family">${chips.join('')}</div>`;
}

export function slaChip(days) {
  if (days == null) return '—';
  const cls = days <= 7 ? 'os-sla os-sla--warn' : 'os-sla os-sla--ok';
  return `<span class="${cls}">${days} days</span>`;
}
