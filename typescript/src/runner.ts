/**
 * Shared CLI entry-point. Applies the uniform exit-code policy from
 * EXAMPLES.md ("Conventions for language implementers") to every example.
 */

import { AuthError, ClientLogicError, RateLimitError, TransientError } from "./errors.js";

export type Main = () => Promise<number | void>;

/**
 * Wrap an example's `main` function with the uniform exit-code mapping.
 *
 * Exit codes:
 *   0  success (default if `main` resolves without a number)
 *   1  TransientError (5xx / network / timeout)
 *   2  AuthError (HTTP 401)
 *   3  RateLimitError (HTTP 429)
 *   4  ClientLogicError (bad env, malformed response, etc.)
 */
export function cliEntry(main: Main): void {
  main()
    .then((code) => {
      process.exit(typeof code === "number" ? code : 0);
    })
    .catch((err: unknown) => {
      const message = err instanceof Error ? err.message : String(err);
      process.stderr.write(`error: ${message}\n`);
      if (err instanceof AuthError) process.exit(2);
      if (err instanceof RateLimitError) process.exit(3);
      if (err instanceof TransientError) process.exit(1);
      if (err instanceof ClientLogicError) process.exit(4);
      process.exit(1);
    });
}
