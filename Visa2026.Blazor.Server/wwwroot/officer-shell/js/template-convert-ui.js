/** Template AI convert modal — PNG parity 01–05 (slice E7a). Host is a modal, never the preview slot. */

import {
  CONVERT_INSTANCES, CONVERT_FILES, CONVERTING_STEPS,
  getConvertState, getConvertFile, getConvertInstance, getHighlights, getSummary, getGaps,
  getErrorTokens, canAnalyze, canApprove, canConvert, canAddManual, isConvertAiEnabled,
} from './template-convert-data.js';

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

const STAGE_LABELS = [
  { key: 'upload', label: 'Upload' },
  { key: 'candidate', label: 'Candidate check' },
  { key: 'converting', label: 'Converting' },
  { key: 'preview', label: 'Preview' },
];

function renderStepper(stage) {
  const activeIndex = STAGE_LABELS.findIndex(s => s.key === stage);
  const index = stage === 'done' ? STAGE_LABELS.length : activeIndex;
  return `<ol class="tac-stepper">${STAGE_LABELS.map((s, i) => {
    const cls = i < index ? 'is-done' : i === index ? 'is-active' : '';
    const mark = i < index ? '<i class="bi bi-check-lg"></i>' : String(i + 1);
    return `<li class="tac-stepper__item ${cls}"><span class="tac-stepper__dot">${mark}</span>
      <span class="tac-stepper__label">${esc(s.label)}</span></li>`;
  }).join('')}</ol>`;
}

/** Walks the paragraph once, emitting escaped plain text between spans — offsets index the raw text. */
function markParagraph(text, spans, mode) {
  const ordered = spans
    .filter(h => h.region.start >= 0 && h.region.start + h.region.length <= text.length)
    .sort((a, b) => a.region.start - b.region.start);
  if (!ordered.length) return esc(text);

  let out = '';
  let cursor = 0;
  for (const h of ordered) {
    const { start, length } = h.region;
    if (start < cursor) continue;
    const body = mode === 'tokens' && h.token ? h.token : text.slice(start, start + length);
    const cls = h.kind === 'Gap' ? 'tac-hl tac-hl--gap' : 'tac-hl tac-hl--match';
    const title = h.kind === 'Gap' ? 'Unmatched phrase — review needed' : `${h.shortCode} · ${h.token}`;
    out += `${esc(text.slice(cursor, start))}<mark class="${cls}" title="${esc(title)}">${esc(body)}</mark>`;
    cursor = start + length;
  }
  return out + esc(text.slice(cursor));
}

/** Marks tokens the validator rejected, so the error rail and the document agree (PNG 10). */
function markErrorTokens(html, errorTokens) {
  let out = html;
  for (const token of errorTokens) {
    out = out.split(esc(token)).join(`<span class="tac-token-error">${esc(token)}<i class="bi bi-exclamation-circle-fill"></i></span>`);
  }
  return out;
}

function renderWordDoc(file, highlights, mode) {
  const errorTokens = mode === 'errors' ? getErrorTokens() : new Set();
  const body = file.paragraphs.map(p => {
    const spans = highlights.filter(h => h.region.kind === 'WordSpan' && h.region.paragraphAddress === p.address);
    const text = markParagraph(p.text, spans, mode);
    return `<p class="tac-doc__p ${p.cls ?? ''}">${errorTokens.size ? markErrorTokens(text, errorTokens) : text}</p>`;
  }).join('');
  return `<div class="tac-doc">${body}</div>`;
}

function renderExcelDoc(file, highlights, mode) {
  const sheet = file.sheet;
  const head = `<tr><th></th>${sheet.columns.map(c => `<th>${esc(c)}</th>`).join('')}</tr>`;
  const headerRow = `<tr><td class="tac-sheet__rownum">4</td>${sheet.columns
    .map(c => `<td class="tac-sheet__header">${esc(sheet.headers[c] ?? '')}</td>`).join('')}</tr>`;
  const rows = sheet.rows.map(r => `<tr><td class="tac-sheet__rownum">${r.row}</td>${sheet.columns.map(c => {
    const ref = `${c}${r.row}`;
    const hit = highlights.find(h => h.region.kind === 'ExcelCell' && h.region.cellReference === ref);
    const value = hit && mode === 'tokens' && hit.token ? hit.token : (r.cells[c] ?? '');
    const cls = hit ? (hit.kind === 'Gap' ? ' class="tac-cell--gap"' : ' class="tac-cell--match"') : '';
    const title = hit ? ` title="${esc(`${hit.shortCode} · ${hit.token}`)}"` : '';
    return `<td${cls}${title}>${esc(value)}</td>`;
  }).join('')}</tr>`).join('');
  return `<div class="tac-sheet"><div class="tac-sheet__tab">${esc(sheet.name)}</div>
    <table class="tac-sheet__grid"><thead>${head}</thead><tbody>${headerRow}${rows}</tbody></table></div>`;
}

function renderDocument(file, highlights, mode = 'values') {
  if (!file) return '';
  return file.format === 'Xlsx' ? renderExcelDoc(file, highlights, mode) : renderWordDoc(file, highlights, mode);
}

function renderLegend() {
  return `<div class="tac-legend">
    <span><i class="tac-swatch tac-swatch--match"></i> Matched library placeholder (used in conversion)</span>
    <span><i class="tac-swatch tac-swatch--gap"></i> Gap / unmatched phrase (review needed)</span>
  </div>`;
}

/**
 * V13 — L12 manual add. Same form minus everything the AI flow needs: no context instance (nothing is
 * matched against a case) and no analysis, because the file already carries its placeholders.
 */
function renderManual(state) {
  const picked = getConvertFile();
  const dropzone = picked
    ? `<div class="tac-drop is-picked"><i class="bi ${picked.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i>
        <strong>${esc(picked.fileName)}</strong><span>${esc(picked.sizeLabel)}</span>
        <button type="button" class="tac-link" data-tac-file="">Choose a different file</button></div>`
    : `<div class="tac-drop">
        <div class="tac-drop__icons"><i class="bi bi-cloud-arrow-up"></i></div>
        <strong>Drag and drop a prepared template</strong>
        <span>A .docx or .xlsx that already contains placeholders, e.g. <code>{{ds.PFN}}</code></span>
        <div class="tac-drop__samples">
          ${CONVERT_FILES.filter(f => f.id === 'draft' || f.id === 'roster').map(f => `<button type="button" class="tac-btn tac-btn--ghost" data-tac-file="${f.id}">
            <i class="bi ${f.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i> ${esc(f.fileName)}</button>`).join('')}
        </div>
        <small>Accepted file types: .docx, .xlsx</small></div>`;

  return `${state.aiEnabled ? '' : `<div class="tac-banner tac-banner--info"><i class="bi bi-info-circle-fill"></i>
      <div><strong>AI conversion is not enabled for this environment.</strong>
      <span>Upload a manually prepared template, or use desktop staging to author one.</span></div></div>`}
  <div class="tac-form">
    <label class="tac-field"><span class="tac-field__label">Template name <em>*</em></span>
      <input class="tac-input" data-tac-field="templateName" placeholder="Enter template name" value="${esc(state.templateName)}" />
      <small>A clear, descriptive name for this template.</small></label>

    <div class="tac-field"><span class="tac-field__label">Catalog target <em>*</em></span>
      <label class="tac-radio"><input type="radio" name="tac-target" data-tac-field="catalogTarget" value="profile" ${state.catalogTarget === 'profile' ? 'checked' : ''} /> Profile-specific (default)</label>
      <label class="tac-radio"><input type="radio" name="tac-target" data-tac-field="catalogTarget" value="shared" ${state.catalogTarget === 'shared' ? 'checked' : ''} /> Shared</label>
      <small>${state.catalogTarget === 'shared'
        ? 'Available to other profiles via Include.'
        : 'Profile-specific templates are only available on this Application Profile.'}</small></div>

    <div class="tac-field"><span class="tac-field__label">Upload document <em>*</em></span>${dropzone}</div>

    <p class="tac-note"><i class="bi bi-info-circle"></i> The file is validated against this profile's placeholder set on save. No content or formatting is changed.</p>
  </div>`;
}

function renderUpload(state) {
  const instance = getConvertInstance();
  const contextField = state.source === 'instance'
    ? `<input class="tac-input" value="${esc(`${instance.id} · ${instance.label.split('·')[1]?.trim() ?? ''}`)}" readonly />
       <small>The context instance for which this template is being created.</small>`
    : `<select class="tac-input" data-tac-field="instanceId">
         ${CONVERT_INSTANCES.map(i => `<option value="${i.id}" ${i.id === state.instanceId ? 'selected' : ''}>${esc(i.label)}</option>`).join('')}
       </select>
       <small>Mapping uses only this case data. Convert is disabled until a case is chosen.</small>`;

  const picked = getConvertFile();
  const dropzone = picked
    ? `<div class="tac-drop is-picked"><i class="bi ${picked.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i>
        <strong>${esc(picked.fileName)}</strong><span>${esc(picked.sizeLabel)}</span>
        <button type="button" class="tac-link" data-tac-file="">Choose a different file</button></div>`
    : `<div class="tac-drop">
        <div class="tac-drop__icons"><i class="bi bi-file-earmark-word"></i><i class="bi bi-file-earmark-excel"></i></div>
        <strong>Drag and drop your file here</strong><span>or pick a sample below</span>
        <div class="tac-drop__samples">
          ${CONVERT_FILES.map(f => `<button type="button" class="tac-btn tac-btn--ghost" data-tac-file="${f.id}">
            <i class="bi ${f.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i> ${esc(f.fileName)}</button>`).join('')}
        </div>
        <small>Accepted file types: .docx, .xlsx</small></div>`;

  return `<div class="tac-form">
    <label class="tac-field"><span class="tac-field__label">Template name <em>*</em></span>
      <input class="tac-input" data-tac-field="templateName" placeholder="Enter template name" value="${esc(state.templateName)}" />
      <small>A clear, descriptive name for this template.</small></label>

    <div class="tac-field"><span class="tac-field__label">Catalog target <em>*</em></span>
      <label class="tac-radio"><input type="radio" name="tac-target" data-tac-field="catalogTarget" value="profile" ${state.catalogTarget === 'profile' ? 'checked' : ''} /> Profile-specific (default)</label>
      <label class="tac-radio"><input type="radio" name="tac-target" data-tac-field="catalogTarget" value="shared" ${state.catalogTarget === 'shared' ? 'checked' : ''} /> Shared</label>
      <small>${state.catalogTarget === 'shared'
        ? 'Available to other profiles via Include.'
        : 'Profile-specific templates are only available on this Application Profile.'}</small></div>

    <label class="tac-field"><span class="tac-field__label">Data scope <em>*</em></span>
      <select class="tac-input" data-tac-field="dataScope">
        <option value="ApplicationHeader" ${state.dataScope === 'ApplicationHeader' ? 'selected' : ''}>Header / case</option>
        <option value="PeopleM2M" ${state.dataScope === 'PeopleM2M' ? 'selected' : ''}>People roster</option>
        <option value="Both" ${state.dataScope === 'Both' ? 'selected' : ''}>Both</option>
      </select>
      <small>Decides which placeholders this template may use.</small></label>

    <div class="tac-field"><span class="tac-field__label">Context instance ${state.source === 'catalog' ? '<em>*</em>' : ''}</span>
      ${contextField}</div>

    <div class="tac-field"><span class="tac-field__label">Upload document <em>*</em></span>${dropzone}</div>
  </div>`;
}

/** V11 — the parent profile is locked: preview everything, save nothing (spec §3 Config lock). */
function renderLockBanner(state) {
  if (!state.configLocked) return '';
  return `<div class="tac-banner tac-banner--lock"><i class="bi bi-lock-fill"></i>
    <div><strong>Profile templates are locked — Approve is disabled.</strong>
    <span>You can still upload, convert, and preview this conversion.</span></div></div>`;
}

const SUITABILITY = {
  Pass: { icon: 'bi-check-circle-fill', heading: 'Criteria', bullet: 'bi-check-circle' },
  Warn: { icon: 'bi-exclamation-triangle-fill', heading: 'Soft warnings', bullet: 'bi-dot' },
  Fail: { icon: 'bi-x-circle-fill', heading: 'Fail reasons', bullet: 'bi-x-circle' },
};

function renderCandidate(state) {
  const file = getConvertFile();
  const summary = getSummary();
  const level = summary.suitability;
  const look = SUITABILITY[level] ?? SUITABILITY.Pass;
  const hasHighlights = file.candidate.highlights.length > 0;

  const ack = level === 'Warn'
    ? `<label class="tac-ack"><input type="checkbox" id="tac-ack-candidate" ${state.acknowledgedCandidate ? 'checked' : ''} />
        <span><strong>Continue with warnings</strong><br />I have reviewed the warnings and want to continue.</span></label>`
    : '';

  return `${renderLockBanner(state)}
  <div class="tac-split">
    <div class="tac-split__doc">
      <div class="tac-filebar"><i class="bi ${file.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i>
        <div><strong>${esc(file.fileName)}</strong><span>Uploaded just now · ${esc(file.sizeLabel)}</span></div></div>
      <div class="tac-viewport">${renderDocument(file, getHighlights(), 'values')}</div>
      ${hasHighlights ? renderLegend() : '<p class="tac-legend"><i class="bi bi-info-circle"></i> No matched placeholders found.</p>'}
      ${ack}
    </div>
    <aside class="tac-rail">
      <h4>Suitability</h4>
      <div class="tac-suit tac-suit--${level.toLowerCase()}"><i class="bi ${look.icon}"></i> ${esc(level)}</div>
      ${summary.matched || summary.gaps ? `<h4>Summary</h4>
      <div class="tac-chips">
        <span class="tac-chip tac-chip--ok">${summary.matched} ${file.format === 'Xlsx' ? 'cell' : 'field'}${summary.matched === 1 ? '' : 's'} matched</span>
        ${summary.rosterRows ? `<span class="tac-chip tac-chip--info">${summary.rosterRows} roster rows</span>` : ''}
        ${summary.gaps ? `<span class="tac-chip tac-chip--warn">${summary.gaps} gap${summary.gaps === 1 ? '' : 's'}</span>` : ''}
      </div>` : ''}
      <h4>${esc(look.heading)}</h4>
      <ul class="tac-criteria tac-criteria--${level.toLowerCase()}">${file.candidate.reasons.map(r => `<li><i class="bi ${look.bullet}"></i>
        <div><strong>${esc(r.code)}</strong><span>${esc(r.message)}</span></div></li>`).join('')}</ul>
      ${level === 'Fail'
        ? '<p class="tac-note tac-note--fail"><i class="bi bi-x-octagon"></i> This document cannot be converted into a template for this profile.</p>'
        : '<p class="tac-note"><i class="bi bi-info-circle"></i> Highlights show library placeholders for this Application Profile only — you do not map tokens manually.</p>'}
    </aside>
  </div>`;
}

function renderConverting(state) {
  const file = getConvertFile();
  const instance = getConvertInstance();
  return `<div class="tac-converting">
    <div class="tac-converting__meta">
      <div><span>Instance</span><strong>${esc(instance.id)}</strong></div>
      <div><span>File</span><strong>${esc(file.fileName)}</strong></div>
      <div><span>Target</span><strong>${state.catalogTarget === 'shared' ? 'Shared' : 'Profile-specific'}</strong></div>
    </div>
    <div class="tac-progress"><div class="tac-progress__bar" style="width:${state.progress}%"></div></div>
    <div class="tac-progress__pct">${state.progress}%</div>
    <ol class="tac-steps">${CONVERTING_STEPS.map((s, i) => {
      const cls = i < state.stepIndex ? 'is-done' : i === state.stepIndex ? 'is-active' : '';
      return `<li class="${cls}"><span class="tac-steps__dot">${i < state.stepIndex ? '<i class="bi bi-check-lg"></i>' : i + 1}</span>
        <span class="tac-steps__label">${esc(s.label)}</span></li>`;
    }).join('')}</ol>
    <p class="tac-converting__copy">Please wait while we convert your document into a reusable template.<br />Mapping uses only this case data.</p>
  </div>`;
}

function renderChat(state) {
  const bubbles = state.chat.length
    ? state.chat.map(m => `<div class="tac-msg tac-msg--${m.role}${m.refused ? ' is-refused' : ''}">
        ${m.role === 'assistant' ? '<span class="tac-msg__avatar"><i class="bi bi-robot"></i></span>' : ''}
        <div class="tac-msg__body">${esc(m.text)}<time>${esc(m.at)}</time></div></div>`).join('')
    : `<p class="tac-chat__empty">Ask to change which fields become placeholders. I cannot change layout or wording.</p>`;

  return `<aside class="tac-chat">
    <h4>Adjust mapping</h4>
    <div class="tac-chat__log">${bubbles}</div>
    <div class="tac-chat__compose">
      <input class="tac-input" id="tac-chat-input" placeholder="Ask me to adjust mapping…" />
      <button type="button" class="tac-btn tac-btn--primary" id="tac-chat-send">Send</button>
    </div>
    <small>Examples: Use passport number for the ID field · Map company name differently</small>
  </aside>`;
}

/** V10 — replaces the chat when Validate hard-fails: no wording change can fix a broken token (spec §6.2). */
function renderValidationRail(file) {
  const order = { Error: 0, Warning: 1 };
  const issues = [...file.validation.issues].sort((a, b) => order[a.severity] - order[b.severity]);
  return `<aside class="tac-rail tac-rail--errors">
    <h4>${file.validation.hasHardFailure ? 'Validation errors' : 'Validation warnings'}</h4>
    <ul class="tac-issues">${issues.map(i => `<li class="tac-issue tac-issue--${i.severity.toLowerCase()}">
      <i class="bi ${i.severity === 'Error' ? 'bi-exclamation-circle-fill' : 'bi-exclamation-triangle-fill'}"></i>
      <div><span>${esc(i.message)}</span><code>${esc(i.code)}</code></div></li>`).join('')}</ul>
    <p class="tac-note"><i class="bi bi-info-circle"></i> Mapping chat is unavailable while tokens are broken — convert again or send a gap packet.</p>
  </aside>`;
}

function renderPreview(state) {
  const file = getConvertFile();
  const instance = getConvertInstance();
  const highlights = getHighlights();
  const failed = file.validation.hasHardFailure;
  const fillFailed = state.fillPreviewFailed;
  const tabs = [
    { key: 'filled', label: fillFailed ? 'Filled preview (error)' : 'Filled preview', bad: fillFailed },
    { key: 'tokens', label: 'Placeholders' },
    { key: 'list', label: 'Highlights' },
  ];

  const listBody = `<ul class="tac-hl-list">${highlights.map(h => `<li>
    <span class="tac-hl-list__badge ${h.kind === 'Gap' ? 'is-gap' : 'is-match'}">${h.kind === 'Gap' ? 'Gap' : esc(h.shortCode)}</span>
    <span class="tac-hl-list__text">${esc(h.matchedText)}</span>
    <code>${esc(h.token ?? '—')}</code>
    <span class="tac-hl-list__where">${h.region.kind === 'WordSpan' ? esc(h.region.paragraphAddress) : esc(`${h.region.sheetName}!${h.region.cellReference}`)}</span>
  </li>`).join('')}</ul>`;

  // V12: the merge failed, so there are no values to show — fall back to the master with tokens.
  const showTokens = state.previewTab === 'tokens' || (state.previewTab === 'filled' && fillFailed);
  const tokenMode = showTokens ? (failed ? 'errors' : 'tokens') : 'values';
  const body = state.previewTab === 'list'
    ? listBody
    : renderDocument(file, highlights, tokenMode);

  const fillNotice = fillFailed && state.previewTab !== 'list'
    ? `<div class="tac-inline-warn"><i class="bi bi-exclamation-triangle-fill"></i>
        <div><strong>Could not fill preview from this instance — showing the master with placeholders.</strong>
        <span>Approve is still allowed because Validate passed.</span></div></div>`
    : '';

  // A hard failure blocks Approve outright, so the acknowledge checkbox would be a dead control.
  const warnings = file.validation.hasWarnings && !failed
    ? `<label class="tac-ack"><input type="checkbox" id="tac-ack" ${state.acknowledgedWarnings ? 'checked' : ''} />
        I understand the warnings and want to use this template.</label>`
    : '';

  const banner = failed
    ? `<div class="tac-banner tac-banner--fail"><i class="bi bi-exclamation-octagon-fill"></i>
        <div><strong>We could not finish this template automatically.</strong>
        <span>Unknown or broken placeholder tokens after convert.</span></div></div>`
    : renderLockBanner(state);

  return `${banner}<div class="tac-split tac-split--preview">
    <div class="tac-split__doc">
      <div class="tac-filebar"><i class="bi ${file.format === 'Xlsx' ? 'bi-file-earmark-excel' : 'bi-file-earmark-word'}"></i>
        <div><strong>${esc(state.templateName)}</strong>
          <span>${esc(instance.profile)} · Instance ${esc(instance.id)}</span></div>
        <span class="tac-pill">${state.catalogTarget === 'shared' ? 'Shared' : 'Profile-specific'}</span></div>
      <div class="tac-tabs">${tabs.map(t => `<button type="button" class="tac-tab ${state.previewTab === t.key ? 'is-active' : ''}${t.bad ? ' is-bad' : ''}"
        data-tac-preview-tab="${t.key}">${esc(t.label)}${t.bad ? ' <i class="bi bi-exclamation-circle-fill"></i>' : ''}</button>`).join('')}</div>
      ${fillNotice}
      <div class="tac-viewport">${body}</div>
      ${warnings}
    </div>
    ${failed ? renderValidationRail(file) : renderChat(state)}
  </div>`;
}

function renderDone(state) {
  const t = state.savedTemplate;
  if (t.manual) {
    return renderDoneRows(t, [
      ['Template name', t.name],
      ['Format', t.format],
      ['Catalog', t.catalog],
      ['Parent Application Profile', t.profile],
      ['Source', 'Manually prepared — no AI conversion'],
    ], 'Template added', 'Uploaded as-is and validated against this profile.<br />No content or formatting was changed.');
  }
  return renderDoneRows(t, [
    ['Template name', t.name],
    ['Format', t.format],
    ['Catalog', t.catalog],
    ['Parent Application Profile', t.profile],
    ['Context instance used for mapping', t.instanceId],
  ], 'Template saved', 'Converted and saved to this Application Profile catalog.<br />Ready to use for new profile instances.');
}

function renderDoneRows(t, rows, heading, blurb) {
  return `<div class="tac-done">
    <div class="tac-done__main">
      <div class="tac-done__icon"><i class="bi bi-check-circle"></i></div>
      <h3>${esc(heading)}</h3>
      <p>${blurb}</p>
      <table class="tac-done__table"><tbody>
        ${rows.map(([k, v]) => `<tr><th>${esc(k)}</th><td>${esc(v)}</td></tr>`).join('')}
        <tr><th>Readiness</th><td><span class="tac-chip tac-chip--ok">${esc(t.readiness)}</span></td></tr>
      </tbody></table>
      <h4>What's next?</h4>
      <div class="tac-done__actions">
        <button type="button" class="tac-btn tac-btn--primary" id="tac-open-catalog">Open in catalog</button>
        <button type="button" class="tac-btn tac-btn--ghost" id="tac-staging">Edit with desktop staging</button>
        <button type="button" class="tac-btn tac-btn--ghost" id="tac-convert-another">${t.manual ? 'Add another' : 'Convert another'}</button>
      </div>
    </div>
    <aside class="tac-done__note"><i class="bi bi-info-circle"></i>
      <h4>Manual add stays available</h4>
      <p>Officers can always upload a manually prepared .docx or .xlsx that already contains placeholders, even when AI conversion is turned off.</p>
    </aside>
  </div>`;
}

/** V6 — the developer handoff described in spec §6.3. */
function renderHelp(state) {
  const gaps = getGaps();
  const file = getConvertFile();
  const rows = gaps.length
    ? `<ul class="tac-hl-list">${gaps.map(h => `<li>
        <span class="tac-hl-list__badge is-gap">Gap</span>
        <span class="tac-hl-list__text">${esc(h.matchedText)}</span>
        <code>no placeholder</code>
        <span class="tac-hl-list__where">${h.region.kind === 'WordSpan' ? esc(h.region.paragraphAddress) : esc(`${h.region.sheetName}!${h.region.cellReference}`)}</span>
      </li>`).join('')}</ul>`
    : '<p class="tac-chat__empty">Nothing is unmatched in this document — every highlighted value resolved to a placeholder.</p>';

  return `<div class="tac-help">
    <h3>What could not be mapped</h3>
    <p>These spans stay as literal text. Send them to a developer if this Application Profile needs a placeholder for them.</p>
    ${rows}
    <div class="tac-help__meta">
      <div><span>File</span><strong>${esc(file.fileName)}</strong></div>
      <div><span>Profile</span><strong>${esc(getConvertInstance().profile)}</strong></div>
      <div><span>Instance</span><strong>${esc(state.instanceId)}</strong></div>
    </div>
  </div>`;
}

/** V7 — a layer, so the view underneath stays on screen and Cancel is genuinely a no-op. */
function renderConfirm(confirm) {
  if (!confirm) return '';
  return `<div class="tac-confirm" role="alertdialog" aria-modal="true">
    <div class="tac-confirm__box">
      <h3>${esc(confirm.title)}</h3>
      ${confirm.lines.length ? `<ul>${confirm.lines.map(l => `<li>${esc(l)}</li>`).join('')}</ul>` : ''}
      <div class="tac-confirm__actions">
        <button type="button" class="tac-btn tac-btn--ghost" data-tac-confirm="cancel">${esc(confirm.cancelLabel)}</button>
        <button type="button" class="tac-btn tac-btn--primary" data-tac-confirm="ok">${esc(confirm.okLabel)}</button>
      </div>
    </div>
  </div>`;
}

function renderFooter(state) {
  const cancel = '<button type="button" class="tac-btn tac-btn--ghost" data-tac-close>Cancel</button>';
  if (state.mode === 'manual' && state.stage === 'upload') {
    return `<div class="tac-foot"><span class="tac-foot__hint"><i class="bi bi-info-circle"></i>
      Placeholders are validated on save — nothing in the file is rewritten</span>
      ${cancel}<button type="button" class="tac-btn tac-btn--primary" id="tac-add-manual" ${canAddManual() ? '' : 'disabled'}>Add template</button></div>`;
  }
  if (state.stage === 'upload') {
    return `<div class="tac-foot"><span class="tac-foot__hint"><i class="bi bi-info-circle"></i>
      AI Convert optional — you can still <a href="#" class="tac-link">add a prepared template</a></span>
      ${cancel}<button type="button" class="tac-btn tac-btn--primary" id="tac-analyze" ${canAnalyze() ? '' : 'disabled'}>Analyze</button></div>`;
  }
  if (state.stage === 'candidate') {
    const level = getConvertFile().candidate.suitability;
    const hint = canConvert() ? ''
      : level === 'Fail'
        ? '<span class="tac-foot__hint"><i class="bi bi-x-octagon"></i> Conversion is disabled for failed checks</span>'
        : '<span class="tac-foot__hint"><i class="bi bi-info-circle"></i> Convert stays disabled until you confirm above</span>';
    return `<div class="tac-foot">${hint}<span class="tac-foot__grow"></span>
      <button type="button" class="tac-btn tac-btn--ghost" id="tac-needs-help">Needs help</button>
      <button type="button" class="tac-btn tac-btn--ghost" id="tac-try-another">Try another file</button>
      ${cancel}<button type="button" class="tac-btn tac-btn--primary" id="tac-convert" ${canConvert() ? '' : 'disabled'}>Convert</button></div>`;
  }
  if (state.stage === 'converting') {
    return `<div class="tac-foot"><span class="tac-foot__grow"></span>
      <button type="button" class="tac-btn tac-btn--ghost" id="tac-abort">Cancel</button></div>`;
  }
  if (state.stage === 'help') {
    return `<div class="tac-foot"><span class="tac-foot__grow"></span>
      <button type="button" class="tac-btn tac-btn--ghost" id="tac-help-back">Back</button>
      <button type="button" class="tac-btn tac-btn--primary" id="tac-help-download">Download gap packet</button></div>`;
  }
  if (state.stage === 'preview') {
    const file = getConvertFile();
    const locked = state.configLocked;
    const hint = locked
      ? '<span class="tac-foot__hint"><i class="bi bi-lock"></i> Profile templates are locked by configuration policy</span>'
      : file.validation.hasHardFailure
        ? '<span class="tac-foot__hint"><i class="bi bi-exclamation-octagon"></i> Fix the validation errors, or send a gap packet</span>'
        : '';
    const help = file.validation.hasHardFailure
      ? '<button type="button" class="tac-btn tac-btn--ghost" id="tac-needs-help">Needs help</button>' : '';
    return `<div class="tac-foot">${hint}<span class="tac-foot__grow"></span>
      <button type="button" class="tac-btn tac-btn--ghost" id="tac-convert-again">Convert again</button>
      ${help}${cancel}<button type="button" class="tac-btn tac-btn--primary" id="tac-approve" ${canApprove() ? '' : 'disabled'}>
        ${locked ? '<i class="bi bi-lock-fill"></i> ' : ''}Approve — save to profile</button></div>`;
  }
  return `<div class="tac-foot"><span class="tac-foot__grow"></span>
    <button type="button" class="tac-btn tac-btn--primary" data-tac-close>Close</button></div>`;
}

export function renderConvertModal() {
  const state = getConvertState();
  if (!state.open) return '';

  const bodies = {
    upload: state.mode === 'manual' ? renderManual : renderUpload,
    candidate: renderCandidate,
    converting: renderConverting,
    preview: renderPreview,
    done: renderDone,
    help: renderHelp,
  };
  const manual = state.mode === 'manual';
  const wide = ['candidate', 'preview', 'done', 'help'].includes(state.stage);
  // The manual path has no stages to step through — it is one form and a save.
  const offFlow = state.stage === 'done' || state.stage === 'help' || manual;

  return `<div class="tac-backdrop" data-tac-backdrop>
    <section class="tac-modal${wide ? ' tac-modal--wide' : ''}" role="dialog" aria-modal="true" aria-label="Convert existing document">
      <header class="tac-modal__head">
        <div><h2>${manual ? 'Add prepared template' : 'Convert existing document'}
          ${state.configLocked ? '<span class="tac-lock-badge"><i class="bi bi-lock-fill"></i> Config locked</span>' : ''}</h2>
          <p>${manual
            ? 'Upload a .docx or .xlsx that already contains placeholders.'
            : state.source === 'instance' ? 'Mapping uses only this case data.' : 'Choose a case, then upload a completed letter or spreadsheet.'}</p></div>
        <button type="button" class="tac-icon-btn" data-tac-close aria-label="Close"><i class="bi bi-x-lg"></i></button>
      </header>
      ${offFlow ? '' : renderStepper(state.stage)}
      <div class="tac-modal__body">${bodies[state.stage](state)}</div>
      ${renderFooter(state)}
      ${renderConfirm(state.confirm)}
    </section>
  </div>`;
}

/**
 * Entry buttons shared by the templates catalog and the case workspace. When the AI provider is off
 * (spec §7) Convert stays visible but disabled — hiding it would leave officers wondering where the
 * feature went — and the manual path (L12) becomes the primary action.
 */
export function renderConvertEntryButton({ source, compact = false } = {}) {
  const aiOff = !isConvertAiEnabled();
  const manual = `<button type="button" class="tac-btn ${aiOff && !compact ? 'tac-btn--primary' : 'tac-btn--ghost'}" data-tac-open="${esc(source)}" data-tac-mode="manual">
    <i class="bi bi-cloud-arrow-up"></i> Add prepared template</button>`;
  const convert = aiOff
    ? `<span class="tac-entry-off" title="AI conversion is not enabled for this environment">
        <button type="button" class="tac-btn tac-btn--ghost" disabled><i class="bi bi-magic"></i> Convert existing document</button>
        <span class="tac-ai-badge">AI off</span></span>`
    : `<button type="button" class="tac-btn ${compact ? 'tac-btn--ghost' : 'tac-btn--primary'}" data-tac-open="${esc(source)}" data-tac-mode="convert">
        <i class="bi bi-magic"></i> Convert existing document</button>`;
  return `<span class="tac-entry-actions">${manual}${convert}</span>`;
}

/** L13 switch — a per-user preference, so it lives in the topbar rather than on any one case. */
export function renderConvertEditorSwitch(enabled) {
  return `<label class="tac-switch" title="Show template conversion entry points inside cases">
    <input type="checkbox" id="tac-editor-switch" ${enabled ? 'checked' : ''} />
    <span class="tac-switch__track"><span class="tac-switch__thumb"></span></span>
    <span class="tac-switch__label">Template convert editor</span></label>`;
}
