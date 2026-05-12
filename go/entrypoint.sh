#!/bin/sh
# Resolve the example to run from the first positional arg or $EXAMPLE.
# Falls back to 01_quickstart when nothing is provided.
set -eu

target="${1:-${EXAMPLE:-01_quickstart}}"
shift 2>/dev/null || true

if [ ! -x "/usr/local/bin/${target}" ]; then
    echo "unknown example: ${target}" >&2
    echo "available examples:" >&2
    ls -1 /usr/local/bin/ | grep -E '^[0-9]{2}_' >&2 || true
    exit 4
fi

exec "/usr/local/bin/${target}" "$@"
