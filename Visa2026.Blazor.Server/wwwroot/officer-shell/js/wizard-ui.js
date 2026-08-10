/** Template wizard markup — parity with application-profile-template-wizard*.png */

export const WIZARD_STEPS = [
  { title: 'Identity & purpose', sub: 'Name, route, audience, action' },
  { title: 'Results & default fields', sub: 'Produce / cancel + defaults' },
  { title: 'Process & SLA', sub: 'Legs, states, durations' },
  { title: 'Templates & person data', sub: 'Resminamalar + toggles' },
  { title: 'Review & publish', sub: 'Summary before publish' },
];

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
}

function wizardStepper(step) {
  return WIZARD_STEPS.map((st, i) => {
    const done = i < step;
    const active = i === step;
    const cls = ['wz-step', active ? 'is-active' : '', done ? 'is-done' : ''].filter(Boolean).join(' ');
    const dot = done
      ? '<i class="bi bi-check-lg"></i>'
      : String(i + 1);
    const status = active
      ? '<span class="wz-step__status">ACTIVE</span>'
      : done
        ? '<span class="wz-step__status wz-step__status--done">COMPLETED</span>'
        : '';
    return `<button type="button" class="${cls}" data-wizard-step="${i}">
      <span class="wz-step__dot">${dot}</span>
      <span class="wz-step__text"><strong>${esc(st.title)}</strong>${status}</span>
    </button>`;
  }).join('');
}

function wizardFooter(step) {
  const dots = WIZARD_STEPS.map((_, i) =>
    `<span class="${i === step ? 'is-active' : ''}" aria-hidden="true"></span>`).join('');
  const nextBtn = step < 4
    ? `<button type="button" class="btn btn-primary" id="wiz-next">Next step <i class="bi bi-arrow-right ms-1"></i></button>`
    : `<button type="button" class="btn btn-success" id="wiz-publish"><i class="bi bi-rocket-takeoff me-1"></i> Publish template</button>`;
  return `<footer class="wz-foot">
    <div class="wz-foot__left">
      <button type="button" class="btn btn-outline-secondary" data-nav="#/templates"><i class="bi bi-x-lg me-1"></i> Cancel</button>
      <button type="button" class="btn btn-outline-primary"><i class="bi bi-save me-1"></i> Save draft</button>
    </div>
    <div class="wz-foot__center">
      <div class="wz-foot__step">Step ${step + 1} of 5</div>
      <div class="wz-dots" aria-label="Wizard progress">${dots}</div>
    </div>
    <div class="wz-foot__right">
      <button type="button" class="btn btn-outline-secondary" id="wiz-back" ${step === 0 ? 'disabled' : ''}><i class="bi bi-arrow-left me-1"></i> Back</button>
      ${nextBtn}
    </div>
  </footer>`;
}

function step0Body(t) {
  const name = t?.name ?? 'Invitation + work permit (employee)';
  const code = t?.code ?? 'INV_WP_EMP';
  return `<h2 class="wz-body__title">${esc(WIZARD_STEPS[0].title)}</h2>
    <div class="alert alert-info d-flex gap-2 align-items-start py-2 px-3 mb-4" role="alert">
      <i class="bi bi-info-circle-fill flex-shrink-0 mt-1"></i>
      <span>Templates define live configuration and default seeds. Cloned staged profiles inherit until merged into in-process case.</span>
    </div>
    <section class="wz-section">
      <h3 class="wz-section__title">Template identity</h3>
      <div class="wz-grid-2">
        <div class="wz-field wz-field--full">
          <label>Name <span class="req">*</span></label>
          <input class="form-control" value="${esc(name)}" />
        </div>
        <div class="wz-field wz-field--full">
          <label>Description</label>
          <textarea class="form-control" rows="3">Describe when officers should use this template for staging and in-process cases.</textarea>
        </div>
        <div class="wz-field">
          <label>Code <span class="req">*</span></label>
          <input class="form-control" value="${esc(code)}" />
        </div>
        <div class="wz-field">
          <label>Selection code</label>
          <select class="form-select"><option>—</option><option selected>INV_WP_EMP</option></select>
        </div>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">Applicability</h3>
      <div class="wz-applicability">
        <div>
          <label class="wz-radio-card is-selected"><input type="radio" name="scope" checked /> Always available</label>
          <label class="wz-radio-card mt-2"><input type="radio" name="scope" /> Scoped</label>
        </div>
        <div class="wz-criteria">
          <div class="wz-criteria__label">Criteria (when scoped)</div>
          <div class="wz-tags">
            <span class="wz-tag">Project contract <button type="button" aria-label="Remove">×</button></span>
            <span class="wz-tag">Employee <button type="button" aria-label="Remove">×</button></span>
            <span class="wz-tag">Family member <button type="button" aria-label="Remove">×</button></span>
          </div>
          <button type="button" class="btn btn-sm btn-outline-secondary">+ Add criterion</button>
        </div>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">Action family</h3>
      <div class="wz-action-row">
        <label class="wz-radio-card"><input type="radio" name="af" /> Issuance</label>
        <label class="wz-radio-card"><input type="radio" name="af" /> Cancellation</label>
        <label class="wz-radio-card"><input type="radio" name="af" /> Registration</label>
        <label class="wz-radio-card is-selected"><input type="radio" name="af" checked /> Business trip — Registration</label>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">Audience</h3>
      <div class="wz-audience">
        <label><input type="checkbox" class="form-check-input" checked /> Employee</label>
        <label><input type="checkbox" class="form-check-input" checked /> Family member</label>
        <label><input type="checkbox" class="form-check-input" /> Temporary visitor</label>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">Progress route</h3>
      <div class="wz-route-row">
        <select class="form-select"><option>Via ministry</option><option selected>Direct migration</option></select>
        <p class="wz-field__hint mb-0">Direct migration skips ministry legs; via ministry requires approval chain and ministry SLA tracking.</p>
      </div>
    </section>`;
}

function step1Body() {
  return `<h2 class="wz-body__title">${esc(WIZARD_STEPS[1].title)}</h2>
    <p class="text-muted small mb-3">Choose produced documents, cancellation targets, and default application fields.</p>
    <div class="wz-check-grid">
      <div><h4>Produce</h4><div class="wz-check-list">
        <label><input type="checkbox" class="form-check-input" checked /> Invitation</label>
        <label><input type="checkbox" class="form-check-input" checked /> Work permit</label>
        <label><input type="checkbox" class="form-check-input" /> Visa</label>
        <label><input type="checkbox" class="form-check-input" /> Border zone</label>
      </div></div>
      <div><h4>Cancel existing</h4><div class="wz-check-list">
        <label><input type="checkbox" class="form-check-input" /> Invitation</label>
        <label><input type="checkbox" class="form-check-input" /> Work permit</label>
        <label><input type="checkbox" class="form-check-input" /> Visa</label>
        <label><input type="checkbox" class="form-check-input" /> Border zone</label>
        <label><input type="checkbox" class="form-check-input" /> Application</label>
      </div></div>
    </div>
    <div class="table-responsive border rounded mb-3">
      <table class="table table-sm mb-0 wz-table">
        <thead><tr><th>Property</th><th>Use</th><th>Property kind</th><th>Has default</th><th>Default value</th><th></th></tr></thead>
        <tbody>
          <tr><td>Visa type</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" checked /></td><td>WP</td><td><i class="bi bi-three-dots-vertical text-muted"></i></td></tr>
          <tr><td>Visa category</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" checked /></td><td>B</td><td><i class="bi bi-three-dots-vertical text-muted"></i></td></tr>
          <tr><td>Visa period</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" checked /></td><td>6 months</td><td><i class="bi bi-three-dots-vertical text-muted"></i></td></tr>
          <tr><td>Project</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Reference</td><td><input type="checkbox" class="form-check-input" /></td><td>—</td><td><i class="bi bi-three-dots-vertical text-muted"></i></td></tr>
          <tr><td>Urgency</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" checked /></td><td>Normal</td><td><i class="bi bi-three-dots-vertical text-muted"></i></td></tr>
          <tr class="is-faded"><td>Entry type</td><td><input type="checkbox" class="form-check-input" /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" /></td><td>—</td><td></td></tr>
          <tr class="is-faded"><td>Number of entries</td><td><input type="checkbox" class="form-check-input" /></td><td>Scalar</td><td><input type="checkbox" class="form-check-input" /></td><td>—</td><td></td></tr>
          <tr class="is-faded"><td>Port of entry</td><td><input type="checkbox" class="form-check-input" /></td><td>Lookup</td><td><input type="checkbox" class="form-check-input" /></td><td>—</td><td></td></tr>
        </tbody>
      </table>
    </div>
    <section class="wz-section">
      <h3 class="wz-section__title">Application signatory</h3>
      <div class="wz-grid-2">
        <div class="wz-field"><label>Authorized signatory</label><select class="form-select"><option selected>Default company signatory</option></select></div>
        <div class="wz-field"><label>Visa representative</label><select class="form-select"><option>—</option></select></div>
      </div>
    </section>`;
}

function step2Body() {
  return `<h2 class="wz-body__title">${esc(WIZARD_STEPS[2].title)}</h2>
    <p class="text-muted small mb-3">Configure the approval process and service level agreements for this profile template.</p>
    <section class="wz-section">
      <h3 class="wz-section__title">Approval legs</h3>
      <ul class="wz-legs list-unstyled">
        <li><span>1</span> Turkmenenergo <button type="button"><i class="bi bi-trash me-1"></i>Remove</button></li>
        <li><span>2</span> Energetika <button type="button"><i class="bi bi-trash me-1"></i>Remove</button></li>
        <li><span>3</span> Gurluşyk <button type="button"><i class="bi bi-trash me-1"></i>Remove</button></li>
      </ul>
      <button type="button" class="btn btn-outline-primary btn-sm">+ Add approval leg</button>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">States related to ministry</h3>
      <div class="table-responsive border rounded mb-3">
        <table class="table table-sm mb-0 wz-table">
          <thead><tr><th>Include</th><th>SLA track</th><th>Submitted</th><th>Approved</th><th>Disapproved</th><th>Cancelled</th><th>Postponed</th></tr></thead>
          <tbody><tr>
            <td><input type="checkbox" class="form-check-input" checked /></td><td>Ministry track</td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
          </tr></tbody>
        </table>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">States related to migration service</h3>
      <div class="table-responsive border rounded mb-3">
        <table class="table table-sm mb-0 wz-table">
          <thead><tr><th>Include</th><th>SLA track</th><th>Submitted</th><th>On process</th><th>Process complete</th><th>Issued</th><th>Rejected</th><th>Cancelled</th></tr></thead>
          <tbody><tr>
            <td><input type="checkbox" class="form-check-input" checked /></td><td>Migration track</td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
            <td><input type="checkbox" class="form-check-input" checked /></td>
          </tr></tbody>
        </table>
      </div>
    </section>
    <section class="wz-section">
      <h3 class="wz-section__title">Expected process duration (SLA)</h3>
      <div class="wz-grid-2">
        <div class="wz-field"><label>Ministry (days)</label><input type="number" class="form-control" value="14" /></div>
        <div class="wz-field"><label>Migration (days)</label><input type="number" class="form-control" value="14" /></div>
      </div>
    </section>`;
}

function step3Body() {
  const personFields = ['Passport', 'Visa', 'Medical', 'Education', 'Address', 'Position', 'Travel history', 'Work permit', 'Invitation', 'Border zone', 'Rejection', 'Application'];
  const checks = personFields.map((f, i) =>
    `<label><input type="checkbox" class="form-check-input" ${i < 10 ? 'checked' : ''} /> ${esc(f)}</label>`).join('');
  return `<h2 class="wz-body__title">${esc(WIZARD_STEPS[3].title)}</h2>
    <p class="text-muted small mb-3">Manage application templates and select the person-related data your application will require.</p>
    <section class="wz-section border rounded p-3">
      <div class="d-flex justify-content-between align-items-center mb-2">
        <h3 class="wz-section__title mb-0">Application templates</h3>
        <button type="button" class="btn btn-sm btn-outline-primary">+ Add template</button>
      </div>
      <div class="table-responsive">
        <table class="table table-sm mb-0 wz-table">
          <thead><tr><th>#</th><th>Template name</th><th>Template type</th><th>Scope</th><th>Actions</th></tr></thead>
          <tbody>
            <tr><td>1</td><td>Forma 16</td><td><i class="bi bi-file-earmark-word text-primary me-1"></i>Word</td><td>Application</td><td><a href="#">Edit</a></td></tr>
            <tr><td>2</td><td>Borçnama</td><td><i class="bi bi-file-earmark-word text-primary me-1"></i>Word</td><td>Application</td><td><a href="#">Edit</a></td></tr>
            <tr><td>3</td><td>Cover letter</td><td><i class="bi bi-file-earmark-excel text-success me-1"></i>Excel</td><td>Application</td><td><a href="#">Edit</a></td></tr>
          </tbody>
        </table>
      </div>
      <div class="wz-add-row">
        <div class="wz-field"><label>Template name</label><input class="form-control form-control-sm" placeholder="New template" /></div>
        <div class="wz-field"><label>Template type</label><select class="form-select form-select-sm"><option>Word</option><option>Excel</option></select></div>
        <div class="wz-field"><label>File</label><input type="file" class="form-control form-control-sm" /></div>
        <button type="button" class="btn btn-primary btn-sm">+ Add</button>
      </div>
    </section>
    <section class="wz-section border rounded p-3 mt-3">
      <h3 class="wz-section__title">Required person-related data</h3>
      <p class="text-muted small">Select the person-related data fields your application will require.</p>
      <div class="wz-person-grid">${checks}</div>
      <div class="alert alert-info d-flex gap-2 align-items-start py-2 px-3 mt-3 mb-0 small">
        <i class="bi bi-info-circle-fill flex-shrink-0"></i>
        Templates and person toggles drive staged profile readiness and document copies.
      </div>
    </section>`;
}

function step4Body() {
  return `<h2 class="wz-body__title">${esc(WIZARD_STEPS[4].title)}</h2>
    <p class="text-muted small mb-3">Review the template summary below. If everything looks correct, publish the template.</p>
    <div class="wz-review-grid">
      <div class="wz-review-card wz-review-card--id">
        <h4><i class="bi bi-person-badge"></i> Identity</h4>
        <ul><li>Invitation + work permit (employee)</li><li>Code INV_WP_EMP</li><li>Direct migration · Employee</li><li>Business trip — Registration · Scoped</li></ul>
      </div>
      <div class="wz-review-card wz-review-card--res">
        <h4><i class="bi bi-file-earmark-check"></i> Results</h4>
        <ul><li>Produces: invitation, work permit</li><li>Defaults: WP / B / 6 months</li><li>Signatories: default company</li></ul>
      </div>
      <div class="wz-review-card wz-review-card--proc">
        <h4><i class="bi bi-diagram-3"></i> Process</h4>
        <ul><li>3 ministry legs</li><li>Ministry SLA 14d · Migration SLA 14d</li><li>Ministry + migration states included</li></ul>
      </div>
      <div class="wz-review-card wz-review-card--tpl">
        <h4><i class="bi bi-folder2-open"></i> Templates & person</h4>
        <ul><li>3 report templates</li><li>Person: passport, education, position, visa, invitation…</li></ul>
      </div>
    </div>
    <div class="alert alert-success d-flex gap-2 align-items-start">
      <i class="bi bi-check-circle-fill flex-shrink-0 fs-5"></i>
      <div><strong>Ready to publish.</strong> Officers can clone this template to stage profiles; merge assigns application number and date.</div>
    </div>`;
}

function stepBody(step, t) {
  if (step === 0) return step0Body(t);
  if (step === 1) return step1Body();
  if (step === 2) return step2Body();
  if (step === 3) return step3Body();
  return step4Body();
}

export function renderWizardPage(step, template) {
  const s = Math.max(0, Math.min(4, step || 0));
  return `<div class="wz-page">
    <nav aria-label="Breadcrumb" class="wz-crumb">
      <a href="#/templates" data-nav="#/templates">Profile templates</a> › New template
    </nav>
    <header class="wz-hero">
      <div class="wz-hero__main">
        <div class="wz-badges">
          <span class="wz-badge wz-badge--tpl"><i class="bi bi-layers"></i> Template</span>
          <span class="wz-badge wz-badge--draft"><i class="bi bi-pencil"></i> Draft</span>
          <span class="wz-badge wz-badge--pub"><span class="wz-badge__dot"></span> Not published</span>
        </div>
        <h1>Create Application Profile Template</h1>
        <p>Configure a reusable template — officers clone this to stage application profiles before assigning an application number.</p>
      </div>
      <button type="button" class="wz-close" data-nav="#/templates" aria-label="Close"><i class="bi bi-x-lg"></i></button>
    </header>
    <div class="wz-card card border-0 shadow-sm">
      <div class="row g-0 h-100">
        <aside class="col-lg-3 wz-stepper border-end" aria-label="Wizard steps">${wizardStepper(s)}</aside>
        <div class="col-lg-9 wz-body d-flex flex-column">
          <div class="wz-body__inner flex-grow-1">${stepBody(s, template)}</div>
          ${wizardFooter(s)}
        </div>
      </div>
    </div>
  </div>`;
}
