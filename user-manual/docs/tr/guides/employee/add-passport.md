---
title: Çalışana pasaport ekleme
slug: employee/add-passport
locale: tr
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-05T08:48:03.3957272Z"
mediaE2eRunId: "20260805-134241"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: true
verifiedAt: "2026-08-05T08:49:33.7498506Z"
verifiedCommit: "2d70b13c"
---

# Çalışana pasaport ekleme

Bu kılavuz mevcut bir çalışana **pasaport** kaydı eklemeyi anlatır. Pasaportlar, çalışan ayrıntı formundaki **Passports** sekmesinden eklenir.

!!! tip "Ön koşul"
    Çalışan kaydı olmalıdır ([Yeni çalışan kaydı](register.md)).

!!! tip "Ekran görüntüleri"
    Görseller **İngilizce** arayüzden (sürüm **2026.08**).

## Adım 1 — Çalışanı açın

1. **Employees** listesinden çalışanı açın.
2. **Passports** sekmesinin göründüğü ayrıntı formunu bekleyin.

![Çalışan ayrıntı formu](../../../assets/screenshots/v2026.08/tr/person-add-passport-step-01-employee-detail.png)

## Adım 2 — Passports sekmesi

1. **Passports** sekmesini seçin.
2. İç listede **New Passport** düğmesini bulun.

## Adım 3 — Yeni pasaport

1. **New Passport** seçin.
2. Pasaport ayrıntı formunun açılmasını bekleyin.

!!! tip "Varsayılan pasaport türü"
    **Passport Type** alanı varsayılan olarak **P — Ulusal pasaport** ile gelir (memurların en sık kullandığı tür). Belge farklı bir türdeyse listeden değiştirin.

![Yeni pasaport formu](../../../assets/screenshots/v2026.08/tr/person-add-passport-step-02-passport-form-new.png)

## Adım 4 — Zorunlu alanlar

| Alan | Giriş |
|------|--------|
| **Passport Number** | Pasaport numarası |
| **Passport Type** | Genelde zaten **P — Ulusal pasaport**; gerekirse listeden değiştirin |
| **Issue Date** | Tarih |
| **Expiration Date** | Tarih |
| **Authority** | Metin |
| **Issued Country** | Listeden |

![Doldurulmuş pasaport formu](../../../assets/screenshots/v2026.08/tr/person-add-passport-step-03-passport-fields-filled.png)

## Adım 5 — Kaydet

1. **Save** seçin.
2. **Passport Number** değerini doğrulayın.

![Kaydedilmiş pasaport](../../../assets/screenshots/v2026.08/tr/person-add-passport-step-04-passport-saved.png)

## Sırada ne var

- [Çalışan kaydı](register.md)
- [Ana gezinme](../../getting-started/navigation.md)
