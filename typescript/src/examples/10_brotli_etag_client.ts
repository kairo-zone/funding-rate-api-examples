/**
 * 10_brotli_etag_client: Brotli-decoded GET, conditional GET via
 * `If-None-Match`, then a `since` delta — all in one program.
 */

import { FundingClient } from "../client.js";
import { ClientLogicError } from "../errors.js";
import { cliEntry } from "../runner.js";

async function main(): Promise<void> {
  const client = new FundingClient();

  // Call A: brotli-encoded full snapshot.
  const a = await client.getRaw("/v1/funding", {
    params: { compact: "true" },
    acceptBrotli: true,
  });
  if (a.status !== 200) {
    throw new ClientLogicError(`call A: unexpected status ${a.status}`);
  }
  const aBody = JSON.parse(a.body.toString("utf8")) as { version: number };
  const etag = a.headers.get("etag") ?? "";
  process.stdout.write(
    `A: status=${a.status}  bytes_compressed=${a.wireBytes}  ` +
      `bytes_decoded=${a.body.length}  version=${aBody.version}  etag=${etag}\n`,
  );

  // Call B: same URL, conditional on the ETag we just received.
  const b = await client.getRaw("/v1/funding", {
    params: { compact: "true" },
    acceptBrotli: true,
    headers: etag ? { "If-None-Match": etag } : {},
  });
  const etagNow = b.headers.get("etag");
  const etagDisplay = b.status === 304 ? "unchanged" : (etagNow ?? "unchanged");
  process.stdout.write(`B: status=${b.status}  etag_now=${etagDisplay}\n`);

  // Call C: delta from the version we observed in call A.
  const c = await client.getDelta(aBody.version);
  process.stdout.write(`C: since=${aBody.version}  count=${c.count}  version=${c.version}\n`);
}

cliEntry(main);
