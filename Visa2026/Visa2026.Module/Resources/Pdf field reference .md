# Visa Application PDF — XFA Field Reference
# Extracted directly from: Visa_Application_TM_QR_08.pdf
# Total fields: 75

==============================================================
PAGE 1  (topmostSubform[0].Page1[0])
==============================================================

--- APPLICATION LEVEL ---

Key:   topmostSubform[0].Page1[0].L02[0]
Label: 3. TIZLIGI (Urgency)
Type:  choiceList
Note:  Currently mapped ✅
Valid: 'ADATY ', 'TIZ', 'ORAN TIZ', 'XX'
       (raw values: '1','2','3')

Key:   topmostSubform[0].Page1[0].L01[0]
Label: Visa operation type
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid:      'GOSM','GECM','RUGS','CYKS','K >1','P >P','1 >K','UZLT','CAKL','WIZA','UZGM','SERH','XXXX'
(raw values:'20',  '23',   '21',  '9',  '8',   '6',   '5',   '3',   '2',   '1',   '24',   '25')

Key:   topmostSubform[0].Page1[0].ImageField1[0]
Label: 1. PHOTO
Type:  imageEdit  ← image field
Note:  Currently mapped ✅ (person.Photo)

Key:   topmostSubform[0].Page1[0].IP[0]
Label: 4. CAGYRYAN TARAP FIZIKI SAHS (Natural Person checkbox)
Type:  checkButton
Note:  NOT in PdfMappingHelper ⚠️  (IP[1] is mapped instead — see below)
Valid: checked value = 'P'

Key:   topmostSubform[0].Page1[0].IP[1].#field[0]
Label: 4. CAGYRYAN TARAP YURIDIKI SAHS (Legal Entity checkbox)
Type:  checkButton  (inside IP[1] subform — unnamed child field)
Note:  Currently mapped ✅ (set to true when Company != null)

Key:   topmostSubform[0].Page1[0].L07[0]
Label: Inviting person surname (natural person)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L06[0]
Label: Inviting person first name
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L04[0]
Label: Inviting person details / registration number
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L05[0]
Label: Inviting person details 2
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L03[0]
Label: Inviting person details 3
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L08[0]
Label: 7. FAX
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L061[0]
Label: Extended inviting party text field
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L09[0]
Label: E-MAIL
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L071[0]
Label: Additional inviting party field
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L13[0]
Label: 8. TELEFON (Company phone)
Type:  textEdit
Note:  Currently mapped ✅ (application.Company.PhoneNumber)

Key:   topmostSubform[0].Page1[0].L12[0]
Label: INN / tax number
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0].L11[0]
Label: 6. HUKUK SALGYSY (Company address)
Type:  textEdit
Note:  Currently mapped ✅ (application.Company.Address)

Key:   topmostSubform[0].Page1[0].L10[0]
Label: 5. KARHANANYN ADY (Company name)
Type:  textEdit
Note:  Currently mapped ✅ (application.Company.Name)

--- PERSON LEVEL ---

Key:   topmostSubform[0].Page1[0].[0]      ← name is empty string!
Label: Unknown checkbox (unnamed field)
Type:  checkButton
Note:  NOT in PdfMappingHelper ⚠️  Key has no name segment — may not be settable
Valid: checked value = 'E'

Key:   topmostSubform[0].Page1[0]._11[0]
Label: 19. PASPORTYNYN BELGISI (Passport number)
Type:  textEdit
Note:  Currently mapped ✅ (passport.PassportNumber)

Key:   topmostSubform[0].Page1[0]._10[0]
Label: 18. RESMINAMASY GORUJI (Document type)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid display: 'P - MILLI PASPORT','APD','AGL','AML','AUN','YG','BS','PD','SP','UN','US','YD','SH','DZ','PG','LBG','PT','EU'
Valid raw:      'P','APD','AGL','AML','AUN','YG','BS','PD','SP','UN','US','YD','SH','DZ','PG','LBG','PT','EU'

Key:   topmostSubform[0].Page1[0]._09[0]
Label: 17. SAHSY BELGISI (Personal/ID number)
Type:  textEdit
Note:  Currently mapped ✅ (passport.PersonalNumber)

Key:   topmostSubform[0].Page1[0]._08[0]
Label: 16. DOGLAN YERI (Birth place)
Type:  textEdit
Note:  Currently mapped ✅ (person.BirthPlace)

Key:   topmostSubform[0].Page1[0]._05[0]
Label: 13. GYNSY (Gender)
Type:  choiceList
Note:  Currently mapped ✅ (person.Gender.Name)
       ⚠️  IMPORTANT: must pass raw value, not display name!
Valid raw: 'M', 'F', 'X'   (display in form: M/F/X)

Key:   topmostSubform[0].Page1[0]._02[0]
Label: 10. ATASYNYÑ ADY (Patronymic / father's name)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._04[0]
Label: 12. DOGLAN SENESI (Date of birth)
Type:  picture  ← date field (XFA picture = formatted input)
Note:  Currently mapped ✅ (person.DateOfBirth)
       ⚠️  Send as DateTime; Spire formats via dateTimeField.Value = dt.ToString("dd.MM.yyyy")

Key:   topmostSubform[0].Page1[0]._07[0]
Label: 15. RAÝATLYGY (Citizenship / nationality)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: ISO 3166-1 alpha-3 country codes (e.g. 'TKM', 'RUS', 'USA' …)

Key:   topmostSubform[0].Page1[0]._06[0]
Label: 14. DOGLAN YURDY (Country of birth)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: ISO 3166-1 alpha-3 country codes

Key:   topmostSubform[0].Page1[0]._03[0]
Label: 11. ADY (First name)
Type:  textEdit
Note:  Currently mapped ✅ (person.FirstName)

Key:   topmostSubform[0].Page1[0]._01[0]
Label: 9. FAMILIYASY (Last name / surname)
Type:  textEdit
Note:  Currently mapped ✅ (person.LastName)

Key:   topmostSubform[0].Page1[0]._16[0]
Label: Address line 2 / additional address
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._15[0]
Label: 23. YASAYAN YERI (Address of residence)
Type:  textEdit
Note:  Currently mapped ✅ (item.CurrentAddressOfResidence.FullAddress)

Key:   topmostSubform[0].Page1[0]._14[0]
Label: 22. ÝAŞAÝAN ÝURDY (Country of residence)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: ISO 3166-1 alpha-3 country codes

Key:   topmostSubform[0].Page1[0]._13[0]
Label: 21. PASPORT MOHLETI (Passport expiration date)
Type:  picture  ← date field
Note:  Currently mapped ✅ (passport.ExpirationDate)

Key:   topmostSubform[0].Page1[0]._12[0]
Label: 20. BERLEN SENESI (Passport issue date)
Type:  picture  ← date field
Note:  Currently mapped ✅ (passport.IssueDate)

Key:   topmostSubform[0].Page1[0]._24[0]
Label: Work place / employer
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._23[0]
Label: Work position / job title
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._22[0]
Label: Work phone
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._21[0]
Label: Work address
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._20[0]
Label: Additional work/contact info
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._181[0]
Label: Spouse last name (marital details)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._18[0]
Label: 25. MASGALA YAGDAY (Marital status)
Type:  choiceList
Note:  Currently mapped ✅ (person.MaritalStatus.Name)
       ⚠️  Must pass raw value not display name!
Valid display: 'Sallah/Durmuşa çykmadyk','Öýlenen/Durmuşa çykan','Aýrylyşan','Dul'
Valid raw:     '1', '2', '3', '4'

Key:   topmostSubform[0].Page1[0]._17[0]
Label: 24. HÜNÄRI (Profession / occupation)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._182[0]
Label: Spouse first name
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._183[0]
Label: Spouse additional info
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page1[0]._19[0]
Label: 26. BILIMI (Education level)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid display: 'ORTA','YOKARY','MEKDEP OKUWCYSY','MEKDEP YASYNA YETMEDIK','YORITE ORTA'
Valid raw:     '5','2','3','4','1'

Key:   topmostSubform[0].Page1[0]._241[0]
Label: Additional work/profession text
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

==============================================================
PAGE 2  (topmostSubform[0].Page2[0])
==============================================================

Key:   topmostSubform[0].Page2[0]._35[0]
Label: Planned stay address in Turkmenistan
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._34[0]
Label: District/Etrap of stay
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: Region+district codes e.g. 'AS69' (Aşgabat city), 'AH47' (Ahal-Gökdepe), etc.

Key:   topmostSubform[0].Page2[0]._33[0]
Label: Region/Welayat of stay
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid display: 'Ashgabat','Ahal','Mary','Lebap','Dashoguz','Balkan'
Valid raw:     'AS','AH','MR','LB','DZ','BN'

Key:   topmostSubform[0].Page2[0]._32[0]
Label: Hotel / place of stay name
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._31[0]
Label: Contact phone at destination
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._30[0]
Label: Additional stay details
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._29[0]
Label: Visa validity date (from)
Type:  picture  ← date field
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._26[0]
Label: 29. Number of entries
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid display: '1x - BIR GEZEKLIK','2x - IKI GEZEKLIK','Kx - KOP GEZEKLIK'
Valid raw:     '1','2','4'

Key:   topmostSubform[0].Page2[0]._28[0]
Label: Visa validity date (to)
Type:  picture  ← date field
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._271[0]
Label: Stay duration unit
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid display: 'Gün','Aý','Ýyl'
Valid raw:     'GUN','AY','YYL'

Key:   topmostSubform[0].Page2[0]._25[0]
Label: 28. WIZA GORUJI (Visa category / purpose)
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: 'BS1','IN','EX','TU','TR1','TR2','ST','DR','HL','PR1','PR2','OF','DP','WP','BS2','SP1','SP2','FM','HM'

Key:   topmostSubform[0].Page2[0]._39[0]
Label: Previous visit details
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._38[0]
Label: Previous visa number
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._37[0]
Label: Previous visa issue date
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._36[0]
Label: Previous visit year
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._50[0]
Label: Accompanying person details
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._49[0]
Label: Accompanying person details 2
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._48[0]
Label: Accompanying person details 3
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._47[0]
Label: Accompanying person details 4
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._46[0]
Label: Accompanying person name
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._45[0]
Label: Accompanying person nationality
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: ISO 3166-1 alpha-3 country codes

Key:   topmostSubform[0].Page2[0]._44[0]
Label: 32. Exit border crossing point
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: 'ASAP','ASAK','TABA','MRAP','DZAP','TBSA','SERA','SRHD','SRHA','FARAP','FRPA','FRPD', etc.

Key:   topmostSubform[0].Page2[0]._43[0]
Label: 31. Entry border crossing point
Type:  choiceList
Note:  NOT in PdfMappingHelper ⚠️
Valid: Same codes as _44[0]

Key:   topmostSubform[0].Page2[0]._42[0]
Label: Purpose of visit (free text)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._40[0]
Label: Planned arrival date
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0]._41[0]
Label: Planned departure date
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

Key:   topmostSubform[0].Page2[0].QRText[0]
Label: QR code data text (auto-generated)
Type:  textEdit
Note:  NOT in PdfMappingHelper — likely auto-populated by form script ⚠️

Key:   topmostSubform[0].Page2[0].Button1[0]
Label: Submit button
Type:  button
Note:  binding=none — not a data field, skip

Key:   topmostSubform[0].Page2[0]._27[0]
Label: Duration of stay (number)
Type:  textEdit
Note:  NOT in PdfMappingHelper ⚠️

==============================================================
CRITICAL FINDINGS FOR YOUR C# CODE
==============================================================

1. DATE FIELDS ARE TYPE "picture" NOT "dateTimeEdit"
   - _04[0] (DoB), _12[0] (IssueDate), _13[0] (ExpiryDate), _29[0], _28[0]
   - In Spire XFA these appear as XfaDateTimeField despite the XML type.
   - If they render blank, try casting to XfaTextField and setting .Value = "dd.MM.yyyy" string directly.

2. GENDER FIELD (_05) — send raw value, not display name
   - WRONG:  data["..._05[0]"] = "M"    ← this IS correct for gender (raw=display here)
   - But for MARITAL STATUS (_18) the raw values are '1','2','3','4' not the Turkmen labels.
   - Check: person.MaritalStatus.Name must equal '1'/'2'/'3'/'4' or Spire will not select anything.

3. URGENCY FIELD (L02) — same issue, raw values are '1','2','3' not 'ADATY'/'TIZ'/'ORAN TIZ'.

4. IMAGE FIELD is "imageEdit" type — confirmed at topmostSubform[0].Page1[0].ImageField1[0].

5. UNMAPPED FIELDS YOU MAY WANT TO ADD:
   - _02[0]   Patronymic
   - _07[0]   Citizenship (country code)
   - _06[0]   Country of birth (country code)
   - _14[0]   Country of residence (country code)
   - _17[0]   Profession
   - _19[0]   Education level
   - _10[0]   Document type
   - _25[0]   Visa category
   - _26[0]   Number of entries
   - _33[0]   Region of stay
   - _43[0]   Entry border point
   - _44[0]   Exit border point