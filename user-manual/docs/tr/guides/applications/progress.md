---
title: Başvuru ilerlemesini izleme
slug: applications/progress
locale: tr
tier: 4
guideStatus: draft
bo: ApplicationProgress
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/overview
  - applications/create
  - applications/add-items
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
---

# Başvuru ilerlemesini izleme

**Application progress** — başvurunun iş akışındaki adımların kronolojik kaydı. Her hareket için **yeni satır** eklenir; mevcut adım yerinde düzenlenmez.

!!! tip "Ön koşullar"
    [Applications özeti](overview.md), [Başvuru oluşturma](create.md), [Application items ekleme](add-items.md).

## Başlamadan önce

| Kavram | Anlam |
|--------|--------|
| Boş geçmiş | Dosya ofiste **hazırlanıyor** kabul edilir |
| **State** | Adım (bakanlık incelemesi, onay, işlem başladı, verildi, reddedildi…) |
| Güncel durum | **Progress history** içindeki son satır |

| Rota | İlk satır genelde |
|------|-------------------|
| **Applications (via ministry)** | İlk bakanlık incelemesine gönderildi |
| **Applications (direct migration)** | Göç müdürlüğünde işlem başladı |

## Adımlar

1. Başvuruyu doğru listeden açın (via ministry veya direct migration).
2. **Progress** sekmesi → **Progress history**.
3. **New** → **State** (yalnızca izin verilen sonraki adımlar) ve **Date**.
4. Gerekirse **Description**, **Process number**, **Ministry letter** (bakanlık kararı).
5. **Save** — liste **Status** / **Status date** güncellenir.

## Tipik akış

- **Bakanlık rotası:** ofis (örtük) → bakanlık satırları → göç → verildi/reddedildi
- **Doğrudan göç:** ofis (örtük) → işlem başladı → verildi/reddedildi

## Sık sorunlar

| Sorun | Çözüm |
|-------|--------|
| Kayıt olmuyor | **Project Contract** eksik; yanlış rota |
| **State** seçilemiyor | Son adım terminal — amire danışın |
| Başlık kilitli | Ofis hazırlığından sonra veya terminal durum — normal |

## Sırada ne var

- [Applications özeti](overview.md)
- **Document copies** — *yakında*
- [Visa2026 ne sunar](../../about/capabilities.md)