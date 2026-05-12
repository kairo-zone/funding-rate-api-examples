// Package main: example 04 (spread_scanner). Cross-exchange spread for
// one base asset. Prints every row sorted by funding_rate ascending and a
// summary line with min/max.
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

func annualizedPct(rate float64, intervalHours int64) float64 {
	if intervalHours <= 0 {
		return 0
	}
	return rate * (24.0 / float64(intervalHours)) * 365.0 * 100.0
}

func run(ctx context.Context) error {
	base := os.Getenv("KAIRO_BASE")
	if base == "" {
		base = "BTC"
	}

	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{Bases: []string{base}})
	if err != nil {
		return err
	}

	rows := append([]client.FundingEntry(nil), snap.Data...)
	if len(rows) == 0 {
		fmt.Printf("no rows for base=%s\n", base)
		return nil
	}

	sort.SliceStable(rows, func(i, j int) bool {
		if rows[i].FundingRate != rows[j].FundingRate {
			return rows[i].FundingRate < rows[j].FundingRate
		}
		if rows[i].Exchange != rows[j].Exchange {
			return rows[i].Exchange < rows[j].Exchange
		}
		return rows[i].Base < rows[j].Base
	})

	for _, row := range rows {
		ann := annualizedPct(row.FundingRate, row.FundingIntervalHours)
		fmt.Printf("%s  rate=%g  ann=%.2f%%  interval=%dh\n",
			row.Exchange, row.FundingRate, ann, row.FundingIntervalHours)
	}

	minRow := rows[0]
	maxRow := rows[len(rows)-1]
	spread := maxRow.FundingRate - minRow.FundingRate
	fmt.Printf("spread = %g (max %s @ %g, min %s @ %g)\n",
		spread, maxRow.Exchange, maxRow.FundingRate, minRow.Exchange, minRow.FundingRate)
	return nil
}
