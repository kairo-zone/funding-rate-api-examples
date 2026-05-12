// Package client defines typed errors and the exit-code policy used by
// every example binary in this module.
//
// The mapping is:
//
//	0 success
//	1 transient error (5xx, DNS failure, connection reset, timeout)
//	2 authentication error (HTTP 401)
//	3 rate-limited (HTTP 429)
//	4 client logic error (missing env var, bad CLI input, malformed response)
package client

import "fmt"

// AuthError is returned when the server responds with HTTP 401.
// CLIEntry maps it to exit code 2.
type AuthError struct {
	Message string
}

// Error implements the error interface.
func (e *AuthError) Error() string {
	if e.Message == "" {
		return "authentication failed (401)"
	}
	return fmt.Sprintf("authentication failed (401): %s", e.Message)
}

// RateLimitError is returned when the server responds with HTTP 429.
// CLIEntry maps it to exit code 3.
type RateLimitError struct {
	Message string
}

// Error implements the error interface.
func (e *RateLimitError) Error() string {
	if e.Message == "" {
		return "rate limited (429)"
	}
	return fmt.Sprintf("rate limited (429): %s", e.Message)
}

// TransientError wraps any error that should be treated as recoverable on a
// later retry (5xx, network failures, malformed compressed payloads).
// CLIEntry maps it to exit code 1.
type TransientError struct {
	Message string
	Cause   error
}

// Error implements the error interface.
func (e *TransientError) Error() string {
	if e.Cause != nil {
		return fmt.Sprintf("transient error: %s: %v", e.Message, e.Cause)
	}
	return fmt.Sprintf("transient error: %s", e.Message)
}

// Unwrap exposes the underlying cause for errors.Is / errors.As.
func (e *TransientError) Unwrap() error { return e.Cause }

// ClientLogicError indicates a problem with how the caller is using the
// client: missing env var, bad CLI input, response shape mismatch.
// CLIEntry maps it to exit code 4.
type ClientLogicError struct {
	Message string
}

// Error implements the error interface.
func (e *ClientLogicError) Error() string {
	return fmt.Sprintf("client logic error: %s", e.Message)
}
