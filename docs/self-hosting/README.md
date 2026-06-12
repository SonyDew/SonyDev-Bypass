# Self-Hosting Guide

This repository ships without production backend credentials. To run the app yourself, you need to host the catalog and update endpoints and point `sonydev.runtime.json` at them.

## Required Endpoints

### Catalog

Preferred option: expose a JSON catalog via `catalog_api_url`.

Expected responses:

```json
{
  "success": true,
  "games": ["Game A", "Game B"]
}
```

```json
{
  "success": true,
  "files": [
    {
      "path": "file1.bin",
      "size": 1234,
      "url": "https://example.invalid/files/Game%20A/file1.bin"
    }
  ]
}
```

Legacy fallback: `games_base_url` may still point to a directory listing root where each game is a subdirectory.

`games_base_url` should point to a directory listing root where each game is a subdirectory.

Example layout:

```text
/bypasses/
  Game A/
    file1.bin
    subfolder/file2.bin
  Game B/
```

The current client still supports web directory listings, so your server can expose a listing format the app can read. The included nginx example uses `autoindex on` for that fallback.

### Updates

`updates_base_url` should contain:

- `latest.json`
- `releases.<channel>.json`
- `assets.<channel>.json` if your workflow uses it
- `SonyDevBypass-<channel>-Setup.exe` or your configured package naming
- the Velopack `.nupkg` files for the channel

The checked-in [`Releases/latest.json`](../../Releases/latest.json) shows the expected shape for `latest.json`.

## Shared Secret

If you want header-based protection, configure your server to expect `X-Secret-Key` and set the same value in `sonydev.runtime.json` or the `SONYDEV_SECRET_KEY` environment variable.
Do not ship a real shared secret inside a public open-source client. If the client is public, the secret is recoverable. Use shared secrets only for private/internal builds or move authorization fully to the server side.

## nginx

Start from [`nginx.example.conf`](nginx.example.conf) and replace the placeholder domains, paths, and secrets with your own values.
