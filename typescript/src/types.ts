/**
 * Wire-format and TypeScript-land type definitions for the Funding API.
 *
 * The HTTP JSON payload uses snake_case (e.g. `funding_rate`).
 * Inside TypeScript code we expose a camelCase view (`fundingRate`) so that
 * normal property access reads idiomatically. The `parseCompactRows` helper
 * in `client.ts` performs the rename.
 */

/** Single funding row as seen by callers (camelCase). */
export interface FundingEntry {
  exchange: string;
  base: string;
  fundingRate: number;
  nextFundingTimeMs: number;
  fundingIntervalHours: number;
  eventTimeMs: number;
}

/** Single symbol-universe row as returned by `/v1/symbols`. */
export interface SymbolEntry {
  exchange: string;
  symbol: string;
  base: string;
  quote: string;
  native: string;
  type: string;
  funding_interval_hours: number;
  is_active: boolean;
}

/** Response shape for `/v1/funding` and its delta variants. */
export interface SnapshotResponse {
  version: number;
  timestampMs?: number;
  count: number;
  data: FundingEntry[];
}

/** Response shape for `/v1/symbols`. */
export interface SymbolsResponse {
  count: number;
  data: SymbolEntry[];
}

/** Low-level response returned by `FundingClient.getRaw`. */
export interface RawResponse {
  status: number;
  headers: Headers;
  /** Decoded body bytes (Brotli already decompressed when applicable). */
  body: Buffer;
  /** Raw wire-byte count of the body as received from the network. */
  wireBytes: number;
}

/** Compact positional row as sent by the server when `compact=true`. */
export type CompactRow = [
  string, // exchange
  string, // base
  number, // funding_rate
  number, // next_funding_time_ms
  number, // funding_interval_hours
  number, // event_time_ms
];
