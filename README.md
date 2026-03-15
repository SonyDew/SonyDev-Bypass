# SonyDev Bypass

Open-source Windows desktop client for syncing a remote catalog, checking releases, and downloading packaged files through a single launcher UI.

## What This Repository Contains

- The current WPF/.NET 8 desktop application
- Release packaging scripts for Velopack and Inno Setup
- A safe public source drop with no production API secrets
- Documentation for self-hosting the catalog and update endpoints

This repository does not include:

- production `X-Secret-Key` values
- private server credentials
- live infrastructure secrets
- internal-only test releases

## Project Status

The source code is published so users can inspect the application, verify what it does, and contribute improvements. The official production backend is not bundled here. To run the app against your own infrastructure, create a local runtime config from [`sonydev.runtime.example.json`](sonydev.runtime.example.json).

## Stack

- .NET 8
- WPF
- Velopack for update packaging
- Inno Setup for the wrapper installer

## Repository Layout

- `SonyDevBypass.App` - main desktop application
- `Installer` - Inno Setup wrapper and helper scripts
- `Releases` - release metadata such as `latest.json`
- `docs/self-hosting` - example deployment and backend notes

## Quick Start

### 1. Install prerequisites

- .NET 8 SDK
- Velopack CLI: `dotnet tool update -g vpk --version 0.0.1298`
- Inno Setup 6 if you want to build the installer wrapper

### 2. Create local runtime config

Copy [`sonydev.runtime.example.json`](sonydev.runtime.example.json) to `sonydev.runtime.json` and replace the placeholder endpoints with your own.

The file is intentionally ignored by git.

### 3. Build the app

```powershell
dotnet build .\SonyDevBypass.sln
```

### 4. Run a release publish

```powershell
dotnet publish .\SonyDevBypass.App\SonyDevBypass.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\artifacts\publish
```

## Runtime Configuration

The app reads runtime settings from `sonydev.runtime.json`. It also supports these environment variables:

- `SONYDEV_RUNTIME_CONFIG`
- `SONYDEV_GAMES_BASE_URL`
- `SONYDEV_UPDATES_BASE_URL`
- `SONYDEV_SECRET_KEY`
- `SONYDEV_PACKAGE_ID`
- `SONYDEV_PACKAGE_TITLE`
- `SONYDEV_UPDATE_CHANNEL`
- `SONYDEV_INSTALL_DIR`
- `SONYDEV_OFFICIAL_WEBSITE`

Default values in the public repository point to `example.invalid` on purpose so the open-source build cannot accidentally target private infrastructure.

## Self-Hosting

The app expects:

- a catalog endpoint that exposes directory listings for downloadable files
- an update endpoint that serves `latest.json`, Velopack metadata, and setup packages
- optional header-based protection if you want to require a shared secret

Start with:

- [`docs/self-hosting/README.md`](docs/self-hosting/README.md)
- [`docs/self-hosting/nginx.example.conf`](docs/self-hosting/nginx.example.conf)

## Release Workflow

### Build packages

```powershell
vpk pack `
  --outputDir .\Releases `
  --channel win-x64-beta `
  --runtime win-x64 `
  --packId SonyDevBypass `
  --packVersion 1.2.4-beta `
  --packDir .\artifacts\publish `
  --mainExe SonyDevBypass.exe `
  --packTitle "SonyDev Bypass" `
  --packAuthors "SonyDev" `
  --icon .\SonyDevBypass.App\Assets\icon.ico
```

### Build the Inno Setup wrapper

```powershell
powershell -ExecutionPolicy Bypass -File .\Installer\build-inno.ps1
```

If you want Windows metadata patched on the published executable, place `rcedit-x64.exe` into `Installer` before running [`Installer/patch-apphost-metadata.ps1`](Installer/patch-apphost-metadata.ps1).

## Open Source, License, and Branding

Source code in this repository is released under GPL-3.0-or-later. See [`LICENSE`](LICENSE).

Brand names, logos, icons, and official project identity are handled separately. If you redistribute a modified build, keep the copyright and license notices, mark your changes, and do not present the result as an official SonyDev release.

Relevant files:

- [`NOTICE`](NOTICE)
- [`TRADEMARKS.md`](TRADEMARKS.md)
- [`SonyDevBypass.App/Assets/README.md`](SonyDevBypass.App/Assets/README.md)

## Contributing and Security

- Contribution guide: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Security reporting: [`SECURITY.md`](SECURITY.md)

## Official Links

- Website: [sonydev.de](https://sonydev.de/)
- Releases: [github.com/SonyDew/SonyDev-Bypass/releases](https://github.com/SonyDew/SonyDev-Bypass/releases)
- Issues: [github.com/SonyDew/SonyDev-Bypass/issues](https://github.com/SonyDew/SonyDev-Bypass/issues)
