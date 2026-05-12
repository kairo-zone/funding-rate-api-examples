// Package main: example 02 (filter_by_exchange). Demonstrates the
// exchange filter by listing the first ten rows for a single venue, sorted
// by base asset ascending.
package main

import (
	"context"
	"fmt"
	"os"
	"sort"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
)

func main() {
	client.CLIEntry(run)
}

func run(ctx context.Context) error {
	exchange := os.Getenv("KAIRO_EXCHANGE")
	if exchange == "" {
		exchange = "bybit"
	}

	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{Exchange: exchange})
	if err != nil {
		return err
	}

	rows := append([]client.FundingEntry(nil), snap.Data...)
	sort.SliceStable(rows, func(i, j int) bool {
		return rows[i].Base < rows[j].Base
	})

	fmt.Printf("exchange=%s  rows=%d\n", exchange, snap.Count)
	fmt.Println()
	fmt.Printf("%-16s  %11s  %4s\n", "base", "rate", "intv")

	limit := len(rows)
	if limit > 10 {
		limit = 10
	}
	for i := 0; i < limit; i++ {
		row := rows[i]
		fmt.Printf("%-16s  %+11.6f  %3dh\n",
			row.Base, row.FundingRate, row.FundingIntervalHours)
	}
	return nil
}
