/**
 * Thin HTTP wrapper around the kairo.zone Funding API.
 *
 * No retries, no caching, no telemetry — the examples are meant to
 * showcase the wire protocol, not hide it behind an SDK.
 */

import { brotliDecompressSync } from "node:zlib";
import { AuthError, ClientLogicError, RateLimitError, TransientError } from "./errors.js";
import type {
  CompactRow,
  FundingEntry,
  RawResponse,
  SnapshotResponse,
  SymbolsResponse,
} from "./types.js";

export type { FundingEntry, RawResponse, SnapshotResponse, SymbolsResponse } from "./types.js";

export interface SnapshotOptions {
  exchange?: string;
  base?: string | string[];
  compact?: boolean;
}

export interface GetRawInit {
  params?: Record<string, string>;
  headers?: Record<string, string>;
  acceptBrotli?: boolean;
}

const DEFAULT_BASE_URL = "https://api.kairo.zone";

/**
 * Re-shape positional compact rows into camelCase {@link FundingEntry} objects.
 */
export function parseCompactRows(rows: unknown[][]): FundingEntry[] {
  const out: FundingEntry[] = [];
  for (const row of rows) {
    const r = row as CompactRow;
    out.push({
      exchange: r[0],
      base: r[1],
      fundingRate: r[2],
      nextFundingTimeMs: r[3],
      fundingIntervalHours: r[4],
      eventTimeMs: r[5],
    });
  }
  return out;
}

/** HTTP client. One instance per process is fine. */
export class FundingClient {
  private readonly apiKey: string;
  private readonly baseUrl: string;

  constructor() {
    const apiKey = process.env.KAIRO_FUNDING_API_KEY;
    if (!apiKey) {
      throw new ClientLogicError("KAIRO_FUNDING_API_KEY environment variable is required");
    }
    this.apiKey = apiKey;
    this.baseUrl = (process.env.KAIRO_FUNDING_BASE_URL ?? DEFAULT_BASE_URL).replace(/\/+$/, "");
  }

  async getSnapshot(opts: SnapshotOptions = {}): Promise<SnapshotResponse> {
    return this.fetchFunding(this.snapshotParams(opts));
  }

  async getDelta(since: number, opts: SnapshotOptions = {}): Promise<SnapshotResponse> {
    const params = this.snapshotParams(opts);
    params.since = String(since);
    return this.fetchFunding(params);
  }

  async getSymbols(opts: { exchange?: string } = {}): Promise<SymbolsResponse> {
    const params: Record<string, string> = {};
    if (opts.exchange) params.exchange = opts.exchange;
    const raw = await this.getRaw("/v1/symbols", { params });
    return JSON.parse(raw.body.toString("utf8")) as SymbolsResponse;
  }

  /** Low-level GET. Surfaces wire bytes + headers so example 10 can inspect them. */
  async getRaw(path: string, init: GetRawInit = {}): Promise<RawResponse> {
    const url = new URL(this.baseUrl + path);
    for (const [k, v] of Object.entries(init.params ?? {})) url.searchParams.set(k, v);

    const headers: Record<string, string> = {
      "X-Api-Key": this.apiKey,
      Accept: "application/json",
      ...(init.headers ?? {}),
    };
    if (init.acceptBrotli) headers["Accept-Encoding"] = "br";

    let res: Response;
    try {
      res = await fetch(url, { headers });
    } catch (err) {
      throw new TransientError(`network failure: ${(err as Error).message}`);
    }

    // Read the raw wire bytes before any decoding. Node's fetch only auto-
    // decompresses gzip/deflate; Brotli we handle by hand.
    const rawBuf = Buffer.from(await res.arrayBuffer());
    const wireBytes = rawBuf.length;

    if (res.status === 401) throw new AuthError("API key was rejected (HTTP 401)");
    if (res.status === 429) throw new RateLimitError("rate limit exceeded (HTTP 429)");
    if (res.status >= 500) throw new TransientError(`server error ${res.status}`);

    let body = rawBuf;
    if (res.headers.get("content-encoding") === "br" && body.length > 0) {
      try {
        body = Buffer.from(brotliDecompressSync(body));
      } catch (err) {
        throw new ClientLogicError(`failed to decode Brotli body: ${(err as Error).message}`);
      }
    }
    return { status: res.status, headers: res.headers, body, wireBytes };
  }

  private snapshotParams(opts: SnapshotOptions): Record<string, string> {
    const params: Record<string, string> = {};
    if (opts.exchange) params.exchange = opts.exchange;
    if (opts.base !== undefined) {
      params.base = Array.isArray(opts.base) ? opts.base.join(",") : opts.base;
    }
    params.compact = opts.compact === false ? "false" : "true";
    return params;
  }

  private async fetchFunding(params: Record<string, string>): Promise<SnapshotResponse> {
    const raw = await this.getRaw("/v1/funding", { params });
    let parsed: {
      version: number;
      timestamp_ms?: number;
      count: number;
      data: unknown[];
    };
    try {
      parsed = JSON.parse(raw.body.toString("utf8"));
    } catch (err) {
      throw new ClientLogicError(`malformed JSON response: ${(err as Error).message}`);
    }
    const isCompact = params.compact !== "false";
    const data = isCompact
      ? parseCompactRows(parsed.data as unknown[][])
      : (parsed.data as FundingEntry[]).map((d) => normalizeObjectRow(d));
    return {
      version: parsed.version,
      timestampMs: parsed.timestamp_ms,
      count: parsed.count,
      data,
    };
  }
}

/** Rename a `compact=false` row from snake_case to camelCase. */
function normalizeObjectRow(d: unknown): FundingEntry {
  const o = d as Record<string, unknown>;
  return {
    exchange: String(o.exchange),
    base: String(o.base),
    fundingRate: Number(o.funding_rate),
    nextFundingTimeMs: Number(o.next_funding_time_ms),
    fundingIntervalHours: Number(o.funding_interval_hours),
    eventTimeMs: Number(o.event_time_ms),
  };
}
