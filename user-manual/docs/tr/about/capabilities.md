---
title: Visa2026 ne sunar
slug: about/capabilities
locale: tr
tier: 0
guideStatus: published
---

# Visa2026 ne sunar

Visa2026, Türkmenistan göç işlemlerinde yabancı **çalışanlar**, **aile üyeleri** ve **geçici ziyaretçiler** için vize dairesinin operasyonel sistemidir.

Aşağıdaki liste **önem sırasına** göredir — günlük kullanımdan destekleyici araçlara. Her madde çözdüğü **sorunu** ve uygulamada ne yaptığınızı özetler.

!!! tip "Nasıl yapılır kılavuzları"
    Bu sayfa **neden** var — adım adım yönergeler **Kılavuzlar** bölümündedir (*yakında* işaretli olanlar). Yayımlama durumu için [Kılavuz yol haritası](roadmap.md).

---

## 1. Report Dashboard

**Sorun:** Hangi vize, pasaport, kayıt veya başvurunun işlem gerektirdiği tek bakışta görülemiyor — iş geç fark ediliyor.

**Visa2026 ne yapar:** Oturum açınca **Report Dashboard** — kategori grafikleri (vize, pasaport, kayıt, çalışma izni, seyahat, eksik kişiler, kişi arama vb.). Grafik parçasına tıklayarak filtrelenmiş listeye veya Excel'e geçin.

**Kılavuz:** [Rapor Panosu](../guides/tracking/report-dashboard.md) — bkz. [Ana gezinme](../getting-started/navigation.md)

---

## 2. Kişi ana verileri

**Sorun:** Bir yabancı işçinin birçok belgesi (pasaport, vize, tıbbi kayıt, adres, eğitim, seyahat…) dağınık — tutarlı kayıt zor.

**Visa2026 ne yapar:** Her kişiyi **Employees**, **Family Members** veya **Temporary visitor** altında bir kez saklayın; iç içe sekmelerde pasaport, vize vb. Başvuru satırları doğru **güncel** belgeleri otomatik çeker.

**Kılavuzlar:** [Kişi bulma](../guides/person/open-and-search.md) · [Çalışan kaydı](../guides/employee/register.md) · [Aile üyesi](../guides/family-member/register.md) · [Geçici ziyaretçi](../guides/temporary-visitor/register.md)

---

## 3. Applications ve application items

**Sorun:** Davetiye, vize+çalışma izni, uzatma, kayıt giriş/çıkış, sınır bölgesi, iş seyahati, iptal vb. tek dosyada izlenmiyor.

**Visa2026 ne yapar:** **Application** (tür, sözleşme, tarihler) oluşturun ve **application items** ekleyin — kişi başına bir satır. Ayrı menü: **Applications (via ministry)** ve **Applications (direct migration)**.

**Kılavuzlar:** [Applications — bakanlık ve doğrudan göç](../guides/applications/overview.md) · [Başvuru oluşturma](../guides/applications/create.md) · [Application items ekleme](../guides/applications/add-items.md) — önce kişi kayıtları tamamlanmalı.

---

## 4. Application progress

**Sorun:** "Dosya nerede?" ve "bakanlıkta ne kadar süredir?" e-posta veya defterde — ortak zaman çizelgesi yok.

**Visa2026 ne yapar:** **Application progress** satırları — her **state** bir iş akışı adımı (hazırlık, bakanlık incelemesi, işlem başladı, verildi, reddedildi…). Bakanlık adımlarında onaylayan bakanlık kısa adı görünebilir. Son satır güncel durumdur; karar mektubu eklenebilir.

**Kılavuz:** [Başvuru ilerlemesini izleme](../guides/applications/progress.md)

---

## 5. Document copies (bakanlık PDF paketi)

**Sorun:** Eksik tarama veya form boşluğu paket kuyruğunda ortaya çıkıyor.

**Visa2026 ne yapar:** **Application item** listesinde **Document copies** — belge başına **hazırlık**, önizleme, boşluk onayı, ardından **PDF paketi** (doldurulmuş formlar + taramalar).

**Kılavuz:** [Bakanlık belge kopyaları (PDF paketi)](../guides/applications/document-copies.md)

---

## 6. State notifications

**Sorun:** Süresi dolan belgeler ve eksik taramalar kayıt açılınca fark ediliyor.

**Visa2026 ne yapar:** *(Planlandı, memur sürümünde yok.)* Üst **zil** ve **Operations → State notifications** gelen kutusu planlanmıştı; ürünleştirilmiyor.

**Kılavuz:** *Ertelendi* — [Rapor Panosu](../guides/tracking/report-dashboard.md) ve [Eksik işaretleme](../guides/person/mark-incomplete.md).

---

## 7. Templates (rapor paketleri)

**Sorun:** Ön yazı, bakanlık mektupları ve Word/Excel raporları elle birleştiriliyor.

**Visa2026 ne yapar:** **Application** veya **application item** üzerinde **Templates** — katalog, hazırlık, önizleme, ZIP indirme.

**Kılavuz:** [Şablon rapor paketi (Resminamalar)](../guides/applications/resminamalar.md). Yöneticiler: [Kullanıcı rapor şablonları](../guides/administration/user-report-templates.md) · [Şablonu düzenle ve senkronize et](../guides/administration/template-staging.md).

---

## 8. Person dossier

**Sorun:** Yönetici "bu çalışan hakkında her şey" deyince birçok ekrandan yeniden toplanıyor.

**Visa2026 ne yapar:** **Report Dashboard → Person search** veya listedeki **Dossier** sütunu — salt okunur **360°** görünüm; isteğe bağlı müdür dışa aktarımı (HTML/PDF + belge ZIP).

**Kılavuz:** [Kişi dosyası (dossier)](../guides/person/dossier.md) — giriş noktaları ayrıca [Kişi bulma](../guides/person/open-and-search.md) kılavuzunda.

---

## 9. Mark incomplete / Incomplete persons

**Sorun:** Eski sistemden geçişten sonra "hâlâ düzeltiliyor" ofis genelinde işaretlenemiyor.

**Visa2026 ne yapar:** **Mark incomplete** / **Mark complete** (notlar ve eksik alan kutuları). **Report Dashboard → Incomplete persons** — ofis listesi.

**Kılavuz:** [Eksik işaretleme](../guides/person/mark-incomplete.md)

---

## 10. Person document copies

**Sorun:** Taramalar birçok sekmede dağınık — bakanlık öncesi tek tek açılıyor.

**Visa2026 ne yapar:** Kişi formunda **Document copies** (veya listede **Copies**) — tüm sekmelerden önizleme paneli. Bakanlık ZIP için **application item** üzerindeki **Document copies** kullanılır.

**Kılavuz:** [Kişi belge kopyaları](../guides/person/document-copies.md). Bakanlık ZIP: [Başvuru belge kopyaları](../guides/applications/document-copies.md).

---

## 11. Yapılandırma (ofis ayarları)

**Sorun:** Şirket bilgisi, numaralandırma, bakanlık rotaları, SLA ve yükleme sınırları dağınık.

**Visa2026 ne yapar:** **Configuration** menüsü (**VisaOffice** / yönetici) — şirket, sözleşme, SLA, son kullanma uyarıları, yükleme limitleri.

**Kılavuzlar:** [Genel bakış](../guides/administration/configuration/overview.md) · [Kuruluş](../guides/administration/configuration/organization.md) · [Sözleşmeler](../guides/administration/configuration/contracts-and-approvals.md) · [SLA](../guides/administration/configuration/sla.md) · [Uyarılar](../guides/administration/configuration/alerts-and-upload-limits.md)

---

## Bu kılavuz kimler için

| Rol | Tipik kullanım |
|-----|----------------|
| **Visa Officer** | Kişi kayıtları, başvurular, belge paketleri, dashboard |
| **Visa Chief / amir** | Dashboard, dossier, Excel, eksik kişiler |
| **Administrator** | [Yapılandırma](../guides/administration/configuration/overview.md), [Kullanıcı rapor şablonları](../guides/administration/user-report-templates.md), [Şablon senkronizasyonu](../guides/administration/template-staging.md), roller |

## Öğrenmeye başlayın

1. [Oturum açma](../getting-started/login.md)  
2. [Ana gezinme](../getting-started/navigation.md)  
3. [Kişi bulma](../guides/person/open-and-search.md)  
4. [Çalışan kaydı](../guides/employee/register.md)
