# legacy-web-net48

The existing system: ASP.NET MVC 5 on .NET Framework 4.8, hosted with IIS
Express. This is the app being migrated away from, one page at a time.

## Requirements

- .NET Framework 4.8
- IIS Express

## Quick start

```bash
"C:\Program Files\IIS Express\iisexpress.exe" /path:"<repo>\legacy-web-net48" /port:5001
```

Served behind the gateway at `http://miicel.local/...` — see the
[root README](../README.md) for the full demo startup sequence.

## Pages

| Route | Behavior |
|---|---|
| `/Account/Login` | Logs in against Auth Manager, sets the shared `am_token` cookie |
| `/Home`, `/Profile` | Migrated — `AuthRoutingFilter` redirects these to the modern app (307, so POST bodies survive) |
| `/Orders` | Still owned by legacy |
| `/Contact` | Local-only demo of raw `Request.Form` vs. a bound model, plus a local file save |
| `/Uploads` | Isolated storage — deliberately **not** shared with the modern app, to contrast with `/SharedFiles` |
| `/SharedFiles`, `/SharedForms` | Call Auth Manager's global `/uploads` and `/forms` endpoints — same data visible from either app |

## How routing works

`AuthRoutingFilter` runs before every action except `Account`. It reads the
`am_token` cookie, asks Auth Manager's `/route` endpoint whether the
requested path belongs to legacy or modern, and redirects to modern when it
doesn't. See [AuthRoutingFilter.cs](Filters/AuthRoutingFilter.cs).

## Services

- `AuthManagerClient` — the HTTP client this app uses to call Auth Manager's `/route`, `/session`, `/uploads`, and `/forms`.
- `MockOrders` — in-memory order data for the `/Orders` demo page.
