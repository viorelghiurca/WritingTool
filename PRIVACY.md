# Privacy Policy

**Last updated:** January 2026

WritingTool is an open-source application that helps you improve your writing using AI providers. This privacy policy explains what data WritingTool handles and how.

## Data Collection

**WritingTool does not collect, store, or transmit any personal data to the developer or any third party.**

The application runs entirely on your local machine. No analytics, telemetry, or tracking is included.

## Data Transmission to AI Providers

When you use WritingTool to process text, the text you select is sent to the AI provider you have configured:

| Provider | Data Destination | Privacy Policy |
|----------|------------------|----------------|
| **Ollama (Local)** | Your local machine only | No external transmission |
| **Google Gemini** | Google's API servers | [Google AI Privacy](https://ai.google.dev/terms) |
| **OpenAI-compatible** | The API endpoint you configure | Depends on provider |

### What is transmitted

- The text you select for processing
- The action prompt (e.g., "proofread", "summarize")
- Your API key (to authenticate with the provider)

### What is NOT transmitted

- Your settings or configuration
- Your system information
- Any usage statistics
- Any other personal data

## Local Storage

WritingTool stores the following files locally on your machine:

- `settings.json` — Your preferences, API keys, and provider configuration
- `options.json` — Your custom action buttons and prompts

These files are stored in the application directory and are never transmitted anywhere.

## Your Control

- **You choose the provider**: You decide which AI provider to use
- **You provide the API key**: You control the relationship with the AI provider
- **You can use Ollama**: For complete privacy, use Ollama to keep all processing local
- **Open source**: You can inspect the source code to verify these claims

## Third-Party Services

When using cloud-based AI providers (Gemini, OpenAI-compatible), your text is subject to that provider's privacy policy and terms of service. We recommend reviewing their policies:

- [Google Gemini Terms](https://ai.google.dev/terms)
- [OpenAI Privacy Policy](https://openai.com/privacy)

## Changes to This Policy

Any changes to this privacy policy will be documented in this file and in the project's release notes.

## Contact

For privacy-related questions, please open an issue on the [GitHub repository](https://github.com/viorelghiurca/WritingTool).
