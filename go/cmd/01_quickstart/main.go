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

	limit := len(snap.Data)
	if limit > 5 {
		limit = 5
	}
	for i := 0; i < limit; i++ {
		row := snap.Data[i]
		fmt.Printf("%s  %s  rate=%g  next=%d  interval=%dh\n",
			row.Exchange, row.Base, row.FundingRate,
			row.NextFundingTimeMS, row.FundingIntervalHours)
	}
	return nil
}
