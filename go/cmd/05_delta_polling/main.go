// Package main: example 05 (delta_polling). Long-running poll loop that
// uses the `since` cursor. Exits cleanly on SIGINT/SIGTERM.
package main

import (
	"context"
	"errors"
	"fmt"
	"time"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
)

const (
	maxIterations = 5
	tickInterval  = 30 * time.Second
)

func main() {
	client.CLIEntry(run)
}

func run(ctx context.Context) error {
	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{})
	if err != nil {
		return err
	}
	cursor := snap.Version
	fmt.Printf("bootstrap: version=%d  count=%d\n", cursor, snap.Count)

	for i := 1; i <= maxIterations; i++ {
		if err := sleepCtx(ctx, tickInterval); err != nil {
			if errors.Is(err, context.Canceled) || errors.Is(err, context.DeadlineExceeded) {
				return nil
			}
			return err
		}

		delta, err := c.GetDelta(ctx, cursor, client.SnapshotOptions{})
		if err != nil {
			if errors.Is(err, context.Canceled) {
				return nil
			}
			return err
		}

		if delta.Count == 0 {
			fmt.Printf("tick %d: no change (version=%d)\n", i, delta.Version)
		} else {
			fmt.Printf("tick %d: %d changes, version=%d\n", i, delta.Count, delta.Version)
			for _, row := range delta.Data {
				fmt.Printf("%s  %s  rate=%g  next=%d  interval=%dh\n",
					row.Exchange, row.Base, row.FundingRate,
					row.NextFundingTimeMS, row.FundingIntervalHours)
			}
		}
		cursor = delta.Version
	}
	return nil
}

func sleepCtx(ctx context.Context, d time.Duration) error {
	t := time.NewTimer(d)
	defer t.Stop()
	select {
	case <-ctx.Done():
		return ctx.Err()
	case <-t.C:
		return nil
	}
}
