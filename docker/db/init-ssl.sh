#!/bin/sh
set -eu

DATA_DIR="/var/lib/postgresql/data"
SSL_CERT="$DATA_DIR/server.crt"
SSL_KEY="$DATA_DIR/server.key"

if ! command -v openssl >/dev/null 2>&1; then
  echo "[db-ssl] Installing openssl ..."
  apk add --no-cache openssl >/dev/null 2>&1 || true
fi

if [ ! -f "$SSL_CERT" ] || [ ! -f "$SSL_KEY" ]; then
  echo "[db-ssl] Generating self-signed certificate (CN=bikontrol-db) ..."
  # Generate key and self-signed cert valid 10 years
  openssl req -new -x509 -days 3650 -nodes \
    -subj "/CN=bikontrol-db" \
    -keyout "$SSL_KEY" -out "$SSL_CERT" >/dev/null 2>&1
  chmod 600 "$SSL_KEY"
  chmod 644 "$SSL_CERT"
  chown postgres:postgres "$SSL_KEY" "$SSL_CERT" 2>/dev/null || true
  echo "[db-ssl] Certificate created at $SSL_CERT"
else
  echo "[db-ssl] Existing certificate found, reusing."
  chmod 600 "$SSL_KEY" 2>/dev/null || true
  chmod 644 "$SSL_CERT" 2>/dev/null || true
fi

# Exec the official entrypoint with SSL enabled
exec docker-entrypoint.sh postgres -c ssl=on -c ssl_cert_file="$SSL_CERT" -c ssl_key_file="$SSL_KEY" "$@"
