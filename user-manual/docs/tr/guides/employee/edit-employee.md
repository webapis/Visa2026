---
title: Çalışan bilgilerini güncelleme
slug: employee/edit-employee
locale: tr
tier: 3
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
  - employee/register
screenshotsVersion: "2026.08"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Çalışan bilgilerini güncelleme

Bu kılavuz mevcut bir **çalışan** kaydındaki alanları değiştirip kaydetmeyi anlatır. İşlem bitince yeni değerler **ayrıntı formunda** ve **Çalışanlar** listesinde görünür.

!!! tip "Ön koşullar"
    [Kişi bulma ve açma](../person/open-and-search.md) ve kayıtlı çalışan ([Yeni çalışan kaydı](register.md)).

!!! tip "Ekran görüntüleri"
    Görseller **İngilizce** arayüzden (sürüm **2026.08**).

## Başlamadan önce

| Gereken | Not |
|---------|-----|
| **Employees** üzerinde düzenleme yetkisi | Alanlar salt okunursa amirinize sorun |
| Doğru çalışanın ayrıntı formu açık | [Kişi bulma ve açma](../person/open-and-search.md) |
| Hassas değişiklik onayı | **Personal Number** veya sözleşme değişimi ofis kuralına bağlı |

Bu kılavuz çalışan üst bilgisindeki alanları günceller. Pasaport veya eğitim sekmeleri için ayrı kılavuzlara bakın.

## Adım 1 — Ayrıntı formunu açın

1. Giriş yapın.
2. **Employees** listesini açın.
3. Gerekirse arayın ([Kişi bulma ve açma](../person/open-and-search.md)).
4. Satıra tıklayın.

**Personal Number** ve **Full Name** doğru kişiye ait olmalı.

![Düzenleme için ayrıntı formu](../../../assets/screenshots/v2026.08/tr/person-edit-employee-step-01-detail-form.png)

## Adım 2 — Alanları değiştirin

Sık güncellenen alanlar:

| Alan | Ne zaman |
|------|----------|
| **Foreign Address** | Yurtdışı adres değişti |
| **Foreign Address Country** | Adres ülkesi değişti |
| **Email** | İletişim e-postası (isteğe bağlı — adım 3) |
| **Project Contract** | Sözleşme değişimi |
| **Company (Subcontractor)** | Alt yüklenici değişimi |
| **Marital Status** | Medeni hal güncellemesi |
| **Hire Date** | İşe giriş tarihi (isteğe bağlı) |

!!! warning "Personal Number"
    **Personal Number** birçok süreçte ana anahtardır. Yalnızca amiriniz onaylarsa değiştirin. Yinelenen numara **Save** hatası verir.

## Adım 3 — İsteğe bağlı alanlar (dişli)

1. Formun üstünde **Show optional fields** / **İsteğe bağlı alanları göster** (dişli) seçin.
2. **Middle Name**, **Email**, **Photo**, **Hire Date** gibi alanlar görünür.
3. Düzenleyin.
4. **Hide optional fields** ile formu kısaltın.

## Adım 4 — Kaydedin

1. Değişiklikleri gözden geçirin.
2. **Save** / **Kaydet** seçin.
3. Listeye dönmek için **Save and Close** / **Kaydet ve Kapat** kullanabilirsiniz.

![Kayıttan sonra ayrıntı formu](../../../assets/screenshots/v2026.08/tr/person-edit-employee-step-02-after-save.png)

## Adım 5 — Listede doğrulayın

1. **Employees** listesinde **Personal Number** ile bulun.
2. Satırı açıp değişiklikleri kontrol edin.
3. Gerekirse **Refresh** / **Yenile**.

!!! success "Güncelleme tamam"
    Ayrıntı formu ve liste yeni değerleri gösteriyorsa işlem başarılıdır.

## Bu kılavuzun kapsamı dışında

| Konu | Kılavuz |
|------|---------|
| Yeni çalışan | [Yeni çalışan kaydı](register.md) |
| Pasaport | [Pasaport ekleme](add-passport.md) |
| Eksik işareti | Yakında |

## Sık sorunlar

| Sorun | Çözüm |
|-------|--------|
| Salt okunur alanlar | Yetki — amirinize sorun |
| Yinelenen **Personal Number** | Numarayı geri alın veya onay alın |
| **Show optional fields** yok | Formun üstüne kaydırın |

## Sırada ne var

- [Kişi bulma ve açma](../person/open-and-search.md)
- [Pasaport ekleme](add-passport.md)
- [Ana gezinme](../../getting-started/navigation.md)
