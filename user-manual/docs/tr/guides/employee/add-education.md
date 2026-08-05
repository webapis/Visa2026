---
title: Eğitim kaydı ekleme
slug: employee/add-education
locale: tr
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-education.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Eğitim kaydı ekleme

Çalışan ayrıntı formundaki **Educations** sekmesinden **eğitim** kaydı ekleyin.

!!! tip "Ön koşul"
    Çalışan kaydı olmalıdır ([Kayıt](register.md)).

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/tr/person-add-education.mp4"
  title="Eğitim kaydı ekleme"></video>

## Adımlar

1. Çalışanı açın → **Educations** sekmesi.
2. **New Education** → formu doldurun:
   - **Education Institution**, **Specialty** (arama + listeden; yoksa **New** — yinelenen kurum eklemeyin)
   - **Education Level**, **Education Country** (varsayılanları kontrol edin)
3. **Save** → listede doğrulayın.

![Çalışan formu](../../../assets/screenshots/v2026.08/tr/person-add-education-step-01-employee-detail.png)

## Sıradaki

- [Tıbbi kayıt](add-medical-record.md) · [Vize](add-visa.md)