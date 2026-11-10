# Link existing person — DxWindow dialog

**Link existing…** opens the person search picker as a **DxWindow** (same pattern as Join/link-review), not an inline panel. Escape, outside click, Cancel, and the window X dismiss it. Choosing **Link** on a row closes the picker and opens the existing review DxWindow.

**Status:** implemented.

## Locked decisions

| Decision | Choice |
|----------|--------|
| Shell | Same **DxWindow** as link-review |
| After row Link | Picker closes; review DxWindow opens |
| Dismiss | Cancel, X, Escape, click outside |
| Review | Unchanged DxWindow |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-link-existing-person-dialog-prototype.jpg](./case-workspace-link-existing-person-dialog-prototype.jpg) | Modal picker over People & links |