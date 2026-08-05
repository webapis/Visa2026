---
title: Поиск и открытие персоны
slug: person/open-and-search
locale: ru
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

# Поиск и открытие персоны

В этом руководстве описано, как найти существующую персону в Visa2026 и открыть её запись. После выполнения шагов вы сможете открыть сотрудника (или другой тип персоны) из списка и просмотреть **форму детализации**.

!!! tip "Предварительно"
    [Вход](../../getting-started/login.md) и [Основная навигация](../../getting-started/navigation.md).

!!! tip "Снимки экрана"
    Изображения с **английского** интерфейса (версия **2026.08**).

## Видеоинструкция

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/ru/person-open-search.mp4"
  title="Поиск и открытие персоны"></video>

<p class="visa-manual-video-caption">Запись из учебной среды (тестовые данные).</p>

## Перед началом

| Нужно | Примечание |
|-------|------------|
| Право чтения списков персон | Если пункта меню нет — обратитесь к руководителю |
| Подсказка для поиска | Имя, **Personal Number** или номер паспорта |

Персоны хранятся в отдельных списках по типу.

| Пункт меню | Содержимое |
|------------|------------|
| **Employees** | Сотрудники компании |
| **Family Members** | Члены семьи сотрудников |
| **Temporary visitor** | Временные посетители |

## Шаг 1 — Откройте нужный список

1. Войдите в Visa2026.
2. В левом меню выберите **Employees** (или **Family Members** / **Temporary visitor**).
3. Дождитесь загрузки списка.

В таблице отображаются **Full Name**, **Personal Number**, **Date Of Birth** и другие столбцы. В начале могут быть **Dossier** и **Copies** — в этом руководстве открывается обычная форма детализации.

![Список сотрудников](../../assets/screenshots/v2026.08/ru/navigation-step-03-employees-list.png)

## Шаг 2 — Поиск в списке

Если список длинный, используйте поле **поиска** на панели инструментов (или кнопку **Search** / **Поиск**).

1. Щёлкните в поле поиска.
2. Введите часть **First Name**, **Last Name**, **Personal Number** или **Passport Number** любого паспорта этой персоны.
3. Нажмите **Enter** или дождитесь обновления списка.

Советы:

- Несколько слов сужают результат.
- Буквы с диакритикой часто находятся без неё.
- Очистите поле, чтобы увидеть весь список.
4. При необходимости нажмите **Refresh** / **Обновить**.

!!! note "По каким полям ищется"
    **First Name**, **Middle Name**, **Last Name**, **Personal Number** и номера паспортов на вкладке **Passports**.

## Шаг 3 — Откройте форму детализации

1. В отфильтрованном списке найдите строку с нужными **Full Name** и **Personal Number**.
2. Щёлкните по строке (или дважды щёлкните).
3. Дождитесь открытия **формы детализации** в основной области.

Доступны вкладки **Passports**, **Educations** и др. Для изменения данных сохраняйте через **Save** — см. [Регистрация нового сотрудника](register.md).

![Форма детализации сотрудника](../../assets/screenshots/v2026.08/ru/navigation-step-04-detail-form.png)

!!! success "Персона открыта"
    Если на форме видны ожидаемые **Personal Number** и имя — вы нашли нужную персону.

## Альтернатива — Person search на Report Dashboard

С главной **Report Dashboard** можно искать по всем типам персон:

1. Откройте **Report Dashboard**.
2. Выберите категорию **Person search**.
3. Введите имя, **Personal Number** или номер паспорта в поле поиска у чипов категорий.
4. Просмотрите таблицу и диаграмму.
5. Щелчок по строке открывает **person dossier** (только чтение), а не редактируемую форму детализации.

Если тип персоны известен — используйте списки в меню. Если нет — **Person search**.

Для редактирования откройте нужный список и форму детализации, как в шаге 3.

## Досье из списка

Столбец **Dossier** в списках **Employees**, **Family Members** и **Temporary visitor** открывает то же досье, что и **Person search**.

## Частые проблемы

| Проблема | Действие |
|----------|----------|
| Пустой список | Очистите поиск; **Refresh**; проверьте пункт меню |
| Слишком много строк | Уточните имя или полный **Personal Number** |
| Не найдено | **Report Dashboard** → **Person search**; другие списки |
| Открылось досье | Вы нажали **Dossier** или **Person search** — для формы детализации щёлкните по строке |

## Далее

- [Основная навигация](../../getting-started/navigation.md)
- [Регистрация нового сотрудника](register.md)
- [Добавление паспорта](add-passport.md)
