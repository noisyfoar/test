# Контекст проекта (для перезапуска)

Этот файл нужен для быстрого восстановления контекста после рестарта агента.

## Главное направление

- Приоритет проекта: **LIS79**
- DLIS направление отключено (не целевой scope).
- Платформа: `.NET Framework 4.8` + `net8.0` (для тестов в Cloud).

## Что уже сделано

1. Удалён старый DLIS-специфичный парсерный слой (`src/Dlisio.Core/Parsing/*`).
2. Добавлен LIS-first модуль (`src/Dlisio.Core/Lis/*`):
   - `LisPhysicalRecordHeader` (PRH),
   - `LisLogicalRecordHeader` (LRH),
   - `LisRecordType` + валидация типов,
   - `LisReader` для чтения logical record с stitching нескольких PR.
3. Тесты полностью переведены на LIS:
   - `tests/Dlisio.Tests/Lis/*`.

## Текущее состояние качества

- Unit-тесты: **40 passed, 0 failed**.
- Проверка:

```bash
dotnet test DlisioNet.sln
```

## Что делать дальше

Следующий шаг (LIS-only):

1. Индексация logical records (позиции, типы, группировка по logical file).
2. Декодирование fixed records:
   - File Header/Trailer,
   - Reel Header/Trailer,
   - Tape Header/Trailer.
3. Затем — DFSR/FData (кривые).

## Быстрый старт после рестарта

1. Проверить SDK:

```bash
dotnet --list-sdks
```

2. Прогнать тесты:

```bash
dotnet test DlisioNet.sln
```

3. Основные файлы для продолжения:
   - `src/Dlisio.Core/Lis/LisReader.cs`
   - `src/Dlisio.Core/Lis/LisHeaderParser.cs`
   - `tests/Dlisio.Tests/Lis/*`
