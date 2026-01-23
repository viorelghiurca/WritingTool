# Contributing

Thanks for your interest in contributing to WritingTool!

## Ways to contribute

- **Bug reports**: include steps to reproduce, expected vs actual behavior, and screenshots if helpful.
- **Feature requests**: describe the use case and why it matters.
- **Pull requests**: fixes, improvements, new providers, UI polish, docs improvements.

## Development setup

### Requirements

- Windows 10/11
- Visual Studio 2022 (recommended) with Windows App SDK / WinUI 3 tooling
- .NET 8 SDK

### Build & run

```powershell
dotnet restore
dotnet build
dotnet run
```

## Pull request guidelines

- Keep PRs focused (one change per PR when possible).
- Follow existing code style and patterns.
- Prefer small, reviewable commits.
- Update docs if behavior changes.
- Do not commit machine-specific files or build outputs (e.g. `bin/`, `obj/`, `*.user`, `settings.json`).

## Security

If you find a security issue, please follow `SECURITY.md` instead of opening a public issue.

## License note

By contributing, you agree that your contributions are licensed under the project license in `LICENSE`.

