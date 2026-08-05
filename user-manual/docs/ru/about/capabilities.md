---
title: Что делает Visa2026
slug: about/capabilities
locale: ru
tier: 0
guideStatus: published
---

# Что делает Visa2026

Visa2026 — операционная система визового отдела для иностранных **сотрудников**, **родственников** и **временных посетителей** в процедурах миграции Туркменистана.

Список упорядочен по **важности** — от ежедневной работы к вспомогательным инструментам. Для каждой возможности указана **проблема** и действия в приложении.

!!! tip "Пошаговые инструкции"
    Эта страница объясняет **зачем** нужна каждая область. Шаги — в разделе **Инструкции** (или *скоро*). Статус публикации: [Дорожная карта](roadmap.md).

---

## 1. Report Dashboard

**Проблема:** Не видно с первого взгляда, какие визы, паспорта, регистрации или заявки требуют действий.

**Что делает Visa2026:** После входа — **Report Dashboard**: диаграммы по категориям (виза, паспорт, регистрация, разрешение на работу, поездки, неполные персоны, поиск и др.). Клик по сегменту — фильтрованный список или Excel.

**Руководство:** [Панель отчётов](../guides/tracking/report-dashboard.md) — [Навигация](../getting-started/navigation.md)

---

## 2. Основные данные о человеке

**Проблема:** У одного работника много документов (паспорт, виза, медосмотр, адрес, образование, поездки…) — сложно держать в порядке.

**Что делает Visa2026:** Каждый человек один раз в **Employees**, **Family Members** или **Temporary visitor**; вложенные вкладки. Строки заявки автоматически подставляют **актуальные** документы.

**Руководства:** [Поиск человека](../guides/person/open-and-search.md) · [Регистрация сотрудника](../guides/employee/register.md) · [Родственник](../guides/family-member/register.md) · [Временный посетитель](../guides/temporary-visitor/register.md)

---

## 3. Applications и application items

**Проблема:** Приглашения, виза+разрешение, продление, регистрация, погранзона, командировка, отмена — нет единого дела на запрос.

**Что делает Visa2026:** **Application** (тип, контракт, даты) + **application items** — строка на человека. Отдельное меню: **Applications (via ministry)** и **Applications (direct migration)**.

**Руководства:** [Applications — через министерство и прямая миграция](../guides/applications/overview.md) · [Создание заявки](../guides/applications/create.md) · [Добавление application items](../guides/applications/add-items.md) — сначала заполните данные о человеке.

---

## 4. Application progress

**Проблема:** «Где дело?» и «сколько в министерстве?» — в почте или блокноте, без общей шкалы времени.

**Что делает Visa2026:** Строки **application progress** — каждый **state** фиксирует шаг workflow. Последняя строка — текущий статус; на шагах решения министерства можно прикрепить письмо.

**Руководство:** [Отслеживание прогресса заявки](../guides/applications/progress.md)

---

## 5. Document copies (пакет PDF для министерства)

**Проблема:** Недостающие сканы обнаруживаются при постановке ZIP в очередь.

**Что делает Visa2026:** **Document copies** в списке **application item** — **готовность**, предпросмотр, подтверждение пробелов, затем **PDF-пакет**.

**Руководство:** [Копии документов для министерства (PDF-пакет)](../guides/applications/document-copies.md)

---

## 6. State notifications

**Проблема:** Истекающие документы замечают только при открытии записи.

**Что делает Visa2026:** *(Запланировано, не в релизе для сотрудников.)* Колокольчик и inbox **State notifications** не внедряются в руководство.

**Руководство:** *Отложено* — [Панель отчётов](../guides/tracking/report-dashboard.md) и [Отметить неполными](../guides/person/mark-incomplete.md).

---

## 7. Templates (пакеты отчётов)

**Проблема:** Сопроводительные письма и Word/Excel собирают вручную.

**Что делает Visa2026:** **Templates** на **Application** или **application item** — каталог, готовность, предпросмотр, ZIP.

**Руководство:** [Пакет отчётов по шаблонам (Resminamalar)](../guides/applications/resminamalar.md). Администраторы: [Пользовательские шаблоны](../guides/administration/user-report-templates.md) · [Редактирование и синхронизация](../guides/administration/template-staging.md).

---

## 8. Person dossier

**Проблема:** Руководителю нужно «всё о сотруднике» — собирают с многих экранов.

**Что делает Visa2026:** **Person dossier** из **Person search** или колонки **Dossier** — обзор **360°**; экспорт для руководства (HTML/PDF + ZIP документов).

**Руководство:** [Досье персоны](../guides/person/dossier.md) — точки входа в [Поиске персоны](../guides/person/open-and-search.md).

---

## 9. Mark incomplete / Incomplete persons

**Проблема:** После миграции нельзя пометить «ещё исправляем» по всему отделу.

**Что делает Visa2026:** **Mark incomplete** / **Mark complete**; **Report Dashboard → Incomplete persons**.

**Руководство:** [Отметить неполным](../guides/person/mark-incomplete.md)

---

## 10. Person document copies

**Проблема:** Сканы на разных вкладках — перед проверкой открывают по одной.

**Что делает Visa2026:** **Document copies** на карточке человека (или **Copies** в списке) — все вкладки в панели предпросмотра. ZIP для министерства — **Document copies** на application item.

**Руководство:** [Копии документов лица](../guides/person/document-copies.md). ZIP министерства: [Копии документов заявки](../guides/applications/document-copies.md).

---

## 11. Configuration (настройки офиса)

**Проблема:** Реквизиты организации, нумерация, маршруты министерств, SLA и лимиты загрузки разрозненны.

**Visa2026:** Меню **Configuration** (роли **VisaOffice** / администратор).

**Руководства:** [Обзор](../guides/administration/configuration/overview.md) · [Организация](../guides/administration/configuration/organization.md) · [Договоры](../guides/administration/configuration/contracts-and-approvals.md) · [SLA](../guides/administration/configuration/sla.md) · [Оповещения](../guides/administration/configuration/alerts-and-upload-limits.md)

---

## Для кого это руководство

| Роль | Типичное использование |
|------|------------------------|
| **Visa Officer** | Персоны, заявки, пакеты документов, dashboard |
| **Visa Chief / руководитель** | Dashboard, досье, Excel, неполные персоны |
| **Administrator** | [Configuration](../guides/administration/configuration/overview.md), [Шаблоны отчётов](../guides/administration/user-report-templates.md), [Синхронизация шаблонов](../guides/administration/template-staging.md), роли |

## С чего начать

1. [Вход](../getting-started/login.md)  
2. [Навигация](../getting-started/navigation.md)  
3. [Поиск человека](../guides/person/open-and-search.md)  
4. [Регистрация сотрудника](../guides/employee/register.md)
