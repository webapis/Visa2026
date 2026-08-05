---
title: Pasaporta vize ekleme
slug: employee/add-visa
locale: tr
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/add-passport
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-visa.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Pasaporta vize ekleme

Bu kılavuz mevcut bir **pasaporta** **vize** kaydı eklemeyi anlatır. Vizeler pasaport ayrıntı formundaki **Visas** sekmesinden eklenir.

!!! tip "Ön koşul"
    Çalışanın kayıtlı bir **pasaportu** olmalıdır ([Pasaport ekleme](add-passport.md)).

## Video anlatımı

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/tr/person-add-visa.mp4"
  title="Pasaporta vize ekleme"></video>

## Adım 1 — Pasaportu açın

1. Çalışanı açın ([Arama](../person/open-and-search.md)).
2. **Passports** sekmesinden pasaport satırını açın.

![Pasaport ayrıntı formu](../../../assets/screenshots/v2026.08/tr/person-add-visa-step-01-passport-detail.png)

## Adım 2 — Visas sekmesi

1. Pasaport formunda **Visas** sekmesini seçin.
2. **New Visa** düğmesine tıklayın.

![Yeni vize formu](../../../assets/screenshots/v2026.08/tr/person-add-visa-step-02-visa-form-new.png)

## Adım 3 — Zorunlu alanlar

| Alan | Açıklama |
|------|----------|
| **Visa Number** | Vize numarası |
| **Visa Type** / **Visa Category** / **Visa Issued Place** | Listeden seçin |
| **Issue Date**, **Start Date**, **Expiration Date** | Tarihler |
| **Border Zone Location** | Sınır bölgesi (çoklu seçim) |

![Doldurulmuş vize formu](../../../assets/screenshots/v2026.08/tr/person-add-visa-step-03-visa-fields-filled.png)

## Adım 4 — Kaydet

1. **Save** seçin.
2. **Visas** sekmesinde satırı doğrulayın.

![Kaydedilmiş vize](../../../assets/screenshots/v2026.08/tr/person-add-visa-step-04-visa-saved.png)

## Sıradaki

- [Eğitim kaydı ekleme](../employee/add-education.md)
- [Pasaport ekleme](add-passport.md)