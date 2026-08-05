---
title: Şablonu düzenle ve veritabanına senkronize et
slug: administration/template-staging
locale: tr
tier: 7
guideStatus: draft
bo: UserReportTemplate
navPath: Templates (Resminamalar)
roles: [Administrator]
prerequisiteSlugs:
  - administration/user-report-templates
  - applications/resminamalar
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/TEMPLATE_STAGING_EDIT.md
---

# Şablonu düzenle ve veritabanına senkronize et

**Templates** panelinden masaüstü Word/Excel ile düzenleme ve **Sync to database** ile yükleme.

## Özet adımlar

1. Başvuruda **Templates** açın.
2. Bir kez: **Change template folder** — seçicide `%LOCALAPPDATA%\Visa2026\TemplateEdit`
3. **Dişli** ile **Edit template** gösterin.
4. Satırda **Edit template** → Word/Excel’de düzenleyin, kaydedin, kapatın.
5. **Sync to database** — Word kapalı olmalı.
6. **Refresh** ve **Preview** ile doğrulayın.

Ayrıntılı kayıt ayarları: [Kullanıcı rapor şablonları](user-report-templates.md).

**Refresh** veri yenilemez; içe aktarma yalnızca **Sync to database**.