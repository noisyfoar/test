# LISIO.NET (LIS-first)

Этот репозиторий ориентирован на реализацию **LIS79**-чтения на C#.

## Цели

- Приоритет №1: корректная работа с **LIS** файлами.
- Целевая платформа библиотеки: **.NET Framework 4.8**.
- Тесты запускаются под `net8.0` (cloud/CI harness на Linux).
- C++ вставки допустимы, но только если действительно нужны для производительности.

## Текущее состояние

- DLIS-специфичный транспортный слой удалён из `src/Dlisio.Core/Parsing`.
- Добавлен LIS-first слой в `src/Dlisio.Core/Lis`:
  - PRH/LRH парсеры,
  - типы LIS79,
  - reader для логической записи с объединением нескольких physical records,
  - индексатор logical records (тип, смещение, длина, class explicit/implicit).
- Добавлены LIS unit-тесты в `tests/Dlisio.Tests/Lis`.

## Быстрая проверка

```bash
dotnet test DlisioNet.sln
```

## План

Детальный план дальнейшей реализации: `docs/implementation-plan.md`.
