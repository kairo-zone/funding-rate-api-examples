/**
 * Typed error classes for the Funding API client.
 *
 * Exit-code mapping (see EXAMPLES.md "Conventions"):
 *   AuthError        -> 2  (HTTP 401)
 *   RateLimitError   -> 3  (HTTP 429)
 *   TransientError   -> 1  (5xx, DNS, connection reset, timeout)
 *   ClientLogicError -> 4  (missing env var, bad CLI input, malformed response)
 */

export class FundingApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = new.target.name;
  }
}

export class AuthError extends FundingApiError {}
export class RateLimitError extends FundingApiError {}
export class TransientError extends FundingApiError {}
export class ClientLogicError extends FundingApiError {}
