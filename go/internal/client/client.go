// Package client is a thin HTTP wrapper around the kairo.zone Funding API
// for use by the example programs in this repository. It deliberately
// avoids SDK features such as retries, caching, or telemetry: each example
// is meant to be a readable, self-contained reference.
package client

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"math"
	"net/http"
	"net/url"
	"os"
	"strconv"
	"strings"
	"time"

	"github.com/andybalholm/brotli"
)

// DefaultBaseURL is the production Funding API base URL used when neither
// Options.BaseURL nor KAIRO_FUNDING_BASE_URL is set.
const DefaultBaseURL = "https://api.kairo.zone"

// Options configures Client construction. Empty fields are filled from
// environment variables (KAIRO_FUNDING_API_KEY, KAIRO_FUNDING_BASE_URL).
type Options struct {
	// APIKey is the value sent as the X-Api-Key header.
	APIKey string
	// BaseURL overrides the default base URL.
	BaseURL string
	// HTTPClient lets callers inject a custom transport. Defaults to a
	// sensible *http.Client with a 30s timeout.
	HTTPClient *http.Client
}

// Client is the thin HTTP wrapper used by every example.
type Client struct {
	baseURL string
	apiKey  string
	http    *http.Client
}

// New constructs a Client, resolving missing fields from env vars. It
// returns a *ClientLogicError when the API key is missing.
func New(opts Options) (*Client, error) {
	apiKey := opts.APIKey
	if apiKey == "" {
		apiKey = os.Getenv("KAIRO_FUNDING_API_KEY")
	}
	if apiKey == "" {
		return nil, &ClientLogicError{Message: "KAIRO_FUNDING_API_KEY is not set"}
	}

	baseURL := opts.BaseURL
	if baseURL == "" {
		baseURL = os.Getenv("KAIRO_FUNDING_BASE_URL")
	}
	if baseURL == "" {
		baseURL = DefaultBaseURL
	}
	baseURL = strings.TrimRight(baseURL, "/")

	hc := opts.HTTPClient
	if hc == nil {
		hc = &http.Client{Timeout: 30 * time.Second}
	}

	return &Client{baseURL: baseURL, apiKey: apiKey, http: hc}, nil
}

// FundingEntry is a single funding-rate row, normalized from either the
// compact positional encoding or the named object encoding.
type FundingEntry struct {
	Exchange             string  `json:"exchange"`
	Base                 string  `json:"base"`
	FundingRate          float64 `json:"funding_rate"`
	NextFundingTimeMS    int64   `json:"next_funding_time_ms"`
	FundingIntervalHours int64   `json:"funding_interval_hours"`
	EventTimeMS          int64   `json:"event_time_ms"`
}

// SymbolEntry is one row from /v1/symbols.
type SymbolEntry struct {
	Exchange             string `json:"exchange"`
	Symbol               string `json:"symbol"`
	Base                 string `json:"base"`
	Quote                string `json:"quote"`
	Native               string `json:"native"`
	Type                 string `json:"type"`
	FundingIntervalHours int64  `json:"funding_interval_hours"`
	IsActive             bool   `json:"is_active"`
}

// SnapshotResponse is the decoded body of a /v1/funding response after
// compact rows have been normalized to FundingEntry.
type SnapshotResponse struct {
	Version     int64          `json:"version"`
	TimestampMS int64          `json:"timestamp_ms,omitempty"`
	Count       int            `json:"count"`
	Data        []FundingEntry `json:"data"`
	ETag        string         `json:"-"`
}

// SymbolsResponse is the decoded body of a /v1/symbols response.
type SymbolsResponse struct {
	Count int           `json:"count"`
	Data  []SymbolEntry `json:"data"`
}

// RawResponse is returned by GetRaw when an example needs to inspect the
// transport layer (compression, headers, byte counts) directly.
type RawResponse struct {
	Status  int
	Headers http.Header
	Body    []byte
}

// SnapshotOptions captures the query-string knobs supported by /v1/funding.
type SnapshotOptions struct {
	Exchange string
	// Bases is rendered as a comma-separated list when non-empty.
	Bases []string
	// IfNoneMatch sets the conditional GET header.
	IfNoneMatch string
}

// SymbolsOptions captures the query-string knobs supported by /v1/symbols.
type SymbolsOptions struct {
	Exchange string
}

// RawOptions exposes the request controls needed by example 10.
type RawOptions struct {
	Query        url.Values
	IfNoneMatch  string
	AcceptBrotli bool
}

// GetSnapshot fetches the current funding-rate snapshot. The compact wire
// format is always requested; rows are normalized to FundingEntry.
func (c *Client) GetSnapshot(ctx context.Context, opts SnapshotOptions) (*SnapshotResponse, error) {
	return c.getFunding(ctx, opts, 0, false)
}

// GetDelta fetches a delta snapshot using the since cursor.
func (c *Client) GetDelta(ctx context.Context, since int64, opts SnapshotOptions) (*SnapshotResponse, error) {
	return c.getFunding(ctx, opts, since, true)
}

func (c *Client) getFunding(ctx context.Context, opts SnapshotOptions, since int64, useSince bool) (*SnapshotResponse, error) {
	q := url.Values{}
	q.Set("compact", "true")
	if useSince {
		q.Set("since", strconv.FormatInt(since, 10))
	}
	if opts.Exchange != "" {
		q.Set("exchange", opts.Exchange)
	}
	if len(opts.Bases) > 0 {
		q.Set("base", strings.Join(opts.Bases, ","))
	}

	req, err := c.newRequest(ctx, "/v1/funding", q, opts.IfNoneMatch, false)
	if err != nil {
		return nil, err
	}

	resp, err := c.http.Do(req)
	if err != nil {
		return nil, &TransientError{Message: "http request failed", Cause: err}
	}
	defer resp.Body.Close()

	body, err := readBody(resp)
	if err != nil {
		return nil, err
	}

	if err := mapStatus(resp.StatusCode, body); err != nil {
		return nil, err
	}

	var raw struct {
		Version     int64           `json:"version"`
		TimestampMS int64           `json:"timestamp_ms"`
		Count       int             `json:"count"`
		Data        json.RawMessage `json:"data"`
	}
	if err := json.Unmarshal(body, &raw); err != nil {
		return nil, &ClientLogicError{Message: fmt.Sprintf("decode snapshot: %v", err)}
	}

	entries, err := decodeFundingRows(raw.Data)
	if err != nil {
		return nil, err
	}

	return &SnapshotResponse{
		Version:     raw.Version,
		TimestampMS: raw.TimestampMS,
		Count:       raw.Count,
		Data:        entries,
		ETag:        resp.Header.Get("ETag"),
	}, nil
}

// GetSymbols fetches the /v1/symbols payload.
func (c *Client) GetSymbols(ctx context.Context, opts SymbolsOptions) (*SymbolsResponse, error) {
	q := url.Values{}
	if opts.Exchange != "" {
		q.Set("exchange", opts.Exchange)
	}
	req, err := c.newRequest(ctx, "/v1/symbols", q, "", false)
	if err != nil {
		return nil, err
	}
	resp, err := c.http.Do(req)
	if err != nil {
		return nil, &TransientError{Message: "http request failed", Cause: err}
	}
	defer resp.Body.Close()

	body, err := readBody(resp)
	if err != nil {
		return nil, err
	}
	if err := mapStatus(resp.StatusCode, body); err != nil {
		return nil, err
	}

	out := &SymbolsResponse{}
	if err := json.Unmarshal(body, out); err != nil {
		return nil, &ClientLogicError{Message: fmt.Sprintf("decode symbols: %v", err)}
	}
	return out, nil
}

// GetRaw issues a generic GET to path with the given options. Used by
// example 10 to inspect compression and conditional-GET behavior.
func (c *Client) GetRaw(ctx context.Context, path string, opts RawOptions) (*RawResponse, error) {
	req, err := c.newRequest(ctx, path, opts.Query, opts.IfNoneMatch, opts.AcceptBrotli)
	if err != nil {
		return nil, err
	}
	resp, err := c.http.Do(req)
	if err != nil {
		return nil, &TransientError{Message: "http request failed", Cause: err}
	}
	defer resp.Body.Close()

	// Capture the raw, on-the-wire body before any decompression so the
	// caller can report bytes_compressed accurately.
	wire, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, &TransientError{Message: "read body", Cause: err}
	}

	headers := resp.Header.Clone()
	// Stash the wire byte count for callers that want it.
	headers.Set("X-Wire-Bytes", strconv.Itoa(len(wire)))

	out := &RawResponse{
		Status:  resp.StatusCode,
		Headers: headers,
		Body:    wire,
	}

	// 304 responses have no body to inspect or decompress.
	if resp.StatusCode == http.StatusNotModified {
		return out, nil
	}

	if err := mapStatus(resp.StatusCode, wire); err != nil {
		return nil, err
	}

	if strings.EqualFold(resp.Header.Get("Content-Encoding"), "br") {
		decoded, err := io.ReadAll(brotli.NewReader(bytes.NewReader(wire)))
		if err != nil {
			return nil, &TransientError{Message: "brotli decode", Cause: err}
		}
		out.Body = decoded
	}
	return out, nil
}

func (c *Client) newRequest(ctx context.Context, path string, q url.Values, ifNoneMatch string, brotliAccepted bool) (*http.Request, error) {
	u := c.baseURL + path
	if len(q) > 0 {
		u += "?" + q.Encode()
	}
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, u, nil)
	if err != nil {
		return nil, &ClientLogicError{Message: fmt.Sprintf("build request: %v", err)}
	}
	req.Header.Set("X-Api-Key", c.apiKey)
	req.Header.Set("Accept", "application/json")
	if brotliAccepted {
		req.Header.Set("Accept-Encoding", "br")
	} else {
		// Keep Go's transport from negotiating gzip transparently when we
		// want raw bytes for example 10. For other calls explicit identity
		// keeps the snapshot decoder simple.
		req.Header.Set("Accept-Encoding", "identity")
	}
	if ifNoneMatch != "" {
		req.Header.Set("If-None-Match", ifNoneMatch)
	}
	return req, nil
}

func readBody(resp *http.Response) ([]byte, error) {
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, &TransientError{Message: "read body", Cause: err}
	}
	if strings.EqualFold(resp.Header.Get("Content-Encoding"), "br") {
		decoded, err := io.ReadAll(brotli.NewReader(bytes.NewReader(body)))
		if err != nil {
			return nil, &TransientError{Message: "brotli decode", Cause: err}
		}
		return decoded, nil
	}
	return body, nil
}

func mapStatus(status int, body []byte) error {
	switch {
	case status >= 200 && status < 300:
		return nil
	case status == http.StatusUnauthorized:
		return &AuthError{Message: extractMessage(body)}
	case status == http.StatusTooManyRequests:
		return &RateLimitError{Message: extractMessage(body)}
	case status >= 500:
		return &TransientError{Message: fmt.Sprintf("server returned %d: %s", status, extractMessage(body))}
	default:
		return &ClientLogicError{Message: fmt.Sprintf("unexpected status %d: %s", status, extractMessage(body))}
	}
}

func extractMessage(body []byte) string {
	if len(body) == 0 {
		return ""
	}
	var er struct {
		Error   string `json:"error"`
		Message string `json:"message"`
	}
	if err := json.Unmarshal(body, &er); err == nil && (er.Error != "" || er.Message != "") {
		if er.Message != "" {
			return er.Message
		}
		return er.Error
	}
	// Fall back to the raw text, trimmed.
	return strings.TrimSpace(string(body))
}

func decodeFundingRows(data json.RawMessage) ([]FundingEntry, error) {
	if len(data) == 0 || bytes.Equal(bytes.TrimSpace(data), []byte("null")) {
		return nil, nil
	}
	var rows []json.RawMessage
	if err := json.Unmarshal(data, &rows); err != nil {
		return nil, &ClientLogicError{Message: fmt.Sprintf("decode rows: %v", err)}
	}
	out := make([]FundingEntry, 0, len(rows))
	for i, raw := range rows {
		trimmed := bytes.TrimSpace(raw)
		if len(trimmed) == 0 {
			continue
		}
		switch trimmed[0] {
		case '[':
			var tuple []any
			if err := json.Unmarshal(raw, &tuple); err != nil {
				return nil, &ClientLogicError{Message: fmt.Sprintf("decode row %d: %v", i, err)}
			}
			entry, err := tupleToEntry(tuple)
			if err != nil {
				return nil, fmt.Errorf("row %d: %w", i, err)
			}
			out = append(out, entry)
		case '{':
			var obj FundingEntry
			if err := json.Unmarshal(raw, &obj); err != nil {
				return nil, &ClientLogicError{Message: fmt.Sprintf("decode row %d: %v", i, err)}
			}
			out = append(out, obj)
		default:
			return nil, &ClientLogicError{Message: fmt.Sprintf("row %d: unsupported shape", i)}
		}
	}
	return out, nil
}

// ParseCompactRows converts a slice of decoded positional tuples to typed
// FundingEntry values. Exposed for callers that want to drive the
// transformation directly from already-decoded JSON.
func ParseCompactRows(rows [][]any) ([]FundingEntry, error) {
	out := make([]FundingEntry, 0, len(rows))
	for i, row := range rows {
		entry, err := tupleToEntry(row)
		if err != nil {
			return nil, fmt.Errorf("row %d: %w", i, err)
		}
		out = append(out, entry)
	}
	return out, nil
}

// tupleToEntry validates and converts one positional row.
func tupleToEntry(tuple []any) (FundingEntry, error) {
	if len(tuple) < 6 {
		return FundingEntry{}, &ClientLogicError{Message: fmt.Sprintf("row has %d fields, expected 6", len(tuple))}
	}
	exchange, ok := tuple[0].(string)
	if !ok {
		return FundingEntry{}, &ClientLogicError{Message: "field 0 (exchange) is not a string"}
	}
	base, ok := tuple[1].(string)
	if !ok {
		return FundingEntry{}, &ClientLogicError{Message: "field 1 (base) is not a string"}
	}
	rate, err := toFloat64(tuple[2])
	if err != nil {
		return FundingEntry{}, &ClientLogicError{Message: fmt.Sprintf("field 2 (funding_rate): %v", err)}
	}
	next, err := toInt64(tuple[3])
	if err != nil {
		return FundingEntry{}, &ClientLogicError{Message: fmt.Sprintf("field 3 (next_funding_time_ms): %v", err)}
	}
	interval, err := toInt64(tuple[4])
	if err != nil {
		return FundingEntry{}, &ClientLogicError{Message: fmt.Sprintf("field 4 (funding_interval_hours): %v", err)}
	}
	event, err := toInt64(tuple[5])
	if err != nil {
		return FundingEntry{}, &ClientLogicError{Message: fmt.Sprintf("field 5 (event_time_ms): %v", err)}
	}
	return FundingEntry{
		Exchange:             exchange,
		Base:                 base,
		FundingRate:          rate,
		NextFundingTimeMS:    next,
		FundingIntervalHours: interval,
		EventTimeMS:          event,
	}, nil
}

func toFloat64(v any) (float64, error) {
	switch n := v.(type) {
	case float64:
		return n, nil
	case json.Number:
		return n.Float64()
	default:
		return 0, errors.New("not a number")
	}
}

func toInt64(v any) (int64, error) {
	switch n := v.(type) {
	case float64:
		if math.IsNaN(n) || math.IsInf(n, 0) {
			return 0, errors.New("not a finite number")
		}
		if n < math.MinInt64 || n > math.MaxInt64 {
			return 0, errors.New("out of int64 range")
		}
		if n != math.Trunc(n) {
			return 0, errors.New("not an integer")
		}
		return int64(n), nil
	case json.Number:
		return n.Int64()
	default:
		return 0, errors.New("not a number")
	}
}
