// Package main: example 10 (brotli_etag_client). Demonstrates Brotli
// negotiation, conditional GET via If-None-Match, and delta polling via
// the `since` cursor.
package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"

	"github.com/kairo-zone/funding-rate-api-examples/go/internal/client"
)

func main() {
	client.CLIEntry(run)
}

type minimalSnapshot struct {
	Version int64 `json:"version"`
	Count   int   `json:"count"`
}

func run(ctx context.Context) error {
	c, err := client.New(client.Options{})
	if err != nil {
		return err
	}

	// Call A: GET /v1/funding with Brotli accepted.
	a, err := c.GetRaw(ctx, "/v1/funding", client.RawOptions{
		Query:        url.Values{"compact": {"true"}},
		AcceptBrotli: true,
	})
	if err != nil {
		return err
	}
	wireA := a.Headers.Get("X-Wire-Bytes")
	var bodyA minimalSnapshot
	if err := json.Unmarshal(a.Body, &bodyA); err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("decode A: %v", err)}
	}
	etag := a.Headers.Get("ETag")
	fmt.Printf("A: status=%d  bytes_compressed=%s  bytes_decoded=%d  version=%d  etag=%s\n",
		a.Status, wireA, len(a.Body), bodyA.Version, etag)

	// Call B: conditional GET with If-None-Match.
	b, err := c.GetRaw(ctx, "/v1/funding", client.RawOptions{
		Query:        url.Values{"compact": {"true"}},
		AcceptBrotli: true,
		IfNoneMatch:  etag,
	})
	if err != nil {
		return err
	}
	etagNow := "unchanged"
	if b.Status == 200 {
		etagNow = b.Headers.Get("ETag")
	}
	fmt.Printf("B: status=%d  etag_now=%s\n", b.Status, etagNow)

	// Call C: delta via since cursor.
	cRaw, err := c.GetRaw(ctx, "/v1/funding", client.RawOptions{
		Query: url.Values{
			"compact": {"true"},
			"since":   {fmt.Sprintf("%d", bodyA.Version)},
		},
	})
	if err != nil {
		return err
	}
	var bodyC minimalSnapshot
	if err := json.Unmarshal(cRaw.Body, &bodyC); err != nil {
		return &client.ClientLogicError{Message: fmt.Sprintf("decode C: %v", err)}
	}
	fmt.Printf("C: since=%d  count=%d  version=%d\n", bodyA.Version, bodyC.Count, bodyC.Version)
	return nil
}
