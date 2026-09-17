# Backend «Вместе в путь»

## Архитектура

Решение состоит из четырёх проектов:

- `Together.Client` — standalone Blazor WebAssembly и MudBlazor;
- `Together.Contracts` — DTO и общие маршруты HTTP API;
- `Together.Core` — доменная модель, расчёты и валидация;
- `Together.Api` — ASP.NET Core Minimal API, Identity, EF Core и PostgreSQL.

В production API раздаёт опубликованные файлы Blazor и обслуживает `/api/*` с того
же origin. В development клиент работает на `http://localhost:5180`, API — на
`http://localhost:5182`; CORS разрешает только адрес клиента и credentials.

## База данных

Начальная миграция находится в `src/Together.Api/Data/Migrations`, её идемпотентный
SQL-вариант — `database/migrations.sql`.

Прикладные таблицы:

| Таблица | Назначение |
| --- | --- |
| `AspNetUsers` и таблицы Identity | Пользователь, пароль и данные входа |
| `trips` | Поездка и идентификатор владельца |
| `trip_children` | Упорядоченные возраста детей |
| `variants` | Варианты поездки |
| `variant_expenses` | Шесть nullable-расходов в целых копейках |
| `trip_selected_variants` | Выбранные столбцы сравнения |
| `trip_selected_criteria` | Выбранные строки сравнения |

`DateOnly` отображается в PostgreSQL `date`. Сумма хранится в `bigint`: `NULL`
означает «неизвестно», `0` — подтверждённый ноль. Диапазоны закреплены CHECK-
ограничениями. Все дочерние записи удаляются каскадно. `revision` поездки и варианта
является EF Core concurrency token.

## Аутентификация и доступ

Используются ASP.NET Core Identity API endpoints и HttpOnly cookie. В браузере вход
выполняется запросом `/api/auth/login?useCookies=true`. Cookie имеет `SameSite=Lax`;
в production приложение должно публиковаться только по HTTPS. Пароли обрабатывает
Identity и они не попадают в логи.

Переменная `SECURE_COOKIES` в Compose по умолчанию равна `true`. Отключать флаг
можно только на изолированном HTTP-стенде; это не production-конфигурация.

Каждый запрос поездки фильтруется одновременно по `id` и `OwnerId`. Для чужого id
возвращается `404`, чтобы не раскрывать существование записи. Прикладных ролей нет.
Изменяющие запросы с cookie защищены SameSite и CORS: сторонний origin не получает
credentialed-доступ. Секреты передаются через переменные окружения.

## API

| Метод | Маршрут | Результат |
| --- | --- | --- |
| POST | `/api/auth/register` | Регистрация |
| POST | `/api/auth/login?useCookies=true` | Вход и cookie |
| POST | `/api/auth/logout` | Выход |
| GET | `/api/auth/me` | Текущий пользователь |
| GET | `/api/trips` | Все поездки пользователя |
| GET | `/api/trips/{id}` | Одна поездка с вариантами |
| POST | `/api/trips` | Создание поездки |
| PUT | `/api/trips/{id}` | Изменение поездки |
| DELETE | `/api/trips/{id}?expectedRevision=1` | Удаление поездки |
| POST | `/api/trips/{tripId}/variants` | Создание варианта |
| PUT | `/api/trips/{tripId}/variants/{id}` | Изменение варианта |
| DELETE | `/api/trips/{tripId}/variants/{id}` | Удаление варианта |
| PUT | `/api/trips/{id}/comparison` | Настройки сравнения |

Пример создания поездки после входа:

```http
POST /api/trips HTTP/1.1
Content-Type: application/json

{
  "expectedRevision": 0,
  "name": "Летняя поездка",
  "startDate": "2027-07-01",
  "endDate": "2027-07-08",
  "adults": 2,
  "childAges": [4]
}
```

Ответ: `201 Created` и полный объект поездки с `revision: 1`. Для изменения клиент
передаёт полученный revision. Устаревшее значение получает `409 Conflict` и не
перезаписывает серверную запись.

Ошибки валидации возвращаются как Validation Problem Details (`400`). Нет сеанса —
`401`; запись не найдена или чужая — `404`; конфликт версии — `409`; необработанная
ошибка — безопасный Problem Details `500`.

## Локальный запуск

Нужны .NET SDK 10.0.302 и PostgreSQL 17 либо Docker с Compose.

```powershell
Copy-Item .env.example .env
# Заменить POSTGRES_PASSWORD в .env.
docker compose up --build
```

Compose сначала ждёт PostgreSQL, запускает одноразовый контейнер миграции, затем
приложение на `http://localhost:8080`. Volume `together-postgres` сохраняет БД, а
`together-data-protection` — ключи шифрования cookie между перезапусками.

Запуск без Docker при доступном PostgreSQL:

```powershell
$env:ConnectionStrings__Together = 'Host=localhost;Port=5432;Database=together;Username=together;Password=...'
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Together.Api
dotnet run --project src/Together.Api
dotnet run --project src/Together.Client
```

## Миграции и резервное копирование

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project src/Together.Api --output-dir Data/Migrations
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/Together.Api
dotnet tool run dotnet-ef migrations script --idempotent --project src/Together.Api --output database/migrations.sql
docker compose exec -T database pg_dump -U together -d together -Fc > together.backup
```

Восстановление выполняется в отдельную пустую БД через `pg_restore`; перед операцией
нужно остановить запись приложения и проверить резервную копию на тестовом окружении.

## Проверки

На 17 сентября 2026 года выполнены:

```text
dotnet build Together.slnx -c Release              — успешно, 0 ошибок, 0 предупреждений
dotnet test Together.slnx -c Release               — успешно, API 4/4, Core/UI 32/32
dotnet ef migrations has-pending-model-changes     — изменений модели нет
docker compose up --build -d                       — успешно на Linux-ВМ, Compose 2.39.1
GET /health/ready                                  — Healthy
Playwright --backend-smoke                         — Chromium, успешно
```

На стенде подтверждены регистрация и cookie-вход, создание и чтение поездки,
создание варианта с расходами `0` и `null`, конфликт revision, изоляция владельцев,
выход и сохранение данных после перезагрузки страницы. HTTPS reverse proxy и
публичный production-домен в рамках локальной проверки не разворачивались.

## Использование AI

AI-ассистент помог сопоставить требования ДЗ № 5 с моделью ДЗ № 4, спроектировать
нормализованную схему, сгенерировать и проверить EF-конфигурацию, API, обработку
конфликтов, Docker-файлы и тесты. Результат проверялся компилятором, EF CLI и xUnit;
контейнерный сценарий дополнительно проверен на Linux-ВМ с настоящим PostgreSQL и
Playwright Chromium.
