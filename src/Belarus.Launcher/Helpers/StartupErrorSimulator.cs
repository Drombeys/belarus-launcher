namespace Belarus.Launcher.Helpers;

/// <summary>
/// Debug-only helper for simulating startup errors.
/// Set the BELARUS_LAUNCHER_SIMULATE_STARTUP_ERROR environment variable to one of the
/// <see cref="BeforeWindowStage"/> or <see cref="DuringInitializationStage"/> values
/// to reproduce the startup error handling without breaking the real environment.
/// </summary>
internal static class StartupErrorSimulator
{
    public const string BeforeWindowStage = "before-window";
    public const string DuringInitializationStage = "during-initialization";

    private const string VariableName = "BELARUS_LAUNCHER_SIMULATE_STARTUP_ERROR";

    public static void ThrowIfRequested(string stage)
    {
#if DEBUG
        var requestedStage = Environment.GetEnvironmentVariable(VariableName);

        if (string.Equals(requestedStage, stage, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Simulated startup error at stage '{stage}'. Unset {VariableName} to start normally.");
        }
#endif
    }
}
