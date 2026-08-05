---
title: Kişi bulma ve açma
slug: person/open-and-search
locale: tr
tier: 1
guideStatus: draft
bo: Person
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-open-search.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Kişi bulma ve açma

Bu kılavuz Visa2026'da mevcut bir kişiyi bulmayı ve kaydını açmayı anlatır. İşlem bitince listeden bir çalışanı (veya başka bir kişi türünü) açıp **ayrıntı formunu** okuyabilirsiniz.

!!! tip "Ön koşullar"
    [Giriş](../../getting-started/login.md) yapın ve kabuğu bilin ([Ana gezinme](../../getting-started/navigation.md)).

!!! tip "Ekran görüntüleri"
    Görseller **İngilizce** arayüzden (sürüm **2026.08**). Etiketler dilinize göre değişir; adımlar aynıdır.

## Video anlatımı

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/tr/person-open-search.mp4"
  title="Kişi bulma ve açma"></video>

<p class="visa-manual-video-caption">Kayıt eğitim ortamından alınmıştır (test verisi). Aşağıdaki adımlar videoyla aynıdır.</p>

## Başlamadan önce

| Gereken | Not |
|---------|-----|
| Kişi listelerinde okuma yetkisi | Menü yoksa amirinize sorun |
| En az bir arama ipucu | Ad, **Personal Number** veya pasaport numarası |

Kişiler türe göre ayrı listelerde tutulur. Doğru listeyi seçin.

| Sol menü | İçerik |
|----------|--------|
| **Employees** | Şirket çalışanları |
| **Family Members** | Çalışanlara bağlı aile üyeleri |
| **Temporary visitor** | Geçici ziyaretçiler |

## Adım 1 — Doğru listeyi açın

1. Visa2026'ya giriş yapın.
2. Sol menüden **Employees** (veya **Family Members** / **Temporary visitor**) seçin.
3. Listenin yüklenmesini bekleyin.

Tabloda **Full Name**, **Personal Number**, **Date Of Birth** gibi sütunlar görünür. İlk sütunlarda **Dossier** ve **Copies** kısayolları olabilir; bu kılavuz standart ayrıntı formunu açmayı anlatır.

![Çalışanlar listesi](../../assets/screenshots/v2026.08/tr/navigation-step-03-employees-list.png)

## Adım 2 — Listede arama

Liste uzunsa araç çubuğundaki **arama** alanını kullanın (veya düzeninizde **Search** / **Ara** düğmesini açın).

1. Arama alanına tıklayın.
2. **First Name**, **Last Name**, **Personal Number** veya kişinin herhangi bir pasaport **Passport Number** değerinin bir kısmını yazın.
3. **Enter** tuşuna basın veya listenin yenilenmesini bekleyin.

İpuçları:

- Birden fazla kelime sonucu daraltır.
- Vurgulu harfler genelde vurgusuz eşleşir (`u` ile `ü` bulunabilir).
- Arama kutusunu temizleyerek tüm listeyi görebilirsiniz.
4. Meslektaşınız kayıt güncellediyse **Refresh** / **Yenile** seçin.

!!! note "Hangi alanlar aranır"
    Liste araması **First Name**, **Middle Name**, **Last Name**, **Personal Number** ve **Passports** sekmesindeki pasaport numaralarına bakar.

## Adım 3 — Ayrıntı formunu açın

1. Filtrelenmiş listede doğru **Full Name** ve **Personal Number** satırını bulun.
2. Satıra tıklayın (veya çift tıklayın).
3. **Ayrıntı formunun** ana alanda açılmasını bekleyin.

**Passports**, **Educations** gibi sekmeleri okuyabilirsiniz. Değişiklik için düzenleyip **Save** / **Kaydet** kullanın — bkz. [Yeni çalışan kaydı](register.md).

![Çalışan ayrıntı formu](../../assets/screenshots/v2026.08/tr/navigation-step-04-detail-form.png)

!!! success "Kişi açıldı"
    Ayrıntı formunda beklenen **Personal Number** ve ad görünüyorsa doğru kişiyi buldunuz.

## Alternatif — Rapor panosunda Person search

Ana **Report Dashboard** üzerinden tüm kişi türlerinde arama yapabilirsiniz:

1. **Report Dashboard**'u açın.
2. **Person search** kategorisini seçin.
3. Kategori çiplerinin yanındaki arama kutusuna ad, **Personal Number** veya pasaport numarası yazın.
4. Sonuç tablosunu ve grafiği inceleyin.
5. Satıra tıklayınca **person dossier** (salt okunur özet) açılır — düzenlenebilir ayrıntı formu değildir.

Kişi türünü biliyorsanız sol menü listelerini kullanın. Türden emin değilseniz **Person search** kullanın.

Düzenleme için doğru listeyi açıp adım 3 ile **ayrıntı formunu** açın.

## Listeden dossier (kısayol)

**Employees**, **Family Members** ve **Temporary visitor** listelerinde **Dossier** sütunu, **Person search** ile aynı salt okunur dosyayı açar.

## Sık sorunlar

| Sorun | Ne yapmalı |
|-------|------------|
| Liste boş | Aramayı temizleyin; **Refresh**; doğru menü öğesini kontrol edin |
| Çok fazla satır | Daha fazla ad veya tam **Personal Number** yazın |
| Kişi yok | **Report Dashboard** → **Person search**; **Family Members** veya **Temporary visitor** deneyin |
| Dossier açıldı | **Dossier** veya **Person search** kullandınız — ayrıntı formu için satırın kendisine tıklayın |

## Sırada ne var

- [Ana gezinme](../../getting-started/navigation.md)
- [Yeni çalışan kaydı](register.md)
- [Pasaport ekleme](add-passport.md)
- [İş nesneleri referansı](../../reference/business-objects.md)
