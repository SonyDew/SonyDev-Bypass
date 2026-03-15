# Contributing

## Before You Start

- Do not commit secrets, tokens, private URLs, or production infrastructure data.
- Use [`sonydev.runtime.example.json`](sonydev.runtime.example.json) as the base for a local `sonydev.runtime.json`.
- The local runtime config is ignored by git on purpose.

## Development Flow

1. Create your local runtime config.
2. Build with `dotnet build .\SonyDevBypass.sln`.
3. Test with your own self-hosted or local backend.
4. Keep changes focused and document any user-visible behavior changes.

## Pull Request Expectations

- Explain what changed and why.
- Mention any config, packaging, or migration impact.
- Do not include production keys or live server credentials.
- Do not submit changes that reintroduce proprietary-only terms into the GPL source tree.

## Branding Reminder

You may contribute to the official project here, but if you redistribute your own modified fork you must follow [`TRADEMARKS.md`](TRADEMARKS.md).
