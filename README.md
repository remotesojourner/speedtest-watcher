# Speedtest Watcher

A self-hosted speed test manager. It runs internet speed tests on a schedule and keeps the history, so you can see how your connection performs over time.

Built with .NET 10, Blazor Interactive Server, MudBlazor and SQLite (Entity Framework Core).

## Features

- Scheduled tests with Ookla Speedtest, LibreSpeed or Cloudflare; the command-line tools are downloaded automatically
- Pick the server automatically, at random from an allow or deny list, or pin a single one — and run a one-off test against any server without changing your saved settings
- Skip a test instead of recording a failure when the line is down, or when your public IP is one you've chosen to sit out (useful while a VPN or backup line is up)
- Results are judged against your optimal values and marked healthy or not, using the targets that were in force when the test ran, so changing your targets later doesn't rewrite history
- Dashboard with averages, min/max, jitter, connection stability, an hour-by-hour table and charts with their average marked
- Live test history (SignalR) grouped by day, filtered by status, by what started the test, or by whether it met your targets
- Notifications through Discord, Telegram, Gotify, ntfy, Pushover, webhooks and Healthchecks.io, including alerts when a test misses your targets or is skipped
- Sign-in with any OpenID Connect provider, such as Authentik, Authelia, Keycloak or Pocket ID, with an optional read-only mode for people who aren't signed in
- Export the results shown, the ones you select, or all of them, as CSV or JSON; import, settings backup, and automatic cleanup of old results
- Prometheus metrics and a generated link-preview image

## Project layout

| Project | Contents |
| --- | --- |
| `src/SpeedtestWatcher.Core` | Models, DTOs, interfaces and calculation helpers |
| `src/SpeedtestWatcher.Infrastructure` | Database and migrations, repositories, speedtest runners, integrations |
| `src/SpeedtestWatcher.Web` | Blazor UI, REST API, SignalR hub and background services |
| `tests/SpeedtestWatcher.Tests` | xUnit tests |

## Running with Docker

```bash
docker compose up -d --build
```

Open http://localhost:2003. Results are stored in `./data` and the downloaded speedtest tools in `./bin`.

To run on another port, set `PORT`, either inline or in a `.env` file next to `docker-compose.yml`. The app listens on that port inside the container and Compose publishes the same one:

```bash
PORT=8080 docker compose up -d --build
```

Without Compose:

```bash
docker build -t speedtest-watcher .
docker run -d --name speedtest-watcher -e PORT=8080 -p 8080:8080 \
  -v "$(pwd)/data:/app/data" -v "$(pwd)/bin:/app/bin" \
  --restart unless-stopped speedtest-watcher
```

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/SpeedtestWatcher.Web
```

Open http://localhost:2003, or set `PORT` to use another port. The database is created on first start from the EF Core migrations.

## Configuration

| Setting | Where | Purpose |
| --- | --- | --- |
| `PORT` | Environment variable | Port the app listens on, in Docker too (default `2003`) |
| `DISABLE_AUTH=true` | Environment variable | Turns sign-in off whatever is saved. For recovering from a broken sign-in setup; see below. |

## Sign-in

Sign-in is off by default. To turn it on:

1. In your identity provider, create an OpenID Connect application (confidential or public) using the authorization code flow.
2. Open **Settings → Security** in Speedtest Watcher and copy the **Redirect URI** shown there into the provider. It ends in `/signin-oidc`.
3. Enter the provider URL (the issuer), the client ID and, for a confidential client, the client secret.
4. Choose what people who aren't signed in can do: nothing, or view results read-only.
5. Switch on **Require sign-in** and save. Speedtest Watcher first reads the provider's discovery document and refuses to turn sign-in on if it can't, then sends you through sign-in.

Anyone the provider authenticates is let in, so control who can use the app in the provider itself, for example by assigning the application to a group.

Changes apply straight away, without a restart. Sign-in settings aren't included in settings backups, so a backup never carries the client secret and restoring one can't switch sign-in on.

**If sign-in stops working**, start Speedtest Watcher with the environment variable `DISABLE_AUTH=true`. Sign-in is then off regardless of the saved settings, and the Security tab says so. Correct the settings there, then remove the variable and restart.

Behind a reverse proxy, forward the `X-Forwarded-Proto` and `X-Forwarded-Host` headers so the redirect URI uses the address people actually visit. Many providers only accept https redirect URIs.

The keys that protect sign-in cookies are kept in `data/keys`, so a restart doesn't sign everyone out.

### API token

Prometheus and scripts can't sign in through a browser. Create a token under **Settings → Security → API Token** and send it as a bearer token. It's only shown once; only a hash of it is stored.

```yaml
- job_name: speedtest-watcher
  metrics_path: /api/prometheus/metrics
  authorization:
    credentials: YOUR_TOKEN
  static_configs:
    - targets: ["speedtest-watcher:2003"]
```

The token isn't needed while sign-in is off, or for reading metrics when people who aren't signed in have read-only access.

## Prometheus metrics

`GET /api/prometheus/metrics` exposes the following, each prefixed with `speedtest_watcher_`:

| Metric | Meaning |
| --- | --- |
| `ping`, `jitter`, `download`, `upload`, `time` | Readings from the latest completed test |
| `healthy` | Whether that test met its targets (1 or 0) |
| `threshold_ping`, `threshold_download`, `threshold_upload` | The targets it was judged against |
| `last_test_timestamp_seconds` | When the latest test ran, whatever its outcome |
| `last_completed_test_timestamp_seconds` | When the latest completed test ran |
| `tests_total` | How many results are stored |
| `server`, `server_info` | Kept for existing dashboards |

Readings come from the last test that produced any, so a failed or skipped attempt doesn't leave holes in your graphs. The two timestamps are deliberately separate: the first tells you tests are still running at all, the second that the data is still fresh. They drift apart exactly when tests are failing or being skipped. Every series is labelled with `server_id`, `server_name`, `server_host`, `status` and `scheduled`.

## Changing the database

After changing the entity model, add a migration:

```bash
dotnet ef migrations add <Name> --project src/SpeedtestWatcher.Infrastructure --startup-project src/SpeedtestWatcher.Web --output-dir Data/Migrations
```

## Tests

```bash
dotnet test
```

## License

AGPL 3.0. See [LICENSE](LICENSE).
