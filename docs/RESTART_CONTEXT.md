# Контекст проекта (для перезапуска)

Этот файл нужен для быстрого восстановления контекста после рестарта агента.

## Главное направление

- Приоритет проекта: **LIS79**
- DLIS направление отключено (не целевой scope).
- Платформа библиотеки: `.NET Framework 4.8` (только).

## Что уже сделано

1. Удалён старый DLIS-специфичный парсерный слой (`src/Dlisio.Core/Parsing/*`).
2. Добавлен LIS-first модуль (`src/Dlisio.Core/Lis/*`):
   - `LisPhysicalRecordHeader` (PRH),
   - `LisLogicalRecordHeader` (LRH),
   - `LisRecordType` + валидация типов,
   - `LisReader` для чтения logical record с stitching нескольких PR,
   - `LisIndexer` / `LisRecordIndex` / `LisRecordInfo` для индексации записей,
   - `LisFixedRecordParser` для fixed/text records,
   - `LisDfsrParser` для базового разбора DFSR,
   - `LisFdataParser` для базового разбора FData.
3. Тесты полностью переведены на LIS:
   - `tests/Dlisio.Tests/Lis/*`.

## Текущее состояние качества

- Unit-тесты: **83 passed, 0 failed**.
- Проверка:

```bash
dotnet test DlisioNet.sln
```

## Что делать дальше

Следующий шаг (LIS-only):

1. Поддержка последовательности нескольких logical files в одном физическом файле.
2. Расширение DFSR-парсера и валидации entry/spec блоков.
3. Расширение FData/curves:
   - поддержка всех reprc,
   - fast-channel сценарии,
   - более полный API доступа к кривым.

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
   - `src/Dlisio.Core/Lis/LisIndexer.cs`
   - `src/Dlisio.Core/Lis/LisFixedRecordParser.cs`
   - `src/Dlisio.Core/Lis/LisDfsrParser.cs`
   - `src/Dlisio.Core/Lis/LisFdataParser.cs`
   - `src/Dlisio.Core/Lis/LisHeaderParser.cs`
   - `tests/Dlisio.Tests/Lis/*`
