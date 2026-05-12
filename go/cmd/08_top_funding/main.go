// Package main: example 08 (top_funding). Top/bottom 10 funding rates
// across the universe with annualized percentages.
package main

import (
	"context"
	"fmt"
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
	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{})
	if err != nil {
		return err
	}

	rows := append([]client.FundingEntry(nil), snap.Data...)

	positives := filter(rows, func(r client.FundingEntry) bool { return r.FundingRate > 0 })
	negatives := filter(rows, func(r client.FundingEntry) bool { return r.FundingRate < 0 })

	sort.SliceStable(positives, func(i, j int) bool {
		if positives[i].FundingRate != positives[j].FundingRate {
			return positives[i].FundingRate > positives[j].FundingRate
		}
		if positives[i].Exchange != positives[j].Exchange {
			return positives[i].Exchange < positives[j].Exchange
		}
		return positives[i].Base < positives[j].Base
	})
	sort.SliceStable(negatives, func(i, j int) bool {
		if negatives[i].FundingRate != negatives[j].FundingRate {
			return negatives[i].FundingRate < negatives[j].FundingRate
		}
		if negatives[i].Exchange != negatives[j].Exchange {
			return negatives[i].Exchange < negatives[j].Exchange
		}
		return negatives[i].Base < negatives[j].Base
	})

	fmt.Println("TOP 10 POSITIVE")
	printHeader()
	printSection(positives, 10)
	fmt.Println()
	fmt.Println("BOTTOM 10 NEGATIVE")
	printHeader()
	printSection(negatives, 10)
	return nil
}

func filter(in []client.FundingEntry, pred func(client.FundingEntry) bool) []client.FundingEntry {
	out := make([]client.FundingEntry, 0, len(in))
	for _, r := range in {
		if pred(r) {
			out = append(out, r)
		}
	}
	return out
}

func printHeader() {
	fmt.Printf("%-12s  %-10s  %11s  %9s\n", "exchange", "base", "rate", "ann%")
}

func printSection(rows []client.FundingEntry, n int) {
	limit := len(rows)
	if limit > n {
		limit = n
	}
	for i := 0; i < limit; i++ {
		row := rows[i]
		ann := annualizedPct(row.FundingRate, row.FundingIntervalHours)
		fmt.Printf("%-12s  %-10s  %+11.6f  %+8.4f%%\n",
			row.Exchange, row.Base, row.FundingRate, ann)
	}
}
