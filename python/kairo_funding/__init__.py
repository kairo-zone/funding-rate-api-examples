"""kairo.zone Funding API thin client used by the example programs."""

from .client import (
    AuthError,
    FundingClient,
    KairoError,
    RateLimitError,
    RawResponse,
    TransientError,
    cli_entry,
)

__all__ = [
    "AuthError",
    "FundingClient",
    "KairoError",
    "RateLimitError",
    "RawResponse",
    "TransientError",
    "cli_entry",
]
