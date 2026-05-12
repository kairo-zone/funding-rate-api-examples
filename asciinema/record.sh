#!/usr/bin/env bash
# Records the kairo.zone Funding API quickstart demo as an asciinema .cast file.
#
# Host prerequisites:
#   - asciinema
#   - python3 with httpx available (e.g. `sudo apt install python3-httpx` on
#     Debian/Ubuntu, or `pip install httpx` in a venv)
#
# Usage:
#   # Either drop your key into the repo .env file, or:
#   export KAIRO_FUNDING_API_KEY=your_key_here
#   bash asciinema/record.sh
#
# Output: asciinema/quickstart.cast
set -euo pipefail

# Resolve repo root so the script works from any cwd.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
OUTPUT="${SCRIPT_DIR}/quickstart.cast"

# --- Preflight ------------------------------------------------------------

# Source .env if present so the user can put the key there instead of exporting.
if [ -f "${REPO_ROOT}/.env" ]; then
    set -a
    # shellcheck disable=SC1091
    . "${REPO_ROOT}/.env"
    set +a
fi

if [ -z "${KAIRO_FUNDING_API_KEY:-}" ]; then
    echo "error: KAIRO_FUNDING_API_KEY is not set." >&2
    echo "       either put it in .env at the repo root or export it:" >&2
    echo "         export KAIRO_FUNDING_API_KEY=your_key_here" >&2
    exit 1
fi

command -v asciinema >/dev/null 2>&1 || { echo "asciinema not installed" >&2; exit 1; }
command -v python3   >/dev/null 2>&1 || { echo "python3 not installed"   >&2; exit 1; }
python3 -c "import httpx" 2>/dev/null || {
    echo "error: python3 cannot import httpx." >&2
    echo "       on Debian/Ubuntu: sudo apt install python3-httpx" >&2
    echo "       or use a venv:    python3 -m venv .venv && .venv/bin/pip install httpx" >&2
    exit 1
}

if [ -f "${OUTPUT}" ]; then
    echo "note: ${OUTPUT} already exists; asciinema will refuse to overwrite." >&2
    echo "      delete it first if you want a fresh take." >&2
fi

# --- Inner demo script ----------------------------------------------------
# Written to a temp file so asciinema's --command can invoke it cleanly
# without nested-quoting hell. Each step pauses 1-2 seconds so the
# resulting cast reads at a human pace.

INNER_SCRIPT="$(mktemp -t kairo-demo.XXXXXX.sh)"
trap 'rm -f "${INNER_SCRIPT}"' EXIT

cat >"${INNER_SCRIPT}" <<'INNER'
#!/usr/bin/env bash
set -e
clear
sleep 1
echo "kairo.zone Funding API - quickstart"
sleep 2

echo
echo "$ cat python/examples/03_get_one_symbol.py"
sleep 1
cat python/examples/03_get_one_symbol.py
sleep 2

echo
echo "--- one funding rate ---"
sleep 1
echo "$ KAIRO_EXCHANGE=bybit KAIRO_BASE=BTC python3 python/examples/03_get_one_symbol.py"
sleep 1
KAIRO_EXCHANGE=bybit KAIRO_BASE=BTC python3 python/examples/03_get_one_symbol.py
sleep 2

echo
echo "--- cross-exchange spread for BTC ---"
sleep 1
echo "$ KAIRO_BASE=BTC python3 python/examples/04_spread_scanner.py"
sleep 1
KAIRO_BASE=BTC python3 python/examples/04_spread_scanner.py
sleep 2

echo
echo "--- bybit perp universe ---"
sleep 1
echo "$ KAIRO_EXCHANGE=bybit python3 python/examples/02_filter_by_exchange.py"
sleep 1
KAIRO_EXCHANGE=bybit python3 python/examples/02_filter_by_exchange.py
sleep 2

echo
echo "docs:    https://kairo.zone/docs"
echo "signup:  https://kairo.zone"
sleep 2
INNER

chmod +x "${INNER_SCRIPT}"

# --- Record ---------------------------------------------------------------

cd "${REPO_ROOT}"

asciinema rec \
    --title "kairo.zone Funding API quickstart" \
    --idle-time-limit 2 \
    --command "bash ${INNER_SCRIPT}" \
    "${OUTPUT}"

# --- Next steps -----------------------------------------------------------

echo
echo "Recording saved to: ${OUTPUT}"
echo
echo "Next steps:"
echo "  1. Preview locally:   asciinema play ${OUTPUT}"
echo "  2. Upload:            asciinema upload ${OUTPUT}"
echo "                        (returns a URL like https://asciinema.org/a/<id>)"
echo "  3. Update root README with the asciinema embed:"
echo "       [![asciicast](https://asciinema.org/a/<id>.svg)](https://asciinema.org/a/<id>)"
