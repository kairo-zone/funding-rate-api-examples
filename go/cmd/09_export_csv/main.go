// Package main: example 09 (export_csv). Persists a snapshot to a CSV
// file named after the response version.
package main

import (
	"context"
	"encoding/csv"
	"fmt"
	"os"
	"strconv"

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

	path := fmt.Sprintf("funding_%d.csv", snap.Version)
	f, err := os.Create(path)
	if err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("create csv: %v", err)}
	}
	defer f.Close()

	w := csv.NewWriter(f)
	defer w.Flush()

	header := []string{
		"exchange", "base", "funding_rate",
		"next_funding_time_ms", "funding_interval_hours", "event_time_ms",
	}
	if err := w.Write(header); err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("write header: %v", err)}
	}

	for _, row := range snap.Data {
		record := []string{
			row.Exchange,
			row.Base,
			strconv.FormatFloat(row.FundingRate, 'g', -1, 64),
			strconv.FormatInt(row.NextFundingTimeMS, 10),
			strconv.FormatInt(row.FundingIntervalHours, 10),
			strconv.FormatInt(row.EventTimeMS, 10),
		}
		if err := w.Write(record); err != nil {
			return &client.ClientLogicError{Message: fmt.Sprintf("write row: %v", err)}
		}
	}
	w.Flush()
	if err := w.Error(); err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("flush csv: %v", err)}
	}

	fmt.Printf("wrote %d rows to %s\n", len(snap.Data), path)
	return nil
}
