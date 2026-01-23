# Providers

WritingTool supports multiple backends. You choose the provider in **Settings**.

## Gemini (Recommended)

1. Create an API key in Google AI Studio.
2. Open WritingTool **Settings**.
3. Select **Gemini (Recommended)**.
4. Paste your API key and (optionally) change the model name.

## OpenAI Compatible (For Experts)

This option works with OpenAI and any OpenAI-compatible API.

In **Settings**:
- **API Key**: your provider key
- **API Base URL**: e.g. `https://api.openai.com/v1`
- **Model**: e.g. `gpt-4o-mini`

If your provider supports them, you can also set:
- **Organisation**
- **Project**

## Ollama (Local)

Ollama runs models on your machine.

1. Install Ollama.
2. Pull and run a model, for example:

```powershell
ollama run llama3.1:8b
```

3. In WritingTool **Settings**, select **Ollama (For Experts)**.
4. Ensure:
   - API base: `http://localhost:11434`
   - Model name matches what you pulled

## Notes

- If you use cloud providers, your text is sent to that provider’s API endpoint.
- If you use Ollama, text stays local on your machine.

