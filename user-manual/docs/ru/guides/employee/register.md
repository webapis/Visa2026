---
title: Регистрация нового сотрудника
slug: employee/register
locale: ru
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
videosVersion: "2026.08"
videoStorage: static
videoFile: person-register.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Регистрация нового сотрудника

В этом руководстве описано создание новой записи **сотрудника** (person) в Visa2026. После сохранения запись появится в списке **Employees**.

!!! tip "Предварительно"
    [Вход](../../getting-started/login.md) и [Основная навигация](../../getting-started/navigation.md).

!!! tip "Снимки экрана"
    Изображения с **английского** интерфейса (версия **2026.08**).

## Видеоинструкция

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/ru/person-register.mp4"
  title="Регистрация нового сотрудника"></video>

<p class="visa-manual-video-caption">Запись из учебной среды (тестовые данные). Шаги ниже соответствуют видео.</p>

## Перед началом

| Нужно | Примечание |
|-------|------------|
| Право создания в **Employees** | Если нет **New**, обратитесь к руководителю |
| Уникальный **Personal Number** | По правилам организации |
| Значения справочников | Выбор из выпадающих списков |

## Шаг 1 — Список Employees

1. Войдите в Visa2026.
2. В левом меню выберите **Employees**.
3. Дождитесь загрузки списка.

![Список сотрудников](../../../assets/screenshots/v2026.08/ru/person-register-step-01-employees-list.png)

## Шаг 2 — Новая запись

1. На панели инструментов выберите **New**.
2. Дождитесь открытия карточки сотрудника.

## Шаг 3 — Обязательные поля

| Поле | Ввод |
|------|------|
| **First Name** | Имя |
| **Last Name** | Фамилия |
| **Personal Number** | Уникальный номер |
| **Date Of Birth** | Дата |
| **Birth Place** | Место рождения |
| **Country Of Birth** | Из списка |
| **Gender** | Из списка |
| **Marital Status** | Из списка |
| **Nationality** | Из списка |
| **Foreign Address** | Адрес |
| **Foreign Address Country** | Из списка |
| **Project Contract** | Договор |
| **Company (Subcontractor)** | Субподрядчик |

## Шаг 4 — Сохранение

1. Проверьте значения.
2. Нажмите **Save**.
3. Дождитесь завершения сохранения.

![Карточка после сохранения](../../../assets/screenshots/v2026.08/ru/person-register-step-02-saved-detail.png)

## Шаг 5 — Проверка в списке

1. Снова откройте **Employees**.
2. Найдите строку по **Personal Number** и откройте её.

![Карточка из списка](../../../assets/screenshots/v2026.08/ru/person-register-step-03-open-from-list.png)

## Дальше

- [Основная навигация](../../getting-started/navigation.md)
- [Добавление паспорта](add-passport.md)
