# Материалы домашнего задания №4

Репозиторий: https://github.com/Tipz/HT4

Проект «Вместе в путь» — адаптивное frontend-приложение для сравнения вариантов
семейной поездки. Реализовано на C# / .NET 10, Blazor WebAssembly и MudBlazor.
Приложение поддерживает параметры поездки, варианты жилья и направлений, бюджет
по шести статьям, сравнение по семи критериям и локальное хранение в IndexedDB.

Локальный запуск:

```powershell
git clone https://github.com/Tipz/HT4.git
cd HT4
dotnet restore Together.slnx --locked-mode
dotnet run --project src/Together.Client
```

После запуска открыть http://localhost:5180. В пустом профиле появится вымышленный
пример; при наличии сохранённых поездок его можно добавить кнопкой «Добавить пример».

Отчёт о разработке: [development_report.md](development_report.md).
Исходное ТЗ: [docs/technical_specification.md](docs/technical_specification.md).
Результаты проверок: [docs/evidence](docs/evidence/).

## Соответствие формату задания

| Требование | Материал |
| --- | --- |
| Репозиторий с кодом | https://github.com/Tipz/HT4 |
| README и запуск | [README.md](README.md) |
| Работающее приложение | Локальный запуск по инструкции выше; отдельный деплой не обязателен по условию |
| Отчёт о разработке | [development_report.md](development_report.md) |
| Техническое задание | [docs/technical_specification.md](docs/technical_specification.md) |
| Все восемь шагов | [docs/homework_progress.md](docs/homework_progress.md) |
| Примеры промптов | [docs/prompt_templates.md](docs/prompt_templates.md) |
| Автоматические тесты | `tests/Together.Tests` и `tests/Together.BrowserTests` |
| Скриншоты и результаты | [docs/evidence](docs/evidence/) |
| package.json | [package.json](package.json); команды-обёртки, зависимости Blazor закреплены в `.csproj` и `packages.lock.json` |
