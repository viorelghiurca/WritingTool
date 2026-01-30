# WritingTool

System-wide writing assistance for Windows — activate anywhere with a hotkey, pick an action, and get clean, paste-ready output.

Built by **Viorel Ghiurca** (Software Developer).  
Support the project: `https://buymeacoffee.com/viorelghiurca`

## Highlights

- **System-wide hotkey**: bring up the UI while you work (default: `Ctrl+Space`)
- **Fast writing actions**: proofread, rewrite, adjust tone, summarize, extract key points, translate, convert to tables
- **Multiple providers**: Google Gemini, OpenAI-compatible APIs, and **Ollama (local)**
- **Modern WinUI 3 UI**: Mica backdrop, smooth animations
- **Configurable buttons**: edit action buttons and prompts in `options.json`
- **Bring your own key**: you control which provider you use and what it costs

## Quick start

### Option A: Download a release

- Go to GitHub **Releases** and download the latest build for your architecture.

### Option B: Build from source

**Requirements**
- Windows 10/11
- Visual Studio 2022 (Desktop development with C++) or `dotnet` SDK
- .NET 8 SDK

**Build / run**
- Open `WritingTool.sln` in Visual Studio and run.
- Or build via CLI:

```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Setup (providers)

Open **Settings** in the tray menu and select a provider:

- **Gemini (Recommended)**: add your API key and (optionally) model name.
- **OpenAI Compatible**: set API base URL + API key + model name.
- **Ollama (Local)**: install Ollama, run a model, and point WritingTool at `http://localhost:11434`.

See detailed guides:
- `docs/getting-started.md`
- `docs/providers.md`

## Configuration files

- **`settings.json`**: user-specific settings (provider choice, keys, theme, hotkey).  
  This file is **ignored by git** on purpose.
- **`settings.example.json`**: safe template you can copy to `settings.json`.
- **`options.json`**: action buttons (names, icons, prompts, "open in window" behavior).

## Privacy

WritingTool sends text only to the provider you select.

- If you use **Ollama**, text stays on your machine.
- If you use a cloud provider, text is sent to that provider's API endpoint.

See [`PRIVACY.md`](PRIVACY.md) for the full privacy policy.

## Code Signing Policy

Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

**Team roles:**
- Owner / Approver / Reviewer: [Viorel Ghiurca](https://github.com/viorelghiurca)

## Windows SmartScreen

When you first run WritingTool, Windows SmartScreen may show a warning because the application is new. This is normal for new software.

**To run the application:**
1. Click **"More info"** in the SmartScreen dialog
2. Click **"Run anyway"**

The application is open source — you can inspect the code and build it yourself if you prefer.

## Documentation

- `docs/getting-started.md` — first run, hotkey, basic workflow
- `docs/buttons-and-prompts.md` — customizing `options.json` (buttons, icons, windows)
- `docs/providers.md` — Gemini / OpenAI-compatible / Ollama setup
- `docs/troubleshooting.md` — common issues and fixes

## Contributing

PRs and issues are welcome. Please read `CONTRIBUTING.md` first.

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

See `LICENSE` for details.
