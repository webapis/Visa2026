// Throwaway smoke check: render each edge-case stage headlessly and assert the key controls.
import {
  openConvert, pickConvertFile, setConvertField, setConfigLocked, setConvertAiEnabled,
  addManualTemplate, canConvert, canApprove, canAddManual,
} from '../js/template-convert-data.js';
import { renderConvertModal, renderConvertEntryButton } from '../js/template-convert-ui.js';

function check(label, cond) {
  console.log(`${cond ? 'ok  ' : 'FAIL'} ${label}`);
  if (!cond) process.exitCode = 1;
}

// V8 — Fail
openConvert({ source: 'catalog' });
pickConvertFile('memo');
setConvertField('stage', 'candidate');
let html = renderConvertModal();
check('V8 fail rail heading', html.includes('Fail reasons'));
check('V8 convert disabled', !canConvert() && html.includes('id="tac-convert" disabled'));
check('V8 fail hint', html.includes('Conversion is disabled for failed checks'));
check('V8 no legend', html.includes('No matched placeholders found'));

// V9 — Warn + acknowledge gate
openConvert({ source: 'catalog' });
pickConvertFile('draft');
setConvertField('stage', 'candidate');
html = renderConvertModal();
check('V9 warn rail heading', html.includes('Soft warnings'));
check('V9 convert blocked before ack', !canConvert());
check('V9 ack checkbox present', html.includes('id="tac-ack-candidate"'));
setConvertField('acknowledgedCandidate', true);
check('V9 convert unlocked after ack', canConvert());

// V10 — validate fail
setConvertField('stage', 'preview');
setConvertField('previewTab', 'tokens');
html = renderConvertModal();
check('V10 error banner', html.includes('We could not finish this template automatically'));
check('V10 error rail', html.includes('Validation errors') && html.includes('UnknownToken'));
check('V10 chat hidden', !html.includes('id="tac-chat-send"'));
check('V10 token marked in document', html.includes('tac-token-error'));
check('V10 approve blocked', !canApprove() && html.includes('id="tac-approve" disabled'));
check('V10 needs help offered', html.includes('id="tac-needs-help"'));

// V11 — config locked on a clean conversion
openConvert({ source: 'catalog' });
pickConvertFile('letter');
setConvertField('stage', 'preview');
setConfigLocked(true);
html = renderConvertModal();
check('V11 lock badge', html.includes('Config locked'));
check('V11 lock banner', html.includes('Profile templates are locked'));
check('V11 approve blocked', !canApprove() && html.includes('id="tac-approve" disabled'));
check('V11 preview still rendered', html.includes('Filled preview'));
setConfigLocked(false);

// V12 — fill preview failed, Validate passed
openConvert({ source: 'catalog' });
pickConvertFile('letter');
setConvertField('stage', 'preview');
setConvertField('fillPreviewFailed', true);
html = renderConvertModal();
check('V12 tab flagged', html.includes('Filled preview (error)'));
check('V12 fallback notice', html.includes('showing the master with placeholders'));
check('V12 approve still allowed', canApprove() && !html.includes('id="tac-approve" disabled'));

// V13 — manual add (L12), and the AI-off entry
openConvert({ source: 'catalog', mode: 'manual' });
html = renderConvertModal();
check('V13 manual title', html.includes('Add prepared template'));
check('V13 no stepper', !html.includes('tac-stepper'));
check('V13 no instance field', !html.includes('data-tac-field="instanceId"'));
check('V13 add disabled without a file', !canAddManual() && html.includes('id="tac-add-manual" disabled'));
pickConvertFile('draft');
setConvertField('templateName', 'Ministry letter (prepared)');
check('V13 add enabled', canAddManual());
addManualTemplate();
html = renderConvertModal();
check('V13 done says added', html.includes('Template added'));
check('V13 done omits context instance', !html.includes('Context instance used for mapping'));

setConvertAiEnabled(false);
const entry = renderConvertEntryButton({ source: 'catalog' });
check('AI off: convert disabled', entry.includes('AI off') && entry.includes('disabled'));
check('AI off: manual is primary', entry.includes('tac-btn--primary') && entry.includes('data-tac-mode="manual"'));
setConvertAiEnabled(true);

// Roster warning-only path still unlocks with the checkbox (E-D2)
openConvert({ source: 'catalog' });
pickConvertFile('roster');
setConvertField('stage', 'preview');
check('roster warning blocks approve', !canApprove());
setConvertField('acknowledgedWarnings', true);
check('roster approve unlocks after ack', canApprove());
