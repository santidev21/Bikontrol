# Monitoring (metrics, dashboards, alerts)

The API exposes [OpenTelemetry](https://opentelemetry.io/) metrics in Prometheus
format at **`/metrics`**. It is off by default (`Metrics:Enabled=false`) and
turned on in `docker-compose.yml`. The endpoint is only reachable on the
internal Docker network (`bikontrol-internal-net`); the public gateway never
proxies it, so it must not be exposed publicly.

## What is exported

| Meter | Examples |
| --- | --- |
| ASP.NET Core | `http_server_request_duration_seconds_*` (rate, latency, status codes) |
| HttpClient | `http_client_request_duration_seconds_*` |
| .NET runtime | `process_runtime_dotnet_gc_*`, thread pool, exceptions |
| Jobs and business | `bikontrol_job_runs_total{task,result}`, `bikontrol_job_duration_seconds`, `bikontrol_job_last_success_timestamp_seconds{task}`, `bikontrol_reminders_total{stage}`, `bikontrol_retention_deleted_total{kind}` |

## Running the stack

The stack lives in `docker-compose.monitoring.yml` (Prometheus, Alertmanager,
Grafana; pinned by version and digest, bound to loopback). Run it next to the app
stack so it shares `bikontrol-internal-net`:

```bash
# .env needs ALERT_EMAIL_TO, GRAFANA_ADMIN_PASSWORD and the Smtp__* account
docker compose -f docker-compose.monitoring.yml up -d
# Grafana: ssh -L 3000:127.0.0.1:3000 <vps>  ->  http://localhost:3000
```

Prometheus scrapes the API (`bikontrol-api:8080`) and the worker (`worker:8080`).
Alertmanager emails through the app's SMTP account (`--config.expand-envvars`
fills `alertmanager.yml`, so no secret lives in the repo). Grafana provisions the
datasource and `grafana-dashboard.json` automatically.

Test the whole chain once after the first start:

```bash
curl -XPOST localhost:9093/api/v2/alerts -H 'Content-Type: application/json' \
  -d '[{"labels":{"alertname":"TestAlert","severity":"warning"}}]'
```

## Alerts

`alerts.yml` ships rules for:

- **Down / DB unreachable** — `up == 0` and a failing `/ready` probe.
- **Errors** — 5xx rate above 5% for 10 minutes.
- **Latency** — p95 above 1s for 10 minutes.
- **Memory** — managed heap above 400 MB for 15 minutes.
- **Jobs** — worker down, a job failing repeatedly, or no successful run for 26 hours.

Disk and blackbox (`/ready`) rules are included as comments since they need a
`node_exporter` / `blackbox_exporter` on the host.

Alertmanager (`alertmanager.yml`) emails every alert; critical ones repeat hourly.
Uptime should also be monitored from outside the host by pointing a third-party
monitor at `/ready`, since this stack dies with the VPS.

## Notes

- `/metrics` has no authentication. Keep it on the internal network (production
  never publishes the API port) and, if you ever expose it, put it behind auth
  or an allow-list.
- Cardinality is bounded: no `user_id` or other high-cardinality labels are used.
