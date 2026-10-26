# Review linked records dialog (2026-10-26)

Implemented for Link existing. Opens as its own popup after a person is selected, before Join commits the link. It is not part of the case page. There is no tick.

The popup shows Overview as one line per record. The record tabs stay hidden until the gear is clicked. Property values on that line are joined with commas. A gear at the top right of Overview turns each line into a dropdown of that person's records and shows the pen and a plus. Click the gear again to hide them. The plus opens an empty form on that record’s tab inside the popup. The pen opens the same form filled with the selected record. Lookup fields are dropdowns of existing catalog values. Text fields are editable. Dates use the officer date control (`dd.MM.yyyy`, advancing caret, calendar). Save on a new form creates the record and selects it for Join, then returns to Overview. Save on the pen updates that record and keeps it selected for Join. Cancel returns to Overview and does not save. A new address is empty and replaces the Case summary copy. Address of residence keeps a pen while the gear is on, including when the line shows the Case summary stay. That pen opens the stay in the form; Save creates the address, and Cancel leaves the stay for Join. Join and Cancel stay hidden while the form is open.

A record tab shows that record's fields in the same layout as the editor prototypes: text fields as values, lookup fields with the lookup buttons. The pen on a lookup field opens the same custom form as the Overview pen.

Join links the selected person and accepts the Case summary address. Cancel returns to the person search without linking. Choosing another address in the Overview dropdown links that address and does not insert the Case summary copy. Saving the address form does the same: Join links that saved address and does not insert another Case summary copy.

Under the title, before the rows, the dialog shows the selected person's portrait and full name so the officer can see whose records are being linked. That header stays visible on Overview, on a record tab, and while a record is being added or edited. The portrait is the person's saved photo at passport ratio. The sample name in the prototype is not a real person.

## Case summary address on Join

Address of residence on an invitation case shows the Case summary stay (region, city, address type, and place). Join inserts that stay as a new `AddressOfResidence` and links it to this case.

Join does not insert when that stay already matches the person's last `AddressOfResidence`. Last means the newest address on the person. A match compares region, city, type, and place (`InvitationStayAddressCopy.Matches`). The last address is left unchanged. Older addresses are not rewritten.

If the person has no address, or the last address differs, Join creates the new row.

## Screens

| File | State |
|------|--------|
| [application-profile-instance-linked-records-dialog-person-header.jpg](./application-profile-instance-linked-records-dialog-person-header.jpg) | Overview with the selected person's portrait and full name under the title. Tabs stay hidden until the gear is clicked. |
| [application-profile-instance-linked-records-dialog-01-overview.jpg](./application-profile-instance-linked-records-dialog-01-overview.jpg) | Earlier overview. Comma-separated lines, a pen on every row, no tick. Join and Cancel at the bottom left. |
| [application-profile-instance-linked-records-dialog-03-passport.jpg](./application-profile-instance-linked-records-dialog-03-passport.jpg) | Passport editor opened from the pen. |
| [application-profile-instance-linked-records-dialog-05-education.jpg](./application-profile-instance-linked-records-dialog-05-education.jpg) | Education editor opened from the pen. |
| [application-profile-instance-linked-records-dialog-06-position.png](./application-profile-instance-linked-records-dialog-06-position.png) | Position editor. Officer sample of the native detail form. |
| [application-profile-instance-linked-records-dialog-02-address.jpg](./application-profile-instance-linked-records-dialog-02-address.jpg) | Address of residence editor opened from the pen. Lodging shows Region, City, Type, Lodging. |
| [application-profile-instance-linked-records-dialog-04-visa.jpg](./application-profile-instance-linked-records-dialog-04-visa.jpg) | Visa editor opened from the pen. |