# FinanceTracker

Настольное приложение для учёта личных доходов и расходов.

## Стек

- C# и .NET 8 LTS
- Avalonia UI 11
- SQLite (`Microsoft.Data.Sqlite`)
- MVVM (`CommunityToolkit.Mvvm`)
- xUnit

## Возможности

- добавление, редактирование и удаление операций;
- подтверждение перед удалением;
- доходы, расходы и автоматический расчёт баланса;
- фильтрация по месяцу и типу, поиск по категории и описанию;
- статистика расходов по категориям;
- месячный бюджет и индикатор использования;
- пользовательские категории;
- выбор обозначения валюты;
- экспорт истории в CSV;
- создание и восстановление резервных копий SQLite;
- локальная работа без браузера и интернета.

## Структура

```text
src/FinanceTracker.App   — интерфейс и ViewModel
src/FinanceTracker.Core  — модели, деньги и расчёты
src/FinanceTracker.Data  — SQLite и репозиторий
tests/FinanceTracker.Tests — автоматические тесты
```

## Сборка и тесты

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet restore
dotnet test
dotnet build -c Release
```

## Self-contained публикация для Linux

```bash
dotnet publish src/FinanceTracker.App \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o publish/linux-x64
```

## Данные

Основная база:

```text
~/.local/share/FinanceTracker/finance.db
```

Резервные копии:

```text
~/Documents/FinanceTracker Backups/
```

Реальная база пользователя не должна публиковаться в GitHub.
