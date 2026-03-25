# LISIO.NET

Этот репозиторий ориентирован на реализацию **LIS79**-чтения на C#.

## Цели

- Приоритет №1: корректная работа с **LIS** файлами.
- Целевая платформа библиотеки: **.NET Framework 4.8**.
- C++ вставки допустимы, но только если действительно нужны для производительности.

## Текущее состояние

- Старый транспортный слой удалён из `src/Lis.Core/Parsing`.
- Core собирается как библиотека `Lis.Core.dll` (project: `src/Lis.Core`).
- Добавлен LIS-first слой в `src/Lis.Core/Lis`:
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
- Добавлены LIS unit-тесты в `tests/Lis.Tests/Lis`.

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
dotnet test LisNet.sln
```

## GUI (отдельный проект, без графиков)

Добавлен отдельный проект:
- `src/Lis.Gui` (`.NET Framework 4.8`, WinForms)

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
dotnet run --project src/Lis.Gui/Lis.Gui.csproj
```

## План

Детальный план дальнейшей реализации: `docs/implementation-plan.md`.

## Поддержка и читаемость кода

Для поддержки проекта и быстрого входа в кодовую базу:
- `docs/MAINTENANCE_SUMMARY.md` — архитектурный summary и правила безопасного рефакторинга.

## Сравнение с Python + dlisio

Добавлен отдельный проект для кросс-проверки данных:

- `src/Lis.Compare.Cli` — C# CLI, формирующий JSON summary из `Lis.Core`
- `python_dlisio_compare` — Python-скрипт, читающий тот же файл через `dlisio` и сравнивающий структуры/каналы

Быстрый запуск:

```bash
cd python_dlisio_compare
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python compare_with_dlisio.py /path/to/file.lis --repo-root /workspace
```

## Использование Python `dlisio` из C# (.NET Framework 4.8)

Если на стороне C# нужен именно разбор через Python-библиотеку `dlisio`, в `Lis.Core` добавлен bridge-клиент:

```csharp
using Lis.Core.Lis;

var client = new LisDlisioClient();
LisDlisioSummary summary = client.ReadSummary(@"C:\data\sample.lis", options);
```

> По умолчанию `LisDlisioClient` работает в **режиме без Python** (`PreferPythonBridge=false`)
> и читает LIS встроенным C# парсером `Lis.Core`.  
> Это позволяет запускать библиотеку на машине, где установлен только `.NET Framework 4.8`.

Если нужно принудительно использовать именно Python `dlisio` (при наличии Python):

```csharp
var options = new LisDlisioOptions
{
    PreferPythonBridge = true,
    EnableCoreFallback = true, // если Python недоступен, вернёмся к C# парсеру
    PythonExecutablePath = "python",
    TimeoutMilliseconds = 120000
};

LisDlisioSummary summary = client.ReadSummary(@"C:\data\sample.lis", options);
```

Что нужно на машине пользователя для базового сценария:
- установлен только `.NET Framework 4.8` (достаточно).

Дополнительно (опционально) для Python-режима:
- установлен `Python`,
- установлен пакет `dlisio` (`pip install dlisio`).

`LisDlisioClient` запускает отдельный Python-процесс, читает LIS через `dlisio` и возвращает структурный summary (`LogicalFiles`, `Dfsrs`, `Channels`) в типизированные C# модели.

### Нужен именно `dlisio`, но без установки Python на машину

Если принципиально требуется парсинг именно `dlisio`, но нельзя устанавливать Python в ОС, используйте
**self-contained bridge executable** (например, собранный заранее `PyInstaller`-ом), который уже содержит Python+`dlisio`.

Пример:

```csharp
var client = new LisDlisioClient();
var options = new LisDlisioOptions
{
    // Включаем приоритет готового bridge exe
    PreferBundledBridge = true,
    DlisioBridgeExecutablePath = @"C:\app\bridges\lis-dlisio-bridge.exe",

    // Требуем именно dlisio-режим (без fallback на Lis.Core)
    RequireDlisio = true,
    EnableCoreFallback = false
};

LisDlisioSummary summary = client.ReadSummary(@"C:\data\sample.lis", options);
```

Поведение:
- `RequireDlisio = true` гарантирует, что будет использован только `dlisio`-bridge.
- Если bridge недоступен/не запускается, вернётся `LisDlisioBridgeException`.
- Python при этом не требуется устанавливать в системе пользователя.
