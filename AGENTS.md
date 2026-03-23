# AGENTS

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
- `Dlisio.Core` builds for `net48` and `net8.0`
- unit tests in `tests/Dlisio.Tests` pass
