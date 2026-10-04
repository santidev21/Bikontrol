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

## Running the stack

Prometheus and Grafana are **not** part of the app compose file — run them
alongside it on the internal network so they can reach the API:

```bash
# Prometheus (scrape + alert rules)
docker run -d --name bikontrol-prometheus \
  --network bikontrol-internal-net \
  -v "$PWD/deploy/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro" \
  -v "$PWD/deploy/monitoring/alerts.yml:/etc/prometheus/alerts.yml:ro" \
  -p 127.0.0.1:9090:9090 prom/prometheus:latest

# Grafana (import deploy/monitoring/grafana-dashboard.json)
docker run -d --name bikontrol-grafana \
  --network bikontrol-internal-net \
  -p 127.0.0.1:3000:3000 grafana/grafana:latest
```

## Alerts

`alerts.yml` ships rules for:

- **Down / DB unreachable** — `up == 0` and a failing `/ready` probe.
- **Errors** — 5xx rate above 5% for 10 minutes.
- **Latency** — p95 above 1s for 10 minutes.
- **Memory** — managed heap above 400 MB for 15 minutes.

Disk and blackbox (`/ready`) rules are included as comments since they need a
`node_exporter` / `blackbox_exporter` on the host.

Point Prometheus at an Alertmanager (or have it forward to email/Slack) so the
rules actually notify someone. Uptime can also be monitored externally by
pointing a third-party monitor at `/ready` (see [1.7 in the plan]).

## Notes

- `/metrics` has no authentication. Keep it on the internal network (production
  never publishes the API port) and, if you ever expose it, put it behind auth
  or an allow-list.
- Cardinality is bounded: no `user_id` or other high-cardinality labels are used.
