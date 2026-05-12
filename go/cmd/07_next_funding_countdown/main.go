// Package main: example 07 (next_funding_countdown). Per-base countdown
// for a watchlist; prefixes alerting rows with "[ALERT] ".
package main

import (
	"context"
	"fmt"
	"os"
	"sort"
	"strconv"
	"strings"
	"time"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
)

func main() {
	client.CLIEntry(run)
}

func run(ctx context.Context) error {
	watchlist := os.Getenv("KAIRO_WATCHLIST")
	if watchlist == "" {
		watchlist = "BTC,ETH,SOL"
	}
	bases := splitCSV(watchlist)
	if len(bases) == 0 {
		return &client.ClientLogicError{Message: "KAIRO_WATCHLIST resolved to no bases"}
	}

	alertMinutes := 10
	if v := os.Getenv("KAIRO_ALERT_MINUTES"); v != "" {
		n, err := strconv.Atoi(v)
		if err != nil || n < 0 {
			return &client.ClientLogicError{Message: fmt.Sprintf("invalid KAIRO_ALERT_MINUTES %q", v)}
		}
		alertMinutes = n
	}

	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	snap, err := c.GetSnapshot(ctx, client.SnapshotOptions{Bases: bases})
	if err != nil {
		return err
	}

	// Pick the earliest next_funding_time_ms per base.
	earliest := make(map[string]client.FundingEntry, len(bases))
	for _, row := range snap.Data {
		cur, ok := earliest[row.Base]
		if !ok || row.NextFundingTimeMS < cur.NextFundingTimeMS {
			earliest[row.Base] = row
		}
	}

	type line struct {
		base      string
		exchange  string
		rate      float64
		remaining int64
		missing   bool
	}

	nowMs := time.Now().UnixMilli()
	threshold := int64(alertMinutes) * 60
	lines := make([]line, 0, len(bases))
	for _, b := range bases {
		row, ok := earliest[b]
		if !ok {
			lines = append(lines, line{base: b, missing: true})
			continue
		}
		remaining := (row.NextFundingTimeMS - nowMs) / 1000
		if remaining < 0 {
			lines = append(lines, line{base: b, missing: true})
			continue
		}
		lines = append(lines, line{
			base:      b,
			exchange:  row.Exchange,
			rate:      row.FundingRate,
			remaining: remaining,
		})
	}

	// Missing rows sort to the bottom; otherwise ascending by remaining.
	sort.SliceStable(lines, func(i, j int) bool {
		if lines[i].missing != lines[j].missing {
			return !lines[i].missing
		}
		return lines[i].remaining < lines[j].remaining
	})

	for _, l := range lines {
		if l.missing {
			fmt.Printf("%s: no upcoming funding\n", l.base)
			continue
		}
		prefix := ""
		if l.remaining <= threshold {
			prefix = "[ALERT] "
		}
		minutes := l.remaining / 60
		seconds := l.remaining % 60
		fmt.Printf("%s%s on %s: in %dm %ds, rate=%g\n",
			prefix, l.base, l.exchange, minutes, seconds, l.rate)
	}
	return nil
}

func splitCSV(s string) []string {
	parts := strings.Split(s, ",")
	out := make([]string, 0, len(parts))
	for _, p := range parts {
		p = strings.TrimSpace(p)
		if p != "" {
			out = append(out, p)
		}
	}
	return out
}
