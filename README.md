<p align="center">
  <img src="src/SpeedtestWatcher.Web/wwwroot/img/logo.svg" alt="Speedtest Watcher Logo" width="160" />
</p>

# Speedtest Watcher

[![CI](https://github.com/remotesojourner/speedtest-watcher/actions/workflows/ci.yml/badge.svg?branch=main&event=push)](https://github.com/remotesojourner/speedtest-watcher/actions/workflows/ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/remotesojourner/speedtest-watcher?sort=semver&style=flat&logo=github&label=release)](https://github.com/remotesojourner/speedtest-watcher/releases/latest)
[![License: AGPL v3](https://img.shields.io/badge/license-AGPL--v3-blue?style=flat&logo=gnu)](LICENSE)

A self-hosted Blazor Server app that runs internet speed tests on a schedule and keeps the history, so you can see how your connection really performs over time.

---

## Features

- **Scheduled speed tests** — runs tests on a cron schedule with [Ookla Speedtest](https://www.speedtest.net/apps/cli), [LibreSpeed](https://github.com/librespeed/speedtest-cli) or [Cloudflare](https://github.com/code-inflation/cfspeedtest). The command-line tools are downloaded automatically
  - Server choice for Ookla and LibreSpeed: automatic, random from an allow or deny list, or a single pinned server
  - One-off tests against any Ookla or LibreSpeed server, without changing your saved settings
- **Health tracking** — every result is judged against the speeds from your internet contract and marked healthy or not, using the targets that were in force when the test ran, so changing them later doesn't rewrite history
- **Smart skipping** — skips a test instead of recording a failure when the line is down, or while your public IP is on a skip list (useful while a VPN or backup line is up)
- **Dashboard** — averages, min/max, jitter, packet loss, connection stability, how much data the tests themselves used, an hour-by-hour table, and charts with the average marked
- **Live history** — results appear as soon as a test finishes, grouped by day and filterable by status, by what started the test, and by whether it met your targets
- **Notifications** — Discord, Telegram, Gotify, ntfy, Pushover, Apprise, webhooks, Healthchecks.io and InfluxDB v2, including alerts when a test misses your targets, is skipped, or meets your targets again
- **Sign-in** — optional OpenID Connect sign-in (Authentik, Authelia, Keycloak, Pocket ID, …), with a read-only mode for people who aren't signed in
- **Your data** — export results as CSV or JSON, import them again, back up your settings, and clean up old results automatically
- **Monitoring** — Prometheus metrics and a generated link-preview image
- **REST API** — results, statistics, settings and backups for your own scripts, described by an OpenAPI document and a built-in reference page

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Docker + Docker Compose | Recommended deployment method. Images are published for `linux/amd64` and `linux/arm64` |
| Internet access | Needed for the tests themselves, and to download the speed test command-line tools on first start |
| OpenID Connect provider | Optional. Only needed if people should sign in |

---

## Quick Start (Docker Compose)

1. **Create a `docker-compose.yml`** with the following content:

   ```yaml
   services:
     speedtest-watcher:
       container_name: speedtest-watcher
       restart: unless-stopped
       image: ghcr.io/remotesojourner/speedtest-watcher:latest
       ports:
         - "2003:2003"
       volumes:
         - ./data:/app/data
         - ./bin:/app/bin
   ```

   **Volume notes:**
   - `./data` — stores the SQLite database, the keys that protect sign-in cookies and the cached server lists. Created automatically on first run.
   - `./bin` — stores the downloaded Ookla, LibreSpeed and Cloudflare command-line tools, so they aren't downloaded again when the container is recreated.

2. **Start the stack**

   ```bash
   docker compose up -d
   ```

   The app will be available on port `2003` of the host you deployed it on (e.g. `http://192.168.1.100:2003`).

3. **Complete the welcome steps**

   The first visit walks you through choosing a provider and entering the speeds from your internet contract. If you choose Ookla, you also need to accept Ookla's Terms of Service, EULA and Privacy Policy before Speedtest Watcher can run tests with it; the welcome steps link to all three.

4. **Review the Settings page**

   Tests run every hour by default. Open **Settings** to change the schedule, choose servers, add notifications or require sign-in (see [Configuration](#configuration) below).

---

## Configuration

Most configuration is stored in the SQLite database and managed through the browser-based Settings UI. A few environment variables control how the app starts (see [Environment Variables](#environment-variables) below).

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `PORT` | `2003` | Port the app listens on. Change the port mapping in `docker-compose.yml` to match |
| `DISABLE_AUTH` | — | Set to `true` to turn sign-in off regardless of the saved settings. Use it to recover from a broken sign-in setup |
| `RUN_TEST_ON_STARTUP` | — | Set to `true` to run a test 5 seconds after the app starts |
| `DATA_DIRECTORY` | `data` in the working directory | Folder for the database, sign-in keys and cached server lists. The Docker image mounts a volume at `/app/data`, so leave it unset there |
| `BIN_DIRECTORY` | `bin` in the working directory | Folder the speedtest command-line tools are downloaded to. The Docker image mounts a volume at `/app/bin`, so leave it unset there |

### Sign-in (OIDC)

Sign-in is **optional** and off by default. To turn it on:

1. In your identity provider, create an OpenID Connect application (confidential or public) using the authorization code flow.
2. Open **Settings → Security** and copy the **Redirect URI** shown there into the provider. It ends in `/signin-oidc`.
3. Enter the provider URL (the issuer), the client ID and, for a confidential client, the client secret.
4. Choose what people who aren't signed in can do: nothing, or view results read-only.
5. Switch on **Require sign-in** and save. Speedtest Watcher first reads the provider's discovery document and refuses to turn sign-in on if it can't, then sends you through sign-in.

When sign-in is enabled:
- Anyone the provider authenticates is let in, so control who can use the app in the provider itself, for example by assigning the application to a group.
- Changes apply straight away, without a restart.
- Sign-in settings aren't included in settings backups, so a backup never carries the client secret and restoring one can't switch sign-in on.
- The keys that protect sign-in cookies are kept in `data/keys`, so a restart doesn't sign everyone out.

> [!TIP]
> If sign-in stops working, start Speedtest Watcher with `DISABLE_AUTH=true`. Sign-in is then off regardless of the saved settings, and the Security tab says so. Correct the settings there, then remove the variable and restart.

Behind a reverse proxy, forward the `X-Forwarded-Proto` and `X-Forwarded-Host` headers so the redirect URI uses the address people actually visit. Many providers only accept https redirect URIs.

---

### Tab: General

| Field | Description |
|---|---|
| **Optimal Values** | Your contracted ping, download and upload speeds. Results are colour-coded against these, marked healthy or not, and integrations can alert you when a test misses them. Recommendations appear after at least 10 tests |
| **Check the connection first** | Skips the test instead of recording a failure when the line is down |
| **Check URL** | Must return your public IP address. Default: `https://icanhazip.com` |
| **Skip tests when the public IP is** | Comma-separated IP addresses. Leave empty to always test |

### Tab: Schedule

| Field | Description |
|---|---|
| **Test Schedule** | Every minute, every 30 minutes, every hour (default), every 3 hours, every 6 hours, or your own cron expression. Cron expressions are evaluated in UTC. Next to the next run time is an estimate of the data that schedule will use a month, from how much your recent tests moved |
| **Offset schedule** | Adds a random delay of 30 seconds to 5 minutes, so tests don't start at exact times |
| **Pause Speedtests** | Pause indefinitely, for 1, 6 or 12 hours, or for a custom number of hours up to 720 (30 days). A pause ends when the app restarts |

### Tab: Provider

| Field | Description |
|---|---|
| **Speedtest Provider** | Ookla, LibreSpeed or Cloudflare |
| **Network interface** | The local address tests are sent from. Default: the default route |
| **Custom LibreSpeed server URL** | LibreSpeed only. Tests against your own LibreSpeed server instead of choosing from the public list |
| **Which server to test against** | Ookla and LibreSpeed only. **Automatically**, **Random** (only the servers you list, or any nearby server except them) or **Single**. The list of nearby servers is downloaded again once it is a week old |

### Tab: Display

| Field | Description |
|---|---|
| **Time format** | 24-hour or 12-hour. Saved in this browser. Times are always shown in this browser's time zone |
| **Speed unit** | Mbps or MB/s. Saved in this browser |
| **Date format** | Day, month or year first |
| **Dashboard charts** | The range the dashboard opens on (last 24 hours, 7 days or 30 days), and whether charts start at zero |

### Tab: Integrations

| Integration | Description |
|---|---|
| **Discord, Telegram, Gotify, ntfy, Pushover** | Messages when a test finishes, fails, misses your targets, meets them again after missing them, or is skipped. Each message can be customised with placeholders such as `%download%`, `%upload%`, `%ping%`, `%server%` and `%error%` |
| **Apprise** | The same messages, sent to any service [Apprise](https://github.com/caronc/apprise) supports through your [Apprise API](https://github.com/caronc/apprise-api) server. Enter Apprise URLs, or the key of a configuration saved on that server with optional tags (`admin, devops` notifies either tag, `all` notifies everything). Finished, failed, missed-target, targets-met-again and skipped messages are sent as success, failure, warning and info |
| **Webhook** | JSON for started, finished and failed tests, missed targets, targets met again, skipped tests, new recommendations, settings changes and a keep-alive |
| **Healthchecks.io** | Heartbeats and test results |
| **InfluxDB v2** | Test results as metrics, written with Line Protocol |

Every integration has a **Send test** button that uses what's in the form, whether it's saved or not, and shows the service's answer if it fails. Message integrations and the webhook send your latest completed result, or sample values before your first test. Healthchecks.io gets a `/log` ping that doesn't change the check's status, and InfluxDB checks the URL, token, organisation and bucket without writing anything.

### Tab: Security

| Field | Description |
|---|---|
| **Require sign-in** | Turns OpenID Connect sign-in on. The provider is checked before this is switched on |
| **Provider URL** | The issuer URL your provider gives for this app |
| **Client ID / Client secret** | From the application registered with your provider. Leave the secret empty for a public client |
| **Scopes** | Space separated. `openid` is always included |
| **Redirect URI** | Read-only — register this with your provider |
| **People who aren't signed in** | **No access** (sent to sign in first) or **Read-only** (can see results, but can't run tests or change settings). The public IP recorded with a result is hidden from read-only visitors, and never appears in exports or the payloads sent to integrations |
| **API Token** | A bearer token for Prometheus and scripts using the [API](#api). It's shown once, and only a hash of it is stored |

### Tab: Storage

| Field | Description |
|---|---|
| **Database** | Number of stored tests and the database size |
| **Data Retention** | How long results are kept before they're deleted automatically. `0` keeps them forever. Default: 365 days |
| **Speedtest Results** | Export results as CSV or JSON, or import a JSON export. Results that are already stored are skipped, so importing the same file twice is safe |
| **Settings Backup** | Export or import settings, integrations and recommendations as a JSON file. Sign-in settings are never included, and anything in a backup that isn't valid is skipped |
| **Danger Zone** | **Clear history** deletes every result; **Factory reset** returns settings to their defaults and deletes integrations and recommendations |

### Tab: About

| Field | Description |
|---|---|
| **Version** | The running version. When GitHub has a newer release, an update notice appears here and an icon in the header. The check needs internet access and runs at most every six hours |
| **Links** | The GitHub repository, the release notes, the issue tracker and the sponsor page |

---

## Prometheus Metrics

`GET /api/prometheus/metrics` exposes the following, each prefixed with `speedtest_watcher_`:

| Metric | Meaning |
|---|---|
| `ping`, `jitter`, `download`, `upload`, `time` | Readings from the latest completed test |
| `packet_loss` | Packet loss of the latest completed test, as a percentage. Only Ookla measures it |
| `last_test_bytes` | Data the latest completed test moved, download and upload together |
| `data_used_bytes` | Data all tests moved, one series per `period` label: `24h`, `7d`, `30d` and `stored` |
| `healthy` | Whether that test met its targets (1 or 0) |
| `threshold_ping`, `threshold_download`, `threshold_upload` | The targets it was judged against |
| `last_test_timestamp_seconds` | When the latest test ran, whatever its outcome |
| `last_completed_test_timestamp_seconds` | When the latest completed test ran |
| `tests_total` | How many results are stored |
| `server`, `server_info` | Kept for existing dashboards |

Readings come from the last test that produced any, so a failed or skipped attempt doesn't leave holes in your graphs. The two timestamps are deliberately separate: the first tells you tests are still running at all, the second that the data is still fresh. They drift apart exactly when tests are failing or being skipped. Every series is labelled with `server_id`, `server_name`, `server_host`, `status` and `scheduled`.

When sign-in is on, create a token under **Settings → Security → API Token** and send it as a bearer token. The token isn't needed while sign-in is off, or when people who aren't signed in have read-only access.

```yaml
- job_name: speedtest-watcher
  metrics_path: /api/prometheus/metrics
  authorization:
    credentials: YOUR_TOKEN
  static_configs:
    - targets: ["speedtest-watcher:2003"]
```

---

## API

Speedtest Watcher has a REST API for scripts and dashboards: results, statistics, running and pausing tests, settings, backups and Prometheus metrics. Every instance serves:

| Path | What it is |
|---|---|
| `/api/docs` | An interactive reference with every operation, its parameters and responses, and a way to send test requests |
| `/api/openapi/v1.json` | The OpenAPI 3.1 document, for API clients and code generators |

While sign-in is on, send an API token from **Settings → Security → API Token** as `Authorization: Bearer swt_…`. Read operations don't need it when people who aren't signed in have read-only access.

```bash
curl -H "Authorization: Bearer $TOKEN" "http://speedtest-watcher:2003/api/speedtests?limit=5"
```

The [API guide](docs/api.md) covers authentication, errors, units and every group of endpoints with examples. The OpenAPI document is also committed as [`docs/openapi.json`](docs/openapi.json) and attached to each release.

---

## Development Setup

### Requirements

- .NET 10 SDK

### Run locally

```bash
dotnet run --project src/SpeedtestWatcher.Web
```

The app listens on `http://localhost:2003`, or on `PORT` if it's set.

### Tests

The unit tests (`tests/SpeedtestWatcher.UnitTests`) use fakes only. The integration tests (`tests/SpeedtestWatcher.IntegrationTests`) run real parts together: SQLite, the app over HTTP, and the app in a browser. Run both with:

```bash
dotnet test
```

The browser tests drive the app in Chromium with [Playwright](https://playwright.dev/dotnet/). Install Chromium once after building, or they're skipped:

```bash
pwsh tests/SpeedtestWatcher.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

To leave them out, add `--filter-not-trait "Category=Browser"` to the test command.

### Database migrations

EF Core migrations are applied automatically on startup. To add a new migration during development:

```bash
dotnet ef migrations add <MigrationName> --project src/SpeedtestWatcher.Application --startup-project src/SpeedtestWatcher.Web --output-dir Storage/Migrations
```

### Docker image

The `docker-compose.yml` in this repository builds the image from source:

```bash
docker compose up -d --build
```

---

## Technology Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10, Blazor Server (Interactive Server render mode) |
| UI components | [MudBlazor](https://mudblazor.com/) |
| Database | SQLite via Entity Framework Core |
| API documentation | OpenAPI 3.1 from `Microsoft.AspNetCore.OpenApi`, reference page by [Scalar](https://github.com/scalar/scalar) |
| Scheduling | [Cronos](https://github.com/HangfireIO/Cronos) |
| Speed tests | Ookla Speedtest CLI, LibreSpeed CLI, cfspeedtest |
| Link-preview image | [SkiaSharp](https://github.com/mono/SkiaSharp) |
| Integration icons | [Dashboard Icons](https://github.com/homarr-labs/dashboard-icons) (Apache License 2.0) |
| Tests | xUnit v3, FakeItEasy, Playwright for the browser tests |
| Containers | Docker + Docker Compose |

---

## License

This project is licensed under the GNU Affero General Public License v3.0. See [LICENSE](LICENSE) for details.

---

## FAQ

### Which provider should I use?

**Ookla** has the largest server network and is what most people know from speedtest.net, but you have to accept Ookla's terms to use it. **LibreSpeed** is open source and can test against your own LibreSpeed server. **Cloudflare** tests against Cloudflare's network and needs no server choice.

---

### Why is a test marked as skipped?

A skipped test didn't run at all. Either the connection check before the test found no internet connection, or your public IP was on the skip list in **Settings → General**. Skipped tests don't count as failures, and they don't affect your averages.

---

### Why doesn't a shared link show a preview?

When you paste a link to your instance into Slack, Discord, Teams, WhatsApp or similar, the chat service's own servers fetch the page and show the preview image with your latest ping, download and upload. So a preview only appears when those servers can reach your instance and open it: it has to be reachable from the internet, and with sign-in on, **People who aren't signed in** in **Settings → Security** has to be **Read-only**. Chat apps also keep a preview for a while, so a link shared again soon afterwards can show older results.

---

### Do you plan to add new features?

If I come across a new idea or receive a suggestion that fits into keeping an eye on an internet connection, I will consider adding it. No roadmap or guarantees — this project exists to solve my own needs first.

---

## A Note on AI

This project was built with extensive help from [Claude Code](https://claude.com/claude-code). I have been a C# developer for more than 20 years, I understand the code fully, and I refactor and rewrite it as I see fit.
