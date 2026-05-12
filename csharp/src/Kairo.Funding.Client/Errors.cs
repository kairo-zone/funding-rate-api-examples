using System;

namespace Kairo.Funding.Client;

/// <summary>
/// Base class for typed errors raised by <see cref="FundingClient"/>.
/// Exit-code mapping (see EXAMPLES.md "Conventions"):
///   <see cref="AuthException"/>        -> 2  (HTTP 401)
///   <see cref="RateLimitException"/>   -> 3  (HTTP 429)
///   <see cref="TransientException"/>   -> 1  (5xx, DNS, connection reset, timeout)
///   <see cref="ClientLogicException"/> -> 4  (missing env var, bad CLI input, malformed response)
/// </summary>
public abstract class FundingApiException : Exception
{
    /// <summary>Process exit code that should be used when this error propagates to the CLI.</summary>
    public abstract int ExitCode { get; }

    /// <summary>Create a new typed Funding API exception with the supplied human-readable message.</summary>
    protected FundingApiException(string message) : base(message) { }

    /// <summary>Create a new typed Funding API exception wrapping an underlying cause.</summary>
    protected FundingApiException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Authentication error. Raised when the server returns HTTP 401.</summary>
public sealed class AuthException : FundingApiException
{
    /// <inheritdoc />
    public override int ExitCode => 2;
    /// <summary>Create a new <see cref="AuthException"/> with the supplied message.</summary>
    public AuthException(string message) : base(message) { }
}

/// <summary>Rate-limit error. Raised when the server returns HTTP 429.</summary>
public sealed class RateLimitException : FundingApiException
{
    /// <inheritdoc />
    public override int ExitCode => 3;
    /// <summary>Create a new <see cref="RateLimitException"/> with the supplied message.</summary>
    public RateLimitException(string message) : base(message) { }
}

/// <summary>Transient error. Network failure, timeout, or HTTP 5xx.</summary>
public sealed class TransientException : FundingApiException
{
    /// <inheritdoc />
    public override int ExitCode => 1;
    /// <summary>Create a new <see cref="TransientException"/> with the supplied message.</summary>
    public TransientException(string message) : base(message) { }
    /// <summary>Create a new <see cref="TransientException"/> wrapping the underlying cause.</summary>
    public TransientException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Client-side logic error. Missing env var, bad CLI input, malformed response.</summary>
public sealed class ClientLogicException : FundingApiException
{
    /// <inheritdoc />
    public override int ExitCode => 4;
    /// <summary>Create a new <see cref="ClientLogicException"/> with the supplied message.</summary>
    public ClientLogicException(string message) : base(message) { }
}
