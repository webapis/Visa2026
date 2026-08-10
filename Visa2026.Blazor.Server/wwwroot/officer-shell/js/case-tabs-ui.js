/** Case workspace tabs — People, Progress, Resminamalar, SLA (P10 PNG parity). */

import { tplLabel } from './mock-data.js';

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

const RECORD_TYPES = [
  { key: 'passport', icon: 'bi-passport', label: 'Passport' },
  { key: 'education', icon: 'bi-mortarboard', label: 'Education' },
  { key: 'position', icon: 'bi-briefcase', label: 'Position' },
  { key: 'address', icon: 'bi-geo-alt', label: 'Address' },
  { key: 'travel', icon: 'bi-airplane', label: 'Travel history' },
  { key: 'medical', icon: 'bi-heart-pulse', label: 'Medical' },
  { key: 'wp', icon: 'bi-file-earmark-text', label: 'Work permit' },
  { key: 'invitation', icon: 'bi-envelope', label: 'Invitation' },
  { key: 'salary', icon: 'bi-cash-stack', label: 'Salary' },
  { key: 'duty', icon: 'bi-clipboard-check', label: 'Work duty' },
  { key: 'border', icon: 'bi-signpost-split', label: 'Border zone' },
];

const PERSON_PASSPORTS = ['TM1234567', 'TM2345678', 'TM3456789'];

function personRecordStatus(personIdx, recKey) {
  if (personIdx === 0) {
    if (recKey === 'border') return { state: 'empty', count: 0 };
    return { state: 'valid', count: recKey === 'travel' ? 2 : 1 };
  }
  if (['passport', 'education', 'address', 'travel', 'medical'].includes(recKey)) {
    return { state: 'valid', count: recKey === 'travel' ? 3 : 1 };
  }
  if (recKey === 'border') return { state: 'empty', count: 0 };
  return { state: 'expired', count: 0 };
}

function recordStatusHtml(st) {
  if (st.state === 'valid') {
    return `<span class="ct-rec__status ct-rec__status--valid"><i class="bi bi-check-circle-fill"></i> Valid</span>`;
  }
  if (st.state === 'expired') {
    return `<span class="ct-rec__status ct-rec__status--bad"><i class="bi bi-exclamation-circle-fill"></i> Expired</span>`;
  }
  return `<span class="ct-rec__status ct-rec__status--muted">—</span>`;
}

function linkedRecordsSummary(caseRow) {
  const totals = {};
  caseRow.people.forEach((_, pIdx) => {
    for (const r of RECORD_TYPES) {
      const st = personRecordStatus(pIdx, r.key);
      if (st.state === 'valid') totals[r.label] = (totals[r.label] || 0) + st.count;
    }
  });
  return totals;
}

export function renderCasePeopleTab(c) {
  const visaBase = c.number.replace(/\D/g, '').slice(-4) || '0147';
  const peopleRows = c.people.map((name, i) => `<tr>
    <td><a href="#" class="ct-link" onclick="return false">${esc(name)}</a></td>
    <td>${i === 0 ? 'Primary' : 'Dependent'}</td>
    <td><code>${PERSON_PASSPORTS[i] ?? '—'}</code></td>
    <td><code>VISA-2026-${visaBase}-0${i + 1}</code></td>
    <td><button type="button" class="os-btn os-btn--ghost ct-btn-sm">Open person detail</button></td>
  </tr>`).join('');

  const personPanels = c.people.map((name, pIdx) => {
    const cards = RECORD_TYPES.map(r => {
      const st = personRecordStatus(pIdx, r.key);
      return `<div class="ct-rec">
        <span class="ct-rec__icon"><i class="bi ${r.icon}"></i></span>
        <span class="ct-rec__label">${esc(r.label)}</span>
        <strong class="ct-rec__count">${st.count || '0'}</strong>
        ${recordStatusHtml(st)}
      </div>`;
    }).join('');
    return `<details class="ct-person-panel" open>
      <summary class="ct-person-panel__head">
        <span><i class="bi bi-person-circle"></i> <strong>${esc(name)}</strong></span>
        <button type="button" class="os-btn os-btn--ghost ct-btn-sm" onclick="event.preventDefault()">Open person detail</button>
      </summary>
      <div class="ct-rec-grid">${cards}</div>
    </details>`;
  }).join('');

  return `<div class="ct-page">
    <h2 class="ct-page__title">People on this case</h2>
    <div class="os-panel" style="margin-bottom:16px"><table class="os-table">
      <thead><tr><th>Person</th><th>Role</th><th>Passport</th><th>Visa</th><th></th></tr></thead>
      <tbody>${peopleRows}</tbody>
    </div>
    <h2 class="ct-page__title">Linked records by person</h2>
    ${personPanels}
  </div>`;
}

export function renderCasePeopleRail(c) {
  const dependents = Math.max(0, c.people.length - 1);
  const totals = linkedRecordsSummary(c);
  const lines = Object.entries(totals).map(([k, v]) =>
    `<li><span>${esc(k)}</span><strong>${v}</strong></li>`).join('');
  return `<section class="cw-rail-block">
    <h4>People summary</h4>
    <ul class="ct-summary-list">
      <li><span>Total</span><strong>${c.people.length}</strong></li>
      <li><span>Primary</span><strong>1</strong></li>
      <li><span>Dependents</span><strong>${dependents}</strong></li>
      <li><span>Sponsors</span><strong>0</strong></li>
    </ul>
  </section>
  <section class="cw-rail-block">
    <h4>Linked records summary</h4>
    <ul class="ct-summary-list">${lines}</ul>
    <p class="ct-rail-foot"><i class="bi bi-arrow-clockwise"></i> Last updated: 10 Aug 2026 14:32</p>
  </section>`;
}

const PROGRESS_V_STEPS = [
  { id: 'office', label: 'Office preparation', date: '10 Aug 2026', state: 'done' },
  { id: 'ministry', label: 'Ministry review', date: '11 Aug 2026', state: 'current' },
  { id: 'migration', label: 'Migration service', date: '', state: 'pending' },
  { id: 'complete', label: 'Complete', date: '', state: 'pending' },
];

function progressStepRow(step) {
  const badge = step.state === 'done' ? 'Completed' : step.state === 'current' ? 'In progress' : 'Pending';
  const badgeCls = step.state === 'done' ? 'ok' : step.state === 'current' ? 'active' : 'muted';
  const icon = step.state === 'done' ? 'bi-check-lg' : step.state === 'current' ? 'bi-pencil' : 'bi-circle';
  const expanded = step.state === 'current' ? `<div class="ct-prog-detail">
    <div class="ct-prog-detail__grid">
      <div>
        <label class="ct-label">Officer notes</label>
        <textarea class="ct-textarea" rows="3" placeholder="Add notes for the current progress step…"></textarea>
        <label class="ct-label" style="margin-top:10px">Ministry letter</label>
        <div class="ct-upload"><i class="bi bi-cloud-arrow-up"></i> Upload file<br><span>PDF, DOCX up to 10 MB</span></div>
      </div>
      <dl class="ct-meta">
        <div><dt>Current state</dt><dd><span class="os-status os-status--process">Under review by ministry</span></dd></div>
        <div><dt>Started on</dt><dd>11 Aug 2026</dd></div>
        <div><dt>SLA target</dt><dd>19 Aug 2026</dd></div>
      </dl>
    </div>
  </div>` : '';
  return `<div class="ct-prog-v ct-prog-v--${step.state}">
    <div class="ct-prog-v__rail">
      <span class="ct-prog-v__date">${esc(step.date || '—')}</span>
      <span class="ct-prog-v__dot"><i class="bi ${icon}"></i></span>
      <span class="ct-prog-v__line"></span>
    </div>
    <div class="ct-prog-v__body">
      <div class="ct-prog-v__head">
        <strong>${esc(step.label)}</strong>
        <span class="cw-prog-step__badge cw-prog-step__badge--${badgeCls}">${badge}</span>
      </div>
      ${expanded}
    </div>
  </div>`;
}

export function renderCaseProgressTab(c) {
  return `<div class="ct-page">
    <h2 class="ct-page__title">Progress</h2>
    <p class="ct-page__sub">Application process for ${esc(tplLabel(c.tplKey))} · current step: ${esc(c.step)}</p>
    <div class="ct-prog-v-list">${PROGRESS_V_STEPS.map(progressStepRow).join('')}</div>
  </div>`;
}

export function renderCaseProgressRail(c) {
  const days = c.slaDays ?? 8;
  const pct = Math.max(10, Math.min(90, 100 - days * 8));
  return `<section class="cw-rail-block">
    <h4>SLA for current step</h4>
    <div class="ct-sla-ring" style="--ct-pct:${pct}%">
      <strong>${days}</strong><span>days left</span>
    </div>
    <ul class="ct-summary-list" style="margin-top:10px">
      <li><span>Target date</span><strong>19 Aug 2026</strong></li>
      <li><span>Total SLA</span><strong>10 days</strong></li>
    </ul>
  </section>
  <section class="cw-rail-block">
    <h4>Assigned officer</h4>
    <div class="ct-officer">
      <span class="ct-officer__avatar">JD</span>
      <div><strong>John Doe</strong><br><span class="ct-muted">Ministry Review Officer</span></div>
    </div>
  </section>
  <section class="cw-rail-block">
    <button type="button" class="os-btn os-btn--primary" style="width:100%;margin-bottom:8px"><i class="bi bi-skip-forward"></i> Advance progress</button>
    <button type="button" class="os-btn" style="width:100%"><i class="bi bi-journal-plus"></i> Add progress note</button>
  </section>
  <section class="cw-rail-block">
    <h4>History</h4>
    <ul class="cw-timeline">
      <li><strong>Moved to Ministry review</strong><span>11 Aug</span></li>
      <li><strong>Moved to Office preparation</strong><span>10 Aug</span></li>
      <li><strong>Application submitted</strong><span>10 Aug</span></li>
    </ul>
    <button type="button" class="cw-link-btn">View full history <i class="bi bi-chevron-right"></i></button>
  </section>`;
}

const RESMI_TEMPLATES = [
  { group: 'Application scalar', items: [
    { id: 'r1', name: 'Arza haty / Cover Letter.docx', type: 'Word', nested: 'Application', readiness: 'ready', merge: 'ok', checked: true },
    { id: 'r2', name: 'Visa uzaltmak barada arza.docx', type: 'Word', nested: 'Application', readiness: 'warn', merge: '2 missing', checked: true },
    { id: 'r3', name: 'Giriş-çykyş seneleri / Travel dates.docx', type: 'Word', nested: 'Application', readiness: 'ready', merge: 'ok', checked: false },
    { id: 'r4', name: 'Maliýe kepillik / Financial guarantee.xlsx', type: 'Excel', nested: 'Application', readiness: 'warn', merge: '1 missing', checked: true },
    { id: 'r5', name: 'Kepillik haty / Sponsor letter.docx', type: 'Word', nested: 'Application', readiness: 'ready', merge: 'ok', checked: true },
    { id: 'r6', name: 'Merkezi maglumatlar / Summary.docx', type: 'Word', nested: 'Application', readiness: 'warn', merge: '3 missing', checked: true },
  ]},
  { group: 'Item per person', items: [
    { id: 'r7', name: 'Pasport maglumaty / Passport data.docx', type: 'Word', nested: 'Item per person', readiness: 'ready', merge: 'ok', checked: false },
    { id: 'r8', name: 'Suratlar sanawy / Photo list.xlsx', type: 'Excel', nested: 'Item per person', readiness: 'ready', merge: 'ok', checked: false },
    { id: 'r9', name: 'Maşgala agzalary / Family members.docx', type: 'Word', nested: 'Item per person', readiness: 'warn', merge: '2 missing', checked: false },
  ]},
];

function resmiReadinessPill(r) {
  if (r === 'ready') return '<span class="ct-resmi-pill ct-resmi-pill--ready">Ready</span>';
  return '<span class="ct-resmi-pill ct-resmi-pill--warn">Warning</span>';
}

function resmiMergeCell(m) {
  if (m === 'ok') return '<span class="ct-resmi-merge ct-resmi-merge--ok"><i class="bi bi-check-circle-fill"></i> All good</span>';
  return `<span class="ct-resmi-merge ct-resmi-merge--warn"><i class="bi bi-exclamation-triangle-fill"></i> ${esc(m)}</span>`;
}

export function renderCaseResminamalarTab(c) {
  const selected = RESMI_TEMPLATES.flatMap(g => g.items).filter(i => i.checked).length;
  const groups = RESMI_TEMPLATES.map(g => {
    const rows = g.items.map(t => `<tr class="ct-resmi-row${t.id === 'r1' ? ' is-preview-active' : ''}" data-resmi-id="${t.id}">
      <td><input type="checkbox" class="form-check-input ct-resmi-check" data-resmi-id="${t.id}" ${t.checked ? 'checked' : ''} /></td>
      <td><i class="bi bi-file-earmark-${t.type === 'Excel' ? 'spreadsheet' : 'word'}"></i> ${esc(t.name)}</td>
      <td>${esc(t.type)}</td>
      <td>${esc(t.nested)}</td>
      <td>${resmiReadinessPill(t.readiness)}</td>
      <td>${resmiMergeCell(t.merge)}</td>
    </tr>`).join('');
    return `<details class="ct-resmi-group" open>
      <summary class="ct-resmi-group__head"><i class="bi bi-chevron-down"></i> ${esc(g.group)}</summary>
      <table class="os-table ct-resmi-table"><thead><tr>
        <th></th><th>Template name</th><th>Type</th><th>Nested under</th><th>Readiness</th><th>Merge fields</th>
      </tr></thead><tbody>${rows}</tbody></table>
    </details>`;
  }).join('');

  return `<div class="ct-resmi-page">
    <div class="ct-resmi-head">
      <div>
        <h2 class="ct-page__title">Report package catalog — Resminamalar</h2>
        <p class="ct-page__sub">Generate a ZIP package of documents from profile nested templates.</p>
      </div>
      <div class="ct-resmi-summary"><i class="bi bi-check2-square"></i> Selection summary: <strong id="resmi-selected-count">${selected}</strong> templates selected
        <button type="button" class="os-btn os-btn--ghost ct-btn-sm" id="resmi-clear">Clear selection</button>
      </div>
    </div>
    <div class="ct-resmi-tabs"><button type="button" class="ct-resmi-tab is-active">Application</button><button type="button" class="ct-resmi-tab">Item per person</button></div>
    <div class="ct-resmi-split">
      <div class="ct-resmi-main">${groups}
        <div class="ct-resmi-info"><i class="bi bi-info-circle"></i> Readiness is evaluated based on profile data and template merge fields. Templates with warnings can still be generated.</div>
        <div class="ct-resmi-actions">
          <button type="button" class="os-btn"><i class="bi bi-eye"></i> Preview template</button>
          <button type="button" class="os-btn os-btn--primary" id="resmi-zip"><i class="bi bi-file-earmark-zip"></i> Generate ZIP package</button>
        </div>
      </div>
      <aside class="ct-resmi-preview">
        <h3>Template preview</h3>
        <p class="ct-resmi-preview__file"><i class="bi bi-file-earmark-word"></i> Arza haty / Cover Letter.docx</p>
        <div class="ct-resmi-preview__doc">
          <p>Dear Sir/Madam,</p>
          <p>We request visa extension for <mark>«FullName»</mark>, passport <mark>«PassportNumber»</mark>, born <mark>«DateOfBirth»</mark>.</p>
          <p>Duration: <mark>«ExtensionDuration»</mark>. Reason: <mark>«ExtensionReason»</mark>.</p>
        </div>
        <div class="ct-resmi-preview__pager"><button type="button" disabled>‹</button> 1 / 2 <button type="button">›</button></div>
        <div class="ct-resmi-warn-box">
          <div class="ct-resmi-warn-box__head"><i class="bi bi-exclamation-triangle-fill"></i> Missing merge fields <span class="ct-badge-count">2</span></div>
          <ul><li>«ExtensionDuration»</li><li>«ExtensionReason»</li></ul>
          <p class="ct-muted">These fields are empty in the profile and will appear blank.</p>
          <button type="button" class="cw-link-btn">Go to profile data <i class="bi bi-box-arrow-up-right"></i></button>
        </div>
      </aside>
    </div>
  </div>`;
}

const SLA_DEADLINES = [
  { step: 'Application received', due: '10 Aug 2026', days: '—', status: 'completed' },
  { step: 'Document check', due: '14 Aug 2026', days: '2', status: 'completed' },
  { step: 'Ministry review', due: '22 Aug 2026', days: '8', status: 'inprogress', bold: true },
  { step: 'Decision', due: '05 Sep 2026', days: '22', status: 'pending' },
  { step: 'Finalization', due: '15 Sep 2026', days: '35', status: 'pending' },
];

function slaStatusPill(s) {
  const map = {
    completed: ['Completed', 'ready'],
    inprogress: ['In progress', 'process'],
    pending: ['Pending', 'hold'],
  };
  const [label, cls] = map[s] ?? ['—', 'hold'];
  return `<span class="os-status os-status--${cls}">${label}</span>`;
}

export function renderCaseSlaTab(c) {
  const days = c.slaDays ?? 12;
  const rows = SLA_DEADLINES.map(r => `<tr class="${r.bold ? 'ct-sla-row--current' : ''}">
    <td>${r.bold ? `<strong>${esc(r.step)}</strong>` : esc(r.step)}</td>
    <td>${esc(r.due)}</td>
    <td class="${r.status === 'inprogress' ? 'ct-sla-days--ok' : ''}">${esc(r.days)}</td>
    <td>${slaStatusPill(r.status)}</td>
  </tr>`).join('');

  return `<div class="ct-page">
    <h2 class="ct-page__title">SLA dashboard</h2>
    <div class="ct-sla-metrics">
      <div class="ct-sla-metric">
        <h3>Overall case SLA</h3>
        <div class="ct-sla-metric__body">
          <div class="ct-sla-ring ct-sla-ring--lg" style="--ct-pct:73%"><strong>${days}</strong><span>days remaining</span></div>
          <div><p class="ct-muted">Total SLA <strong>45 days</strong></p><p class="ct-muted">Elapsed <strong>33 days</strong></p>
          <p class="ct-on-track"><span></span> On track</p></div>
        </div>
      </div>
      <div class="ct-sla-metric">
        <h3>Current step</h3>
        <p class="ct-muted">Ministry review deadline · Due 22 Aug 2026</p>
        <div class="ct-sla-metric__body">
          <div class="ct-sla-ring" style="--ct-pct:20%"><strong>8</strong><span>days remaining</span></div>
          <p class="ct-on-track"><span></span> On track</p>
        </div>
      </div>
      <div class="ct-sla-metric">
        <h3>Migration SLA</h3>
        <p class="ct-sla-metric__big">45 <span>days</span></p>
        <p class="ct-muted">Total configured on profile <i class="bi bi-info-circle"></i></p>
      </div>
    </div>
    <div class="ct-sla-timeline">
      <div class="ct-sla-timeline__node"><i class="bi bi-flag"></i><strong>Started</strong><span>10 Aug 2026</span></div>
      <div class="ct-sla-timeline__line"></div>
      <div class="ct-sla-timeline__node"><i class="bi bi-building"></i><strong>Ministry due</strong><span>22 Aug 2026</span></div>
      <div class="ct-sla-timeline__line"></div>
      <div class="ct-sla-timeline__node"><i class="bi bi-flag-fill"></i><strong>Expected completion</strong><span>15 Sep 2026</span></div>
    </div>
    <div class="ct-sla-lower">
      <div class="ct-sla-table-wrap">
        <h3>Deadlines</h3>
        <table class="os-table"><thead><tr><th>Step</th><th>Due date</th><th>Days left</th><th>Status</th></tr></thead><tbody>${rows}</tbody></table>
      </div>
      <div class="ct-sla-aside">
        <div class="ct-sla-alert"><i class="bi bi-exclamation-triangle-fill"></i>
          <div><strong>Ministry review deadline approaching</strong>
          <p>Due in 8 days on 22 Aug 2026. Ensure all reviews and required actions are completed on time.</p></div>
        </div>
        <div class="cw-rail-block" style="box-shadow:none;margin:0">
          <h4>SLA source (Profile template) <i class="bi bi-info-circle"></i></h4>
          <button type="button" class="cw-link-btn">MigrationSlaDays from template</button>
          <p class="ct-muted" style="margin:8px 0 0">Total SLA (days): <strong>45</strong><br>Last updated: 01 Aug 2026</p>
        </div>
      </div>
    </div>
    <p class="ct-sla-foot">All dates are displayed in system time zone (UTC+5:00) Ashgabat.</p>
  </div>`;
}

export function countResmiSelected(root) {
  if (!root) return 0;
  return root.querySelectorAll('.ct-resmi-check:checked').length;
}
