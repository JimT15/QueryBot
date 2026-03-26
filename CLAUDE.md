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
- `http://localhost:5057`

`Program.cs` serves static files, hosts Razor Pages for authenticated routes, and exposes a config endpoint for the static signup page.

## Architecture

QueryBot is a .NET 8 Razor Pages app with cookie authentication and EF Core persistence against the shared MySQL `user` table.

```text
Program.cs          -> app wiring: EF Core, cookie auth, Razor Pages, static files, config endpoint
Auth/               -> QueryBotAuthService (authenticates system="querybot" users)
Data/               -> QueryBotDbContext + User entity (shared user table, read-only from QueryBot's perspective)
Security/           -> IPasswordHasher + PasswordHasher (PBKDF2-SHA256)
Pages/              -> Razor Pages (Dashboard, Account/Login, Account/Logout)
wwwroot/            -> static marketing site (index.html, signup.html, signup.js, styles.css, assets/)
```

### Key routes

- `/` → redirects to `/index.html` (public static home page)
- `/index.html`, `/signup.html` → public static pages served by middleware
- `/querybot-config.js` → dynamic endpoint serving captcha site key and dev mode flag to the static signup page
- `/Account/Login` → anonymous login page (Razor Page)
- `/Account/Logout` → POST-only logout (Razor Page)
- `/dashboard` → authenticated dashboard (Razor Page; redirects to login if unauthenticated)

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
No EF migrations are needed here — the schema is owned by `QuexPlatform.Infrastructure`.

QueryBot users are created in two ways:
- **Self-service signup**: users register via `signup.html` → `POST /api/querybot/signup` on the QuexPlatform API, which handles account creation, welcome email, and email verification.
- **Admin seed** (internal accounts only): `.\scripts\seed-querybot-user.ps1` from `S:\quex\dev\QuexPlatform` calls `POST /users` on the QuexPlatform API and is idempotent (409 = already exists).

## QueryBot Signup and Onboarding Flow

QueryBot users are created and activated entirely through the QuexPlatform API and Worker — this app only handles the authenticated dashboard session.

1. User submits `signup.html` (hCaptcha-protected) → `POST /api/querybot/signup` (anonymous endpoint on QuexPlatform API).
2. QuexPlatform creates a shadow client + user record (unverified) and queues a `QBWelcomeEmail` job.
3. Worker sends a branded HTML email with a one-time verification link: `https://quexai.co.uk/api/querybot/verify?userId=X&token=Y`.
4. User clicks the link → `GET /api/querybot/verify` (anonymous, QuexPlatform API) → marks user verified, queues `QBOnboardingEmail`, returns a branded HTML confirmation page.
5. Worker sends the onboarding email with the dashboard URL and personalised document submission address: `client-{clientId}-{routingToken}@{inbound-domain}`.
6. User logs in at `/Account/Login` using the password set at signup.

Inbound document submission:
- User emails a document to their personalised address.
- Postmark delivers to `POST /webhooks/inbound-email` (QuexPlatform API).
- QuexPlatform creates a `QBDocRequest` parent job and one `QBRequestDocUpload` child job per attachment.
- Per-attachment processing chain: `QBRequestDocUpload → IdentifyPrompts → GenerateAnswers → CompleteDocument → QBRequestDocEmail`.
- `QBRequestDocEmail` fetches the assembled file from DocumentHandler and emails it back to the user as an attachment.
- **Only `.docx` and `.xlsx` attachments are supported end-to-end.** PDF, `.txt`, and other formats are not supported — the pipeline uses Word/Excel COM automation and will fail on unsupported types.

Dashboard model-doc training:
- User uploads a model document via the dashboard.
- Processing chain: `QBTrainAI → QBModelDocUpload → ExtractLibraryDocContent → ChunkDocument`.
- `QBTrainAIEmail` sends a confirmation email to the user when training (chunking) completes.

This app has no involvement in the email or document processing pipeline — it only provides the authenticated dashboard UI.

## Ecosystem Context

QueryBot is part of the QuexPlatform production deployment at `quexai.co.uk/querybot`.

- Runs as the `querybot` Docker service defined in `/opt/quex/QuexPlatform/docker-compose.yml`
- Source on VM: `/opt/quex/QuexPlatform/querybot-site` (a clone of this repo)
- Reverse-proxied by Caddy: `/querybot*` → querybot container (PathBase=/querybot)
- All signup, email verification, and document processing is driven by the QuexPlatform `api` and `worker` services

Deploy (QueryBot changes only):
```bash
cd /opt/quex/QuexPlatform
cd querybot-site && git pull --ff-only && cd ..
docker compose build querybot && docker compose up -d
```

Deploy (combined QuexPlatform + QueryBot changes):
```bash
cd /opt/quex/QuexPlatform
git pull --ff-only
cd querybot-site && git pull --ff-only && cd ..
docker compose build && docker compose up -d
```

## Code Conventions

- `Nullable` and `ImplicitUsings` enabled
- Prefer Razor Pages over controllers for authenticated pages
- Static marketing pages remain as plain HTML in `wwwroot/`
- Do not remove or replace the static home page (`index.html`) with a Razor Page
