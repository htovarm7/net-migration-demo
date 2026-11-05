# gateway-net10

A reverse proxy that hides which app serves a request. The browser's URL
never changes and never reveals whether legacy or modern answered — that
decision is made server-side, per request.

## Requirements

- .NET 10 SDK

## Quick start

```bash
dotnet run
```

Listens on `http://localhost:5003` ([launchSettings.json](Properties/launchSettings.json)).

## How it works

Built directly on YARP's `IHttpForwarder.SendAsync` rather than YARP's
declarative route config, because the destination has to be decided
per-request by calling out to Auth Manager — not something a static route
table can express.

For every incoming request:

1. `/Account/*` always goes to legacy — login/logout live nowhere else.
2. No `am_token` cookie yet → legacy, whose own filter will redirect to login.
3. Otherwise, call Auth Manager's `GET /route?path=...` with the cookie as a
   Bearer token, and forward to whichever app it names. If Auth Manager is
   unreachable, fail toward legacy rather than dropping the request.

See [Program.cs](Program.cs).

## Configuration

| Setting | Description |
|---|---|
| `Upstreams:Legacy` | Base URL of the legacy app (`http://localhost:5001`) |
| `Upstreams:Modern` | Base URL of the modern app (`http://localhost:5002`) |
| `AuthManager:BaseUrl` | Base URL of Auth Manager (`http://localhost:5000`) |

Requests forwarded to legacy get their `Host` header overridden to
`localhost` — IIS Express only accepts that exact value, the same
workaround Traefik used before this gateway existed.
