// Package main: example 01 (quickstart). Smallest possible client - one
// GET to /v1/funding, prints the version and the first five rows.
package main

import (
	"context"
	"fmt"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
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

	fmt.Printf("version=%d  count=%d\n", snap.Version, snap.Count)
	fmt.Println()
	fmt.Printf("%-12s  %-10s  %11s  %13s  %4s\n", "exchange", "base", "rate", "next_ms", "intv")

	limit := len(snap.Data)
	if limit > 5 {
		limit = 5
	}
	for i := 0; i < limit; i++ {
		row := snap.Data[i]
		fmt.Printf("%-12s  %-10s  %+11.6f  %13d  %3dh\n",
			row.Exchange, row.Base, row.FundingRate,
			row.NextFundingTimeMS, row.FundingIntervalHours)
	}
	return nil
}
