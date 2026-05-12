"""Brotli decoding plus conditional GET with ETag / If-None-Match."""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from kairo_funding import ClientLogicError, FundingClient, cli_entry  # noqa: E402


def main() -> int:
    with FundingClient() as client:
        # Call A: full snapshot with Brotli encoding. httpx decodes the body
        # transparently when the `brotli` package is installed, so body_bytes
        # contains the decoded payload while Content-Length reflects the
        # compressed wire size reported by the server.
        a = client.get_raw("/v1/funding", accept_brotli=True)
        if a.status != 200 or a.json is None:
            raise ClientLogicError(f"unexpected A response: status={a.status}")
        etag = a.headers.get("ETag", "")
        version = a.json.get("version")
        wire_bytes = a.headers.get("Content-Length") or str(len(a.body_bytes))
        decoded_bytes = len(json.dumps(a.json, separators=(",", ":")).encode("utf-8"))
        print(
            f"A: status={a.status}  bytes_compressed={wire_bytes}  "
            f"bytes_decoded={decoded_bytes}  version={version}  etag={etag}"
        )

        # Call B: conditional GET using the ETag from A.
        b = client.get_raw(
            "/v1/funding",
            headers={"If-None-Match": etag} if etag else None,
            accept_brotli=True,
        )
        if b.status == 304:
            print("B: status=304  etag_now=unchanged")
        else:
            new_etag = b.headers.get("ETag", "")
            print(f"B: status={b.status}  etag_now={new_etag}")

        # Call C: delta call using the version cursor from A.
        c = client.get_delta(since=version)
        print(
            f"C: since={version}  count={c.get('count', 0)}  version={c.get('version')}"
        )
    return 0


if __name__ == "__main__":
    cli_entry(main)
