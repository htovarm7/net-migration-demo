# Auth Manager (PoC)

Proof of concept on **.NET 10** to migrate services from .NET Framework 4.x to .NET 10 without breaking login:

1. **Password hash migration**: users with ASP.NET Identity hashes from .NET Framework (v2 format) sign in against Identity Core; on a successful check the hash is regenerated as v3.
2. **Per-page routing**: after login, the response tells whether the page is served by the legacy app or the new one (`Routing:Pages`).

## Legacy hash format (v2)
`Base64( 0x00 | salt[16] | PBKDF2-HMACSHA1(password, salt, 1000 iterations, 32 bytes) )`.
Identity Core's `PasswordHasher<T>` verifies it and returns `SuccessRehashNeeded`; `UserManager.CheckPasswordAsync` then stores the new v3 hash (`0x01`).

## Endpoints
- `POST /auth/login` `{ "user", "password", "guid"?, "returnPath"? }` → `{ token, expiresUtc, hashMigrated, destination, redirectUrl }`
- `GET /route?path=/Orders` (Bearer) → `{ destination, url }`
- `GET /health`

## Run
```bash
dotnet run --project src/AuthManager.Api   # Development profile: SQLite auth.db + seeded users
dotnet test
```
Seeded users (legacy hashes): `user1` / `12345678`, `user2` / `AbcD$1234`.
Outside Development, set `Jwt__Key` (at least 32 characters) and `Seed__Enabled`.

## Next steps
- Replace the redirect with a reverse proxy (YARP) using the same route table.
- Share token validation with the legacy app (OWIN JWT) for SSO.
- Replace SQLite with the real user database (map the existing `AspNetUsers` table).
