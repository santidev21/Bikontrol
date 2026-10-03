#!/bin/bash
set -euo pipefail

# Bikontrol — post-deploy smoke test.
#
# Hits the running deployment over HTTP(S) and asserts the critical surfaces are
# alive after a deploy. It is read-only and safe to run against production
# (no writes, no auth required for the checks below). Run it in the deploy
# pipeline after containers report healthy; a non-zero exit triggers rollback.
#
# Usage:
#   scripts/smoke-test.sh <base-url>
#   scripts/smoke-test.sh https://bikontrol.santidev21.tech
#   SMOKE_TIMEOUT=10 scripts/smoke-test.sh http://127.0.0.1:8080

BASE_URL="${1:-}"
if [ -z "$BASE_URL" ]; then
    echo "Usage: $0 <base-url>  (e.g. https://bikontrol.santidev21.tech)" >&2
    exit 2
fi
BASE_URL="${BASE_URL%/}"
TIMEOUT="${SMOKE_TIMEOUT:-10}"

fail() { echo "[smoke] FAIL: $1" >&2; exit 1; }
ok() { echo "[smoke] ok: $1"; }

# Check a URL returns an expected HTTP status (any of the given list).
check_status() {
    local url="$1"; shift
    local label="$1"; shift
    local code
    code="$(curl -s -o /dev/null -w '%{http_code}' --max-time "$TIMEOUT" "$url" || echo "000")"
    for expected in "$@"; do
        if [ "$code" = "$expected" ]; then
            ok "$label -> $code"
            return 0
        fi
    done
    fail "$label -> $code (expected one of: $*)"
}

# Check a URL body contains a substring.
check_body() {
    local url="$1"; local needle="$2"; local label="$3"
    local body
    body="$(curl -s --max-time "$TIMEOUT" "$url" || true)"
    echo "$body" | grep -q "$needle" || fail "$label: body does not contain '$needle'"
    ok "$label contains '$needle'"
}

# POST a JSON body and expect one of the given status codes.
check_post_status() {
    local url="$1"; local body="$2"; local label="$3"; shift 3
    local code
    code="$(curl -s -o /dev/null -w '%{http_code}' --max-time "$TIMEOUT" \
        -X POST -H 'Content-Type: application/json' -d "$body" "$url" || echo "000")"
    for expected in "$@"; do
        if [ "$code" = "$expected" ]; then
            ok "$label -> $code"
            return 0
        fi
    done
    fail "$label -> $code (expected one of: $*)"
}

echo "[smoke] Target: $BASE_URL"

# 1. API readiness (checks PostgreSQL) and liveness.
check_status "$BASE_URL/health" "health" 200
check_status "$BASE_URL/ready" "ready (DB)" 200
check_body "$BASE_URL/ready" "Healthy" "ready body"

# 2. Frontend shell.
check_status "$BASE_URL/" "frontend index" 200
check_body "$BASE_URL/" "Bikontrol" "frontend title"

# 3. An anonymous API surface works end-to-end (auth endpoint, no write).
#    A malformed login must be validated (400), proving the API is serving and
#    the gateway routes POST requests.
check_post_status "$BASE_URL/api/auth/login" '{}' "login (empty body -> 400)" 400
# A well-formed but unknown login is unauthorized, not an error.
check_post_status "$BASE_URL/api/auth/login" \
    '{"email":"smoke@example.invalid","password":"notreal123"}' "login (unknown -> 401)" 401
# Not found must be a 4xx (routing/booting fine), never a 5xx.
check_status "$BASE_URL/api/does-not-exist" "unknown route (-> 4xx)" 400 401 403 404

echo "[smoke] PASS"
