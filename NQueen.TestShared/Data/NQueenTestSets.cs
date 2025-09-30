namespace NQueen.TestShared.Data;

public static class NQueenTestSets
{
    public static TheoryData<string, SolutionMode, bool, string> LargeValueCases =>
        new()
        {
            { "1000", SolutionMode.Single, false, nameof(ErrorMessages.SizeTooLargeForSingle) },
            { "1000", SolutionMode.Unique, false, nameof(ErrorMessages.SizeTooLargeForUnique) },
            { "1000", SolutionMode.All, false, nameof(ErrorMessages.SizeTooLargeForAll) },

            // 20 is VALID for Unique & All (max). Keep a positive case explicitly in other tests.
            { "21", SolutionMode.Unique, false, nameof(ErrorMessages.SizeTooLargeForUnique) },
            { "21", SolutionMode.All, false, nameof(ErrorMessages.SizeTooLargeForAll) }
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
            { "0", false, nameof(ErrorMessages.SizeTooSmallMsg) },
            { "-1", false, nameof(ErrorMessages.SizeTooSmallMsg) },
            { "8.0", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "4,5", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "abc", false, nameof(ErrorMessages.InvalidIntegerError) },
            { "1", true, null! },
            { "8", true, null! },
            { "17", true, null! },
            { "20", true, null! }, // still valid for Single (limit is 37)
            { "37", true, null! },
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
            { 20, SolutionMode.Single },
            { 21, SolutionMode.Single },
            { 22, SolutionMode.Single },
            { 23, SolutionMode.Single },
            { 24, SolutionMode.Single },
            { 25, SolutionMode.Single },
            { 26, SolutionMode.Single },
            { 27, SolutionMode.Single },
            { 28, SolutionMode.Single }
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
            {38, SolutionMode.Single },
            {-1, SolutionMode.Unique },
            {0, SolutionMode.Unique},
            {21, SolutionMode.Unique },   // updated invalid (>=21)
            {-1, SolutionMode.All },
            {0, SolutionMode.All},
            {21, SolutionMode.All},       // updated invalid (>=21)
            {8, (SolutionMode)999 },
        };
}
