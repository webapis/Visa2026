/** In-process case workspace — PNG parity (overview tab + chrome). */

import { tplLabel } from './mock-data.js';

const TAB_META = [
  ['overview', 'Overview', 'bi-house-door'],
  ['people', 'People & links', 'bi-people'],
  ['progress', 'Progress', 'bi-bar-chart-steps'],
  ['documents', 'Document copies', 'bi-files'],
  ['resminamalar', 'Resminamalar', 'bi-folder2-open'],
  ['sla', 'SLA & deadlines', 'bi-clock-history'],
];

const PERSON_COLORS = ['#7c3aed', '#0f9d58', '#ea580c', '#2563eb', '#db2777'];

const PROGRESS_STEPS = [
  { id: 'office', label: 'Office preparation' },
  { id: 'ministry', label: 'Ministry review' },
  { id: 'migration', label: 'Migration service' },
  { id: 'complete', label: 'Complete' },
];

const LINKED_RECORDS = [
  { icon: 'bi-passport', tone: 'blue', label: 'Passport', count: 2 },
  { icon: 'bi-credit-card-2-front', tone: 'purple', label: 'Visa', count: 2 },
  { icon: 'bi-mortarboard', tone: 'green', label: 'Education', count: 1 },
  { icon: 'bi-briefcase', tone: 'orange', label: 'Position', count: 1 },
  { icon: 'bi-airplane', tone: 'teal', label: 'Travel history', count: 3 },
];

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function initials(name) {
  return String(name).split(/\s+/).filter(Boolean).map(w => w[0]).slice(0, 2).join('').toUpperCase();
}

function progressIndex(stepLabel) {
  const s = (stepLabel || '').toLowerCase();
  if (s.includes('office')) return 0;
  if (s.includes('ministry') || s.includes('awaiting')) return 1;
  if (s.includes('migration')) return 2;
  if (s.includes('complete')) return 3;
  return 1;
}

export function renderCaseNav(activeTab) {
  return TAB_META.map(([id, label, icon]) =>
    `<button type="button" class="cw-nav__item${activeTab === id ? ' is-active' : ''}" data-ws-tab="${id}">
      <i class="bi ${icon}"></i><span>${esc(label)}</span></button>`).join('');
}

export function renderCaseHeader(c) {
  const people = c.people.map((name, i) => {
    const bg = PERSON_COLORS[i % PERSON_COLORS.length];
    return `<span class="cw-person"><span class="cw-person__avatar" style="background:${bg}">${esc(initials(name))}</span>
      <i class="bi bi-person cw-person__ico"></i>${esc(name)}</span>`;
  }).join('');
  const merge = c.mergedFrom
    ? `<span class="cw-merge"><i class="bi bi-layers"></i> Merged from ${c.mergedFrom} staged profiles</span>`
    : '';
  const sla = c.slaDays != null
    ? `<span class="cw-head-badge cw-head-badge--sla"><i class="bi bi-calendar3"></i> ${c.slaDays} days remaining</span>`
    : '';
  const status = c.status === 'hold'
    ? `<span class="cw-head-badge cw-head-badge--hold"><i class="bi bi-pause-circle"></i> On hold</span>`
    : `<span class="cw-head-badge cw-head-badge--process"><i class="bi bi-gear-wide-connected"></i> In process</span>`;

  return `<header class="cw-head">
    <div class="cw-head__top">
      <div class="cw-head__title-block">
        <div class="cw-head__accent"></div>
        <div>
          <h1>Application № ${esc(c.number)}</h1>
          <p class="cw-head__sub">${esc(tplLabel(c.tplKey))} · Started ${esc(c.started)}</p>
        </div>
      </div>
      <div class="cw-head__badges">${sla}${status}
        <button type="button" class="os-btn os-btn--ghost cw-head__back" data-nav="#/in-process"><i class="bi bi-arrow-left"></i> Back to list</button>
      </div>
    </div>
    <div class="cw-head__people">${people}${merge}</div>
  </header>`;
}

function summaryTile(icon, tone, label, value) {
  return `<div class="cw-sum-tile cw-sum-tile--${tone}">
    <span class="cw-sum-tile__icon"><i class="bi ${icon}"></i></span>
    <span class="cw-sum-tile__label">${esc(label)}</span>
    <strong class="cw-sum-tile__value">${esc(value)}</strong>
  </div>`;
}

function renderProgressStepper(currentIdx) {
  return `<div class="cw-prog-stepper">${PROGRESS_STEPS.map((st, i) => {
    const done = i < currentIdx;
    const current = i === currentIdx;
    const state = done ? 'done' : current ? 'current' : 'pending';
    const badge = done ? 'Completed' : current ? 'In progress' : 'Pending';
    const badgeCls = done ? 'ok' : current ? 'active' : 'muted';
    const dot = done ? '<i class="bi bi-check-lg"></i>' : String(i + 1);
    const line = i < PROGRESS_STEPS.length - 1
      ? `<span class="cw-prog-step__line cw-prog-step__line--${i < currentIdx ? 'done' : i === currentIdx ? 'current' : 'pending'}"></span>`
      : '';
    return `<div class="cw-prog-step cw-prog-step--${state}">
      <div class="cw-prog-step__track"><span class="cw-prog-step__dot">${dot}</span>${line}</div>
      <div class="cw-prog-step__body"><strong>${esc(st.label)}</strong>
      <span class="cw-prog-step__badge cw-prog-step__badge--${badgeCls}">${badge}</span></div></div>`;
  }).join('')}</div>`;
}

function linkedRecordTile(rec) {
  return `<button type="button" class="cw-link-tile cw-link-tile--${rec.tone}">
    <span class="cw-link-tile__icon"><i class="bi ${rec.icon}"></i></span>
    <span class="cw-link-tile__label">${esc(rec.label)}</span>
    <strong class="cw-link-tile__count">${rec.count}</strong>
    <i class="bi bi-chevron-right cw-link-tile__chev"></i>
  </button>`;
}

export function renderCaseOverview(c) {
  const pIdx = progressIndex(c.step);
  return `<div class="cw-overview">
    <section class="cw-card">
      <h2 class="cw-card__title">Case summary</h2>
      <div class="cw-summary-grid">
        ${summaryTile('bi-passport', 'blue', 'Visa type', 'WP')}
        ${summaryTile('bi-gem', 'purple', 'Category', 'B')}
        ${summaryTile('bi-calendar-range', 'green', 'Period', '6 months')}
        ${summaryTile('bi-briefcase', 'orange', 'Project / Contract', c.project)}
        ${summaryTile('bi-geo-alt', 'blue', 'Entry checkpoint', 'Ashgabat')}
      </div>
    </section>
    <section class="cw-card">
      <h2 class="cw-card__title">Application progress</h2>
      ${renderProgressStepper(pIdx)}
    </section>
    <section class="cw-card">
      <h2 class="cw-card__title">Linked records</h2>
      <div class="cw-link-row">${LINKED_RECORDS.map(linkedRecordTile).join('')}</div>
    </section>
    <p class="cw-foot">Application Profile instance · Template: ${esc(tplLabel(c.tplKey))}</p>
  </div>`;
}

export function renderCaseRail(c, { full = false } = {}) {
  let html = '';
  if (full) {
    html += `<section class="cw-rail-block">
      <h4>Readiness</h4>
      <ul class="cw-checklist">
        <li><i class="bi bi-check-circle-fill"></i> Required data complete</li>
        <li><i class="bi bi-check-circle-fill"></i> BO states valid</li>
      </ul>
    </section>`;
  }
  html += `<section class="cw-rail-block">
    <h4>Quick actions</h4>
    <button type="button" class="cw-rail-btn" data-ws-tab="documents"><i class="bi bi-folder2-open"></i> Open document copies</button>
    <button type="button" class="cw-rail-btn" data-ws-tab="resminamalar"><i class="bi bi-file-earmark-zip"></i> Generate Resminamalar package</button>
    <button type="button" class="cw-rail-btn cw-rail-btn--primary" data-ws-tab="progress"><i class="bi bi-play-fill"></i> Advance progress</button>
  </section>`;
  if (full) {
    html += `<section class="cw-rail-block">
      <h4>Activity</h4>
      <ul class="cw-timeline">
        ${c.mergedFrom ? `<li><strong>Merged ${c.mergedFrom} profiles</strong><span>10 Aug 2026 09:02</span></li>` : ''}
        <li><strong>Number assigned</strong><span>10 Aug 2026 09:15</span></li>
        <li><strong>Progress: ${esc(c.step)}</strong><span>10 Aug 2026 11:40</span></li>
      </ul>
      <button type="button" class="cw-link-btn">View full activity <i class="bi bi-chevron-right"></i></button>
    </section>`;
  }
  return html;
}
