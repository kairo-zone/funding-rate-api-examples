# Postman collection — kairo.zone Funding API

This directory contains a ready-to-import Postman v2.1 collection and a
matching environment template for the kairo.zone Funding API.

- `collection.json` — request library, grouped into `Funding` and `Symbols`
  folders. Authentication is configured at the collection level: every
  request inherits an `X-Api-Key` header sourced from the `{{api_key}}`
  environment variable.
- `environment.json` — `base_url` and `api_key` placeholders.

## How to import

Inside the Postman app:

1. File → Import.
2. Drop `collection.json` and `environment.json` onto the dialog (or use
   the file picker).
3. Confirm both items appear in your workspace and select the
   `kairo.zone Funding API (production)` environment from the top-right
   environment dropdown.

From the CLI (requires the `postman` CLI to be logged in):

```sh
postman collection import postman/collection.json
postman environment import postman/environment.json
```

## How to use

1. Open the imported environment, set `api_key` to your kairo.zone API
   key, and save. `base_url` defaults to `https://api.kairo.zone` — point
   it at a staging host if you need to.
2. Open the collection and click "Run" (Postman Runner) to execute every
   request, or fire requests individually.
3. Run `Funding / GET /v1/funding - full snapshot (compact)` first. Its
   test script writes the response's `version` field into the
   `{{version}}` collection variable, which is then consumed by:
   - `GET /v1/funding - delta since cursor` (as the `since` query
     parameter), and
   - `GET /v1/funding - conditional GET` (as the `If-None-Match`
     request header).

If you fire a delta or conditional request without first populating
`{{version}}`, a pre-request console warning will tell you to run the
snapshot first.

## See also

- Repository root: [`../README.md`](../README.md).
- OpenAPI specification: [`../openapi.yaml`](../openapi.yaml).
- Shared examples contract: [`../EXAMPLES.md`](../EXAMPLES.md).
