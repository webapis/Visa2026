/** Profile templates catalog + overview — PNG parity (P4/P5). */

import { TPL_KEYS } from './mock-data.js';

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function tplColor(t) {
  return TPL_KEYS[t.tplKey]?.color ?? '#94a3b8';
}

function tplIcon(t) {
  return TPL_KEYS[t.tplKey]?.icon ?? 'bi-file-earmark-text';
}

export function templateStatusPill(status, { compact = false } = {}) {
  const labels = { active: 'Active', locked: 'Locked', draft: 'Draft' };
  const label = labels[status] ?? status;
  if (status === 'active') {
    return `<span class="tc-status tc-status--active"><span class="tc-status__dot tc-status__dot--active"></span>${esc(label)}</span>`;
  }
  if (status === 'locked') {
    return `<span class="tc-status tc-status--locked"><i class="bi bi-lock-fill"></i> ${esc(label)}</span>`;
  }
  if (status === 'draft') {
    return `<span class="tc-status tc-status--draft"><span class="tc-status__dot tc-status__dot--draft"></span>${esc(label)}</span>`;
  }
  return `<span class="tc-status">${esc(label)}</span>`;
}

function statusHint(status) {
  if (status === 'locked') return '<div class="tc-tpl-card__hint">Template is locked and cannot be edited</div>';
  if (status === 'draft') return '<div class="tc-tpl-card__hint">Not published</div>';
  return '';
}

function railStatusLabel(status) {
  if (status === 'active') return { text: 'Active', dot: '#22c55e' };
  if (status === 'locked') return { text: 'Published', dot: '#22c55e' };
  return { text: 'Draft', dot: '#f59e0b' };
}

export function renderTemplateGridCard(t) {
  const color = tplColor(t);
  const inProc = t.inProcessUses > 0
    ? `<div class="tc-tpl-card__stat"><strong>${t.inProcessUses}</strong><span>In process</span></div>`
    : '';
  return `<article class="tc-tpl-card" data-tpl-id="${t.id}">
    <div class="tc-tpl-card__stripe" style="background:${color}"></div>
    <div class="tc-tpl-card__body">
      <div class="tc-tpl-card__head">
        <div class="tc-tpl-card__icon" style="background:${color}"><i class="bi ${tplIcon(t)}"></i></div>
        <div>
          <h3 class="tc-tpl-card__title">${esc(t.name)}</h3>
          <span class="tc-tpl-card__code">${esc(t.code)}</span>
        </div>
      </div>
      <dl class="tc-tpl-card__kv">
        <dt>Action route</dt><dd>${esc(t.action)}</dd>
        <dt>Issuance via</dt><dd>${esc(t.route)}</dd>
      </dl>
      <div class="tc-tpl-card__status">
        ${templateStatusPill(t.status)}
        ${statusHint(t.status)}
      </div>
      <div class="tc-tpl-card__stats">
        <div class="tc-tpl-card__stat"><strong>${t.stagedUses}</strong><span>Staged</span></div>
        ${inProc}
      </div>
    </div>
    <div class="tc-tpl-card__foot">
      <button type="button" class="os-btn os-btn--ghost" data-configure="${t.id}"><i class="bi bi-gear"></i> Configure</button>
      <span class="os-table__chev">›</span>
    </div>
  </article>`;
}

export function renderTemplateListRow(t) {
  return `<tr class="is-clickable" data-tpl-id="${t.id}">
    <td><strong>${esc(t.name)}</strong></td>
    <td><code style="font-size:0.8rem">${esc(t.code)}</code></td>
    <td>${esc(t.action)}</td>
    <td>${esc(t.route)}</td>
    <td>${esc(t.audience)}</td>
    <td>${templateStatusPill(t.status)}</td>
    <td>${t.stagedUses}</td>
    <td>${t.inProcessUses}</td>
    <td class="os-table__chev">›</td>
  </tr>`;
}

/** @param {object} opts — visible, paginationHtml, globalSearch, viewToggleHtml, chipsHtml */
export function renderTemplateCatalog(opts) {
  const { visible, globalSearch, viewToggleHtml, chipsHtml, paginationHtml, headActionsHtml } = opts;
  const rows = visible.map(renderTemplateListRow).join('');
  const cards = visible.map(renderTemplateGridCard).join('');
  const list = `<div class="os-panel"><table class="os-table"><thead><tr>
    <th>Template name</th><th>Code</th><th>Action family</th><th>Progress route</th><th>Audience</th>
    <th>Status</th><th>Staged uses</th><th>In process uses</th><th></th>
  </tr></thead><tbody>${rows || '<tr><td colspan="9" class="os-empty">No templates match the current filters.</td></tr>'}</tbody></table></div>`;
  const grid = `<div class="tc-tpl-grid">${cards || '<p class="os-empty">No templates match the current filters.</p>'}</div>`;

  return `<div class="os-page-head">
    <div><h1>Profile templates</h1>
    <p>Configure reusable templates officers clone to stage application profiles.</p></div>
    <div class="os-page-head__actions">${headActionsHtml ?? ''}
      <button type="button" class="os-btn os-btn--success" id="btn-new-template">+ New template</button></div>
  </div>
  <div class="os-toolbar">
    <div class="tc-toolbar-search"><i class="bi bi-search"></i>
      <input type="search" placeholder="Search templates…" id="global-search" value="${esc(globalSearch)}" />
    </div>
    <select class="tc-select" aria-label="Action family filter"><option>All action families</option>
      <option>Issuance</option><option>Registration</option><option>Cancellation</option><option>Business trip</option>
    </select>
    <select class="tc-select" aria-label="Sort order"><option>Name A–Z</option><option>Name Z–A</option><option>Newest configured</option></select>
    ${viewToggleHtml}
  </div>
  ${chipsHtml}
  ${opts.viewMode === 'list' ? list : grid}
  ${paginationHtml ?? ''}`;
}

function overviewBadges(t) {
  const parts = [];
  if (t.status === 'active') {
    parts.push('<span class="tc-badge tc-badge--active"><i class="bi bi-check-circle-fill"></i> Active</span>');
    parts.push('<span class="tc-badge tc-badge--published"><i class="bi bi-globe2"></i> Published</span>');
  } else if (t.status === 'locked') {
    parts.push('<span class="tc-badge tc-badge--published"><i class="bi bi-globe2"></i> Published</span>');
    parts.push(`<span class="tc-badge" style="background:var(--os-hold-soft);color:#475569"><i class="bi bi-lock-fill"></i> Locked</span>`);
  } else {
    parts.push(`<span class="tc-badge" style="background:var(--os-warn-soft);color:var(--os-warn)"><i class="bi bi-pencil"></i> Draft</span>`);
  }
  return parts.join('');
}

function renderRailItem(x, activeId, railSearch) {
  if (railSearch && !`${x.name} ${x.code}`.toLowerCase().includes(railSearch.toLowerCase())) return '';
  const color = tplColor(x);
  const st = railStatusLabel(x.status);
  return `<li class="tc-rail__item${x.id === activeId ? ' is-active' : ''}" data-tpl-id="${x.id}">
    <div class="tc-rail__icon" style="background:${color}"><i class="bi ${tplIcon(x)}"></i></div>
    <div>
      <p class="tc-rail__name">${esc(x.name)}</p>
      <div class="tc-rail__sub"><span class="tc-status__dot" style="background:${st.dot}"></span>${esc(st.text)}</div>
    </div>
    <i class="bi bi-chevron-right" style="color:#94a3b8;font-size:0.8rem"></i>
  </li>`;
}

export function renderTemplateOverviewPage(store, activeId, railSearch = '') {
  const t = store.templates.find(x => x.id === activeId) || store.templates[0];
  const user = store.user?.name ?? 'Officer';
  const lastCfg = t.lastConfigured && t.lastConfigured !== '—' ? t.lastConfigured : '—';
  const railItems = store.templates.map(x => renderRailItem(x, t.id, railSearch)).join('');

  const col1 = `<div class="tc-col">
    <div class="tc-col__head"><span class="tc-col__num">1</span><i class="bi bi-person-badge"></i> Identity</div>
    <dl class="tc-col__rows">
      <div class="tc-col__row"><dt>Code</dt><dd>${esc(t.code)}</dd></div>
      <div class="tc-col__row"><dt>Selection</dt><dd>${esc(t.selectionCode ?? '—')}</dd></div>
      <div class="tc-col__row"><dt>Via</dt><dd>${esc(t.route.replace(/^Via /i, '').toLowerCase())}</dd></div>
      <div class="tc-col__row"><dt>Employee</dt><dd>${esc(t.audience.split(',')[0])}</dd></div>
      <div class="tc-col__row"><dt>Issuance</dt><dd>scoped project contract</dd></div>
    </dl>
  </div>`;

  const col2 = `<div class="tc-col">
    <div class="tc-col__head"><span class="tc-col__num">2</span><i class="bi bi-file-earmark-ruled"></i> Results &amp; defaults</div>
    <dl class="tc-col__rows">
      <div class="tc-col__row"><dt>Produces</dt><dd>invitation, work permit</dd></div>
      <div class="tc-col__row"><dt>Defaults</dt><dd>WP</dd></div>
      <div class="tc-col__row"><dt>Multiple</dt><dd>Month6</dd></div>
      <div class="tc-col__row"><dt>Signatories</dt><dd>default</dd></div>
    </dl>
  </div>`;

  const col3 = `<div class="tc-col">
    <div class="tc-col__head"><span class="tc-col__num">3</span><i class="bi bi-clock-history"></i> Process &amp; SLA</div>
    <p style="margin:0 0 6px;font-size:0.84rem;font-weight:650">3 ministry legs</p>
    <p style="margin:0 0 8px;font-size:0.8rem;color:var(--os-muted)">Turkmenenergo · Energetika Gurlushyk Ministry</p>
    <div class="tc-col__sla"><span>14d Migration</span><span>14d Ministry</span></div>
  </div>`;

  const col4 = `<div class="tc-col">
    <div class="tc-col__head"><span class="tc-col__num">4</span><i class="bi bi-person-lines-fill"></i> Templates &amp; person</div>
    <p style="margin:0 0 8px;font-size:0.84rem;font-weight:650">3 Resminamalar templates</p>
    <ul class="tc-col__list">
      <li><span><i class="bi bi-file-earmark"></i> Passport</span><span class="tc-col__tag">Required</span></li>
      <li><span><i class="bi bi-file-earmark"></i> Education</span><span class="tc-col__tag">Required</span></li>
      <li><span><i class="bi bi-file-earmark"></i> Position</span><span class="tc-col__tag">Required</span></li>
      <li><span><i class="bi bi-file-earmark"></i> Visa invitation</span><span class="tc-col__tag">Required</span></li>
    </ul>
  </div>`;

  return `<div class="tc-overview">
    <aside class="tc-rail">
      <div class="tc-rail__search">
        <i class="bi bi-search" style="color:var(--os-muted)"></i>
        <input type="search" placeholder="Search templates…" id="tpl-rail-search" value="${esc(railSearch)}" />
        <button type="button" class="os-btn os-btn--ghost" style="padding:4px 8px" title="Filters"><i class="bi bi-funnel"></i></button>
      </div>
      <div class="tc-rail__label">Templates</div>
      <ul class="tc-rail__list">${railItems || '<li class="os-empty" style="padding:12px">No templates match.</li>'}</ul>
    </aside>
    <div>
      <div class="tc-overview__head">
        <div>
          <div class="tc-overview__title-row">
            <h1>${esc(t.name)}</h1>
            ${overviewBadges(t)}
          </div>
        </div>
        <div class="tc-overview__actions">
          <button type="button" class="os-btn os-btn--primary" id="btn-configure" data-id="${t.id}"><i class="bi bi-gear"></i> Configure template</button>
          <button type="button" class="os-btn"><i class="bi bi-copy"></i> Duplicate template</button>
        </div>
      </div>
      <div class="tc-cols">${col1}${col2}${col3}${col4}</div>
      <div class="tc-usage">
        <div class="tc-usage__title"><i class="bi bi-bar-chart-line"></i> Usage stats</div>
        <div class="tc-usage__num"><strong>${t.stagedUses}</strong><span>staged profiles</span></div>
        <div class="tc-usage__num"><strong>${t.inProcessUses}</strong><span>in process cases</span></div>
        <div class="tc-usage__meta">
          <span><i class="bi bi-calendar3"></i> Last configured</span>
          <strong>${esc(lastCfg)}</strong>
          <span>by ${esc(user)}</span>
        </div>
      </div>
      <div class="tc-lock-banner">
        <div style="display:flex;gap:10px;align-items:flex-start">
          <i class="bi bi-info-circle"></i>
          <span>Officers clone this template from Person actions or auto-triggers. Locked when in-process cases exist.</span>
        </div>
        <div class="tc-lock-banner__lock" aria-hidden="true"><i class="bi bi-lock-fill"></i></div>
      </div>
      <button type="button" class="os-btn os-btn--ghost" data-nav="#/templates" style="margin-top:12px">← Back to catalog</button>
    </div>
  </div>`;
}
