#!/usr/bin/env bash
# Records the kairo.zone Funding API quickstart demo as an asciinema .cast file.
#
# Usage:
#   export KAIRO_FUNDING_API_KEY=your_key_here
#   bash asciinema/record.sh
#
# Output: asciinema/quickstart.cast
set -euo pipefail

# --- Preflight ------------------------------------------------------------

if [ -z "${KAIRO_FUNDING_API_KEY:-}" ]; then
    echo "error: KAIRO_FUNDING_API_KEY is not set." >&2
    echo "       export it before recording, e.g.:" >&2
    echo "         export KAIRO_FUNDING_API_KEY=your_key_here" >&2
    exit 1
fi

command -v asciinema >/dev/null 2>&1 || { echo "asciinema not installed" >&2; exit 1; }
command -v python >/dev/null 2>&1 || { echo "python not installed" >&2; exit 1; }

# Resolve repo root so the script works from any cwd.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
OUTPUT="${SCRIPT_DIR}/quickstart.cast"

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
echo "$ cat python/examples/01_quickstart.py | head -20"
sleep 1
cat python/examples/01_quickstart.py | head -20
sleep 2

echo
echo "$ python python/examples/01_quickstart.py"
sleep 1
python python/examples/01_quickstart.py
sleep 2

echo
echo "--- cross-exchange spread ---"
sleep 1
echo "$ KAIRO_BASE=BTC python python/examples/04_spread_scanner.py"
sleep 1
KAIRO_BASE=BTC python python/examples/04_spread_scanner.py
sleep 2

echo
echo "--- top 10 funding rates ---"
sleep 1
echo "$ python python/examples/08_top_funding.py"
sleep 1
python python/examples/08_top_funding.py
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
