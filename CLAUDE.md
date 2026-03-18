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

`Program.cs` serves static files, hosts Razor Pages for authenticated routes, and redirects `/` to `/index.html`.

## Architecture

QueryBot is a .NET 8 Razor Pages app with cookie authentication and EF Core persistence against the shared MySQL `user` table.

```text
Program.cs          -> app wiring: EF Core, cookie auth, Razor Pages, static files
Auth/               -> QueryBotAuthService (authenticates system="querybot" users)
Data/               -> QueryBotDbContext + User entity (shared user table)
Security/           -> IPasswordHasher + PasswordHasher (PBKDF2-SHA256)
Pages/              -> Razor Pages (Dashboard, Account/Login, Account/Logout)
wwwroot/            -> static marketing site (index.html, signup.html, styles.css, assets/)
```

### Key routes

- `/` → redirects to `/index.html` (public static home page)
- `/index.html`, `/signup.html` → public static pages served by middleware
- `/Account/Login` → anonymous login page (Razor Page)
- `/Account/Logout` → POST-only logout (Razor Page)
- `/Dashboard` → authenticated dashboard (Razor Page; redirects to login if unauthenticated)

### Auth pattern

Cookie authentication backed by the shared QuexPlatform MySQL `user` table.
Users are scoped by `system = "querybot"`. Password hashing uses PBKDF2-SHA256
in the format `$pbkdf2-sha256$<iterations>$<base64-salt>$<base64-hash>`, compatible
with QuexPlatform and Crafty.

`AuthorizeFolder("/")` protects all Razor Pages by default. Login and Logout are
explicitly `AllowAnonymous`. Static files bypass Razor Pages auth entirely.

### PathBase

In production QueryBot runs at `/querybot` behind Caddy. `PathBase=/querybot` is
injected via the docker-compose environment variable. Locally the PathBase is not
set and the app runs at root.

## Database

QueryBot reads from the shared `user` table (QuexPlatform MySQL database).
No EF migrations are needed — the table is owned by `QuexPlatform.Infrastructure`.

To create a QueryBot user, run:
```bash
.\scripts\seed-querybot-user.ps1   # from S:\quex\dev\quexplatform
```
The script calls `POST /users` on the QuexPlatform API and is idempotent (409 = already exists).

## Ecosystem Context

QueryBot is part of the QuexPlatform production deployment at `quexai.co.uk/querybot`.
It runs as a Docker container (`querybot` service in `docker-compose.yml`) reverse-proxied
by Caddy.

## Code Conventions

- `Nullable` and `ImplicitUsings` enabled
- Prefer Razor Pages over controllers for authenticated pages
- Static marketing pages remain as plain HTML in `wwwroot/`
- Do not remove or replace the static home page (`index.html`) with a Razor Page
