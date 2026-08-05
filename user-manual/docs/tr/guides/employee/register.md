---
title: Yeni çalışan kaydı
slug: employee/register
locale: tr
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-05T08:48:03.3957272Z"
mediaE2eRunId: "20260805-134241"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: true
verifiedAt: "2026-08-05T08:49:33.7498506Z"
verifiedCommit: "2d70b13c"
---

# Yeni çalışan kaydı

Bu kılavuz Visa2026'da yeni bir **çalışan** (person) kaydı oluşturmayı anlatır. İşlem bitince kayıt **Çalışanlar** listesinde görünür ve ayrıntı formunu açabilirsiniz.

!!! tip "Ön koşullar"
    [Giriş](../../getting-started/login.md) yapın ve sol menüyü bilin ([Ana gezinme](../../getting-started/navigation.md)).

!!! tip "Ekran görüntüleri"
    Görseller **İngilizce** arayüzden (sürüm **2026.08**). Etiketler dilinize göre değişir; adımlar aynıdır.

## Başlamadan önce

| Gereken | Not |
|---------|-----|
| **Çalışanlar** üzerinde oluşturma yetkisi | **Yeni** yoksa amirinize sorun |
| Benzersiz **Personal Number** | Format ofis kurallarınıza göre |
| Sözleşme ve alt yüklenici listeleri | Formdaki açılır listelerden seçin |

## Adım 1 — Çalışanlar listesini açın

1. Visa2026'ya giriş yapın.
2. Sol menüden **Employees** (Çalışanlar) öğesini seçin.
3. Listenin yüklenmesini bekleyin.

Araç çubuğunda **New** ve **Refresh** görünmelidir.

![Çalışanlar listesi](../../../assets/screenshots/v2026.08/tr/person-register-step-01-employees-list.png)

## Adım 2 — Yeni çalışan başlatın

1. **Employees** listesinde **New** seçin.
2. Çalışan **detail form** açılana kadar bekleyin.

Boş bir çalışan kaydındasınız. Zorunlu alanlar formda ve aşağıdaki tabloda gösterilir.

## Adım 3 — Zorunlu alanları doldurun

| Alan | Giriş |
|------|--------|
| **First Name** | Ad |
| **Last Name** | Soyad |
| **Personal Number** | Benzersiz personel numarası |
| **Date Of Birth** | Tarih |
| **Birth Place** | Metin |
| **Country Of Birth** | Listeden |
| **Gender** | Listeden |
| **Marital Status** | Listeden |
| **Nationality** | Listeden |
| **Foreign Address** | Adres metni |
| **Foreign Address Country** | Listeden |
| **Project Contract** | Aktif sözleşme |
| **Company (Subcontractor)** | Alt yüklenici şirket |

**Company (Subcontractor)** kiracı katalogdur — formdan **New** ile eklenebilir. **New** öncesi arayın; aynı şirketi iki kez eklemeyin. **Project Contract** yalnızca listeden seçilir (VisaOffice / Yapılandırma).

!!! note "Liste alanları"

## Adım 4 — Kaydedin

1. Değerleri kontrol edin.
2. **Save** seçin.
3. Kaydın tamamlanmasını bekleyin.

Hata durumunda mesajı okuyun; *already uses this personal number* için başka **Personal Number** kullanın.

![Kayıt sonrası ayrıntı formu](../../../assets/screenshots/v2026.08/tr/person-register-step-02-saved-detail.png)

## Adım 5 — Listede doğrulayın

1. Sol menüden tekrar **Employees** açın (veya **Save and Close**).
2. **Personal Number** satırını bulun.
3. Satırı açarak **First Name**, **Last Name** ve **Personal Number** değerlerini kontrol edin.

![Listeden açılan kayıt](../../../assets/screenshots/v2026.08/tr/person-register-step-03-open-from-list.png)

## Sırada ne var

- [Ana gezinme](../../getting-started/navigation.md)
- [Pasaport ekleme](add-passport.md)
- [İş nesneleri referansı](../../reference/business-objects.md)
