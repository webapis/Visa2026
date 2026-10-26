# Linked address of residence (2026-10-24)

Prototype approved 2026-10-24 and implemented in the case workspace.

Invitation region, city, address type, and other invitation places show on Çakylyk Almak, Çakylyk we Iş Rugsatnamasyny Almak, Çakylyk Almak FM, Iş Rugsatnama görä Çakylyk Almak, and Çakylygy üýtgetmek. Çakylyk Almak also shows the Address tile. Work visa does not collect the stay. Region, city, address type, and the place are required. Other invitation places stay optional. A person with no address gets one from the case summary. A tick creates a new address and relinks, leaving the previous address on the person. The pen edits that person's address. Private-house expiration is filled in the pen editor.

Case summary address (Invitation region, Invitation city, Address type, and the place) is the source. Each linked person's Address of residence is the target. Other invitation places stay on the case. They are not copied onto the person.

## Proposed behavior

| Situation | What the officer sees |
|-----------|------------------------|
| Person has no Address of residence | On link, the case summary address is saved as a new Address of residence and linked. The address row shows **From case summary** and a pen. No tick. |
| Person already has an Address of residence | The existing row stays. A green tick sits on the right of that row. Tick creates a new Address of residence from the case summary and relinks this case to it. The previous address stays on the person and is no longer the linked row. |
| Pen | Opens a custom Address of residence editor in the right preview. Save writes the person's address. |
| Case summary address not filled yet | No auto-insert and no tick until region, city, and address type are set. |

## Screens

| File | State |
|------|--------|
| [application-profile-instance-address-residence-01-existing-tick.jpg](./application-profile-instance-address-residence-01-existing-tick.jpg) | Previous address. Tick copies the case summary address and relinks. Pen is beside the tick. |
| [application-profile-instance-address-residence-02-preview-editor.jpg](./application-profile-instance-address-residence-02-preview-editor.jpg) | Pen opens the editor in the right preview. |
| [application-profile-instance-address-residence-03-empty-auto.jpg](./application-profile-instance-address-residence-03-empty-auto.jpg) | No previous address. Case summary address already linked. Pen only. |

## Open before implementation

- Which templates show the case-summary address fields (today only Çakylyk Almak).
- Tick creates a new address and relinks, versus overwriting the existing record.
- Private-house expiration is not on the case summary. Officer fills it in the pen editor.
- Editing in the preview changes the person's address, including other cases that link the same record.