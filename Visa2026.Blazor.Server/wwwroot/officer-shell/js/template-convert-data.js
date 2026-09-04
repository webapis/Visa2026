/**
 * Template AI convert — mock store (slice E7a).
 *
 * Shapes mirror the shipped Module DTOs field-for-field so the Blazor lift (E7b) is a fetch swap:
 *   candidate  -> Services/TemplateConvert/TemplateCandidateModels.cs   (TemplateCandidateReport)
 *   validation -> Services/TemplateConvert/EphemeralTemplateValidationModels.cs (TemplateValidationReport)
 *   region     -> Services/TemplateConvert/TemplateConvertModels.cs     (DocumentRegion.WordSpan / .ExcelCell)
 * Do not invent fields here. If a screen needs something the DTO lacks, the DTO is the thing to change.
 */

/** Flow: docs/TEMPLATE_AI_CONVERT_UI_FLOW.md — `help` (V6) is off-flow and returns to its caller. */
export const CONVERT_STAGES = ['upload', 'candidate', 'converting', 'preview', 'done', 'help'];

/** Officers on this profile, for the instance picker when convert starts from the template catalog (PNG 15). */
export const CONVERT_INSTANCES = [
  { id: 'INV-2026-0142', label: 'INV-2026-0142 · Amanow D. and 2 others', profile: 'Iş Rugsatnamasyny Uzaltmak (App_WP_Ext)' },
  { id: 'INV-2026-0139', label: 'INV-2026-0139 · Ýazyjyýew B.', profile: 'Iş Rugsatnamasyny Uzaltmak (App_WP_Ext)' },
  { id: 'INV-2026-0131', label: 'INV-2026-0131 · Çaryýew M. and 5 others', profile: 'Iş Rugsatnamasyny Uzaltmak (App_WP_Ext)' },
];

/** Word letter — paragraph addresses match WordTemplateAddressing ordinals (`body/0`, `header0/0`). */
const LETTER_PARAGRAPHS = [
  { address: 'body/0', text: 'TÜRKMENISTANYŇ MINISTRLER KABINETI', cls: 'tac-doc__title' },
  { address: 'body/1', text: 'MINISTRY OF LABOUR AND SOCIAL PROTECTION', cls: 'tac-doc__title tac-doc__title--sub' },
  { address: 'body/2', text: '№ 03-18/2457                                             Aşgabat, 17.05.2026', cls: 'tac-doc__meta' },
  { address: 'body/3', text: 'Türkmenistanyň Ministrler Kabinetiniň 2024-nji ýylyň 6-njy iýulynda yerine ýetirilen 563-nji kararynyň esasynda aşakda görkezilen daşary ýurt raýatynyň iş rugsadynyň uzaltmak haýyş edilýär.' },
  { address: 'body/4', text: 'Ady (full name)          :  Amanow Döwletmyrat Serdarowiç', cls: 'tac-doc__row' },
  { address: 'body/5', text: 'Pasport belgisi          :  T12345678', cls: 'tac-doc__row' },
  { address: 'body/6', text: 'Doglan senesi            :  18.01.1977', cls: 'tac-doc__row' },
  { address: 'body/7', text: 'Kärhana                  :  Çalyk Enerji', cls: 'tac-doc__row' },
  { address: 'body/8', text: 'Şahsy belgi              :  A12345678', cls: 'tac-doc__row' },
  { address: 'body/9', text: 'Görkezilen maglumatlaryň dogrudygyna we Türkmenistanyň kanunçylygyna laýykdygyny tassyklaýarys.' },
  { address: 'body/10', text: 'Begençmyrat Geldiýew', cls: 'tac-doc__sign' },
  { address: 'body/11', text: 'Deputy Minister', cls: 'tac-doc__sign tac-doc__sign--muted' },
];

/** Excel roster — cells match DocumentRegion.ExcelCell (sheet + A1 reference). */
const ROSTER_SHEET = {
  name: 'Sanaw',
  columns: ['A', 'B', 'C', 'D'],
  headers: { A: '№', B: 'Familiýasy, ady', C: 'Pasport belgisi', D: 'Wezipesi' },
  rows: [
    { row: 5, cells: { A: '1', B: 'Amanow Döwletmyrat', C: 'T12345678', D: 'Buraw ussasy' } },
    { row: 6, cells: { A: '2', B: 'Ýazyjyýew Begenç', C: 'T22456190', D: 'Elektrik' } },
    { row: 7, cells: { A: '3', B: 'Çaryýew Myrat', C: 'T30918822', D: 'Kebşirleýji' } },
  ],
};

function wordSpan(address, needle, extra) {
  const paragraph = LETTER_PARAGRAPHS.find(p => p.address === address);
  const start = paragraph ? paragraph.text.indexOf(needle) : -1;
  return {
    region: { kind: 'WordSpan', paragraphAddress: address, start, length: needle.length },
    matchedText: needle,
    rowIndex: null,
    ...extra,
  };
}

function excelCell(cellReference, matchedText, extra) {
  return {
    region: { kind: 'ExcelCell', sheetName: ROSTER_SHEET.name, cellReference },
    matchedText,
    ...extra,
  };
}

const LETTER_FILE = {
  id: 'letter',
  fileName: 'Ministry_Request_Letter.docx',
  format: 'Docx',
  sizeLabel: '48 KB',
  suggestedName: 'Ministry request letter — work permit extension',
  suggestedScope: 'ApplicationHeader',
  paragraphs: LETTER_PARAGRAPHS,
  candidate: {
    suitability: 'Pass',
    reasons: [
      { code: 'FormatOk', message: 'The document structure and key elements match this template type.' },
      { code: 'InstanceOverlapOk', message: 'The content fits the expected context and effective date range.' },
      { code: 'PlaceholderSetOk', message: 'All matched values resolve to placeholders this Application Profile can fill.' },
    ],
    matchedCount: 6,
    rosterCellCount: 0,
    gapCount: 1,
    highlights: [
      wordSpan('body/2', '№ 03-18/2457', { kind: 'Match', token: '{{ds.AFNUM}}', shortCode: 'AFNUM' }),
      wordSpan('body/2', '17.05.2026', { kind: 'Match', token: '{{ds.ADAT}}', shortCode: 'ADAT' }),
      wordSpan('body/4', 'Amanow Döwletmyrat Serdarowiç', { kind: 'Match', token: '{{.PFN}}', shortCode: 'PFN' }),
      wordSpan('body/5', 'T12345678', { kind: 'Match', token: '{{.PPN}}', shortCode: 'PPN' }),
      wordSpan('body/6', '18.01.1977', { kind: 'Match', token: '{{.PDBT}}', shortCode: 'PDBT' }),
      wordSpan('body/7', 'Çalyk Enerji', { kind: 'Match', token: '{{ds.ACNAM}}', shortCode: 'ACNAM' }),
      wordSpan('body/8', 'A12345678', { kind: 'Gap', token: null, shortCode: null }),
    ],
  },
  validation: {
    tokens: ['ds.ACNAM', 'ds.ADAT', 'ds.AFNUM', '.PDBT', '.PFN', '.PPN'],
    issues: [],
    hasHardFailure: false,
    hasWarnings: false,
  },
};

const ROSTER_FILE = {
  id: 'roster',
  fileName: 'Isgarler_sanawy.xlsx',
  format: 'Xlsx',
  sizeLabel: '22 KB',
  suggestedName: 'Işgärler sanawy — work permit extension',
  suggestedScope: 'PeopleM2M',
  sheet: ROSTER_SHEET,
  candidate: {
    suitability: 'Pass',
    reasons: [
      { code: 'FormatOk', message: 'The workbook structure and header row match this template type.' },
      { code: 'RosterLoopDetected', message: 'Three roster rows repeat the same columns — a row loop will be written.' },
      { code: 'PlaceholderSetOk', message: 'All matched cells resolve to placeholders this Application Profile can fill.' },
    ],
    matchedCount: 3,
    rosterCellCount: 9,
    gapCount: 0,
    highlights: [
      excelCell('B5', 'Amanow Döwletmyrat', { kind: 'Match', token: '{{.PFN}}', shortCode: 'PFN', rowIndex: 0 }),
      excelCell('C5', 'T12345678', { kind: 'Match', token: '{{.PPN}}', shortCode: 'PPN', rowIndex: 0 }),
      excelCell('B6', 'Ýazyjyýew Begenç', { kind: 'Match', token: '{{.PFN}}', shortCode: 'PFN', rowIndex: 1 }),
      excelCell('C6', 'T22456190', { kind: 'Match', token: '{{.PPN}}', shortCode: 'PPN', rowIndex: 1 }),
      excelCell('B7', 'Çaryýew Myrat', { kind: 'Match', token: '{{.PFN}}', shortCode: 'PFN', rowIndex: 2 }),
      excelCell('C7', 'T30918822', { kind: 'Match', token: '{{.PPN}}', shortCode: 'PPN', rowIndex: 2 }),
    ],
  },
  validation: {
    tokens: ['#ds.rows', '/ds.rows', '.PFN', '.PPN', '.PPOS'],
    // Warning only: the token resolves and merges as empty text, so Approve unlocks with the checkbox (E-D2).
    issues: [
      { code: 'PackDisabledToken', severity: 'Warning', token: '.PPOS', message: 'Position pack is off for this profile \u2014 the Wezipesi column merges as empty text' },
    ],
    hasHardFailure: false,
    hasWarnings: true,
  },
};

/** V8 — an internal memo: parseable, but nothing in it belongs to this case (PNG 06). */
const MEMO_PARAGRAPHS = [
  { address: 'body/0', text: 'To: All Staff', cls: 'tac-doc__row' },
  { address: 'body/1', text: 'From: Office Manager', cls: 'tac-doc__row' },
  { address: 'body/2', text: 'Date: May 12, 2025', cls: 'tac-doc__row' },
  { address: 'body/3', text: 'Subject: Office Procedures and Working Guidelines', cls: 'tac-doc__row' },
  { address: 'body/4', text: 'This memo provides general information about our office procedures and expectations. It is intended to help all team members understand how we work together and maintain an efficient, professional, and respectful workplace.' },
  { address: 'body/5', text: 'We rely on each person\u2019s cooperation to keep our office running smoothly. Following these guidelines helps us support our clients, meet our commitments, and achieve our goals as a team.' },
  { address: 'body/6', text: 'Office Procedures', cls: 'tac-doc__sign' },
  { address: 'body/7', text: '\u2022 Start the workday on time and be prepared.' },
  { address: 'body/8', text: '\u2022 Keep your workspace tidy and organized.' },
  { address: 'body/9', text: '\u2022 Follow established processes for requests and approvals.' },
  { address: 'body/10', text: 'Sincerely,', cls: 'tac-doc__sign' },
  { address: 'body/11', text: 'Office Manager', cls: 'tac-doc__sign tac-doc__sign--muted' },
];

const MEMO_FILE = {
  id: 'memo',
  fileName: 'Internal_HR_Policy_Notes.docx',
  format: 'Docx',
  sizeLabel: '62 KB',
  suggestedName: 'Internal HR policy notes',
  suggestedScope: 'ApplicationHeader',
  paragraphs: MEMO_PARAGRAPHS,
  candidate: {
    suitability: 'Fail',
    reasons: [
      { code: 'NoInstanceMatches', message: 'Almost no overlap with this instance data.' },
      { code: 'NoProfileTokens', message: 'No tokens from this Application Profile placeholder set.' },
      { code: 'NoHeaderOrTable', message: 'No header or people table detected.' },
    ],
    matchedCount: 0,
    rosterCellCount: 0,
    gapCount: 0,
    highlights: [],
  },
  validation: { tokens: [], issues: [], hasHardFailure: true, hasWarnings: false },
};

/** V9 + V10 — a half-finished template someone already tokenized by hand, with broken tokens (PNGs 07, 10). */
const DRAFT_PARAGRAPHS = [
  { address: 'body/0', text: 'T\u00dcRKMENISTANY\u0147 MINISTRLER KABINETI', cls: 'tac-doc__title' },
  { address: 'body/1', text: 'MINISTRY OF LABOUR AND SOCIAL PROTECTION', cls: 'tac-doc__title tac-doc__title--sub' },
  { address: 'body/2', text: '\u00c7yky\u015f \u2116 {{ds.AFNUM}}                                   Sene: {{ds.ADAT}}', cls: 'tac-doc__meta' },
  { address: 'body/3', text: 'I\u015e RUGSATNAMASYNY UZALTMAK BARADA HAT', cls: 'tac-doc__title tac-doc__title--sub' },
  { address: 'body/4', text: '\u015eu hat bilen m\u00e4lim edily\u00e4ris, {{.PFN}}, pasport \u2116 {{.PPN}}, \u00c7alyk Enerji \u015firketinde i\u015fley\u00e4r.' },
  { address: 'body/5', text: 'I\u015f rugsatnamasyny\u0148 uzaldyly\u00fdan m\u00f6hleti: {{ds.WorkPermit_Duration}}', cls: 'tac-doc__row' },
  { address: 'body/6', text: 'A\u00fdlyk hak t\u00f6legi: {{.PSAL}}', cls: 'tac-doc__row' },
  { address: 'body/7', text: '{{#ds.rows}} I\u015fg\u00e4rler sanawy', cls: 'tac-doc__row' },
  { address: 'body/8', text: 'Hormatly bilen, Ministrligi\u0148 \u00fd\u00f6rta\u00e7\u00e7ysy', cls: 'tac-doc__sign' },
];

function draftSpan(address, needle, extra) {
  const paragraph = DRAFT_PARAGRAPHS.find(p => p.address === address);
  const start = paragraph ? paragraph.text.indexOf(needle) : -1;
  return {
    region: { kind: 'WordSpan', paragraphAddress: address, start, length: needle.length },
    matchedText: needle,
    rowIndex: null,
    ...extra,
  };
}

const DRAFT_FILE = {
  id: 'draft',
  fileName: 'Taslama_shablon.docx',
  format: 'Docx',
  sizeLabel: '54 KB',
  suggestedName: 'Ministry request letter \u2014 hand-written draft',
  suggestedScope: 'ApplicationHeader',
  paragraphs: DRAFT_PARAGRAPHS,
  candidate: {
    suitability: 'Warn',
    reasons: [
      { code: 'AlreadyTokenized', message: 'The file already contains placeholders \u2014 converting again may duplicate them.' },
      { code: 'TooFewHeaderMatches', message: 'Only 1 distinct value matched this case \u2014 below the 6 needed for a clean pass.' },
      { code: 'RosterPackReferenced', message: 'Roster pack referenced but the People toggle is off for this profile.' },
      { code: 'AmbiguousMatch', message: 'One amount has no field in this profile placeholder set.' },
    ],
    matchedCount: 2,
    rosterCellCount: 0,
    gapCount: 2,
    highlights: [
      draftSpan('body/4', '\u00c7alyk Enerji', { kind: 'Match', token: '{{ds.ACNAM}}', shortCode: 'ACNAM' }),
      draftSpan('body/5', '{{ds.WorkPermit_Duration}}', { kind: 'Gap', token: null, shortCode: null }),
      draftSpan('body/6', '{{.PSAL}}', { kind: 'Gap', token: null, shortCode: null }),
    ],
  },
  validation: {
    tokens: ['ds.ACNAM', 'ds.ADAT', 'ds.AFNUM', 'ds.WorkPermit_Duration', '#ds.rows', '.PFN', '.PPN', '.PSAL'],
    issues: [
      { code: 'UnknownToken', severity: 'Error', token: 'ds.WorkPermit_Duration', message: 'Unknown token: ds.WorkPermit_Duration' },
      { code: 'BrokenLoop', severity: 'Error', token: '#ds.rows', message: 'Unclosed loop marker near paragraph 7' },
      { code: 'OutOfDataScopeToken', severity: 'Error', token: '.PFN', message: 'Row token used in a header-only template' },
      { code: 'PackDisabledToken', severity: 'Warning', token: '.PSAL', message: 'Salary pack is off for this profile \u2014 this token merges as empty text' },
    ],
    hasHardFailure: true,
    hasWarnings: true,
  },
};

export const CONVERT_FILES = [LETTER_FILE, ROSTER_FILE, DRAFT_FILE, MEMO_FILE];

export const CONVERTING_STEPS = [
  { key: 'read', label: 'Reading document' },
  { key: 'match', label: 'Matching fields' },
  { key: 'build', label: 'Building template' },
  { key: 'check', label: 'Checking' },
];

/**
 * Canned assistant replies. Anything that is not a mapping change is refused (L8) — the refusal
 * is the point of the panel, not a nicety.
 */
const CHAT_RULES = [
  {
    test: /font|bold|layout|logo|wording|rewrite|colour|color|margin|spacing|format/i,
    reply: 'I can only change which values become placeholders. Layout, wording, and formatting stay exactly as you uploaded them — open the template in desktop staging for that.',
    refused: true,
  },
  {
    test: /passport/i,
    reply: 'Remapped the highlighted ID span to {{.PPN}} (Passport number) from this profile set. Layout unchanged.',
    apply: { address: 'body/8', shortCode: 'PPN', token: '{{.PPN}}' },
  },
  {
    test: /personal number|şahsy|sahsy|national/i,
    reply: 'Remapped the highlighted ID span to {{.PPIN}} (Personal number) from this profile set. Layout unchanged.',
    apply: { address: 'body/8', shortCode: 'PPIN', token: '{{.PPIN}}' },
  },
  {
    test: /company|kärhana|karhana/i,
    reply: 'The company name already maps to {{ds.ACNAM}}. Tell me which other span should use it and I will move it.',
  },
];

const state = {
  /** L13: per-user UI preference. Off by default; only the case-workspace entry depends on it. */
  editorEnabled: false,
  open: false,
  stage: 'upload',
  /** 'instance' = opened from a case (context fixed) · 'catalog' = opened from the profile templates page. */
  source: 'instance',
  instanceId: CONVERT_INSTANCES[0].id,
  fileId: null,
  templateName: '',
  catalogTarget: 'profile',
  dataScope: 'ApplicationHeader',
  progress: 0,
  stepIndex: 0,
  previewTab: 'filled',
  acknowledgedWarnings: false,
  /** V9: a Warn candidate must be acknowledged before Convert — a conversion run costs an AI call. */
  acknowledgedCandidate: false,
  /** V11: mirrors the parent profile's config lock. Not a preference — the officer cannot turn it off. */
  configLocked: false,
  /** V13: 'convert' runs the AI flow, 'manual' is the L12 prepared-template upload. */
  mode: 'convert',
  aiEnabled: true,
  /** V12: Validate passed but the instance merge failed — Approve stays allowed (spec §6.1). */
  fillPreviewFailed: false,
  remaps: {},
  chat: [],
  savedTemplate: null,
  /** V7 layer: `{ key, title, lines, okLabel, cancelLabel }` or null. Never a stage — V4 stays rendered underneath. */
  confirm: null,
  /** Where V6 (Needs help) goes Back to. */
  returnStage: null,
};

export function getConvertState() { return state; }

export function isConvertEditorEnabled() { return state.editorEnabled; }

export function setConvertEditorEnabled(on) { state.editorEnabled = !!on; }

/**
 * V13 — the AI provider flag (spec §7). A deployment setting per slot, not a preference, so the
 * prototype exposes it through `?ai=off` rather than a switch an officer could flip.
 */
export function isConvertAiEnabled() { return state.aiEnabled; }

export function setConvertAiEnabled(on) { state.aiEnabled = !!on; }

export function getConvertFile() {
  return CONVERT_FILES.find(f => f.id === state.fileId) ?? null;
}

export function getConvertInstance() {
  return CONVERT_INSTANCES.find(i => i.id === state.instanceId) ?? CONVERT_INSTANCES[0];
}

export function openConvert({ source = 'instance', instanceId = null, mode = 'convert' } = {}) {
  state.open = true;
  state.stage = 'upload';
  state.source = source;
  state.mode = mode;
  state.fillPreviewFailed = false;
  state.instanceId = instanceId ?? CONVERT_INSTANCES[0].id;
  state.fileId = null;
  state.templateName = '';
  state.catalogTarget = 'profile';
  state.dataScope = 'ApplicationHeader';
  state.progress = 0;
  state.stepIndex = 0;
  state.previewTab = 'filled';
  state.acknowledgedWarnings = false;
  state.acknowledgedCandidate = false;
  state.remaps = {};
  state.chat = [];
  state.savedTemplate = null;
  state.confirm = null;
  state.returnStage = null;
}

export function setConfigLocked(locked) {
  state.configLocked = !!locked;
}

export function closeConvert() {
  state.open = false;
  state.stage = 'upload';
  state.confirm = null;
}

/** Everything between V1 and V5 holds work worth protecting; V1 and V5 do not. */
export function needsDiscardConfirm() {
  return state.open && state.stage !== 'upload' && state.stage !== 'done';
}

export function setConfirm(confirm) {
  state.confirm = confirm ?? null;
}

/** Cancel on V3 aborts the run and lands back on V2 — it does not close the modal. */
export function abortConverting() {
  state.stage = 'candidate';
  state.progress = 0;
  state.stepIndex = 0;
}

export function openHelp() {
  state.returnStage = state.stage;
  state.stage = 'help';
}

export function closeHelp() {
  state.stage = state.returnStage ?? 'candidate';
  state.returnStage = null;
}

export function setConvertField(field, value) {
  if (!(field in state)) return;
  state[field] = value;
}

export function pickConvertFile(fileId) {
  const file = CONVERT_FILES.find(f => f.id === fileId);
  if (!file) return;
  state.fileId = file.id;
  if (!state.templateName) state.templateName = file.suggestedName;
  state.dataScope = file.suggestedScope;
}

export function canAnalyze() {
  return !!state.fileId && state.templateName.trim().length > 0 && !!state.instanceId;
}

/** L7: Fail can never convert; Warn converts only after the officer acknowledges (PNG 07). */
export function canConvert() {
  const level = getConvertFile()?.candidate.suitability;
  if (level === 'Fail') return false;
  if (level === 'Warn') return state.acknowledgedCandidate;
  return true;
}

/** Approve mirrors E6: hard failures block, warnings need the checkbox (E-D2), config lock blocks outright. */
export function canApprove() {
  const file = getConvertFile();
  if (!file) return false;
  if (state.configLocked) return false;
  if (file.validation.hasHardFailure) return false;
  return !file.validation.hasWarnings || state.acknowledgedWarnings;
}

/** Tokens the validator rejected — the document view marks them so the error list is not a scavenger hunt. */
export function getErrorTokens() {
  const file = getConvertFile();
  if (!file) return new Set();
  return new Set(file.validation.issues
    .filter(i => i.severity === 'Error' && i.token)
    .map(i => `{{${i.token}}}`));
}

/** Shared target and remaining gaps are shown in one dialog rather than chained prompts (spec §6.4). */
export function approvalConfirmLines() {
  const lines = [];
  if (state.catalogTarget === 'shared') {
    lines.push('Saved to the Shared catalog — available to other profiles via Include.');
  }
  const { gaps } = getSummary();
  if (gaps) {
    lines.push(`${gaps} highlighted span${gaps === 1 ? '' : 's'} stayed literal text and will repeat in every generated document.`);
  }
  return lines;
}

export function getGaps() {
  return getHighlights().filter(h => h.kind === 'Gap');
}

export function getHighlights() {
  const file = getConvertFile();
  if (!file) return [];
  return file.candidate.highlights.map(h => {
    const remap = h.region.kind === 'WordSpan' ? state.remaps[h.region.paragraphAddress] : null;
    if (!remap) return h;
    return { ...h, kind: 'Match', token: remap.token, shortCode: remap.shortCode };
  });
}

/** Same arithmetic the real report does, so the chips move when chat remaps a gap. */
export function getSummary() {
  const highlights = getHighlights();
  const file = getConvertFile();
  const matches = highlights.filter(h => h.kind === 'Match');
  const cells = matches.filter(h => h.region.kind === 'ExcelCell');
  return {
    matched: matches.length,
    rosterCells: cells.length,
    rosterRows: new Set(cells.map(h => h.rowIndex).filter(i => i != null)).size,
    gaps: highlights.filter(h => h.kind === 'Gap').length,
    suitability: file?.candidate.suitability ?? 'Fail',
  };
}

export function advanceConverting() {
  state.progress = Math.min(100, state.progress + 25);
  state.stepIndex = Math.min(CONVERTING_STEPS.length, Math.round(state.progress / 25));
  if (state.progress >= 100) {
    state.stage = 'preview';
    // Open on the token view when the filled view cannot be trusted: broken tokens, or a merge that failed.
    const degraded = getConvertFile()?.validation.hasHardFailure || state.fillPreviewFailed;
    state.previewTab = degraded ? 'tokens' : 'filled';
    return true;
  }
  return false;
}

export function sendConvertChat(text) {
  const message = String(text ?? '').trim();
  if (!message) return;
  state.chat.push({ role: 'officer', text: message, at: clock() });

  const rule = CHAT_RULES.find(r => r.test.test(message));
  if (!rule) {
    state.chat.push({
      role: 'assistant',
      text: 'Tell me which value should become a placeholder and I will remap it. I only change mapping, never layout or wording.',
      at: clock(),
    });
    return;
  }

  if (rule.apply) state.remaps[rule.apply.address] = { token: rule.apply.token, shortCode: rule.apply.shortCode };
  state.chat.push({ role: 'assistant', text: rule.reply, refused: !!rule.refused, at: clock() });
}

export function approveConvert() {
  const file = getConvertFile();
  const instance = getConvertInstance();
  state.savedTemplate = {
    name: state.templateName,
    format: file?.format === 'Xlsx' ? 'Excel (.xlsx)' : 'Word (.docx)',
    catalog: state.catalogTarget === 'shared' ? 'Shared' : 'Profile-specific',
    profile: instance.profile,
    instanceId: state.mode === 'manual' ? null : instance.id,
    readiness: 'Ready',
    manual: state.mode === 'manual',
  };
  state.stage = 'done';
}

/**
 * L12 manual add: the file already carries placeholders, so it skips candidate check and conversion
 * entirely and only has to pass Validate.
 */
export function canAddManual() {
  return !!state.fileId && state.templateName.trim().length > 0;
}

export function addManualTemplate() {
  if (!canAddManual()) return;
  approveConvert();
}

function clock() {
  const d = new Date();
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
