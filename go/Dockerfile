# syntax=docker/dockerfile:1.6
FROM golang:1.23-alpine AS build
WORKDIR /src

# Cache modules separately for faster rebuilds.
COPY go/go.mod go/go.sum ./
RUN go mod download

COPY go/ ./

# Build every cmd/<NN>_<stem>/main.go into a standalone static binary.
RUN set -eux; \
    mkdir -p /out; \
    for dir in cmd/*/; do \
        name="$(basename "$dir")"; \
        CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o "/out/$name" "./$dir"; \
    done

FROM alpine:3.20
RUN apk add --no-cache ca-certificates
COPY --from=build /out/ /usr/local/bin/
COPY go/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# Default invocation runs the quickstart example. Override with the first
# positional argument or the EXAMPLE env var, e.g.:
#   docker run kairo-funding-go 04_spread_scanner
#   docker run -e EXAMPLE=04_spread_scanner kairo-funding-go
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
CMD ["01_quickstart"]
