# Материалы домашнего задания № 5

Проект «Вместе в путь» — клиент-серверное приложение для сравнения вариантов
семейной поездки. Frontend реализован на Blazor WebAssembly и MudBlazor, backend —
ASP.NET Core Minimal API, данные — PostgreSQL через EF Core/Npgsql.

## Состав сдачи

| Требование | Материал |
| --- | --- |
| Код backend и frontend | `src/Together.Api`, `src/Together.Client`, `src/Together.Core`, `src/Together.Contracts` |
| Миграции БД | `src/Together.Api/Data/Migrations` и `database/migrations.sql` |
| Docker-конфигурация | `Dockerfile`, `database/Dockerfile`, `docker-compose.yml`, `.env.example` |
| Аутентификация и доступ | ASP.NET Core Identity, HttpOnly cookie, проверка владельца поездки |
| API и примеры запросов | [backend_documentation.md](backend_documentation.md) |
| Требования и архитектурное решение | [docs/backend_requirements.md](docs/backend_requirements.md) |
| README и запуск | [README.md](README.md) |
| Автоматические тесты | `tests/Together.Api.Tests`, `tests/Together.Tests`, `tests/Together.BrowserTests` |
| Описание применения AI | [backend_documentation.md](backend_documentation.md#использование-ai) |

## Локальный запуск

```powershell
Copy-Item .env.example .env
# Заменить POSTGRES_PASSWORD в .env
docker compose up --build
```

После применения миграции приложение должно быть доступно по адресу
`http://localhost:8080`. Для production необходим HTTPS reverse proxy.

## Текущий статус проверок

- Release-сборка решения: успешно, без предупреждений.
- Модульные и компонентные тесты: 32/32.
- API-тесты: 4/4.
- EF-модель соответствует миграции.
- Docker/настоящий PostgreSQL и обновлённые браузерные end-to-end сценарии пока не
  проверены: Docker отсутствует в рабочей среде.

Перед окончательной сдачей нужно выполнить Compose-проверку, браузерные сценарии,
развёртывание по HTTPS и указать фактические ссылки на GitHub и деплой.
