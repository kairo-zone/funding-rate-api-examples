// Package main: example 06 (funding_alert). Threshold-based alert with an
// optional webhook sink. Webhook failures never crash the process.
package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"math"
	"net/http"
	"os"
	"strconv"
	"time"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
)

func main() {
	client.CLIEntry(run)
}

func run(ctx context.Context) error {
	thresholdStr := os.Getenv("KAIRO_THRESHOLD")
	if thresholdStr == "" {
		thresholdStr = "0.001"
	}
	threshold, err := strconv.ParseFloat(thresholdStr, 64)
	if err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("invalid KAIRO_THRESHOLD %q: %v", thresholdStr, err)}
	}

	webhook := os.Getenv("KAIRO_WEBHOOK_URL")

	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{})
	if err != nil {
		return err
	}

	hookClient := &http.Client{Timeout: 5 * time.Second}

	matched := 0
	for _, row := range snap.Data {
		if math.Abs(row.FundingRate) < threshold {
			continue
		}
		matched++
		fmt.Printf("ALERT  %s  %s  rate=%g  next=%d\n",
			row.Exchange, row.Base, row.FundingRate, row.NextFundingTimeMS)

		if webhook != "" {
			if err := postWebhook(ctx, hookClient, webhook, row); err != nil {
				fmt.Fprintf(os.Stderr, "webhook failed for %s: %v\n", row.Base, err)
			}
		}
	}

	fmt.Printf("matched %d/%d rows above threshold %g\n", matched, snap.Count, threshold)
	return nil
}

func postWebhook(ctx context.Context, hc *http.Client, url string, row client.FundingEntry) error {
	payload := map[string]any{
		"exchange":             row.Exchange,
		"base":                 row.Base,
		"funding_rate":         row.FundingRate,
		"next_funding_time_ms": row.NextFundingTimeMS,
	}
	body, err := json.Marshal(payload)
	if err != nil {
		return err
	}
	req, err := http.NewRequestWithContext(ctx, http.MethodPost, url, bytes.NewReader(body))
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")
	resp, err := hc.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()
	if resp.StatusCode >= 400 {
		return fmt.Errorf("status %d", resp.StatusCode)
	}
	return nil
}
