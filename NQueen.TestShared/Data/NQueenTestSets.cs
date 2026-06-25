namespace NQueen.TestShared.Data;

public static class NQueenTestSets
{
    public static TheoryData<string, SolutionMode, bool, string> LargeValueCases =>
        new()
        {
            {
                "1000", SolutionMode.Single, false, nameof(ErrorMessages.OutOfRangeSingle)
            },
            {
                "1000", SolutionMode.Unique, false, nameof(ErrorMessages.OutOfRangeUnique)
            },
            {
                "1000", SolutionMode.All, false, nameof(ErrorMessages.OutOfRangeAll)
            },
            {
                (BoardSettings.MaxSizeForUnique + 1).ToString(), SolutionMode.Unique, false,
                nameof(ErrorMessages.OutOfRangeUnique)
            },
            {
                (BoardSettings.MaxSizeForAll + 1).ToString(), SolutionMode.All, false,
                nameof(ErrorMessages.OutOfRangeAll) }
        };

    public static TheoryData<int, SolutionMode> SmallValueCases =>
        new()
        {
            { 4, SolutionMode.Single},
            { 4, SolutionMode.Unique},
            { 4, SolutionMode.All},
            { 8, SolutionMode.Single},
            { 8, SolutionMode.Unique},
            { 8, SolutionMode.All},
            { 12, SolutionMode.Single},
            { 12, SolutionMode.Unique},
            { 12, SolutionMode.All},
        };

    public static TheoryData<string, bool, string> SingleDataModeHandling =>
        new()
        {
            { null!, false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) },
            { "   ", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) },
            { "       ", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) },
            { "", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) },
            { "0", false, nameof(ErrorMessages.OutOfRangeMsg) },
            { "-1", false, nameof(ErrorMessages.OutOfRangeMsg) },
            { "8.0", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "4,5", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "abc", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "1", true, null! },
            { "8", true, null! },
            { "17", true, null! },
            { BoardSettings.MaxSizeForUnique.ToString(), true, null! },
            { BoardSettings.MaxSizeForSingle.ToString(), true, null! },
        };

    public static TheoryData<int, SolutionMode> SolverShouldNotGenerateAnySolutionData =>
        new()
        {
            { 2, SolutionMode.Single },
            { 3, SolutionMode.Single },
            { 2, SolutionMode.Unique },
            { 3, SolutionMode.Unique },
            { 2, SolutionMode.All },
            { 3, SolutionMode.All }
        };

    public static TheoryData<int, SolutionMode> SolverShouldGenerateOneSingleSolutionData =>
        new()
        {
            { 1, SolutionMode.Single },
            { 4, SolutionMode.Single },
            { 5, SolutionMode.Single },
            { 6, SolutionMode.Single },
            { 7, SolutionMode.Single },
            { 8, SolutionMode.Single },
            { 9, SolutionMode.Single },
            { 10, SolutionMode.Single },
            { 11, SolutionMode.Single },
            { 12, SolutionMode.Single },
            { 13, SolutionMode.Single },
            { BoardSettings.MaxSizeForUnique, SolutionMode.Single },
            { BoardSettings.MaxSizeForSingle, SolutionMode.Single },
        };

    public static TheoryData<int, SolutionMode> SolverShouldGenerateCorrectListOfUniqueSolutions =>
        new()
        {
            { 4, SolutionMode.Unique },
            { 5, SolutionMode.Unique },
            { 6, SolutionMode.Unique },
            { 7, SolutionMode.Unique },
            { 8, SolutionMode.Unique },
        };

    public static TheoryData<int, SolutionMode> SolverShouldGenerateCorrectListOfAllSolutionsData =>
        new()
        {
            {4, SolutionMode.All},
            {5, SolutionMode.All},
            {6, SolutionMode.All},
            {7, SolutionMode.All},
            {8, SolutionMode.All}
        };

    public static TheoryData<int, SolutionMode> InvalidInputs =>
        new()
        {
            {-1, SolutionMode.Single },
            {0, SolutionMode.Single },
            {BoardSettings.MaxSizeForSingle + 1, SolutionMode.Single },
            {-1, SolutionMode.Unique },
            {0, SolutionMode.Unique},
            {BoardSettings.MaxSizeForUnique + 1, SolutionMode.Unique },
            {-1, SolutionMode.All },
            {0, SolutionMode.All},
            {BoardSettings.MaxSizeForAll + 1, SolutionMode.All},
            {8, (SolutionMode)999 },
        };

    public static TheoryData<int, SolutionMode> ValidBoardSizes =>
        new()
        {
            { BoardSettings.MaxSizeForSingle, SolutionMode.Single },
            { BoardSettings.MaxSizeForUnique, SolutionMode.Unique },
            { BoardSettings.MaxSizeForAll, SolutionMode.All },
        };
}
