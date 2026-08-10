export const TPL_KEYS = {
  reg: { color: 'var(--os-tpl-reg)', label: 'Registration upon arrival', icon: 'bi-person-check' },
  inv: { color: 'var(--os-tpl-inv)', label: 'Invitation', icon: 'bi-envelope' },
  ext: { color: 'var(--os-tpl-ext)', label: 'Visa extension', icon: 'bi-calendar-plus' },
  wp: { color: 'var(--os-tpl-wp)', label: 'Work permit', icon: 'bi-briefcase' },
  can: { color: 'var(--os-tpl-can)', label: 'Cancellation', icon: 'bi-x-circle' },
  bt: { color: '#7c3aed', label: 'Business trip', icon: 'bi-airplane' },
  fm: { color: '#64748b', label: 'Family registration', icon: 'bi-people' },
};

export const MOCKUP_FILES = [
  { file: 'visa2026-custom-left-navigation-shell-mockup.png', title: 'Custom left navigation shell' },
  { file: 'application-profiles-navigation-sidebar-mockup.png', title: 'Application profiles nav' },
  { file: 'staged-application-profiles-workspace-mockup.png', title: 'Staged — grouped workspace' },
  { file: 'staged-profiles-listview-table-mockup.png', title: 'Staged — ListView' },
  { file: 'staged-profiles-grid-cards-mockup.png', title: 'Staged — Grid' },
  { file: 'process-started-profiles-listview-table-mockup.png', title: 'In process — ListView' },
  { file: 'process-started-profiles-list-cards-mockup.png', title: 'In process — Grid' },
  { file: 'process-started-application-profile-workspace-mockup.png', title: 'Workspace — Overview' },
  { file: 'process-started-nav-overview.png', title: 'Workspace — Overview (alt)' },
  { file: 'process-started-nav-people-links.png', title: 'Workspace — People & links' },
  { file: 'process-started-nav-progress.png', title: 'Workspace — Progress' },
  { file: 'process-started-nav-document-copies.png', title: 'Workspace — Document copies' },
  { file: 'process-started-nav-resminamalar.png', title: 'Workspace — Resminamalar' },
  { file: 'process-started-nav-sla-deadlines.png', title: 'Workspace — SLA & deadlines' },
  { file: 'application-profile-templates-listview-mockup.png', title: 'Templates — ListView' },
  { file: 'application-profile-templates-grid-mockup.png', title: 'Templates — Grid' },
  { file: 'application-profile-template-overview-mockup.png', title: 'Template overview' },
  { file: 'application-profile-template-wizard-mockup.png', title: 'Wizard step 1' },
  { file: 'application-profile-template-wizard-step2-mockup.png', title: 'Wizard step 2' },
  { file: 'application-profile-template-wizard-step3-mockup.png', title: 'Wizard step 3' },
  { file: 'application-profile-template-wizard-step4-mockup.png', title: 'Wizard step 4' },
  { file: 'application-profile-template-wizard-step5-mockup.png', title: 'Wizard step 5' },
];

const IN_PROCESS_CORE = [
  { id: 'p1', number: '2026-0147', tplKey: 'ext', people: ['Maksat Orazow', 'Döwran Ataýew', 'Aýgul Berdiýewa'], project: 'Plant Expansion 2026', started: '10 Aug 2026', step: 'Ministry review', slaDays: 12, status: 'process', mergedFrom: 3 },
  { id: 'p2', number: '2026-0142', tplKey: 'inv', people: ['Batyr Hojayew'], project: 'Solar Field Phase 2', started: '08 Aug 2026', step: 'Office preparation', slaDays: 5, status: 'process' },
  { id: 'p3', number: '2026-0138', tplKey: 'reg', people: ['Aýgul Berdiýewa', 'Maksat Orazow'], project: 'Infrastructure 2025', started: '05 Aug 2026', step: 'Migration service', slaDays: 18, status: 'process' },
  { id: 'p4', number: '2026-0135', tplKey: 'ext', people: ['Döwran Ataýew'], project: 'Plant Expansion 2026', started: '03 Aug 2026', step: 'Awaiting visa category', slaDays: null, status: 'hold' },
  { id: 'p5', number: '2026-0130', tplKey: 'wp', people: ['Serdar Gurbanow'], project: 'Solar Field Phase 2', started: '02 Aug 2026', step: 'Migration service', slaDays: 9, status: 'process' },
  { id: 'p6', number: '2026-0128', tplKey: 'reg', people: ['Jennet Orazowa', 'Hemra Annaberdiýew'], project: 'Infrastructure 2025', started: '01 Aug 2026', step: 'Office preparation', slaDays: 21, status: 'process' },
];

/** PNG nav parity: 24 in-process cases (All 24 · ext 8 · inv 6 · reg 5 · wp 5). */
function seedInProcessDemoCases() {
  const people = [
    'Li Wei', 'Maria Hernandez', 'Oleg Petrov', 'Carlos Mendes', 'Gülşat Annagulyýewa',
    'Batyr Hojayew', 'Hemra Annaberdiýew', 'Jennet Orazowa', 'Serdar Gurbanow', 'Aýgul Berdiýewa',
  ];
  const projects = ['Plant Expansion 2026', 'Solar Field Phase 2', 'Infrastructure 2025', 'TechNova Ltd.', '—'];
  const steps = ['Office preparation', 'Ministry review', 'Migration service', 'Awaiting visa category'];
  const extraTpl = [
    'ext', 'ext', 'ext', 'ext', 'ext', 'ext',
    'inv', 'inv', 'inv', 'inv', 'inv',
    'reg', 'reg', 'reg',
    'wp', 'wp', 'wp', 'wp',
  ];
  const extras = extraTpl.map((tplKey, i) => {
    const num = 127 - i;
    return {
      id: `pd${i + 7}`,
      number: `2026-${String(num).padStart(4, '0')}`,
      tplKey,
      people: [people[i % people.length]],
      project: projects[i % projects.length],
      started: `${String(28 - (i % 27)).padStart(2, '0')} Jul 2026`,
      step: steps[i % steps.length],
      slaDays: i % 6 === 0 ? null : 6 + (i % 16),
      status: i % 8 === 0 ? 'hold' : 'process',
    };
  });
  return [...IN_PROCESS_CORE, ...extras];
}

const store = {
  user: { name: 'Aýlar Kulyýewa', role: 'Visa officer', office: 'Ashgabat', initials: 'AK' },
  templates: [
    { id: 't1', name: 'Invitation + work permit (employee)', code: 'INV_WP_EMP', action: 'Issuance', route: 'Via ministry', audience: 'Employee', status: 'active', tplKey: 'inv', stagedUses: 24, inProcessUses: 8, selectionCode: '101', lastConfigured: '8 Aug 2026' },
    { id: 't2', name: 'Registration upon arrival', code: 'REG_ARR', action: 'Registration', route: 'Direct migration', audience: 'Employee, Family', status: 'active', tplKey: 'reg', stagedUses: 18, inProcessUses: 5, selectionCode: '102', lastConfigured: '6 Aug 2026' },
    { id: 't3', name: 'Visa extension — WP 6 months', code: 'VISA_EXT_WP', action: 'Issuance', route: 'Via ministry', audience: 'Employee', status: 'active', tplKey: 'ext', stagedUses: 31, inProcessUses: 12, selectionCode: '103', lastConfigured: '5 Aug 2026' },
    { id: 't4', name: 'Invitation scoped GT-15', code: 'INV_GT15', action: 'Issuance', route: 'Via ministry', audience: 'Employee', status: 'locked', tplKey: 'inv', stagedUses: 12, inProcessUses: 0, selectionCode: '104', lastConfigured: '1 Aug 2026' },
    { id: 't5', name: 'Cancellation — visa', code: 'VISA_CANCEL', action: 'Cancellation', route: 'Direct migration', audience: 'Employee', status: 'active', tplKey: 'can', stagedUses: 9, inProcessUses: 4, selectionCode: '105', lastConfigured: '7 Aug 2026' },
    { id: 't6', name: 'Work permit extension', code: 'WP_EXT', action: 'Issuance', route: 'Via ministry', audience: 'Employee', status: 'draft', tplKey: 'wp', stagedUses: 5, inProcessUses: 0, selectionCode: '106', lastConfigured: '—' },
    { id: 't7', name: 'Business trip — registration pack', code: 'BT_REG', action: 'Business trip', route: 'Direct migration', audience: 'Employee', status: 'locked', tplKey: 'bt', stagedUses: 7, inProcessUses: 0, selectionCode: '107', lastConfigured: '3 Aug 2026' },
    { id: 't8', name: 'Family registration', code: 'FM_REG', action: 'Registration', route: 'Direct migration', audience: 'Employee Family', status: 'draft', tplKey: 'fm', stagedUses: 3, inProcessUses: 0, selectionCode: '108', lastConfigured: '—' },
    { id: 't9', name: 'Cancellation — work permit', code: 'WP_CANCEL', action: 'Cancellation', route: 'Via ministry', audience: 'Employee', status: 'active', tplKey: 'can', stagedUses: 4, inProcessUses: 1, selectionCode: '109', lastConfigured: '4 Aug 2026' },
    { id: 't10', name: 'Business trip — short stay', code: 'BT_SHORT', action: 'Business trip', route: 'Direct migration', audience: 'Employee', status: 'active', tplKey: 'bt', stagedUses: 6, inProcessUses: 2, selectionCode: '110', lastConfigured: '2 Aug 2026' },
    { id: 't11', name: 'Business trip — visa pack', code: 'BT_VISA', action: 'Business trip', route: 'Via ministry', audience: 'Employee', status: 'active', tplKey: 'bt', stagedUses: 5, inProcessUses: 1, selectionCode: '111', lastConfigured: '30 Jul 2026' },
    { id: 't12', name: 'Border zone registration', code: 'BZ_REG', action: 'Registration', route: 'Direct migration', audience: 'Employee Scoped', status: 'active', tplKey: 'reg', stagedUses: 8, inProcessUses: 3, selectionCode: '112', lastConfigured: '28 Jul 2026' },
  ],
  staged: [
    { id: 's1', tplKey: 'reg', person: 'Aýgul Berdiýewa', project: 'Plant Expansion 2026', stagedOn: '10 Aug 2026 08:12', missing: 'No entry date', readiness: 'incomplete' },
    { id: 's2', tplKey: 'reg', person: 'Maksat Orazow', project: 'Plant Expansion 2026', stagedOn: '10 Aug 2026 08:05', missing: '—', readiness: 'ready' },
    { id: 's3', tplKey: 'reg', person: 'Gülşat Annagulyýewa', project: '—', stagedOn: '09 Aug 2026', missing: 'Visa category', readiness: 'incomplete' },
    { id: 's4', tplKey: 'inv', person: 'Aýgul Berdiýewa', project: 'Plant Expansion 2026', stagedOn: '09 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's5', tplKey: 'inv', person: 'Batyr Hojayew', project: '—', stagedOn: '09 Aug 2026', missing: 'Contract not linked', readiness: 'incomplete' },
    { id: 's6', tplKey: 'ext', person: 'Maksat Orazow', project: 'Solar Field Phase 2', stagedOn: '08 Aug 2026', missing: 'Visa type, Visa period', readiness: 'awaiting' },
    { id: 's7', tplKey: 'ext', person: 'Döwran Ataýew', project: 'Plant Expansion 2026', stagedOn: '08 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's8', tplKey: 'wp', person: 'Serdar Gurbanow', project: 'Solar Field Phase 2', stagedOn: '08 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's9', tplKey: 'wp', person: 'Jennet Orazowa', project: 'Infrastructure 2025', stagedOn: '07 Aug 2026', missing: 'Work permit scan', readiness: 'incomplete' },
    { id: 's10', tplKey: 'wp', person: 'Hemra Annaberdiýew', project: 'Plant Expansion 2026', stagedOn: '07 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's11', tplKey: 'inv', person: 'Li Wei', project: 'TechNova Ltd.', stagedOn: '07 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's12', tplKey: 'inv', person: 'Maria Hernandez', project: 'TechNova Ltd.', stagedOn: '06 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's13', tplKey: 'inv', person: 'Oleg Petrov', project: '—', stagedOn: '06 Aug 2026', missing: 'Contract not linked', readiness: 'incomplete' },
    { id: 's14', tplKey: 'ext', person: 'Aýgul Berdiýewa', project: 'Plant Expansion 2026', stagedOn: '05 Aug 2026', missing: 'Visa type', readiness: 'awaiting' },
    { id: 's15', tplKey: 'ext', person: 'Gülşat Annagulyýewa', project: 'Solar Field Phase 2', stagedOn: '05 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's16', tplKey: 'reg', person: 'Döwran Ataýew', project: 'Infrastructure 2025', stagedOn: '04 Aug 2026', missing: '—', readiness: 'ready' },
    { id: 's17', tplKey: 'reg', person: 'Batyr Hojayew', project: 'Plant Expansion 2026', stagedOn: '04 Aug 2026', missing: 'No entry date', readiness: 'incomplete' },
    { id: 's18', tplKey: 'inv', person: 'Maksat Orazow', project: 'Solar Field Phase 2', stagedOn: '03 Aug 2026', missing: '—', readiness: 'ready' },
  ],
  inProcess: seedInProcessDemoCases(),
  stagedSelected: new Set(),
  stagedGroupCollapsed: new Set(),
  viewMode: { staged: 'list', inProcess: 'list', templates: 'list' },
  familyFilter: { staged: 'all', inProcess: 'all', templates: 'all' },
  pagination: { staged: { page: 1, pageSize: 10 }, inProcess: { page: 1, pageSize: 10 }, templates: { page: 1, pageSize: 10 } },
  wizardTemplateId: null,
};

export function getStore() { return store; }

export function tplLabel(key) { return TPL_KEYS[key]?.label ?? key; }

export function isSelectable(s) { return s.readiness === 'ready'; }

export function toggleStaged(id, checked) {
  if (checked) store.stagedSelected.add(id);
  else store.stagedSelected.delete(id);
}

export function toggleStagedGroup(tplKey) {
  if (store.stagedGroupCollapsed.has(tplKey)) store.stagedGroupCollapsed.delete(tplKey);
  else store.stagedGroupCollapsed.add(tplKey);
}

export function startProcess() {
  const ids = [...store.stagedSelected].filter(id => {
    const row = store.staged.find(s => s.id === id);
    return row && isSelectable(row);
  });
  if (ids.length === 0) return null;
  const people = ids.map(id => store.staged.find(s => s.id === id).person);
  const n = 147 + store.inProcess.length;
  const caseId = 'p' + Date.now();
  const row = {
    id: caseId,
    number: `2026-${String(n).padStart(4, '0')}`,
    tplKey: 'ext',
    people,
    project: 'Plant Expansion 2026',
    started: new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }),
    step: 'Office preparation',
    slaDays: 14,
    status: 'process',
    mergedFrom: ids.length,
  };
  store.inProcess.unshift(row);
  ids.forEach(id => {
    const i = store.staged.findIndex(s => s.id === id);
    if (i >= 0) store.staged.splice(i, 1);
  });
  store.stagedSelected.clear();
  return caseId;
}

export function setViewMode(page, mode) {
  if (store.viewMode[page] !== undefined) store.viewMode[page] = mode;
}

export function setFamilyFilter(page, key) {
  if (store.familyFilter[page] !== undefined) store.familyFilter[page] = key;
  resetPaginationPage(page);
}

export function setPaginationPage(page, num) {
  if (store.pagination[page]) store.pagination[page].page = Math.max(1, num);
}

export function setPaginationPageSize(page, size) {
  if (!store.pagination[page]) return;
  store.pagination[page].pageSize = size;
  store.pagination[page].page = 1;
}

export function resetPaginationPage(page) {
  if (store.pagination[page]) store.pagination[page].page = 1;
}

export function publishTemplate() {
  const t = store.templates.find(x => x.id === store.wizardTemplateId);
  if (t) t.status = 'active';
  else {
    store.templates.push({
      id: 't' + Date.now(),
      name: 'New profile template',
      code: 'NEW_TPL',
      action: 'Issuance',
      route: 'Via ministry',
      audience: 'Employee',
      status: 'active',
      tplKey: 'inv',
      stagedUses: 0,
      inProcessUses: 0,
    });
  }
}
