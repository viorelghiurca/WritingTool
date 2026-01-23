# Troubleshooting

## Hotkey does not work

- Check that no other app is using the same hotkey.
- Change the shortcut in Settings (try something like `ctrl+alt+space`).
- Run WritingTool as a normal user (avoid mixed admin/non-admin hotkey conflicts).

## “API key” errors

- Make sure the key is valid and not expired.
- Verify you selected the correct provider in Settings.
- For OpenAI-compatible providers, confirm the API base URL is correct.

## Ollama does not respond

- Verify Ollama is running:

```powershell
ollama list
```

- Confirm the base URL is `http://localhost:11434`.
- Make sure the model name in Settings matches the model you pulled.

## The app shows only a single default button

WritingTool loads `options.json` from the application directory. If it can’t find it, it falls back to a minimal default configuration.

- Ensure `options.json` is present next to the executable when running unpackaged.

