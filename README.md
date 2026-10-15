# Auth Manager

Auth Manager helps you move services from .NET Framework 4.x to .NET 10 without breaking user login. It does two things:

1. **Password migration.** Users with old .NET Framework password hashes can still log in. After a successful login, their hash is upgraded to the new format automatically.
2. **Page routing.** After login, the response tells you whether a page is served by the legacy app or the new one.

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
  "destination": "legacy",
  "redirectUrl": "..."
}
```

- `hashMigrated` is `true` when the password hash was upgraded during this login.
- `destination` is `Legacy` or `Modern`.

### Get the route for a page

```
GET /route?path=/Orders
```

Send the token as a Bearer header. The response contains `destination` and `url`.

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

## Next steps

- Replace the redirect with a reverse proxy (YARP) that uses the same route table.
- Share token validation with the legacy app (OWIN JWT) so both apps support single sign-on.
- Replace SQLite with the real user database by mapping the existing `AspNetUsers` table.
