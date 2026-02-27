using System.Diagnostics;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.Sentinel;

namespace AIOrchestrator.CliRunner.Sessions;

public sealed class CliSession : ICliSession
{
    private readonly Process _process;
    private readonly ModelBinaryConfig _config;
    private bool _disposed;

    public CliSession(ModelBinaryConfig config)
    {
        _config = config;
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = config.BinaryPath,
                Arguments = string.Join(" ", config.DefaultArgs),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
    }

    public async Task<SessionResult> ExecuteAsync(string prompt, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var (injectedPrompt, marker) = SentinelMarkerInjector.Inject(prompt);
        var detector = new SentinelDetector(marker);
        var silenceTimeout = TimeSpan.FromSeconds(_config.SilenceTimeoutSeconds);
        var timedOut = false;

        try
        {
            // Start the process
            _process.Start();

            // Write the prompt to stdin and close it
            await _process.StandardInput.WriteLineAsync(injectedPrompt);
            _process.StandardInput.Close();

            // Read all output with a timeout
            var readTask = _process.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_config.SilenceTimeoutSeconds + 5), ct);

            var completedTask = await Task.WhenAny(readTask, timeoutTask);

            string output;
            if (completedTask == timeoutTask)
            {
                // Timeout - kill the process and get whatever output we have
                timedOut = true;
                if (!_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit();
                }
                output = await readTask;
            }
            else
            {
                output = await readTask;

                // Wait for process to exit
                using var exitTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                exitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    await _process.WaitForExitAsync(exitTimeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Process exit timeout
                    if (!_process.HasExited)
                        _process.Kill();
                }
            }

            // Check for sentinel in the output
            foreach (var line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (detector.CheckLine(line))
                    break;
            }

            var exitCode = _process.HasExited ? _process.ExitCode : -1;

            return new SessionResult(
                Output: output,
                ExitCode: exitCode,
                TimedOut: timedOut,
                SentinelDetected: detector.Detected
            );
        }
        catch
        {
            if (!_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_process != null)
        {
            try
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit();
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process was never started or already disposed
                }
            }
            finally
            {
                _process.Dispose();
            }
        }

        await Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliSession));
    }
}
