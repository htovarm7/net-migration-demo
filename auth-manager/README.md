# Auth Manager

Auth Manager is the one service neither `legacy-web-net48` nor `modern-web-net10`
owns. Both apps call it for everything that has to stay consistent between them:

1. **Password migration.** Users with old .NET Framework password hashes can still log in. After a successful login, their hash is upgraded to the new format automatically.
2. **Page routing.** A gateway asks Auth Manager, per request, whether a page is currently served by the legacy app or the new one.
3. **Shared session.** Company, name, and role live in Redis, keyed by user — both apps read the exact same object.
4. **Global endpoints.** `/uploads` and `/forms` have a single contract and a single store, callable from either app with identical results.

## Requirements

- .NET 10 SDK

## Quick start

```bash
dotnet run --project src/AuthManager.Api
```

This starts the API in the Development profile. It uses a SQLite database (`auth.db`) and creates some test users.

Run the tests with:

```bash
dotnet test
```

## Test users

| User  | Password     |
|-------|--------------|
| user1 | 12345678     |
| user2 | AbcD$1234    |

These users only exist in the Development profile.

## Configuration

Outside Development, you must set these values:

| Setting     | Description                                  |
|-------------|----------------------------------------------|
| `Jwt__Key`  | Signing key for tokens. At least 32 characters. |
| `Seed__Enabled` | Set to `true` to create the test users. |

Page routing is set in the `Routing` section of `appsettings.json`.

## API

### Log in

```
POST /auth/login
```

Request body:

```json
{
  "user": "user1",
  "password": "12345678",
  "guid": "optional",
  "returnPath": "optional"
}
```

Response:

```json
{
  "token": "...",
  "expiresUtc": "...",
  "hashMigrated": true,
  "destination": "Legacy",
  "redirectUrl": "...",
  "companyId": "...",
  "companyName": "..."
}
```

- `hashMigrated` is `true` when the password hash was upgraded during this login.
- `destination` is `Legacy` or `Modern`.

### Get the route for a page

```
GET /route?path=/Orders
```

Send the token as a Bearer header. The response contains `destination` and `url`. The gateway calls this on every request to decide which app answers.

### Get the shared session

```
GET /session
```

Bearer token required. Returns the `SessionContext` stored in Redis for that user: `userId`, `userName`, `companyId`, `companyName`, `firstName`, `lastName`, `role`.

### Global uploads

```
POST /uploads   (multipart: file, source)
GET  /uploads
```

Bearer token required. Either app can post a file here; both see the same list back. Files live on disk under `App_Data/SharedUploads`.

### Global forms

```
POST /forms   (multipart: firstName, lastName, email, source, file?)
GET  /forms
```

Same idea as uploads, but for structured data plus history. Records live under `App_Data/SharedForms`.

### Health check

```
GET /health
```

## How the password migration works

Old .NET Framework hashes (format v2) look like this:

```
Base64( 0x00 | salt[16] | PBKDF2-HMACSHA1(password, salt, 1000 iterations, 32 bytes) )
```

ASP.NET Core Identity can verify these hashes. When it sees an old hash and the password is correct, it returns `SuccessRehashNeeded`. `UserManager.CheckPasswordAsync` then saves the password again in the new format (v3).

## How the shared session works

Redis is the single source of truth. `LoginService` writes a `SessionContext`
(company, name, role) to Redis on every successful login, keyed by user ID.
Both `legacy-web-net48` and `modern-web-net10` read it back through `GET
/session` — neither app keeps its own copy, so they can never drift apart.

## Who calls this service

- `gateway-net10` — asks `/route` on every request to decide which app answers, and `/session` isn't called by the gateway itself.
- `legacy-web-net48` and `modern-web-net10` — call `/session`, `/uploads`, and `/forms` server-to-server with the user's own token.

## Next steps

- Replace SQLite with the real user database by mapping the existing `AspNetUsers` table.
- Share Redis session invalidation on logout across both apps.
