"""Thin HTTP wrapper for the kairo.zone Funding API.

httpx is the chosen transport: single sync client, built-in Brotli support
when installed with the `[brotli]` extra, and a stable typed Response object.
"""

from __future__ import annotations

import os
import sys
from dataclasses import dataclass
from typing import Any, Callable, Mapping

import httpx

DEFAULT_BASE_URL = "https://api.kairo.zone"
COMPACT_FIELDS = (
    "exchange",
    "base",
    "funding_rate",
    "next_funding_time_ms",
    "funding_interval_hours",
    "event_time_ms",
)


class KairoError(Exception):
    """Base class for typed errors raised by FundingClient."""

    exit_code = 1


class TransientError(KairoError):
    """5xx, DNS, connection reset, timeout. Exit code 1."""

    exit_code = 1


class AuthError(KairoError):
    """HTTP 401. Exit code 2."""

    exit_code = 2


class RateLimitError(KairoError):
    """HTTP 429. Exit code 3."""

    exit_code = 3


class ClientLogicError(KairoError):
    """Missing env var, bad CLI input, malformed response. Exit code 4."""

    exit_code = 4


@dataclass
class RawResponse:
    """Lightweight view over an httpx.Response for the Brotli/ETag example."""

    status: int
    headers: Mapping[str, str]
    body_bytes: bytes
    json: Any | None


class FundingClient:
    """Synchronous client over httpx with typed error mapping."""

    def __init__(
        self,
        api_key: str | None = None,
        base_url: str | None = None,
        timeout: float = 15.0,
    ) -> None:
        key = api_key if api_key is not None else os.environ.get("KAIRO_FUNDING_API_KEY")
        if not key:
            raise ClientLogicError("KAIRO_FUNDING_API_KEY is not set")
        self._api_key = key
        self._base_url = (base_url or os.environ.get("KAIRO_FUNDING_BASE_URL") or DEFAULT_BASE_URL).rstrip("/")
        self._http = httpx.Client(
            base_url=self._base_url,
            timeout=timeout,
            headers={"X-Api-Key": self._api_key, "Accept": "application/json"},
        )

    # ---- public API --------------------------------------------------------

    def get_snapshot(self, exchange: str | None = None, base: str | None = None, compact: bool = True) -> dict:
        return self._json("/v1/funding", self._funding_params(exchange=exchange, base=base, compact=compact))

    def get_delta(self, since: int, exchange: str | None = None, base: str | None = None, compact: bool = True) -> dict:
        params = self._funding_params(exchange=exchange, base=base, compact=compact)
        params["since"] = since
        return self._json("/v1/funding", params)

    def get_symbols(self, exchange: str | None = None) -> dict:
        params: dict[str, Any] = {}
        if exchange:
            params["exchange"] = exchange
        return self._json("/v1/symbols", params)

    def get_raw(
        self,
        path: str,
        params: dict | None = None,
        headers: dict | None = None,
        accept_brotli: bool = False,
    ) -> RawResponse:
        """Issue a request and return a RawResponse without decoding errors to typed exceptions for 200/304."""
        send_headers = dict(headers or {})
        if accept_brotli:
            send_headers["Accept-Encoding"] = "br"
        try:
            resp = self._http.get(path, params=params or {}, headers=send_headers)
        except httpx.HTTPError as exc:
            raise TransientError(f"network error: {exc}") from exc
        if resp.status_code == 401:
            raise AuthError("unauthorized (401)")
        if resp.status_code == 429:
            raise RateLimitError("rate limited (429)")
        if resp.status_code >= 500:
            raise TransientError(f"server error ({resp.status_code})")
        if resp.status_code not in (200, 304):
            raise ClientLogicError(f"unexpected status {resp.status_code}: {resp.text[:200]}")
        body = resp.content if resp.status_code == 200 else b""
        parsed: Any | None = None
        if resp.status_code == 200 and body:
            try:
                parsed = resp.json()
            except ValueError:
                parsed = None
        return RawResponse(status=resp.status_code, headers=resp.headers, body_bytes=body, json=parsed)

    # ---- helpers -----------------------------------------------------------

    @staticmethod
    def parse_compact_rows(data: list[list]) -> list[dict]:
        """Turn each positional tuple into a dict keyed by COMPACT_FIELDS."""
        out: list[dict] = []
        for row in data:
            if isinstance(row, dict):
                out.append(row)
                continue
            if not isinstance(row, list) or len(row) < len(COMPACT_FIELDS):
                raise ClientLogicError(f"malformed compact row: {row!r}")
            out.append(dict(zip(COMPACT_FIELDS, row)))
        return out

    def close(self) -> None:
        self._http.close()

    def __enter__(self) -> "FundingClient":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()

    # ---- internals ---------------------------------------------------------

    def _funding_params(self, exchange: str | None, base: str | None, compact: bool) -> dict:
        params: dict[str, Any] = {}
        if exchange:
            params["exchange"] = exchange
        if base:
            params["base"] = base
        if not compact:
            params["compact"] = "false"
        return params

    def _json(self, path: str, params: dict) -> dict:
        try:
            resp = self._http.get(path, params=params)
        except httpx.HTTPError as exc:
            raise TransientError(f"network error: {exc}") from exc
        if resp.status_code == 401:
            raise AuthError("unauthorized (401)")
        if resp.status_code == 429:
            raise RateLimitError("rate limited (429)")
        if resp.status_code >= 500:
            raise TransientError(f"server error ({resp.status_code})")
        if resp.status_code != 200:
            raise ClientLogicError(f"unexpected status {resp.status_code}: {resp.text[:200]}")
        try:
            return resp.json()
        except ValueError as exc:
            raise ClientLogicError(f"malformed JSON body: {exc}") from exc


def cli_entry(main_fn: Callable[[], int | None]) -> None:
    """Run an example's main(), map typed errors to exit codes, print to stderr."""
    try:
        rc = main_fn()
        sys.exit(int(rc) if rc is not None else 0)
    except KairoError as exc:
        print(f"error: {exc}", file=sys.stderr)
        sys.exit(exc.exit_code)
    except KeyboardInterrupt:
        sys.exit(0)
