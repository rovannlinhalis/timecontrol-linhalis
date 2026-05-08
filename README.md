# TimeControl Agent

TimeControl Agent is a small Windows desktop application that collects activity events from the active window and sends them to a TimeControl API.

The agent runs in the Windows notification area and keeps collecting events in the background. It does not show reports, charts, grouping screens or time summaries. Those features belong to the TimeControl web application.

## What It Collects

Every 5 seconds, the agent checks the current foreground window and records:

- event date and time;
- process name;
- active window title;
- computer name;
- Windows user name;
- mouse pointer X/Y position.

Events are sent to the API endpoint configured by the user. If the API is offline, unavailable or the computer is disconnected, the events remain cached locally and are sent later.

## Requirements

- Windows.
- .NET Framework 4.5 or newer.
- A TimeControl account with an agent token.
- Network access to the TimeControl API.

## Build

From the solution folder:

```powershell
dotnet build TimeControl.Agent.WinForms\TimeControl.Agent.WinForms.csproj -c Release
```

The executable is generated at:

```text
TimeControl.Agent.WinForms\bin\Release\TimeControl.Agent.exe
```

## First Run

1. Open `TimeControl.Agent.exe`.
2. The configuration window appears automatically if the agent is not configured.
3. Fill in the API URL.
4. Fill in the token generated in your TimeControl profile.
5. Keep `Iniciar com o Windows` checked if the agent should start automatically.
6. Click `Testar` to validate the connection.
7. Click `Salvar`.

The API URL must point to the ingest endpoint, for example:

```text
https://your-domain.com/api/ingest
```

## Daily Use

After configuration, the agent stays in the Windows notification area.

Use the tray icon menu to:

- open `Configuracoes`;
- run `Sincronizar agora`;
- exit the agent.

Double-clicking the tray icon also opens the configuration window.

## Local Files

Settings are stored at:

```text
%APPDATA%\TimeControl\settings.json
```

Local event cache files are stored beside the executable:

```text
yyyyMMdd.atc
```

These cache files prevent event loss when the API cannot be reached. The agent reads the cached files, asks the API for the last received event through `/last`, and sends only newer events in batches.

## Startup With Windows

When `Iniciar com o Windows` is enabled, the agent adds a registry entry under the current user:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\TimeControl
```

Disabling the option removes that entry.

## Troubleshooting

If the connection test fails, check:

- the API URL includes `/api/ingest`;
- the token was copied from the TimeControl profile page;
- the API is reachable from this computer;
- the token has not been regenerated after this agent was configured.

If events are not appearing immediately, use `Sincronizar agora` from the tray menu. The agent also syncs automatically every 60 seconds.

If you move the executable to another folder, existing local `.atc` cache files will not move automatically.

## Project Scope

This project intentionally contains only the public desktop collector:

- Windows Forms shell and tray icon;
- active window event collection;
- local cache;
- API synchronization;
- minimal settings screen.

It does not include legacy update screens, event visualization, grouping, charts, AppCenter telemetry or database dependencies.
