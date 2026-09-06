# Troubleshooting

## Other devices can't reach the server (works on `127.0.0.1`, not LAN IP)

Almost always **Windows Firewall** blocking inbound connections to Docker's
published ports. Fix: Windows Defender Firewall → Advanced settings →
Inbound Rules → New Rule → Port → TCP → specific ports `7350,7351,7349` →
Allow the connection → apply to Private networks (skip Public/Domain unless
you know you need them).

Also confirm both machines are actually on the same network/subnet (same
Wi-Fi/router, not one on a guest network or VPN).

## `docker compose up` fails with a port already in use

Something else on the host is already using `7350`/`7351`/`26257`/`8080`.
Stop that process, or edit the `ports:` mappings in `docker-compose.yml`
(left side only, e.g. `"17350:7350"`) and update the Unity `NakamaConnection`
`port` field to match.

## Nakama container keeps restarting / migration errors

Usually means CockroachDB wasn't ready yet on first attempt. Run
`docker compose down` then `docker compose up` again — the `depends_on`
healthcheck should wait properly on a clean start.

## Unity throws an authentication/connection exception

- Confirm the server is actually running (`http://127.0.0.1:7351` loads).
- Double-check `scheme`/`host`/`port`/`serverKey` on the `NakamaConnection`
  component match the table in `README.md` exactly (`http`, not `https` -
  this local setup has no TLS).
- If testing two Unity Editor instances on the *same* machine, both should
  use `127.0.0.1` as `host`.

## Wiping all server data and starting fresh

```
docker compose down -v
docker compose up
```

The `-v` flag removes the named volume (`data`), deleting all accounts,
matches, and stored state.
