# modern-web-net10

The system the legacy app is migrating to: ASP.NET Core MVC on .NET 10. Pages
land here as they're migrated off `legacy-web-net48`.

## Requirements

- .NET 10 SDK

## Quick start

```bash
dotnet run
```

Listens on `http://localhost:5002` ([launchSettings.json](Properties/launchSettings.json)).
Served behind the gateway at `http://miicel.local/...` — see the
[root README](../README.md) for the full demo startup sequence.

## Pages

| Route | Behavior |
|---|---|
| `/Home`, `/Profile` | Migrated from legacy — show the shared session (company, name, role) from Auth Manager |
| `/Contact` | Local-only demo of model binding |
| `/Uploads` | Isolated storage — deliberately **not** shared with legacy, to contrast with `/SharedFiles` |
| `/SharedFiles`, `/SharedForms` | Call Auth Manager's global `/uploads` and `/forms` endpoints — same data visible from either app |

## Authentication

JWT bearer auth, but the token is read from the `am_token` cookie instead of
an `Authorization` header (`OnMessageReceived` in [Program.cs](Program.cs)) —
the same cookie legacy sets on login, so a session started in one app is
valid in the other. `MapInboundClaims = false` is required so the JWT's
`sub` claim isn't remapped to `NameIdentifier`. An unauthenticated request is
redirected to legacy's `/Account/Login`, not to a local login page — this app
has none.

## Services

- `SessionClient` — calls Auth Manager's `/session`.
- `SharedFilesClient`, `SharedFormsClient` — call Auth Manager's `/uploads` and `/forms`.
- `CompanyContextMiddleware` — loads the session once per request and makes it available to controllers via `ViewBag`.
