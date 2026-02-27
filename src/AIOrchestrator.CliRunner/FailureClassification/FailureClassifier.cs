using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.FailureClassification;

/// <summary>
/// Classifies step failures using a 4-step pipeline:
/// 1. Exit code mapping
/// 2. Regex pattern matching
/// 3. Agent output validation (placeholder)
/// 4. Error fingerprint hashing
///
/// Implements IFailureClassifier from spec Section 8.2.
/// </summary>
public class FailureClassifier : IFailureClassifier
{
    /// <summary>
    /// Regex patterns for each failure type, checked in priority order.
    /// Patterns use case-insensitive matching.
    /// </summary>
    private static readonly Dictionary<FailureType, Regex[]> FailurePatterns = new()
    {
        {
            FailureType.CompileError,
            new[]
            {
                new Regex(@"error CS\d+", RegexOptions.IgnoreCase),
                new Regex(@"Parse error", RegexOptions.IgnoreCase),
                new Regex(@"syntax error", RegexOptions.IgnoreCase),
                new Regex(@"type error", RegexOptions.IgnoreCase),
                new Regex(@"compilation failed", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.TestFailure,
            new[]
            {
                new Regex(@"FAILED", RegexOptions.IgnoreCase),
                new Regex(@"AssertionError", RegexOptions.IgnoreCase),
                new Regex(@"tests failed", RegexOptions.IgnoreCase),
                new Regex(@"failures", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.DependencyMissing,
            new[]
            {
                new Regex(@"ERESOLVE", RegexOptions.IgnoreCase),
                new Regex(@"cannot find", RegexOptions.IgnoreCase),
                new Regex(@"not found", RegexOptions.IgnoreCase),
                new Regex(@"ModuleNotFoundError", RegexOptions.IgnoreCase),
                new Regex(@"command not found", RegexOptions.IgnoreCase),
                new Regex(@"No package", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.RuntimeException,
            new[]
            {
                new Regex(@"Exception:", RegexOptions.IgnoreCase),
                new Regex(@"Unhandled exception", RegexOptions.IgnoreCase),
                new Regex(@"fatal error", RegexOptions.IgnoreCase),
                new Regex(@"segmentation fault", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.GitConflict,
            new[]
            {
                new Regex(@"CONFLICT", RegexOptions.IgnoreCase),
                new Regex(@"merge conflict", RegexOptions.IgnoreCase),
                new Regex(@"<<<<<<<", RegexOptions.IgnoreCase),
                new Regex(@"Your local changes", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.PermissionError,
            new[]
            {
                new Regex(@"Permission denied", RegexOptions.IgnoreCase),
                new Regex(@"Access is denied", RegexOptions.IgnoreCase),
                new Regex(@"EACCES", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.Timeout,
            new[]
            {
                new Regex(@"timeout", RegexOptions.IgnoreCase),
                new Regex(@"No output", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.AgentInvalidOutput,
            new[]
            {
                new Regex(@"Expected JSON", RegexOptions.IgnoreCase),
                new Regex(@"missing required field", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.DangerousCommandBlocked,
            new[]
            {
                new Regex(@"DangerousCommandBlocked", RegexOptions.IgnoreCase),
                new Regex(@"Blocklist violation", RegexOptions.IgnoreCase),
            }
        },
        {
            FailureType.CliCrash,
            new[]
            {
                new Regex(@"CLI process crashed", RegexOptions.IgnoreCase),
                new Regex(@"Unexpected EOF", RegexOptions.IgnoreCase),
            }
        },
    };

    /// <summary>
    /// Classify a step failure using the 4-step pipeline.
    /// </summary>
    public FailureContext Classify(
        int? exitCode,
        string output,
        ModelType? plannerModel,
        ModelType? executorModel)
    {
        var failureType = ClassifyType(exitCode, output);
        var hash = ComputeErrorHash(output);
        var retryable = IsRetryable(failureType);

        return new FailureContext(
            Type: failureType,
            RawOutput: output,
            ExitCode: exitCode,
            ErrorHash: hash,
            Retryable: retryable,
            PlannerModel: plannerModel,
            ExecutorModel: executorModel,
            OccurredAt: DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Compute SHA256 fingerprint of normalized error output.
    /// Returns a 64-character lowercase hex string.
    /// </summary>
    public string ComputeErrorHash(string output)
    {
        string normalized = NormalizeErrorOutput(output);
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// Step 1-4 of the classification pipeline.
    /// Returns the most specific failure type detected.
    /// </summary>
    private FailureType ClassifyType(int? exitCode, string output)
    {
        // Step 1: Exit code mapping (only for specific exit codes that always indicate a type)
        if (exitCode == 127)
            return FailureType.DependencyMissing;
        if (exitCode == 124)
            return FailureType.Timeout;
        if (exitCode == -1)
            return FailureType.CliCrash;

        // Step 2: Regex pattern matching (ordered by type priority)
        // Check patterns in a defined priority order to ensure specific types
        // are detected before generic ones
        foreach (var (type, patterns) in FailurePatterns)
        {
            if (patterns.Any(p => p.IsMatch(output)))
                return type;
        }

        // Step 3: Agent output validation (placeholder - no specific validation implemented yet)
        // In the future, this would validate JSON structure for agent steps

        // Step 4: Default to Unknown
        return FailureType.Unknown;
    }

    /// <summary>
    /// Determine if a failure type is retryable.
    /// Only DependencyMissing, Timeout, and CliCrash are retryable.
    /// </summary>
    private bool IsRetryable(FailureType failureType) =>
        failureType switch
        {
            FailureType.DependencyMissing => true,
            FailureType.Timeout => true,
            FailureType.CliCrash => true,
            _ => false,
        };

    /// <summary>
    /// Normalize error output for hashing:
    /// - Collapse whitespace (multiple spaces to single space)
    /// - Remove ISO 8601 timestamps
    /// - Remove file paths
    /// - Trim result
    /// </summary>
    private string NormalizeErrorOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return string.Empty;

        var normalized = output;

        // Collapse multiple whitespace characters to single space
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Remove ISO 8601 timestamps (e.g., 2024-02-27T10:30:45.123Z)
        normalized = Regex.Replace(normalized, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?", "");

        // Remove file paths (both Windows and Unix style)
        // Windows: C:\Users\..., D:\path\...
        // Unix: /home/user/..., /var/...
        normalized = Regex.Replace(normalized, @"[A-Za-z]:\\[^\s]+", "");
        normalized = Regex.Replace(normalized, @"/[^\s]+", "");

        // Collapse whitespace again after removals
        normalized = Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }
}
