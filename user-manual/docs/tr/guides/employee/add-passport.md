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
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-passport.mp4
videoSource: recordings/passport-create-with-shots.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Çalışana pasaport ekleme

Bu kılavuz mevcut bir çalışana **pasaport** kaydı eklemeyi anlatır. Pasaportlar, çalışan ayrıntı formundaki **Passports** sekmesinden eklenir.

!!! tip "Ön koşul"
    Çalışan kaydı olmalıdır ([Yeni çalışan kaydı](register.md)).

!!! tip "Ekran görüntüleri"
    Görseller **İngilizce** arayüzden (sürüm **2026.08**).

## Video anlatımı

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/tr/person-add-passport.mp4"
  title="Çalışana pasaport ekleme"></video>

<p class="visa-manual-video-caption">Kayıt eğitim ortamından alınmıştır (test verisi). Aşağıdaki adımlar videoyla aynıdır.</p>

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

![Yeni pasaport formu](../../../assets/screenshots/v2026.08/tr/person-add-passport-step-02-passport-form-new.png)

## Adım 4 — Zorunlu alanlar

| Alan | Giriş |
|------|--------|
| **Passport Number** | Pasaport numarası |
| **Passport Type** | Listeden |
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
