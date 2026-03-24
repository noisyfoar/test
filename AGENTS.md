# AGENTS

Current project scope: **LIS79 only**. Do not add or reintroduce non-LIS-specific implementation unless explicitly requested.

## Local development baseline (Windows / .NET Framework 4.8)

- Primary target: `.NET Framework 4.8`.
- Linux/Cloud runtime compatibility is not a project goal.

### Quick environment checks

Run these commands first in local sessions:

```bash
dotnet --info
```

Expected: toolchain is available and can run .NET Framework builds/tests.

### Build and test baseline

```bash
dotnet test LisNet.sln
```

This validates:
- `Lis.Core` builds for `net48` (library target)
- unit tests in `tests/Lis.Tests` pass

## Implementation workflow rule

After each completed implementation step, follow this checklist before moving on:

1. Run relevant tests (at minimum `dotnet test LisNet.sln`).
2. Review test output and build logs for warnings/regressions.
3. Do a short reflection:
   - what changed,
   - what risks remain,
   - what should be improved in the next step.
4. If issues are found, fix them and rerun tests.
5. Only then proceed to the next implementation step.
