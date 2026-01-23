# Buttons and prompts (`options.json`)

WritingTool actions are configured in `options.json`.

Each button has:
- `name`: label shown in the UI
- `prefix`: text prepended before your selected/copied content
- `instruction`: the system instruction for the provider
- `icon`: path to an `.ico` file inside `Assets/Icons`
- `openInWindow`: when `true`, opens the result in a larger window

## Common examples

### Proofread

Good for emails, tickets, and documents. The app prompt is designed to output only corrected text.

### Rewrite (tone)

“Friendly”, “Professional”, and “Concise” are all variants of rewriting with a different instruction.

### Summary / Key points

These actions are typically configured to output Markdown for readability.

## Tips for strong prompts

- Be explicit about output format (e.g. “output ONLY the rewritten text”).
- Keep instructions short and consistent.
- If you want structured output, specify Markdown.
- Avoid asking the model to “explain” unless you really want commentary.

