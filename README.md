# LISIO.NET

Этот репозиторий ориентирован на реализацию **LIS79**-чтения на C#.

## Цели

- Приоритет №1: корректная работа с **LIS** файлами.
- Целевая платформа библиотеки: **.NET Framework 4.8**.
- C++ вставки допустимы, но только если действительно нужны для производительности.

## Текущее состояние

- DLIS-специфичный транспортный слой удалён из `src/Dlisio.Core/Parsing`.
- Core собирается как библиотека `Lis.Core.dll` (project: `src/Dlisio.Core`).
- Добавлен LIS-first слой в `src/Dlisio.Core/Lis`:
  - PRH/LRH парсеры,
  - типы LIS79,
  - reader для логической записи с объединением нескольких physical records,
  - индексатор logical records (тип, смещение, длина, class explicit/implicit) без materialize payload,
  - парсер fixed/text records (File/Reel/Tape Header/Trailer, text records),
  - начальный DFSR-парсер (entry blocks и spec blocks subtype 0/1).
  - начальный FData-парсер кадров (Normal/Alternate Data) с fast-path декодом fixed-size reprc.
  - high-level API:
    - `LisLogicalFileParser`,
    - `LisFileParser`,
    - `LisReadOptions` (выбор кривых, режим curves-only),
    - `LisReadMetrics` (счётчики read/decode),
    - `LisImporter` / `LisExporter` (импорт/экспорт LIS logical records).
- Добавлены LIS unit-тесты в `tests/Dlisio.Tests/Lis`.

## Как загружать LIS и что получать

### 1) Полный разбор logical files (metadata + frames)

```csharp
using var stream = File.OpenRead("sample.lis");
var parser = new LisFileParser();
IReadOnlyList<LisLogicalFileData> files = parser.Parse(stream);
```

Результат `LisLogicalFileData` содержит:
- `FileHeader`, `FileTrailer`,
- `TextRecords`,
- `DataFormatSpecifications`,
- `Frames`.

### 2) Выбрать только нужные кривые из FData

```csharp
using var stream = File.OpenRead("sample.lis");
var parser = new LisFileParser();
var options = new LisReadOptions(
    selectedCurveMnemonics: new[] { "GR", "RHOB", "NPHI" },
    includeFrames: true,
    includeCurves: false);

IReadOnlyList<LisLogicalFileData> files = parser.Parse(stream, options);
```

В `Frames` останутся только выбранные каналы (`GR`, `RHOB`, `NPHI`).

### 3) Режим "только кривые" (минимум памяти)

```csharp
using var stream = File.OpenRead("sample.lis");
var parser = new LisFileParser();
var metrics = new LisReadMetrics();

IReadOnlyList<LisLogicalFileData> files =
    parser.ParseCurves(stream, selectedCurveMnemonics: new[] { "GR" }, metrics: metrics);
```

В этом режиме:
- `Frames` не заполняются,
- данные идут в `Curves` (`mnemonic -> samples`),
- доступны метрики в `metrics`:
  - `LogicalRecordsRead`,
  - `FdataBytesRead`,
  - `SamplesDecoded`,
  - `SamplesSkipped`,
  - `ParseElapsedMilliseconds`.

### 4) Импорт LIS (raw logical records)

```csharp
using var stream = File.OpenRead("input.lis");
var importer = new LisImporter();
LisDocument document = importer.Import(stream);
```

`LisDocument` содержит последовательность `LisLogicalRecord` (type/attributes/data).

### 5) Экспорт LIS (raw logical records -> файл)

```csharp
var exporter = new LisExporter();
var options = new LisExportOptions(maxPhysicalRecordLength: 4096);
exporter.Export("output.lis", document, options);
```

Поддерживается запись как в `Stream`, так и сразу в файл (`path`).

## Быстрая проверка

```bash
dotnet test DlisioNet.sln
```

## GUI (отдельный проект, без графиков)

Добавлен отдельный проект:
- `src/Dlisio.Gui` (`.NET Framework 4.8`, WinForms)

Что умеет GUI:
- выбрать `.lis` файл,
- задать список curve mnemonics (через запятую),
- открыть файл в режиме:
  - полный разбор (с `Frames`),
  - curves-only (без `Frames`, ниже расход памяти),
- показать текстовый отчёт:
  - header/trailer, количество records/frames/curves,
  - список curves и количество samples,
  - метрики чтения (`LisReadMetrics`).
- отдельная вкладка **Raw records** (без графиков):
  - `Logical File`,
  - `Offset`,
  - `Type`,
  - `Attributes`,
  - `Physical Records`,
  - `Data Length`,
  - `Class` (implicit/explicit).

Запуск (на Windows):

```bash
dotnet run --project src/Dlisio.Gui/Dlisio.Gui.csproj
```

## План

Детальный план дальнейшей реализации: `docs/implementation-plan.md`.
