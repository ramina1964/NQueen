namespace NQueen.TestShared;

public static class TestSettings
{
    // Environment variable names used to gate slow or optional test suites
    public const string EnvFullHighboardCoverage = "FULL_HIGHBOARD_COVERAGE"; // "true" to enable full slow set
    public const string EnvEnableFullAllEnum = "ENABLE_FULL_ALL_ENUM"; // "1" to run all-mode slow enumerations
    public const string EnvPerfN19 = "PERF_N19"; // "1" to run N=19 perf enumeration
    public const string EnvRunUnique19Enum = "RUN_UNIQUE19_ENUM"; // "1" to run heavy unique N=19 enumeration

    // Common toggles for CI skipping traits
    public const string TraitSkipInCI = "SkipInCI";
    public const string TraitHighBoard = "HighBoard";
    public const string TraitLargeBoardAllCounts = "LargeBoardAllCounts";
    public const string TraitPerf = "Perf";
    public const string TraitHeavy = "Heavy";
}
