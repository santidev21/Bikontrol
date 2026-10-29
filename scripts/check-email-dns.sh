#!/bin/bash
set -euo pipefail

# Checks the DNS records that decide whether Bikontrol's email lands in the inbox:
# SPF, DKIM and DMARC for the sending domain. Read-only (dig lookups only).
#
# Usage: ./scripts/check-email-dns.sh <domain> [dkim-selector ...]
#   ./scripts/check-email-dns.sh santidev21.tech resend._domainkey
# Find the DKIM selector in your provider's domain settings (Resend, Postmark and
# SES each publish one or more <selector>._domainkey records).

domain="${1:-}"
if [[ -z "$domain" ]]; then
    echo "Usage: $0 <domain> [dkim-selector ...]" >&2
    exit 2
fi
shift || true

command -v dig >/dev/null 2>&1 || { echo "dig is required (dnsutils / bind-utils)" >&2; exit 2; }

failures=0
ok() { echo "  OK    $1"; }
bad() { echo "  FAIL  $1"; failures=$((failures + 1)); }
warn() { echo "  WARN  $1"; }

txt() { dig +short TXT "$1" | tr -d '"' ; }

echo "SPF (${domain})"
spf="$(txt "$domain" | grep -i '^v=spf1' || true)"
if [[ -z "$spf" ]]; then
    bad "no v=spf1 TXT record"
elif [[ "$(echo "$spf" | wc -l)" -gt 1 ]]; then
    bad "more than one SPF record (receivers treat that as a permanent error)"
else
    ok "$spf"
    [[ "$spf" == *" -all"* || "$spf" == *" ~all"* || "$spf" == *"redirect="* ]] || warn "SPF does not end in -all or ~all"
fi

echo "DMARC (_dmarc.${domain})"
dmarc="$(txt "_dmarc.${domain}" | grep -i '^v=DMARC1' || true)"
if [[ -z "$dmarc" ]]; then
    bad "no DMARC record (Gmail/Yahoo require one for bulk senders)"
else
    ok "$dmarc"
    if [[ "$dmarc" == *"p=none"* ]]; then
        warn "policy is p=none (monitoring only): move to quarantine/reject once reports look clean"
    fi
fi

for selector in "$@"; do
    echo "DKIM (${selector}.${domain})"
    name="${selector}"
    [[ "$name" == *"._domainkey"* ]] || name="${selector}._domainkey"
    dkim="$( { txt "${name}.${domain}"; dig +short CNAME "${name}.${domain}"; } | head -c 200 || true)"
    if [[ -z "$dkim" ]]; then
        bad "no record at ${name}.${domain}"
    else
        ok "${dkim}"
    fi
done

if [[ $# -eq 0 ]]; then
    echo "DKIM"
    warn "no selector given, skipped (pass the selector from your provider)"
fi

echo
if [[ "$failures" -gt 0 ]]; then
    echo "${failures} check(s) failed"
    exit 1
fi
echo "All checks passed"
