---
title: Добавление паспорта сотруднику
slug: employee/add-passport
locale: ru
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
screenshotsVersion: "2026.08"
screenshotsCapturedAt: "2026-08-05T08:48:03.3957272Z"
mediaE2eRunId: "20260805-134241"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: true
verifiedAt: "2026-08-05T08:49:33.7498506Z"
verifiedCommit: "2d70b13c"
---

# Добавление паспорта сотруднику

В этом руководстве описано добавление **паспорта** к существующему сотруднику. Паспорта создаются на вкладке **Passports** в карточке сотрудника.

!!! tip "Предварительно"
    Сотрудник должен быть зарегистрирован ([Регистрация нового сотрудника](register.md)).

!!! tip "Снимки экрана"
    Изображения с **английского** интерфейса (версия **2026.08**).

## Видеоинструкция

## Шаг 1 — Откройте сотрудника

1. Откройте сотрудника в списке **Employees**.
2. Дождитесь карточки с вкладками, включая **Passports**.

![Карточка сотрудника](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-01-employee-detail.png)

## Шаг 2 — Вкладка Passports

1. Выберите вкладку **Passports**.
2. Найдите кнопку **New Passport** на вложенной панели.

## Шаг 3 — Новый паспорт

1. Нажмите **New Passport**.
2. Дождитесь открытия карточки паспорта.

!!! tip "Тип паспорта по умолчанию"
    Поле **Passport Type** заранее заполнено значением **P — Национальный паспорт** (самый частый выбор сотрудников). Меняйте только если документ другого типа.

![Новая карточка паспорта](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-02-passport-form-new.png)

## Шаг 4 — Обязательные поля

| Поле | Ввод |
|------|------|
| **Passport Number** | Номер паспорта |
| **Passport Type** | Обычно уже **P — Национальный паспорт**; при необходимости выберите другой тип из списка |
| **Issue Date** | Дата |
| **Expiration Date** | Дата |
| **Authority** | Текст |
| **Issued Country** | Из списка |

![Заполненная форма](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-03-passport-fields-filled.png)

## Шаг 5 — Сохранение

1. Нажмите **Save**.
2. Проверьте **Passport Number**.

![Сохранённый паспорт](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-04-passport-saved.png)

## Дальше

- [Регистрация сотрудника](register.md)
- [Основная навигация](../../getting-started/navigation.md)
