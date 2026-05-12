using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kairo.Funding.Client;

/// <summary>
/// Helper that runs an example's <c>RunAsync</c> entry point and maps typed
/// <see cref="FundingApiException"/> instances onto the documented exit codes.
/// </summary>
public static class CliEntry
{
    /// <summary>
    /// Run an example body, returning the resulting exit code. Catches typed
    /// errors, prints a human-readable diagnostic to stderr, and translates
    /// SIGINT/Ctrl+C into a clean <c>0</c> exit.
    /// </summary>
    public static async Task<int> RunAsync(Func<CancellationToken, Task<int>> body)
    {
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) =>
        {
            e.Cancel = true;
            try { cts.Cancel(); } catch { /* ignore */ }
        };
        Console.CancelKeyPress += handler;
        try
        {
            return await body(cts.Token).ConfigureAwait(false);
        }
        catch (FundingApiException ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return ex.ExitCode;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}
