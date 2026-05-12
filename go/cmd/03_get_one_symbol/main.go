// Package main: example 03 (get_one_symbol). Single-row lookup. Exits 4
// with a "no row" message if the exchange/base combination returns zero
// rows.
package main

import (
	"context"
	"fmt"
	"os"

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
	base := os.Getenv("KAIRO_BASE")
	if base == "" {
		base = "BTC"
	}

	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{
		Exchange: exchange,
		Bases:    []string{base},
	})
	if err != nil {
		return err
	}

	if snap.Count == 0 || len(snap.Data) == 0 {
		fmt.Printf("no row for %s/%s\n", exchange, base)
		return &client.ClientLogicError{Message: fmt.Sprintf("no row for %s/%s", exchange, base)}
	}

	row := snap.Data[0]
	fmt.Printf("%s  %s  rate=%g  next=%d  interval=%dh  event=%d\n",
		row.Exchange, row.Base, row.FundingRate,
		row.NextFundingTimeMS, row.FundingIntervalHours, row.EventTimeMS)
	return nil
}
