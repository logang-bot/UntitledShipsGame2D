# Nakama Local-Network Setup

Steps to run a self-hosted Nakama server on your own machine so anyone on
the same LAN can connect to it for online co-op testing (see `roadmap.md`'s
"Nakama networking" and the Unity-side implementation this pairs with).
This is a **local-network setup, not the eventual Fly.io production
deployment** `overview.md` describes — that's separate, later work.

## 1. Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
  installed and running.
- Know your machine's **LAN IP address** (not `127.0.0.1`) so other devices
  on the network can reach it:
  - Windows: `ipconfig` → look for `IPv4 Address` under your active adapter
    (e.g. `192.168.1.42`).

## 2. Start the server

1. Copy `docker-compose.yml` (in this folder) into any folder on your
   machine, e.g. `C:\nakama-server\`.
2. Open a terminal in that folder and run:
   ```
   docker compose up
   ```
3. Wait for `nakama` container logs to show it's listening — you'll see a
   line like `Startup done` and no repeated errors. First run takes longer
   (pulls images, runs DB migrations).
4. Leave this terminal running for the whole play session. `Ctrl+C` stops
   the server; `docker compose up` again resumes with the same data
   (`docker compose down` also removes the containers, but keeps the data
   volume; add `-v` to wipe it entirely).

## 3. Verify it's reachable

- On the host machine: open `http://127.0.0.1:7351` in a browser — this is
  Nakama's developer console (default login `admin` / `password`). Seeing
  the dashboard confirms the server is up.
- From **another device on the LAN**: open
  `http://<host-LAN-IP>:7351` (e.g. `http://192.168.1.42:7351`) in a
  browser. If it doesn't load, see `troubleshooting.md` — this is almost
  always a Windows Firewall rule blocking inbound Docker traffic.

## 4. Point the Unity project at your server

In the Unity Editor, select the `NakamaConnection` GameObject (in
`MainMenu.unity`) and set, on the `Nakama Connection` component:

| Field | Host machine (running the server) | Every other machine (joining) |
| --- | --- | --- |
| `scheme` | `http` | `http` |
| `host` | `127.0.0.1` | `<host-LAN-IP>` (e.g. `192.168.1.42`) |
| `port` | `7350` | `7350` |
| `serverKey` | `defaultkey` | `defaultkey` |

`defaultkey` is Nakama's out-of-the-box server key — fine for LAN testing,
not something to expose publicly.

## 5. Ports reference

| Port | Purpose |
| --- | --- |
| `7350` | Client API + realtime socket — the one the game actually connects to. |
| `7351` | Developer console (browser dashboard). |
| `7349` | gRPC API (not used by the Unity client). |

See `troubleshooting.md` for common connection problems.

## 6. Play

Server running and every machine's `NakamaConnection` pointed at it? The
actual in-game navigation (start scene, Lobby → Online → creating/joining a
match, Role Select, playing the fight) is covered step-by-step in
[../current-state.md](../current-state.md)'s "How to test it — Online"
section — that's the one to follow from here.
