/** Staged profiles — grouped-by-template workspace (PNG P8). */

import { TPL_KEYS } from './mock-data.js';

const GROUP_ORDER = ['reg', 'inv', 'ext', 'wp'];

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function initials(name) {
  const parts = String(name ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function readinessPill(readiness) {
  const labels = { ready: 'Ready', incomplete: 'Incomplete', awaiting: 'Awaiting data' };
  const label = labels[readiness] ?? readiness;
  return `<span class="sw-ready sw-ready--${readiness}"><span class="sw-ready__dot" aria-hidden="true"></span>${esc(label)}</span>`;
}

function rowMeta(row) {
  const parts = [];
  if (row.tplKey === 'inv' && row.project && row.project !== '—') {
    parts.push(`<span class="sw-meta">Project: ${esc(row.project)}</span>`);
  }
  if (row.tplKey === 'ext') {
    if (row.readiness === 'awaiting') {
      parts.push('<span class="sw-meta">Visa type —</span>', '<span class="sw-meta">Visa period —</span>');
    } else if (row.readiness === 'ready') {
      parts.push('<span class="sw-meta">WP · 6 months · Category B</span>');
    }
  }
  if (row.missing !== '—') {
    const cls = row.missing.includes('Contract') ? 'sw-badge--muted' : 'sw-badge--info';
    parts.push(`<span class="sw-badge ${cls}">${esc(row.missing)}</span>`);
  }
  return parts.join('');
}

function renderGroupRow(row, store, isSelectable) {
  const selectable = isSelectable(row);
  const checked = store.stagedSelected.has(row.id);
  const disabled = !selectable ? ' disabled' : '';
  const rowCls = !selectable ? ' sw-row--disabled' : '';
  return `<div class="sw-row${rowCls}">
    <label class="sw-row__check">
      <input type="checkbox" data-staged-id="${row.id}"${checked ? ' checked' : ''}${disabled} />
    </label>
    <div class="sw-row__avatar" aria-hidden="true">${esc(initials(row.person))}</div>
    <div class="sw-row__main">
      <div class="sw-row__name">${esc(row.person)}</div>
      <div class="sw-row__meta">${rowMeta(row)}</div>
    </div>
    <div class="sw-row__status">${readinessPill(row.readiness)}</div>
    <span class="sw-row__chev" aria-hidden="true">›</span>
  </div>`;
}

function renderGroup(key, items, store, collapsed, isSelectable) {
  if (items.length === 0) return '';
  const meta = TPL_KEYS[key];
  const color = meta?.color ?? '#94a3b8';
  const isCollapsed = collapsed.has(key);
  const rows = items.map(row => renderGroupRow(row, store, isSelectable)).join('');
  return `<section class="sw-group${isCollapsed ? ' is-collapsed' : ''}" data-sw-group="${key}">
    <button type="button" class="sw-group__head" data-sw-toggle="${key}" style="--sw-color:${color}">
      <span class="sw-group__accent"></span>
      <span class="os-dot" style="background:${color}"></span>
      <span class="sw-group__title">${esc(meta?.label ?? key)}</span>
      <span class="sw-group__count">${items.length}</span>
      <i class="bi bi-chevron-down sw-group__chev"></i>
    </button>
    <div class="sw-group__body">${rows}</div>
  </section>`;
}

export function renderStagedFamilyLegend() {
  return `<div class="os-legend sw-legend">${GROUP_ORDER.map(key =>
    `<span class="os-legend__item"><span class="os-dot" style="background:${TPL_KEYS[key].color}"></span>${esc(TPL_KEYS[key].label)}</span>`).join('')}</div>`;
}

/**
 * @param {object} store
 * @param {Array} filtered — already search/chip filtered rows
 * @param {object} opts
 */
export function renderStagedGroupedWorkspace(store, filtered, opts) {
  const { globalSearch, esc: escFn, isSelectable } = opts;
  const selected = [...store.stagedSelected];
  const ready = selected.filter(id => isSelectable(store.staged.find(r => r.id === id)));
  const blocked = selected.length > ready.length;
  const incompleteCount = selected.length - ready.length;

  const groups = GROUP_ORDER.map(key => ({
    key,
    items: filtered.filter(r => r.tplKey === key),
  }));

  const body = groups.map(g => renderGroup(g.key, g.items, store, store.stagedGroupCollapsed, isSelectable)).join('')
    || '<p class="os-empty">No profiles match the current filters.</p>';

  const selectionBar = selected.length > 0
    ? `<div class="sw-selection-bar">
        <i class="bi bi-info-circle"></i>
        <span>${selected.length} selected${incompleteCount > 0 ? ` · <strong class="sw-selection-bar__warn">${incompleteCount} incomplete (cannot start)</strong>` : ' · ready to start'}</span>
      </div>`
    : '';

  return `<div class="sw-page">
    <div class="os-page-head">
      <div>
        <h1>Staged Application Profiles</h1>
        <p>Select profiles to merge into one application case.</p>
      </div>
    </div>
    <div class="sw-toolbar">
      <div class="tc-toolbar-search sw-toolbar__search">
        <i class="bi bi-search"></i>
        <input type="search" placeholder="Search profiles…" id="global-search" value="${escFn(globalSearch)}" />
      </div>
      <select class="tc-select" aria-label="Template filter"><option>All templates</option></select>
      <div class="sw-toolbar__grow"></div>
      ${opts.viewToggleHtml}
      <button type="button" class="os-btn os-btn--primary" id="btn-start-process"
        ${ready.length < 1 || blocked ? 'disabled' : ''}>Start process</button>
    </div>
    ${renderStagedFamilyLegend()}
    <div class="sw-groups">${body}</div>
    ${selectionBar}
  </div>`;
}

export { GROUP_ORDER };
