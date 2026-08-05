---
title: Şablon rapor paketi (Resminamalar)
slug: applications/resminamalar
locale: tr
tier: 5
guideStatus: draft
bo: Application
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/create
  - applications/add-items
screenshotsVersion: "2026.08"
videoStorage: static
videoFile: application-resminamalar.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Şablon rapor paketi (Resminamalar)

**Templates** (Resminamalar) — Word/Excel kullanıcı şablonlarından rapor paketi: katalog, hazırlık, önizleme, ZIP.

Ministry **PDF** için [Belge kopyaları](document-copies.md) kullanın.

!!! tip "Ön koşullar"
    [Başvuru oluşturma](create.md), [Application items ekleme](add-items.md).

## İki giriş noktası

| Kapsam | Nereden |
|--------|---------|
| Başvuru | Application detay → **Templates** |
| Kişi satırları | Application items listesi → satır seç → **Templates** (aynı başvuru) |

## Adımlar

1. **Templates** → sağ önizleme paneli.
2. Katalog — onay kutusu, **Ready** / **Check**, **Preview**.
3. İşaretli şablonlar → **Download package** → onay (Check uyarısı).
4. **Report generation** toast → ZIP indir.
5. Veri değişince **Refresh**.

## Sık sorunlar

| Sorun | Çözüm |
|-------|--------|
| Boş katalog | Yanlış kapsam veya tür — yöneticiye danışın |
| PDF ile karışıklık | PDF = **Document copies**; Word/Excel = **Templates** |

## Sırada ne var

- [Belge kopyaları](document-copies.md)
- [Visa2026 ne sunar](../../about/capabilities.md)