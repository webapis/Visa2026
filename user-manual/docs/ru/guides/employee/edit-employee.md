---
title: Обновление данных сотрудника
slug: employee/edit-employee
locale: ru
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
videosVersion: "2026.08"
videoStorage: static
videoFile: person-edit-employee.mp4
videoSource: recordings/passport-create.mp4
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Обновление данных сотрудника

В этом руководстве описано, как изменить поля существующей записи **сотрудника** и сохранить изменения.

!!! tip "Предварительно"
    [Поиск и открытие персоны](../person/open-and-search.md) и [Регистрация сотрудника](register.md).

!!! tip "Снимки экрана"
    Изображения с **английского** интерфейса (версия **2026.08**).

## Видеоинструкция

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/ru/person-edit-employee.mp4"
  title="Обновление данных сотрудника"></video>

## Перед началом

| Нужно | Примечание |
|-------|------------|
| Право редактирования **Employees** | Если поля только для чтения — обратитесь к руководителю |
| Открыта форма нужного сотрудника | [Поиск и открытие персоны](../person/open-and-search.md) |

Руководство обновляет поля заголовка записи. Вкладки паспорта и образования — в отдельных инструкциях.

## Шаг 1 — Откройте форму детализации

1. Войдите в систему.
2. Откройте список **Employees**.
3. При необходимости выполните поиск.
4. Щёлкните по строке.

Проверьте **Personal Number** и **Full Name**.

![Форма детализации для редактирования](../../../assets/screenshots/v2026.08/ru/person-edit-employee-step-01-detail-form.png)

## Шаг 2 — Измените поля

| Поле | Когда меняют |
|------|----------------|
| **Foreign Address** | Изменился адрес за рубежом |
| **Foreign Address Country** | Изменилась страна адреса |
| **Email** | Контактный e-mail (необязательное — шаг 3) |
| **Project Contract** | Смена договора |
| **Company (Subcontractor)** | Смена субподрядчика |
| **Marital Status** | Обновление семейного положения |
| **Hire Date** | Дата приёма (необязательное) |

!!! warning "Personal Number"
    **Personal Number** — основной идентификатор. Меняйте только с одобрения руководителя. Дубликат вызовет ошибку **Save**.

## Шаг 3 — Необязательные поля (шестерёнка)

1. Выберите **Show optional fields** / **Показать необязательные поля**.
2. Появятся **Middle Name**, **Email**, **Photo**, **Hire Date** и др.
3. Внесите изменения.
4. **Hide optional fields** — скрыть снова.

## Шаг 4 — Сохраните

1. Нажмите **Save** / **Сохранить**.
2. Для возврата к списку — **Save and Close**.

![После сохранения](../../../assets/screenshots/v2026.08/ru/person-edit-employee-step-02-after-save.png)

## Шаг 5 — Проверьте в списке

Найдите сотрудника по **Personal Number** и убедитесь, что изменения отображаются.

!!! success "Обновлено"
    Форма и список показывают новые значения.

## Далее

- [Поиск и открытие персоны](../person/open-and-search.md)
- [Добавление паспорта](add-passport.md)
