import {
  getStore, TPL_KEYS, MOCKUP_FILES, tplLabel, isSelectable,
  toggleStaged, startProcess, setViewMode, setFamilyFilter, publishTemplate,
  setPaginationPage, setPaginationPageSize, resetPaginationPage, toggleStagedGroup,
} from './mock-data.js';
import { renderWizardPage } from './wizard-ui.js';
import {
  renderTplFamilyChips, renderActionFamilyChips,
  matchesTplFilter, matchesActionFilter, matchesSearch, slaChip,
} from './filter-ui.js';
import {
  renderCaseNav, renderCaseHeader, renderCaseOverview, renderCaseRail,
} from './case-workspace-ui.js';
import {
  renderCaseDocumentCopies, buildDocumentCopiesModel, countSelectedDocChecks,
} from './document-copies-ui.js';
import {
  renderCasePeopleTab, renderCasePeopleRail,
  renderCaseProgressTab, renderCaseProgressRail,
  renderCaseResminamalarTab, renderCaseSlaTab, countResmiSelected,
} from './case-tabs-ui.js';
import {
  renderTemplateCatalog, renderTemplateOverviewPage,
} from './template-catalog-ui.js';
import { paginateSlice, renderPaginationBar } from './pagination-ui.js';
import { renderStagedGroupedWorkspace } from './staged-workspace-ui.js';
import { getApplicationProfileNavItems, renderNavBadge } from './nav-ui.js';

let route = parseRoute();
let globalSearch = '';
let tplRailSearch = '';
let issuedFocusKey = null;

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function statusClass(r) {
  if (r === 'ready' || r === 'active') return 'ready';
  if (r === 'incomplete') return 'incomplete';
  if (r === 'awaiting') return 'awaiting';
  if (r === 'process') return 'process';
  if (r === 'hold') return 'hold';
  return r;
}

function dot(key) {
  const c = TPL_KEYS[key]?.color ?? '#94a3b8';
  return `<span class="os-dot" style="background:${c}"></span>`;
}

function pill(readiness) {
  const labels = { ready: 'Ready', incomplete: 'Incomplete', awaiting: 'Awaiting data', process: 'In process', hold: 'On hold' };
  const label = labels[readiness] ?? readiness;
  return `<span class="os-status os-status--${statusClass(readiness)}">${esc(label)}</span>`;
}

function parseRoute() {
  const raw = (location.hash || '#/staged').slice(1);
  const [pathPart, queryPart] = raw.split('?');
  const query = new URLSearchParams(queryPart || '');
  const parts = pathPart.split('/').filter(Boolean);
  if (parts.length === 0) return { name: 'staged', grouped: query.get('group') === 'template' };
  if (parts[0] === 'staged') return { name: 'staged', grouped: query.get('group') === 'template' };
  if (parts[0] === 'case' && parts[1]) return { name: 'case', id: parts[1], tab: parts[2] || 'overview' };
  if (parts[0] === 'templates' && parts[1] === 'wizard') {
    const q = queryPart || '';
    const step = parseInt(parts[2] ?? new URLSearchParams(q).get('step') ?? '0', 10);
    return { name: 'wizard', step: Number.isNaN(step) ? 0 : step };
  }
  if (parts[0] === 'templates' && parts[1]) return { name: 'template', id: parts[1] };
  return { name: parts[0] };
}

function navigate(path) {
  location.hash = path;
}

function setRoute(r) {
  route = r;
  render();
}

function viewToggle(page, current) {
  if (page === 'staged') {
    return `<div class="os-toggle" data-toggle-page="${page}">
      <button type="button" data-mode="list" class="${current === 'list' ? 'is-active' : ''}">List</button>
      <button type="button" data-mode="grid" class="${current === 'grid' ? 'is-active' : ''}">Grid</button>
      <button type="button" data-mode="grouped" class="${current === 'grouped' ? 'is-active' : ''}">Grouped</button>
    </div>`;
  }
  return `<div class="os-toggle" data-toggle-page="${page}">
    <button type="button" data-mode="list" class="${current === 'list' ? 'is-active' : ''}">List</button>
    <button type="button" data-mode="grid" class="${current === 'grid' ? 'is-active' : ''}">Grid</button>
  </div>`;
}

function renderSidebar(compact) {
  const s = getStore();
  const nav = [
    { section: 'Dashboard', items: [{ path: '#/dashboard', icon: '▦', title: 'Dashboard', sub: 'Home overview' }] },
    { section: 'People and records', items: [
      { path: '#/people', icon: '👤', title: 'People' },
      { path: '#/organizations', icon: '🏢', title: 'Organizations' },
    ]},
    { section: 'ApplicationProfileInstance profiles', items: getApplicationProfileNavItems(s) },
    { section: 'Projects and contracts', items: [{ path: '#/projects', icon: '💼', title: 'Projects / Contracts' }] },
    { section: 'Compliance and reports', items: [
      { path: '#/report-dashboard', icon: '📊', title: 'Report Dashboard' },
      { path: '#/sla-monitor', icon: '⏱', title: 'SLA monitor' },
    ]},
  ];
  const activePath = '#' + (location.hash || '#/staged').slice(1).split('?')[0];
  const logoLabel = compact ? 'VISA<br>2026' : 'V26';
  let html = `<div class="os-brand"><div class="os-brand__logo">${logoLabel}</div><div>
    <span class="os-brand__title">VISA2026</span><span class="os-brand__sub">Global Mobility</span></div></div>`;
  if (compact) {
    const icons = [
      { path: '#/dashboard', icon: 'bi-grid', title: 'Dashboard' },
      { path: '#/staged', icon: 'bi-collection', title: 'Staged profiles' },
      { path: '#/in-process', icon: 'bi-folder2-open', title: 'In process' },
      { path: '#/templates', icon: 'bi-layout-text-window', title: 'Profile templates' },
      { path: '#/report-dashboard', icon: 'bi-bar-chart', title: 'Report Dashboard' },
      { path: '#/sla-monitor', icon: 'bi-gear', title: 'Settings' },
    ];
    for (const item of icons) {
      const isActive = item.path === '#/templates'
        ? activePath.startsWith('#/templates')
        : activePath === item.path;
      html += `<button type="button" class="os-nav-item${isActive ? ' is-active' : ''}" data-nav="${item.path}" title="${esc(item.title)}">
        <span class="os-nav-item__ico"><i class="bi ${item.icon}"></i></span><span class="os-nav-item__grow"></span></button>`;
    }
    html += `<div class="os-sidebar-foot">
      <div class="os-user-card"><div class="os-avatar">${esc(s.user.initials)}</div><div></div></div></div>`;
    return html;
  }
  for (const g of nav) {
    html += `<div class="os-nav-section">${esc(g.section)}</div>`;
    for (const item of g.items) {
      const isActive = item.path === '#/templates'
        ? activePath.startsWith('#/templates')
        : item.path === '#/in-process'
          ? activePath === '#/in-process' || activePath.startsWith('#/case')
          : activePath === item.path;
      const badge = item.count != null
        ? renderNavBadge(item.count, item.badge, item.badgeLabel)
        : '';
      html += `<button type="button" class="os-nav-item${isActive ? ' is-active' : ''}" data-nav="${item.path}">
        <span class="os-nav-item__ico">${item.icon}</span>
        <span class="os-nav-item__grow"><span class="os-nav-item__title">${esc(item.title)}</span>
        ${item.sub ? `<span class="os-nav-item__sub">${esc(item.sub)}</span>` : ''}</span>${badge}</button>`;
    }
  }
  html += `<div class="os-sidebar-foot">
    <button type="button" class="os-nav-item${activePath === '#/mockups' ? ' is-active' : ''}" data-nav="#/mockups">
      <span class="os-nav-item__ico">🖼</span><span class="os-nav-item__grow"><span class="os-nav-item__title">Reference mockups</span>
      <span class="os-nav-item__sub">PNG gallery</span></span></button>
    <div class="os-user-card"><div class="os-avatar">${esc(s.user.initials)}</div><div>
      <strong style="color:#fff">${esc(s.user.name)}</strong><br>
      <span style="color:var(--os-nav-muted);font-size:0.76rem">${esc(s.user.role)} · ${esc(s.user.office)}</span></div></div></div>`;
  return html;
}

function breadcrumbs() {
  const parts = ['Visa2026'];
  if (route.name === 'staged') parts.push('Staged profiles');
  else if (route.name === 'in-process') parts.push('In process');
  else if (route.name === 'case') {
    const c = getStore().inProcess.find(p => p.id === route.id);
    parts.push('In process', c ? `№ ${c.number}` : 'Case');
  } else if (route.name === 'templates') parts.push('Profile templates');
  else if (route.name === 'template') {
    const t = getStore().templates.find(x => x.id === route.id);
    parts.push('Profile templates', t?.name ?? 'Template');
  } else if (route.name === 'wizard') parts.push('Profile templates', 'New template');
  else if (route.name === 'mockups') parts.push('Reference mockups');
  else parts.push(route.name.replace(/-/g, ' '));
  return parts.map((p, i) => i === parts.length - 1 ? `<strong>${esc(p)}</strong>` : esc(p)).join(' › ');
}

function filterStaged(rows, s) {
  const q = globalSearch.trim();
  const fk = s.familyFilter.staged;
  return rows.filter(row => {
    if (!matchesTplFilter(row, fk)) return false;
    if (!q) return true;
    return matchesSearch(`${row.person} ${row.project} ${tplLabel(row.tplKey)}`, q);
  });
}

function filterInProcess(rows, s) {
  const q = globalSearch.trim();
  const fk = s.familyFilter.inProcess;
  return rows.filter(row => {
    if (!matchesTplFilter(row, fk)) return false;
    if (!q) return true;
    return matchesSearch(`${row.number} ${row.people.join(' ')} ${row.project} ${tplLabel(row.tplKey)}`, q);
  });
}

function filterTemplates(rows, s) {
  const q = globalSearch.trim();
  const fk = s.familyFilter.templates;
  return rows.filter(t => {
    if (!matchesActionFilter(t, fk)) return false;
    if (!q) return true;
    return matchesSearch(`${t.name} ${t.code} ${t.action} ${t.audience}`, q);
  });
}

function renderStaged() {
  const s = getStore();
  const filtered = filterStaged(s.staged, s);
  if (route.grouped || s.viewMode.staged === 'grouped') {
    s.viewMode.staged = 'grouped';
    return renderStagedGroupedWorkspace(s, filtered, {
      globalSearch,
      esc,
      isSelectable,
      viewToggleHtml: viewToggle('staged', 'grouped'),
    });
  }
  const pg = paginateSlice(filtered, s.pagination.staged);
  if (pg.page !== s.pagination.staged.page) s.pagination.staged.page = pg.page;
  const visible = pg.pageItems;
  const selected = [...s.stagedSelected];
  const ready = selected.filter(id => isSelectable(s.staged.find(r => r.id === id)));
  const blocked = selected.length > ready.length;
  const rows = visible.map(row => {
    const sel = isSelectable(row);
    return `<tr class="${sel ? '' : 'is-disabled'}">
      <td><input type="checkbox" data-staged-id="${row.id}" ${s.stagedSelected.has(row.id) ? 'checked' : ''} ${sel ? '' : 'disabled'} /></td>
      <td>${dot(row.tplKey)}${esc(tplLabel(row.tplKey))}</td>
      <td>${esc(row.person)}</td><td>${esc(row.project)}</td><td>${esc(row.stagedOn)}</td>
      <td>${row.missing !== '—' ? `<span class="os-status os-status--incomplete">${esc(row.missing)}</span>` : '—'}</td>
      <td>${pill(row.readiness)}</td><td class="os-table__chev">›</td></tr>`;
  }).join('');
  const cards = visible.map(row => {
    const sel = isSelectable(row);
    const missing = row.missing !== '—' ? `<div class="os-card__warn"><i class="bi bi-exclamation-triangle"></i> ${esc(row.missing)}</div>` : '';
    return `<article class="os-card${s.stagedSelected.has(row.id) ? ' is-selected' : ''}"><div class="os-card__stripe" style="background:${TPL_KEYS[row.tplKey]?.color}"></div>
      <div class="os-card__body"><span class="os-card__type-pill">${esc(tplLabel(row.tplKey))}</span>
        <label class="os-card__check"><input type="checkbox" data-staged-id="${row.id}" ${s.stagedSelected.has(row.id) ? 'checked' : ''} ${sel ? '' : 'disabled'} />
        <h3>${esc(row.person)}</h3></label>
        <div class="os-card__meta"><i class="bi bi-briefcase"></i> ${esc(row.project)}</div>
        <div class="os-card__meta"><i class="bi bi-calendar3"></i> ${esc(row.stagedOn)}</div>${missing}</div>
      <div class="os-card__foot">${pill(row.readiness)}<span>›</span></div></article>`;
  }).join('');
  const chips = renderTplFamilyChips('staged', s.staged, s.familyFilter.staged, { showLegend: true });
  const banner = blocked
    ? `<div class="os-banner os-banner--blocked"><i class="bi bi-info-circle"></i> ${selected.length} selected · ${selected.length - ready.length} incomplete profile cannot be included — complete required data or fix BO states before Start process.</div>`
    : '';
  return `<div class="os-page-head"><div><h1>Staged Application Profiles</h1>
    <p>Select ready profiles and <strong>Start process</strong> to merge into one numbered case.</p></div>
    <div class="os-page-head__actions"><span class="os-selected-count">${selected.length} selected</span>
    <button type="button" class="os-btn os-btn--primary" id="btn-start-process" ${ready.length < 1 || blocked ? 'disabled' : ''}>Start process</button></div></div>
    <div class="os-toolbar"><input class="os-search" type="search" placeholder="Search by person, project…" value="${esc(globalSearch)}" id="global-search" />
    <select class="os-search"><option>All templates</option></select><select class="os-search"><option>Newest staged</option></select>
    ${viewToggle('staged', s.viewMode.staged)}</div>
    ${chips}
    ${s.viewMode.staged === 'list' ? `<div class="os-panel"><table class="os-table"><thead><tr>
      <th></th><th>Template</th><th>Person</th><th>Project / Contract</th><th>Staged on</th><th>Missing fields</th><th>Readiness</th><th></th>
    </tr></thead><tbody>${rows || '<tr><td colspan="8" class="os-empty">No profiles match the current filters.</td></tr>'}</tbody></table></div>` : `<div class="os-card-grid">${cards || '<p class="os-empty">No profiles match the current filters.</p>'}</div>`}
    ${banner}
    ${renderPaginationBar('staged', pg)}`;
}

function renderInProcess() {
  const s = getStore();
  const filtered = filterInProcess(s.inProcess, s);
  const pg = paginateSlice(filtered, s.pagination.inProcess);
  if (pg.page !== s.pagination.inProcess.page) s.pagination.inProcess.page = pg.page;
  const visible = pg.pageItems;
  const rows = visible.map(p => `<tr class="is-clickable" data-case-id="${p.id}">
    <td><input type="checkbox" class="form-check-input" onclick="event.stopPropagation()" /></td>
    <td><strong>${esc(p.number)}</strong></td><td>${dot(p.tplKey)}${esc(tplLabel(p.tplKey))}</td>
    <td>${esc(p.people.join(', '))}</td><td>${esc(p.project)}</td><td>${esc(p.started)}</td><td>${esc(p.step)}</td>
    <td>${slaChip(p.slaDays)}</td>
    <td><span class="os-status os-status--${p.status === 'process' ? 'process' : 'hold'}">${p.status === 'process' ? 'In process' : 'On hold'}</span></td><td class="os-table__chev">›</td></tr>`).join('');
  const cards = visible.map(p => `<article class="os-card" data-case-id="${p.id}">
    <div class="os-card__stripe" style="background:${TPL_KEYS[p.tplKey]?.color}"></div>
    <div class="os-card__body"><h3>№ ${esc(p.number)}</h3>
    <div class="os-card__meta">${dot(p.tplKey)}${esc(tplLabel(p.tplKey))} · ${esc(p.started)}</div>
    <div class="os-card__meta">${esc(p.people.join(', '))}</div><div class="os-card__meta">${esc(p.project)}</div>
    <div class="os-card__meta">${slaChip(p.slaDays)} · ${esc(p.step)}</div></div>
    <div class="os-card__foot"><span class="os-status os-status--${p.status === 'process' ? 'process' : 'hold'}">${p.status === 'process' ? 'In process' : 'On hold'}</span><span>›</span></div></article>`).join('');
  const chips = renderTplFamilyChips('inProcess', s.inProcess, s.familyFilter.inProcess);
  return `<div class="os-page-head"><div><h1>Application Profiles — In process</h1>
    <p>Open a row to manage progress, documents, and linked records.</p></div></div>
    <div class="os-toolbar"><input class="os-search" type="search" placeholder="Search by number, person, project…" id="global-search" value="${esc(globalSearch)}" />
    <select class="os-search"><option>All templates</option></select><select class="os-search"><option>Newest first</option></select>
    ${viewToggle('inProcess', s.viewMode.inProcess)}</div>
    ${chips}
    ${s.viewMode.inProcess === 'list' ? `<div class="os-panel"><table class="os-table"><thead><tr>
      <th></th><th>№</th><th>Template</th><th>Person(s)</th><th>Project / Contract</th><th>Started</th><th>Current step</th><th>SLA</th><th>Status</th><th></th>
    </tr></thead><tbody>${rows || '<tr><td colspan="10" class="os-empty">No cases match the current filters.</td></tr>'}</tbody></table></div>` : `<div class="os-card-grid">${cards || '<p class="os-empty">No cases match the current filters.</p>'}</div>`}
    ${renderPaginationBar('inProcess', pg)}`;
}

function renderCase() {
  const s = getStore();
  const c = s.inProcess.find(p => p.id === route.id) || s.inProcess[0];
  if (!c) return renderInProcess();
  route.id = c.id;
  const tab = route.tab || 'overview';
  let main = '';
  let rail = '';
  let layoutCls = '';
  let mainCls = '';

  if (tab === 'overview') {
    main = renderCaseOverview(c, issuedFocusKey);
    rail = renderCaseRail(c, { full: true });
  } else if (tab === 'people') {
    main = renderCasePeopleTab(c);
    rail = renderCasePeopleRail(c);
  } else if (tab === 'progress') {
    main = renderCaseProgressTab(c);
    rail = renderCaseProgressRail(c);
  } else if (tab === 'documents') {
    main = renderCaseDocumentCopies(c);
    layoutCls = ' cw-layout--docs';
    mainCls = ' cw-main--wide';
  } else if (tab === 'resminamalar') {
    main = renderCaseResminamalarTab(c);
    layoutCls = ' cw-layout--docs';
    mainCls = ' cw-main--wide';
  } else if (tab === 'sla') {
    main = renderCaseSlaTab(c);
    layoutCls = ' cw-layout--wide';
    mainCls = ' cw-main--full';
  } else {
    main = renderCaseOverview(c, issuedFocusKey);
    rail = renderCaseRail(c, { full: true });
  }

  return `${renderCaseHeader(c)}
    <div class="cw-layout${layoutCls}">
      <nav class="cw-nav" aria-label="Case sections">${renderCaseNav(tab)}</nav>
      <div class="cw-main${mainCls}">${main}</div>
      ${rail ? `<aside class="cw-rail">${rail}</aside>` : ''}
    </div>`;
}

function renderTemplates() {
  const s = getStore();
  const filtered = filterTemplates(s.templates, s);
  const pg = paginateSlice(filtered, s.pagination.templates);
  if (pg.page !== s.pagination.templates.page) s.pagination.templates.page = pg.page;
  return renderTemplateCatalog({
    visible: pg.pageItems,
    paginationHtml: renderPaginationBar('templates', pg),
    globalSearch,
    viewMode: s.viewMode.templates,
    viewToggleHtml: viewToggle('templates', s.viewMode.templates),
    chipsHtml: renderActionFamilyChips(s.templates, s.familyFilter.templates),
  });
}

function renderTemplateOverview() {
  const s = getStore();
  const t = s.templates.find(x => x.id === route.id) || s.templates[0];
  route.id = t.id;
  return renderTemplateOverviewPage(s, t.id, tplRailSearch);
}

function renderWizard() {
  const s = getStore();
  const t = s.wizardTemplateId ? s.templates.find(x => x.id === s.wizardTemplateId) : null;
  return renderWizardPage(route.step || 0, t);
}

function renderMockups() {
  const items = MOCKUP_FILES.map(m => `<figure><img src="assets/png/${esc(m.file)}" alt="${esc(m.title)}" loading="lazy" />
    <figcaption>${esc(m.title)}</figcaption></figure>`).join('');
  return `<div class="os-page-head"><div><h1>Reference mockup gallery</h1>
    <p>PNG artifacts from 2026-08-10 design session — compare with interactive screens.</p></div></div>
    <div class="os-gallery">${items}</div>`;
}

function renderPlaceholder(title, note) {
  return `<div class="os-placeholder"><h2>${esc(title)}</h2><p>${esc(note)}</p></div>`;
}

function renderContent() {
  switch (route.name) {
    case 'dashboard': return renderPlaceholder('Dashboard', 'Officer home — summary cards (out of scope for v1).');
    case 'people': return renderPlaceholder('People', 'Person DetailView — staging actions deferred to slice H7.');
    case 'organizations': return renderPlaceholder('Organizations', 'Out of scope for v1.');
    case 'projects': return renderPlaceholder('Projects / Contracts', 'Filters invitation templates by contract.');
    case 'report-dashboard': return renderPlaceholder('Report Dashboard', 'Link to existing dashboard later.');
    case 'sla-monitor': return renderPlaceholder('SLA monitor', 'Out of scope for v1.');
    case 'staged': return renderStaged();
    case 'in-process': return renderInProcess();
    case 'case': return renderCase();
    case 'templates': return renderTemplates();
    case 'template': return renderTemplateOverview();
    case 'wizard': return renderWizard();
    case 'mockups': return renderMockups();
    default: return renderStaged();
  }
}

function bindEvents() {
  document.querySelectorAll('[data-nav]').forEach(el => {
    el.addEventListener('click', () => navigate(el.dataset.nav));
  });
  document.querySelectorAll('[data-toggle-page]').forEach(toggle => {
    toggle.querySelectorAll('[data-mode]').forEach(btn => {
      btn.addEventListener('click', () => {
        const page = toggle.dataset.togglePage;
        const mode = btn.dataset.mode;
        setViewMode(page, mode);
        if (page === 'staged') {
          navigate(mode === 'grouped' ? '#/staged?group=template' : '#/staged');
        } else {
          setRoute(parseRoute());
        }
      });
    });
  });
  document.querySelectorAll('[data-staged-id]').forEach(cb => {
    cb.addEventListener('change', () => {
      toggleStaged(cb.dataset.stagedId, cb.checked);
      setRoute(parseRoute());
    });
  });
  document.getElementById('btn-start-process')?.addEventListener('click', () => {
    const id = startProcess();
    if (id) navigate(`#/case/${id}/overview`);
  });
  document.querySelectorAll('[data-case-id]').forEach(el => {
    el.addEventListener('click', () => navigate(`#/case/${el.dataset.caseId}/overview`));
  });
  document.querySelectorAll('[data-tpl-id]').forEach(el => {
    el.addEventListener('click', () => navigate(`#/templates/${el.dataset.tplId}`));
  });
  document.querySelectorAll('[data-configure]').forEach(btn => {
    btn.addEventListener('click', e => {
      e.stopPropagation();
      getStore().wizardTemplateId = btn.dataset.configure;
      navigate('#/templates/wizard/0');
    });
  });
  document.getElementById('btn-new-template')?.addEventListener('click', () => {
    getStore().wizardTemplateId = null;
    navigate('#/templates/wizard/0');
  });
  document.getElementById('btn-configure')?.addEventListener('click', () => {
    getStore().wizardTemplateId = document.getElementById('btn-configure').dataset.id;
    navigate('#/templates/wizard/0');
  });
  document.querySelectorAll('[data-ws-tab]').forEach(btn => {
    btn.addEventListener('click', () => navigate(`#/case/${route.id}/${btn.dataset.wsTab}`));
  });
  document.querySelectorAll('[data-issued-key]').forEach(btn => {
    btn.addEventListener('click', () => {
      const key = btn.dataset.issuedKey;
      issuedFocusKey = issuedFocusKey === key ? null : key;
      setRoute(parseRoute());
    });
  });
  document.querySelectorAll('[data-wizard-step]').forEach(btn => {
    btn.addEventListener('click', () => navigate(`#/templates/wizard/${btn.dataset.wizardStep}`));
  });
  document.getElementById('wiz-back')?.addEventListener('click', () => {
    const step = Math.max(0, (route.step || 0) - 1);
    navigate(`#/templates/wizard/${step}`);
  });
  document.getElementById('wiz-next')?.addEventListener('click', () => {
    const step = Math.min(4, (route.step || 0) + 1);
    navigate(`#/templates/wizard/${step}`);
  });
  document.getElementById('wiz-publish')?.addEventListener('click', () => {
    publishTemplate();
    navigate('#/templates');
  });
  document.querySelectorAll('[data-family-filter]').forEach(btn => {
    btn.addEventListener('click', () => {
      setFamilyFilter(btn.dataset.familyFilter, btn.dataset.filterKey);
      setRoute(parseRoute());
    });
  });
  document.querySelectorAll('.dc-slot-check').forEach(cb => {
    cb.addEventListener('change', () => {
      const el = document.getElementById('dc-selected-count');
      if (el) el.textContent = String(countSelectedDocChecks(document.querySelector('.dc-page')));
    });
  });
  document.getElementById('dc-enqueue')?.addEventListener('click', () => {
    const n = countSelectedDocChecks(document.querySelector('.dc-page'));
    alert(`PDF generation enqueued for ${n} document slot(s) — mock only.`);
  });
  document.getElementById('resmi-zip')?.addEventListener('click', () => {
    const n = countResmiSelected(document.querySelector('.ct-resmi-page'));
    alert(`Resminamalar ZIP enqueued for ${n} template(s) — mock only.`);
  });
  document.getElementById('resmi-clear')?.addEventListener('click', () => {
    document.querySelectorAll('.ct-resmi-check').forEach(cb => { cb.checked = false; });
    const el = document.getElementById('resmi-selected-count');
    if (el) el.textContent = '0';
  });
  document.querySelectorAll('.ct-resmi-check').forEach(cb => {
    cb.addEventListener('change', () => {
      const el = document.getElementById('resmi-selected-count');
      if (el) el.textContent = String(countResmiSelected(document.querySelector('.ct-resmi-page')));
    });
  });
  document.querySelectorAll('[data-sw-toggle]').forEach(btn => {
    btn.addEventListener('click', () => {
      toggleStagedGroup(btn.dataset.swToggle);
      setRoute(parseRoute());
    });
  });
  document.getElementById('global-search')?.addEventListener('input', e => {
    globalSearch = e.target.value;
    resetPaginationForRoute();
    setRoute(parseRoute());
  });
  document.getElementById('tpl-rail-search')?.addEventListener('input', e => {
    tplRailSearch = e.target.value;
    setRoute(parseRoute());
  });
  document.querySelectorAll('[data-pager-page]').forEach(btn => {
    btn.addEventListener('click', () => {
      const num = parseInt(btn.dataset.pageNum, 10);
      if (Number.isNaN(num) || btn.disabled) return;
      setPaginationPage(btn.dataset.pagerPage, num);
      setRoute(parseRoute());
    });
  });
  document.querySelectorAll('[data-pager-size]').forEach(sel => {
    sel.addEventListener('change', () => {
      setPaginationPageSize(sel.dataset.pagerSize, parseInt(sel.value, 10));
      setRoute(parseRoute());
    });
  });
}

function resetPaginationForRoute() {
  const key = { staged: 'staged', 'in-process': 'inProcess', templates: 'templates' }[route.name];
  if (key) resetPaginationPage(key);
}

function render() {
  route = parseRoute();
  if (route.name === 'staged') {
    const s = getStore();
    if (route.grouped) s.viewMode.staged = 'grouped';
    else if (s.viewMode.staged === 'grouped') s.viewMode.staged = 'list';
  }
  const isWizard = route.name === 'wizard';
  document.querySelector('.os-app').classList.toggle('os-app--wizard', isWizard);
  document.getElementById('os-sidebar').innerHTML = renderSidebar(isWizard);
  document.getElementById('os-breadcrumb').innerHTML = breadcrumbs();
  document.getElementById('os-content').innerHTML = renderContent();
  bindEvents();
}

window.addEventListener('hashchange', () => setRoute(parseRoute()));
if (!location.hash) location.hash = '#/staged';
render();
