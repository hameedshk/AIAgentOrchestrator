namespace AIOrchestrator.Domain.Enums;

/// <summary>
/// Typed classification of task/step failures. Maps to Section 8.1 of the spec.
/// </summary>
public enum FailureType
{
    /// <summary>Build system reports syntax or type errors</summary>
    CompileError,

    /// <summary>Test runner reports failing assertions</summary>
    TestFailure,

    /// <summary>Missing package, import, or tool not found</summary>
    DependencyMissing,

    /// <summary>Unhandled exception during execution</summary>
    RuntimeException,

    /// <summary>Merge or rebase conflict detected</summary>
    GitConflict,

    /// <summary>File system access denied</summary>
    PermissionError,

    /// <summary>Step exceeded stepTimeout or output silence threshold</summary>
    Timeout,

    /// <summary>Agent output fails schema or structural validation</summary>
    AgentInvalidOutput,

    /// <summary>Dangerous command blocklist enforcement triggered</summary>
    DangerousCommandBlocked,

    /// <summary>CLI process exited unexpectedly</summary>
    CliCrash,

    /// <summary>Cannot classify - treated as non-retryable by default</summary>
    Unknown
}
