# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run
```

Default dev URL from launch settings:
- `https://localhost:7240`

`Program.cs` serves static files and redirects `/` to `/index.html`.

## Architecture

QueryBot is a minimal .NET 8 web app with:
- no controllers
- no database
- no current integration with the rest of the Quex ecosystem

It exists as a standalone static-file site with a landing page and placeholder sign-up form.

Key files:
- `Program.cs`
  - builds the web app
  - enables static file hosting
  - redirects `/` to `/index.html`
- `wwwroot/index.html`
  - landing page markup
- `wwwroot/styles.css`
  - site styling

## Ecosystem Context

The workspace-level ecosystem document treats QueryBot as part of the documented workspace, but not as part of the production QuexPlatform deployment described for `quexai.co.uk`.

When editing:
- do not assume it shares runtime infrastructure with QuexPlatform
- do not introduce backend or database dependencies unless explicitly requested
- keep changes consistent with a static marketing-site style app unless the user asks for expansion

## Code Conventions

- `Nullable` and `ImplicitUsings` enabled
- Prefer simple static-file changes over unnecessary ASP.NET complexity
