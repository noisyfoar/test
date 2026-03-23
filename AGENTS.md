# AGENTS

Current project scope: **LIS79 only**. Do not add or reintroduce DLIS-specific implementation unless explicitly requested.

## Cursor Cloud specific instructions

- This repository configures Cloud Agent environment via `.cursor/environment.json`.
- On agent startup, `.cursor/install-dotnet8.sh` ensures `.NET SDK 8` is installed and available on `PATH`.

### Quick environment checks

Run these commands first in Cloud sessions:

```bash
dotnet --info
dotnet --list-sdks
```

Expected: SDK list includes a `8.x` entry.

### Build and test baseline

```bash
dotnet test DlisioNet.sln
```

This validates:
- `Dlisio.Core` builds for `net48` (library target)
- LIS test harness builds/runs under `net8.0`
- unit tests in `tests/Dlisio.Tests` pass

## Implementation workflow rule

After each completed implementation step, follow this checklist before moving on:

1. Run relevant tests (at minimum `dotnet test DlisioNet.sln`).
2. Review test output and build logs for warnings/regressions.
3. Do a short reflection:
   - what changed,
   - what risks remain,
   - what should be improved in the next step.
4. If issues are found, fix them and rerun tests.
5. Only then proceed to the next implementation step.
