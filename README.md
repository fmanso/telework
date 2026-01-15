# Telework Tracker

Telework Tracker is a lightweight desktop app for tracking your office/home work distribution.

It is built as a **Windows desktop app** using:

- **Electron** for the desktop shell
- **ASP.NET (Blazor Server)** for the UI and application logic
- **SQLite** for local storage

The Electron process starts the .NET server locally and loads it at `http://localhost:5123`.

## Features

- Track telework vs office distribution
- Local-first: everything stored in a local SQLite database
- Runs fully offline (after installation)

## Download / Install (Windows)

1. Download the latest installer from the project Releases page.
2. Run the installer: `Telework Tracker Setup <version>.exe`.

If Windows SmartScreen warns about an unknown publisher, that’s expected for unsigned builds.

## Quick Start (Development)

### Prerequisites

- Windows 10/11
- **.NET SDK** (the project targets `net10.0`)
- **Node.js + npm**

### Run in dev mode

From the repo root:

```bat
.\run-dev.bat
```

This will:

- Build the .NET project (`TeleworkApp`)
- Install Electron dependencies (if needed)
- Start Electron with `--dev`, which runs `dotnet run --no-build`

### Stop / free the port

The app uses `http://localhost:5123`.

If you end up with a stuck process occupying the port, run:

```bat
.\kill-app.bat
```

## Build a Windows installer

From the repo root:

```bat
.\build.bat
```

This will:

1. `dotnet publish` self-contained to `electron\dotnet\`
2. `npm install` in `electron\`
3. `electron-builder --win` to produce an NSIS installer

Output:

- Installer: `electron\dist\Telework Tracker Setup <version>.exe`

### Build troubleshooting

#### `Cannot create symbolic link : A required privilege is not held by the client`

Electron Builder downloads a helper archive (`winCodeSign`) that contains symlinks. On Windows, creating symlinks requires elevated rights unless Developer Mode is enabled.

Fix options:

- Enable **Developer Mode** in Windows (allows symlink creation without admin)
- Or run the build from an **Administrator** terminal

#### `cannot find specified resource "icon.ico"`

This repo currently ships without branding assets. Electron Builder will use a default icon.

## Project structure

- `TeleworkApp/` — ASP.NET (Blazor Server) app
- `TeleworkApp.sln` — Visual Studio solution
- `electron/` — Electron wrapper and packaging
  - `electron/main.js` — starts the .NET process and opens the window
  - `electron/preload.js` — safe preload bridge (currently minimal)
  - `electron/package.json` — Electron Builder configuration

## Architecture (How it works)

- Electron starts.
- `electron/main.js` spawns the .NET app:
  - Dev: `dotnet run --no-build` in `TeleworkApp/`
  - Prod: runs `TeleworkApp.exe` bundled under `resources/dotnet/`
- The .NET app listens on `http://localhost:5123`.
- Electron loads that URL in a `BrowserWindow`.

### Data storage

The .NET app uses SQLite via Entity Framework Core.

- DB file: `telework.db`
- Location: next to the .NET executable (`AppContext.BaseDirectory`)

## Contributing

Contributions are welcome.

- Open an issue for bugs/feature requests.
- Keep PRs focused and small.
- Prefer changes that keep the app **offline-first** and **local-first**.

### Development tips

- If port `5123` is busy, use `kill-app.bat`.
- The Electron process attempts to kill the .NET process tree on exit (Windows `taskkill /t`).

## License

MIT
