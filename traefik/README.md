# traefik

The public edge. One domain for the whole demo, so the migration is
invisible from the outside.

## Requirements

- Docker

## Quick start

```bash
docker compose up -d
```

Dashboard: `http://localhost:8080`

## Routing

| Host | Routed to |
|---|---|
| `miicel.local` | `gateway-net10` (`host.docker.internal:5003`) |
| `auth.miicel.local` | `auth-manager` (`host.docker.internal:5000`) |

`miicel.local` and `auth.miicel.local` both need to resolve to `127.0.0.1` —
add them to your hosts file. `auth.miicel.local` is infrastructure shared by
both apps, not "legacy vs. modern," so it's routed directly rather than
through the gateway.

See [dynamic.yml](dynamic.yml).
