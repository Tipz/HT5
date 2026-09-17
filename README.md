# Вместе в путь

Текущая версия — домашнее задание № 5: Blazor WebAssembly-клиент подключён к
ASP.NET Core API и PostgreSQL. Реализованы регистрация, cookie-вход, изоляция поездок
по владельцу, CRUD поездок и вариантов, optimistic concurrency и Docker Compose.

Архитектура, API и развёртывание описаны в [backend_documentation.md](backend_documentation.md),
а требования ДЗ № 5 — в [docs/backend_requirements.md](docs/backend_requirements.md).

## Быстрый запуск ДЗ № 5

```powershell
Copy-Item .env.example .env
# Укажите длинный случайный POSTGRES_PASSWORD в .env
docker compose up --build
```

После успешной миграции приложение доступно на `http://localhost:8080`. Файл `.env`
исключён из Git; секреты нельзя добавлять в `appsettings.json` или Blazor bundle.
`SECURE_COOKIES=true` оставляйте для production и HTTPS. Значение `false` допустимо
только для изолированного HTTP-стенда в локальной сети.

Адаптивное приложение для сравнения семейных поездок по бюджету, дороге и удобствам для детей.
Frontend создан в ДЗ № 4 по [исходному ТЗ](docs/technical_specification.md) и расширен backend в ДЗ № 5.

## Возможности

- Несколько независимых поездок: даты, ночи, взрослые и возраст детей.
- Создание, редактирование и удаление вариантов направления и жилья.
- Шесть статей бюджета в рублях за всю семью за всю поездку. Расчёт в целых копейках; пустое значение отличается от нуля.
- Сравнение двух и более вариантов по семи переключаемым критериям.
- Пометка проверки расходов при изменении дат или семьи.
- Серверное сохранение после входа, обработка сетевых ошибок и конфликтов версий.
- Добровольный импорт старых поездок из IndexedDB без автоматического удаления локальной копии.

Основной экран следует концепции [«План поездки»](docs/ui_concepts/03-analytical.html): сине-серая палитра, сравнение перед карточками, боковая навигация на широком экране.

![Основной экран, вымышленные данные](docs/evidence/chromium-1440.png)

## Стек

C# / .NET 10, **Blazor WebAssembly Standalone**, **ASP.NET Core Minimal API**, **PostgreSQL**, **MudBlazor 9.7.0**.
Бизнес-правила и валидация — C#; EF Core/Npgsql — серверные данные; IndexedDB используется только для импорта старой версии.
Тесты: xUnit, bUnit, Playwright .NET.

## Запуск

Нужен .NET SDK **10.0.302** или более новый patch из той же линии (см. global.json).
Node.js и npm для сборки и запуска не нужны.

Для запуска без Docker сначала подготовьте PostgreSQL и примените миграцию, затем из корня репозитория в двух терминалах:

```powershell
dotnet restore Together.slnx --locked-mode
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Together.Api
dotnet run --project src/Together.Api
dotnet run --project src/Together.Client
```

Откройте **http://localhost:5180**, зарегистрируйтесь или войдите. Для новой учётной
записи приложение создаёт вымышленный пример. Найденные поездки IndexedDB не
отправляются до подтверждения импорта и не удаляются автоматически.

Для разработки с hot reload:

```powershell
dotnet watch --project src/Together.Client
```

Сохраняйте один адрес и порт: браузер разделяет данные localhost и 127.0.0.1, а также разных портов.


## Проверки

```powershell
dotnet build Together.slnx -c Release
dotnet test Together.slnx -c Release
dotnet run --project tests/Together.BrowserTests -- --install
```

Установщик загружает отдельные Chromium и Firefox для автоматических тестов.

При работающем приложении на localhost:5180 запустите в другом терминале:

```powershell
dotnet run --project tests/Together.BrowserTests
```

Браузерные тесты создают изолированные профили, используют вымышленные данные,
проверяют оба движка и возвращают ненулевой код при ошибке.
Не запускайте несколько копий набора одновременно: они записывают одни и те же файлы отчёта.

Результаты и скриншоты: [docs/evidence](docs/evidence/).
Итог для ДЗ № 5: 32 модульных/компонентных и 4 API-теста прошли; Compose с
PostgreSQL и отдельный backend smoke-сценарий Chromium проверены на Linux-ВМ.
Существующие 22 браузерных сценария относятся к IndexedDB-версии ДЗ № 4.
Описание проверок, найденных дефектов и ограничений: [development_report.md](development_report.md).

Дополнительные команды в PowerShell:

```powershell
# Только отдельные сценарии:
$env:TEST_FILTER = 'keyboard,storage-failure'
dotnet run --project tests/Together.BrowserTests
Remove-Item Env:TEST_FILTER

# Проверка CSS hot reload; в первом терминале должен работать dotnet watch:
dotnet run --project tests/Together.BrowserTests -- --hot-reload

# Сквозная проверка опубликованного backend/frontend стенда:
$env:APP_URL = 'http://localhost:8080'
dotnet run --project tests/Together.BrowserTests -- --backend-smoke
Remove-Item Env:APP_URL
```

## Сборка для размещения

```powershell
dotnet publish src/Together.Client -c Release -o artifacts/publish
```

Результат — статические файлы в artifacts/publish/wwwroot. Для размещения нужен HTTP(S)-хостинг с поддержкой MIME application/wasm.
Сборка не требует wasm-tools; без этой необязательной нагрузки SDK сообщает о пропуске дополнительных оптимизаций WebAssembly.

Для локальной проверки опубликованных файлов предусмотрен **тестовый** статический сервер:

```powershell
dotnet run --project tests/Together.BrowserTests -- --serve-published
```

Откройте http://localhost:5181. Для браузерной проверки этой сборки в отдельном терминале:

```powershell
$env:APP_URL = 'http://localhost:5181'
dotnet run --project tests/Together.BrowserTests --no-build
Remove-Item Env:APP_URL
```

## Структура

| Путь | Назначение |
| --- | --- |
| src/Together.Core | Модель, календарные расчёты, бюджет и валидация |
| src/Together.Client/Components | Формы, таблица, карточки и общие элементы |
| src/Together.Client/Pages | Основной экран и управление сохранением |
| src/Together.Contracts | DTO и маршруты API |
| src/Together.Api | Identity, HTTP API, EF Core и миграции PostgreSQL |
| src/Together.Client/Storage | API-клиент и адаптер старой IndexedDB для импорта |
| src/Together.Client/wwwroot/js | Транзакции и управление диалогом |
| tests/Together.Tests | Модульные и компонентные тесты |
| tests/Together.Api.Tests | Интеграционные тесты API и изоляции пользователей |
| tests/Together.BrowserTests | Сценарии двух браузеров, mock-данные, измерения, статический preview |
| docs | Исходное ТЗ, концепции, план и доказательства проверок |

Зависимости закреплены в .csproj и packages.lock.json.
package.json включён как дополнительная точка входа для команд из формата задания; npm-зависимостей у приложения нет.

## Хранение и границы MVP

Поездки хранятся в PostgreSQL и принадлежат вошедшему пользователю. Секреты находятся
только в переменных окружения. Импорта цен, бронирования и конвертации валют нет.
Копирование, сортировка и отметка выбора семьи отложены согласно ТЗ.

API проверяет revision и возвращает `409`, если запись уже изменена. Интерфейс
предлагает загрузить актуальные данные и сохраняет черновик до подтверждения.
Сохранение и новый вход требуют сети; расчёт открытой формы выполняется локально.

## Материалы для сдачи

- [Документация backend](backend_documentation.md)
- [Требования ДЗ № 5](docs/backend_requirements.md)
- [Материалы ДЗ № 4](SUBMISSION.md)
- [Отчёт о разработке ДЗ № 4](development_report.md)
- [Адаптированные промпт-шаблоны ДЗ 2](docs/prompt_templates.md)
- [Правила работы агента](AGENTS.md)
- [Результаты браузерных тестов](docs/evidence/browser-results.json)
- [Измерения производительности](docs/evidence/performance.json)

Материалы подготовлены для сдачи. Пользователь самостоятельно публикует репозиторий на GitHub и предоставляет его ссылку.

## Использованная документация

- [Встроенные шаблоны .NET, включая blazorwasm](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates)
- [MudBlazor: установка](https://mudblazor.com/getting-started/installation)
- [Playwright .NET: установка и запуск](https://playwright.dev/dotnet/docs/intro)
