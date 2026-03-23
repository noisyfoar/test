# Maintenance Summary (LIS-only)

This document is a quick reference for future contributors.

## 1) Project layout

- `src/Lis.Core`  
  Core library (`Lis.Core.dll`) with all parsing/import/export logic.
- `src/Lis.Gui`  
  WinForms viewer for manual inspection (text and tables only, no charts).
- `tests/Lis.Tests`  
  Unit tests for transport parsing, record parsing, import/export, and high-level APIs.

## 2) Main entry points

### High-level parsing

- `LisFileParser`
  - `Parse(stream, options, metrics)` for full logical-file parsing.
  - `ParseCurves(stream, selectedMnemonics, metrics)` for memory-saving curve extraction.

### Raw transport import/export

- `LisImporter`
  - Reads LIS into `LisDocument` (list of `LisLogicalRecord`).
- `LisExporter`
  - Writes `LisDocument` back to LIS transport form.
  - Supports physical record splitting via `LisExportOptions.MaxPhysicalRecordLength`.

## 3) Parsing flow (simplified)

1. `LisIndexer` scans stream and builds offsets/types.
2. `LisLogicalFilePartitioner` groups records into logical files.
3. `LisLogicalFileParser` resolves each record into typed payload:
   - fixed/text records,
   - DFSR records,
   - FData frames/curves.

## 4) Readability conventions used in this codebase

- Keep public API methods small and explicit.
- Preserve caller stream position in high-level APIs.
- Throw clear, domain-specific exceptions (`LisParseException`) for format errors.
- Prefer helper methods over very large switch/case blocks.
- Add focused comments only where behavior is non-obvious (record stitching, segmentation).

## 5) Safe refactoring checklist

Before and after each non-trivial refactor:

1. Run:
   - `dotnet test LisNet.sln`
2. If GUI was touched, also run:
   - `dotnet build src/Lis.Gui/Lis.Gui.csproj`
3. Verify there are no accidental changes in file/namespace naming.
4. Update `README.md` and this summary if API behavior changed.
