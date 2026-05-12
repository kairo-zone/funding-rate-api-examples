# Convenience wrappers around `docker compose`. Targets are language-keyed.
.PHONY: help build-all run-py run-ts run-go run-cs clean

help: ## Show this help.
	@awk 'BEGIN {FS=":.*##"} /^[a-zA-Z_-]+:.*##/ {printf "  %-12s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

build-all: ## Build every language image.
	docker compose --profile smoke build

run-py: ## Run example 01 in Python.
	docker compose run --rm python

run-ts: ## Run example 01 in TypeScript.
	docker compose run --rm typescript

run-go: ## Run example 01 in Go.
	docker compose run --rm go

run-cs: ## Run example 01 in C#.
	docker compose run --rm csharp

clean: ## Remove local images built by compose.
	docker compose down --rmi local --remove-orphans
