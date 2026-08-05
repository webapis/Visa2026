---
title: Application items ekleme
slug: applications/add-items
locale: tr
tier: 4
guideStatus: draft
bo: ApplicationItem
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/overview
  - applications/create
  - employee/register
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
---

# Application items ekleme

Kayıtlı bir **application** üzerine kişi başına bir satır (**application item**) eklemeyi anlatır.

**İki rota:** bakanlık başlıkları **Applications (via ministry)** ve **Application items (ministry)**; doğrudan göç **Applications (direct migration)** ve **Application items (migration)**. Bkz. [Applications — bakanlık ve doğrudan göç](overview.md).

!!! tip "Ön koşullar"
    [Başvuru oluşturma](create.md) ve tam kişi kayıtları ([Çalışan kaydı](../employee/register.md), [Pasaport ekleme](../employee/add-passport.md)).

## Başlamadan önce

| Rota | Başlığı aç | Satırları ara |
|------|------------|---------------|
| Bakanlık | **Applications (via ministry)** | **Application items (ministry)** |
| Doğrudan göç | **Applications (direct migration)** | **Application items (migration)** |

Aynı kişi aynı başvuruda **yalnızca bir kez** yer alabilir.

## Adım 1 — Başvuruyu açın

Başlığı oluşturduğunuz **aynı listeden** açın:

- **Applications (via ministry)** — bakanlık rotası
- **Applications (direct migration)** — doğrudan göç rotası

**Full Application Number** ile detay formu açın.

## Adım 2 — Application items sekmesi

Detay formda **Application items** sekmesine geçin → **New**.

## Adım 3 — Person seçin ve kaydedin

**Person** seçin; **Current\*** alanları dolar (**Current Passport** zorunlu). Görünen alanları kontrol edin → **Save**.

Her ek kişi için **New** ile tekrarlayın.

## Bağımsız satır listeleri

Çok sayıda başvuruda satır aramak için:

- Bakanlık rotası → **Application items (ministry)**
- Doğrudan göç → **Application items (migration)**

## Sık sorunlar

| Sorun | Çözüm |
|-------|--------|
| Başvuru diğer listede | Yanlış rota — doğru başlık listesini kullanın |
| Kişi listede yok | Yanlış tür (çalışan/aile); kişi zaten ekli |
| **Current Passport** boş | Kişiye pasaport ekleyin |

## Sırada ne var

- [Applications — bakanlık ve doğrudan göç](overview.md)
- [Başvuru oluşturma](create.md)
- [Visa2026 ne sunar](../../about/capabilities.md)
