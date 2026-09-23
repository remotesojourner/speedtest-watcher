# Speedtest Watcher API

Speedtest Watcher has a REST API for scripts, dashboards and backups. It covers stored results, statistics, running and pausing tests, settings, settings backups, and Prometheus metrics. Configuring integrations and sign-in stays in the web UI.

Every instance describes its API in two places:

- **Reference page:** `/api/docs` shows every operation with its parameters, responses and examples, and can send test requests.
- **OpenAPI document:** `/api/openapi/v1.json` is an OpenAPI 3.1 description you can feed to code generators or API clients.

The same document is committed as [`docs/openapi.json`](openapi.json) and attached to each [GitHub release](https://github.com/remotesojourner/speedtest-watcher/releases), so you can read it without a running instance.

The examples below use `http://speedtest-watcher:2003`. Replace that with your instance's address.

## Authentication

What a request needs depends on the **Security** tab:

| Sign-in | People who aren't signed in | Read operations | Other operations |
|---|---|---|---|
| Off | — | Open to everyone | Open to everyone |
| On | Read-only | Open to everyone | Need a token |
| On | No access | Need a token | Need a token |

To create a token, go to **Settings → Security → API Token**. The token starts with `swt_` and is shown only once, because only a hash of it is stored. Creating a new token replaces the old one. Send it as a bearer token:

```bash
curl -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/speedtests/status
```

A browser that is signed in to Speedtest Watcher can call the API with its session cookie too.

The reference page and the OpenAPI document follow the same rules as read operations. Each operation's description starts with its access level, and the document marks it as `x-access: read` or `x-access: full`. These operations are read operations:

- `GET /api/speedtests`, `GET /api/speedtests/{id}`, `GET /api/speedtests/statistics`, `GET /api/speedtests/status` and `POST /api/speedtests/export`
- `GET /api/config`
- `GET /api/prometheus/metrics`
- `GET /api/opengraph/image`

Everything else needs full access.

## Errors

Every error has the same body:

```json
{ "message": "Speedtest is already running" }
```

The message is written for people, so you can show it as it is.

| Status | Meaning |
|---|---|
| `400` | The request isn't valid: an unknown filter value, a setting value that fails validation, a body that isn't valid JSON, or an unknown time zone |
| `401` | The request needs a valid token. The response has a `WWW-Authenticate: Bearer` header |
| `404` | The result doesn't exist, or there are no recommendations yet |
| `409` | A test can't start: one is already running, tests are paused, or no provider is chosen |
| `500` | Something unexpected went wrong. The details are only in the app's log |

## Units and timestamps

- Timestamps are UTC in ISO 8601, such as `2026-09-14T20:35:00Z`.
- Speeds are in Mbps. Ping and jitter are in milliseconds, and a test's `time` is its duration in seconds.
- `packetLoss` is a percentage, and `downloadBytes` and `uploadBytes` are the data the test itself moved. A provider that doesn't measure them leaves them `null`.
- Failed and skipped results store `-1` for ping, download and upload. Check `status` before using the readings.
- Settings are strings, as they are stored. `none` means unset.
- Enums use lowercase names: status is `completed`, `failed` or `skipped`, and type is `auto` (the schedule) or `custom` (started by hand).

## Versioning

The API follows the app's version. A breaking change to the API, such as removing an endpoint or changing a response's shape, bumps the major version, and the release notes list what changed. New endpoints and new response properties can arrive in any release, so ignore properties you don't know.

## Results

List the latest results, newest first:

```bash
curl "http://speedtest-watcher:2003/api/speedtests?limit=20"
```

Each result also carries `publicIp`, the address the connection check saw when the test ran. It's `null` for read-only visitors, and it's left out of exports and of the payloads sent to integrations.

Filter with `status`, `type` and `healthy`, which is `true` for results that met their targets and `false` for those that missed them. For the next page, pass the `id` of the last result you received as `afterId`:

```bash
curl "http://speedtest-watcher:2003/api/speedtests?status=completed&limit=20&afterId=412"
```

Get or delete one result by id:

```bash
curl http://speedtest-watcher:2003/api/speedtests/412
curl -X DELETE -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/speedtests/412
```

Export results as a file. The body takes the same filters, a list of `ids`, and a `format` of `csv` (the default) or `json`:

```bash
curl -X POST -H "Content-Type: application/json" \
  -d '{"format":"json","status":"failed"}' \
  -o failed.json http://speedtest-watcher:2003/api/speedtests/export
```

## Statistics

```bash
curl "http://speedtest-watcher:2003/api/speedtests/statistics?from=2026-09-01&to=2026-09-14&tz=Europe/London"
```

`from` and `to` take a date, which covers the whole day, or a date and time. They default to the last seven days. `tz` is an IANA time zone for day boundaries and the hourly averages, and defaults to UTC. The response has:

- `tests`: how many ran and how many failed.
- `ping`, `jitter`, `download`, `upload`, `time` and `packetLoss`: the lowest, average and highest value of the completed tests, or `null` when none completed. `packetLoss` is also `null` when no test in the period measured it.
- `dataUsedBytes`: how much data the tests in the period moved, download and upload together.
- `consistency`: the standard deviation of each reading, and for speeds a consistency score from 0 to 100.
- `hourlyAverages`: one entry for each hour of the day, 0 to 23.
- `points`: chart points, oldest first. Up to 300 tests give one point each. Longer periods are averaged into at most 300 points, and `downsampled` is `true`.

## Connection monitoring

```bash
curl http://speedtest-watcher:2003/api/monitoring/status
```

`state` is `up`, `down`, or `unknown` before the monitor has enough rounds, and `watching` says whether the monitor is on at all. The response also carries the outage in progress, if any, and uptime for the last 24 hours, 7 days and 30 days. Uptime counts only the time the app was watching, so `watchedSeconds` is what the percentage is a share of, and a restart is neither uptime nor downtime.

```bash
curl "http://speedtest-watcher:2003/api/monitoring/outages?limit=20"
curl "http://speedtest-watcher:2003/api/monitoring/days?tz=Europe/London"
curl -X DELETE -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/monitoring/outages/7
```

`days` is the uptime calendar: one entry for each of the last 365 days, oldest first. Deleting an outage stops it counting against your uptime, which is there for planned maintenance.

## Running and pausing tests

```bash
curl -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/speedtests/status
curl -X POST -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/speedtests/run
```

`run` starts a test and answers straight away. Poll `status` until `running` is `false`, then read the newest result. Add `?serverId=12345` to test against one server, for providers that support choosing a server.

Pause for up to 720 hours, or leave `resumeIn` out to pause until you resume. A pause is kept in memory, so a restart resumes tests.

```bash
curl -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"resumeIn":6}' http://speedtest-watcher:2003/api/speedtests/pause
curl -X POST -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/speedtests/continue
```

## Settings

`GET /api/config` returns every setting as a string, plus flags such as `authActive`. Read-only visitors don't get the settings that need full access, and secrets are never returned. The OpenAPI document lists every key with its default, and the [README](../README.md#configuration) explains what each setting does.

Change one or more settings in one request. Either every change is saved or none is:

```bash
curl -X PATCH -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"cron":"*/30 * * * *","download":"900"}' http://speedtest-watcher:2003/api/config
```

Sign-in settings can only be changed on the Security tab. Open browser tabs, the scheduler and integrations pick changes up straight away.

`GET /api/recommendations` returns the best ping, download and upload of the last 10 completed tests, or `404` until 10 tests have completed.

## Storage and backups

```bash
curl -H "Authorization: Bearer $TOKEN" http://speedtest-watcher:2003/api/storage
```

Export every result, or import results from a JSON export. The import skips results whose timestamp is already stored, so importing the same file twice is safe:

```bash
curl -H "Authorization: Bearer $TOKEN" -o speedtests.json http://speedtest-watcher:2003/api/storage/tests/history/json
curl -H "Authorization: Bearer $TOKEN" -o speedtests.csv http://speedtest-watcher:2003/api/storage/tests/history/csv
curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data @speedtests.json http://speedtest-watcher:2003/api/storage/tests/history
```

`DELETE /api/storage/tests/history` deletes every result and can't be undone.

Back up and restore settings, integrations and recommendations. The file is the same as the Storage tab's backup, and sign-in settings are never included:

```bash
curl -H "Authorization: Bearer $TOKEN" -o settings.json http://speedtest-watcher:2003/api/storage/config
curl -X PUT -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data @settings.json http://speedtest-watcher:2003/api/storage/config
```

The restore answers with how many settings and integrations were restored, and how many entries were skipped because they weren't valid.

## Prometheus and link previews

`GET /api/prometheus/metrics` serves the metrics described in the [README](../README.md#prometheus-metrics). `GET /api/opengraph/image` is the PNG that chat apps show when someone shares a link to your instance. Every page names it, by its full address, in its Open Graph tags.

`GET /api/info/version` returns this instance's version and the latest release on GitHub. The release is checked at most every six hours, and `remote` is `0` when it couldn't be checked.

## Upgrading from an earlier version

The release that added this guide changed the API. If you used it before:

- **Removed endpoints.** They only served the web UI, which no longer uses HTTP to talk to itself:
  - `GET /api/speedtests/count`
  - `PATCH /api/config/{key}`: use `PATCH /api/config` with `{ "key": "value" }`
  - `DELETE /api/storage/config`: factory reset is in the Storage tab only
  - All of `/api/integrations`: integrations are configured in the UI, and restored with the settings backup
  - `PUT /api/auth/settings`, `POST /api/auth/token` and `DELETE /api/auth/token`: sign-in and tokens are managed on the Security tab
  - `GET /api/info/server/{provider}` and `GET /api/info/interfaces`
- **Statistics.** `GET /api/speedtests/statistics` returns a `points` list of objects instead of the parallel `labels`, `failed`, `errors` and `data` arrays, and `dataPoints` is gone.
- **Timestamps** leave out zero fractional seconds: `2026-09-14T20:35:00Z` instead of `2026-09-14T20:35:00.0000000Z`.
- **Errors.** Invalid query values and request bodies answer with `{ "message": … }` instead of a problem-details object.
- **Request bodies** must be sent as `application/json`.
- **`POST /api/speedtests/run`** answers `409` instead of `410` when tests are paused or no provider is chosen.
