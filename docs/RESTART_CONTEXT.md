# Контекст проекта (для перезапуска)

Этот файл нужен для быстрого восстановления контекста после рестарта агента.

## Главное направление

- Приоритет проекта: **LIS79**
- Альтернативное направление отключено (не целевой scope).
- Платформа библиотеки: `.NET Framework 4.8` (только).

## Что уже сделано

1. Удалён старый парсерный слой (`src/Lis.Core/Parsing/*`).
2. Добавлен LIS-first модуль (`src/Lis.Core/Lis/*`):
   - `LisPhysicalRecordHeader` (PRH),
   - `LisLogicalRecordHeader` (LRH),
   - `LisRecordType` + валидация типов,
   - `LisReader` для чтения logical record с stitching нескольких PR,
   - `LisIndexer` / `LisRecordIndex` / `LisRecordInfo` для индексации записей,
   - `LisFixedRecordParser` для fixed/text records,
   - `LisDfsrParser` для базового разбора DFSR,
   - `LisFdataParser` для базового разбора FData + выборочной декодировки каналов,
   - `LisLogicalFileParser` и `LisFileParser` для high-level парсинга.
   - `LisReadOptions` для настройки (selected curves, curves-only режим).
   - `LisReadMetrics` для счётчиков производительности.
   - `LisImporter` / `LisExporter` / `LisExportOptions` для raw импорта/экспорта LIS.
   - `LisDocument` как контейнер logical records.
   - сборка ядра как библиотека `Lis.Core.dll`.
3. Добавлен отдельный GUI-проект:
   - `src/Lis.Gui` (WinForms, `.NET Framework 4.8`),
   - открыть `.lis`, выбрать curves, получить текстовый отчёт без графиков,
   - отдельная вкладка `Raw records` с offset/type/length/class и привязкой к logical file.
4. Добавлен проект сравнения с Python+dlisio:
   - `src/Lis.Compare.Cli` формирует JSON summary из `Lis.Core`,
   - `python_dlisio_compare/compare_with_dlisio.py` читает тот же файл через `dlisio` и сравнивает структуры/каналы,
   - по умолчанию сравнение использует tolerant-режим `Lis.Core` для повреждённых данных.
5. Тесты полностью переведены на LIS:
   - `tests/Lis.Tests/Lis/*`.

## Текущее состояние качества

- Unit-тесты: **105 passed, 0 failed**.
- Проверка:

```bash
dotnet test LisNet.sln
```

## Что делать дальше

Следующий шаг (LIS-only):

1. Расширение DFSR-парсера и валидации entry/spec блоков.
2. Дальнейшее расширение FData/curves:
   - поддержка всех reprc edge-case сценариев,
   - fast-channel сценарии.
3. Профилирование на больших LIS и точечная оптимизация горячих участков.

## Быстрый старт после рестарта

1. Проверить SDK:

```bash
dotnet --list-sdks
```

2. Прогнать тесты:

```bash
dotnet test LisNet.sln
```

3. Основные файлы для продолжения:
   - `src/Lis.Core/Lis/LisReader.cs`
   - `src/Lis.Core/Lis/LisIndexer.cs`
   - `src/Lis.Core/Lis/LisFixedRecordParser.cs`
   - `src/Lis.Core/Lis/LisDfsrParser.cs`
   - `src/Lis.Core/Lis/LisFdataParser.cs`
   - `src/Lis.Core/Lis/LisLogicalFileParser.cs`
   - `src/Lis.Core/Lis/LisFileParser.cs`
   - `src/Lis.Core/Lis/LisReadOptions.cs`
   - `src/Lis.Core/Lis/LisReadMetrics.cs`
   - `src/Lis.Core/Lis/LisImporter.cs`
   - `src/Lis.Core/Lis/LisExporter.cs`
   - `src/Lis.Core/Lis/LisDocument.cs`
   - `src/Lis.Compare.Cli/Program.cs`
   - `python_dlisio_compare/compare_with_dlisio.py`
   - `src/Lis.Gui/MainForm.cs`
   - `src/Lis.Core/Lis/LisHeaderParser.cs`
   - `tests/Lis.Tests/Lis/*`
   - `docs/MAINTENANCE_SUMMARY.md`
