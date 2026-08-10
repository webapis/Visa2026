/** Document copies tab — PNG parity (process-started-nav-document-copies.png). */

import { tplLabel } from './mock-data.js';

const DOC_SLOTS = [
  'Passport bio page',
  'Current visa',
  'Proof of legal stay',
  'Employment letter',
  'Medical certificate',
  'Photo / biometric',
];

/** @returns {{ people: Array<{name, slots, ready, total}>, readyTotal, totalSlots, selectedIds: Set<string> }} */
export function buildDocumentCopiesModel(caseRow) {
  const people = (caseRow.people || []).map((name, pIdx) => {
    const missing = missingForPerson(pIdx);
    const slots = DOC_SLOTS.map((label, sIdx) => {
      const id = `doc-${pIdx}-${sIdx}`;
      const ready = !missing.has(sIdx);
      return { id, label, ready, num: sIdx + 1 };
    });
    const ready = slots.filter(s => s.ready).length;
    return { name, slots, ready, total: slots.length };
  });

  let readyTotal = 0;
  let totalSlots = 0;
  for (const p of people) {
    readyTotal += p.ready;
    totalSlots += p.total;
  }

  const selectedIds = new Set();
  let picked = 0;
  for (const p of people) {
    for (const s of p.slots) {
      if (s.ready && picked < 4) {
        selectedIds.add(s.id);
        picked += 1;
      }
    }
  }

  return { people, readyTotal, totalSlots, selectedIds, pct: totalSlots ? Math.round((readyTotal / totalSlots) * 100) : 0 };
}

function missingForPerson(pIdx) {
  if (pIdx === 0) return new Set();
  if (pIdx === 1) return new Set([1, 4]);
  return new Set([2, 4]);
}

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function statusBadge(ready) {
  return ready
    ? '<span class="dc-status dc-status--ready">Ready</span>'
    : '<span class="dc-status dc-status--missing">Missing</span>';
}

function personBlock(person, pIdx, selectedIds) {
  const rows = person.slots.map(s => {
    const checked = selectedIds.has(s.id) && s.ready;
    const disabled = !s.ready ? 'disabled' : '';
    return `<tr class="dc-row${s.ready ? '' : ' is-muted'}">
      <td class="dc-row__num">${s.num}.</td>
      <td>${esc(s.label)}</td>
      <td>${statusBadge(s.ready)}</td>
      <td class="dc-row__check"><input type="checkbox" class="form-check-input dc-slot-check" data-doc-id="${s.id}" ${checked ? 'checked' : ''} ${disabled} /></td>
    </tr>`;
  }).join('');

  return `<details class="dc-person" open>
    <summary class="dc-person__head">
      <span class="dc-person__left"><i class="bi bi-person-circle"></i>
        <strong>${esc(person.name)}</strong>
        <span class="dc-person__count">${person.ready} of ${person.total} ready</span>
      </span>
      <i class="bi bi-chevron-down dc-person__chev"></i>
    </summary>
    <table class="dc-table"><tbody>${rows}</tbody></table>
  </details>`;
}

function previewIncludes(people) {
  return people.map(p => `<li>${esc(p.name)} — ${p.ready} documents</li>`).join('');
}

export function renderCaseDocumentCopies(caseRow, model) {
  const m = model || buildDocumentCopiesModel(caseRow);
  const sel = m.selectedIds.size;
  const fileName = `Application_${caseRow.number.replace(/-/g, '_')}_Document_Package.pdf`;

  return `<div class="dc-page">
    <header class="dc-page__head">
      <h2>Document copies</h2>
      <p>Scan and manage required document copies for all people in this application.</p>
    </header>
    <div class="dc-summary">
      <div class="dc-summary__stat">
        <span class="dc-summary__label">Readiness summary</span>
        <div class="dc-summary__row">
          <strong>${m.readyTotal} of ${m.totalSlots} ready</strong>
          <span class="dc-summary__pct">${m.pct}%</span>
        </div>
        <div class="dc-progress" role="progressbar" aria-valuenow="${m.pct}" aria-valuemin="0" aria-valuemax="100">
          <div class="dc-progress__bar" style="width:${m.pct}%"></div>
        </div>
      </div>
      <div class="dc-summary__actions">
        <button type="button" class="dc-btn dc-btn--outline"><i class="bi bi-eye"></i> Preview selected</button>
        <button type="button" class="dc-btn dc-btn--outline"><i class="bi bi-download"></i> Download package</button>
        <button type="button" class="dc-btn dc-btn--primary" id="dc-enqueue"><i class="bi bi-file-earmark-plus"></i> Enqueue PDF generation</button>
      </div>
    </div>
    <div class="dc-split">
      <div class="dc-list">
        <div class="dc-list__head"><span>Document slot</span><span>Readiness status</span><span>Preview</span></div>
        ${m.people.map((p, i) => personBlock(p, i, m.selectedIds)).join('')}
      </div>
      <aside class="dc-preview">
        <h3 class="dc-preview__title">Preview (<span id="dc-selected-count">${sel}</span> selected) <i class="bi bi-info-circle" title="Package preview"></i></h3>
        <div class="dc-preview__frame">
          <div class="dc-preview__doc">
            <div class="dc-preview__doc-head">MINISTRY OF INTERNAL AFFAIRS</div>
            <h4>APPLICATION ${esc(caseRow.number)}</h4>
            <p class="dc-preview__tpl">${esc(tplLabel(caseRow.tplKey))}</p>
            <p class="dc-preview__gen">Document package preview · Generated on 10 Aug 2026</p>
            <p class="dc-preview__includes"><strong>Includes:</strong></p>
            <ul>${previewIncludes(m.people)}</ul>
            <i class="bi bi-file-earmark-pdf dc-preview__pdf-icon"></i>
          </div>
        </div>
        <div class="dc-preview__controls">
          <button type="button" class="dc-icon-btn" disabled aria-label="Previous page"><i class="bi bi-chevron-left"></i></button>
          <span>1 / 1</span>
          <button type="button" class="dc-icon-btn" disabled aria-label="Next page"><i class="bi bi-chevron-right"></i></button>
          <span class="dc-preview__zoom">
            <button type="button" class="dc-icon-btn" aria-label="Zoom out"><i class="bi bi-dash-lg"></i></button>
            <i class="bi bi-zoom-in"></i>
            <button type="button" class="dc-icon-btn" aria-label="Zoom in"><i class="bi bi-plus-lg"></i></button>
          </span>
        </div>
        <dl class="dc-preview__meta">
          <div><dt>File name</dt><dd>${esc(fileName)}</dd></div>
          <div><dt>Page size</dt><dd>A4</dd></div>
          <div><dt>Estimated pages</dt><dd>1</dd></div>
          <div><dt>Estimated size</dt><dd>1.2 MB</dd></div>
        </dl>
        <div class="dc-preview__note"><i class="bi bi-lock-fill"></i>
          This preview is generated from the selected documents. The final PDF will be generated when you enqueue.</div>
      </aside>
    </div>
  </div>`;
}

export function countSelectedDocChecks(root) {
  return root?.querySelectorAll('.dc-slot-check:checked:not(:disabled)').length ?? 0;
}
