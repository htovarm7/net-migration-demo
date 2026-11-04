# Migration Platform

A proof of concept for migrating a .NET Framework 4.8 system to .NET 10 one screen at a
time, without breaking the user's login or session.

## Projects

- **[auth-manager](auth-manager/)** — the identity and migration service. Validates
  credentials, migrates legacy password hashes transparently, decides which app owns a
  given page, and backs the shared session (Redis) both apps read.
- **[legacy-web-net48](legacy-web-net48/)** — the existing system, ASP.NET MVC 5 on
  .NET Framework 4.8.
- **[modern-web-net10](modern-web-net10/)** — the system it's migrating to, ASP.NET
  Core MVC on .NET 10.
- **[gateway-net10](gateway-net10/)** — a reverse proxy (YARP) that decides, per
  request, which of the two apps answers. The browser's URL never changes and never
  reveals which one served a given page.
- **[traefik](traefik/)** — the edge: one public domain routing to the gateway and to
  Auth Manager.

## How it fits together

```
Browser → Traefik (miicel.local) → Gateway → Legacy (.NET 4.8) or Modern (.NET 10)
                                       ↓                              ↓
                                  Auth Manager ──────── Redis ────────┘
```

See each project's own README/code comments for details on the specific mechanisms
(password hash migration, page-by-page routing, shared session, global endpoints).
