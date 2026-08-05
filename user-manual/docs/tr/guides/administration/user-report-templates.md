---
title: Kullanıcı rapor şablonları
slug: administration/user-report-templates
locale: tr
tier: 7
guideStatus: draft
bo: UserReportTemplate
navPath: Reports / User Report Template
roles: [Administrator]
prerequisiteSlugs:
  - applications/resminamalar
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: user-report-templates.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_REPORT_PACKAGE.md
---

# Kullanıcı rapor şablonları

**Yöneticiler** için: **Reports → User Report Template** altında Word/Excel düzenlerini saklama. Memurlar bunları başvuruda **Templates** (Resminamalar) kataloğunda görür.

## Özet adımlar

1. **Reports → User Report Template** listesini açın.
2. **New** ile kayıt oluşturun — **Template Name**, **Template File** yükleyin.
3. **Output Format**, **Root Business Object**, **Excel Merge Mode** (Excel ise) ayarlayın.
4. **Applicable Application Types** / gruplar / sözleşmeler ve **Is Active** ile görünürlüğü belirleyin.
5. **Extract Placeholders** → **Validate Placeholders**.
6. **Placeholder manual** ile izin verilen kodları kopyalayın.
7. Test başvurusunda **Templates** ile doğrulayın.

Masaüstü düzenleme: [Şablonu düzenle ve senkronize et](template-staging.md).

Memur akışı: [Resminamalar](../applications/resminamalar.md).

## Sık sorunlar

| Sorun | Çözüm |
|-------|--------|
| Katalogda yok | **Is Active**; uygulama türü/grup eşleşmesi |
| Geçersiz yer tutucular | Dosyayı düzeltin; yeniden **Extract** / **Validate** |
| Yanlış kapsam | **Root Business Object** ve memurun açtığı ekran uyumlu olmalı |