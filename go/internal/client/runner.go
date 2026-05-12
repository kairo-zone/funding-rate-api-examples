// Package client: CLIEntry wraps an example's main body and translates the
// typed error tree into the contract-mandated process exit codes.
package client

import (
	"context"
	"errors"
	"fmt"
	"os"
	"os/signal"
	"syscall"
)

// CLIEntry runs fn with a SIGINT/SIGTERM-aware context and calls os.Exit
// with a code that matches the language-agnostic exit policy described in
// EXAMPLES.md. SIGINT during fn execution is a clean shutdown (exit 0).
func CLIEntry(fn func(ctx context.Context) error) {
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	err := fn(ctx)
	os.Exit(ExitCodeFor(ctx, err))
}

// ExitCodeFor converts an error returned by an example main function into
// the process exit code mandated by the shared contract. It is exported so
// tests can validate the mapping without spawning a subprocess.
func ExitCodeFor(ctx context.Context, err error) int {
	if err == nil {
		return 0
	}

	// A cancelled context (Ctrl+C) is a clean exit, not a failure.
	if ctx != nil && ctx.Err() != nil && errors.Is(err, context.Canceled) {
		return 0
	}

	var authErr *AuthError
	if errors.As(err, &authErr) {
		fmt.Fprintln(os.Stderr, err.Error())
		return 2
	}
	var rlErr *RateLimitError
	if errors.As(err, &rlErr) {
		fmt.Fprintln(os.Stderr, err.Error())
		return 3
	}
	var logicErr *ClientLogicError
	if errors.As(err, &logicErr) {
		fmt.Fprintln(os.Stderr, err.Error())
		return 4
	}
	var transientErr *TransientError
	if errors.As(err, &transientErr) {
		fmt.Fprintln(os.Stderr, err.Error())
		return 1
	}

	// Unclassified errors are treated as transient by default.
	fmt.Fprintln(os.Stderr, err.Error())
	return 1
}
