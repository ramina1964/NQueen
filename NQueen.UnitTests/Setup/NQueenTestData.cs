namespace NQueen.UnitTests.Setup;

public static class NQueenTestData
{
    public static IEnumerable<object[]> SolverShouldNotGenerateAnySolutionData =>
        [
            [2, SolutionMode.Single],
            [3, SolutionMode.Single ],
            [2, SolutionMode.Unique ],
            [3, SolutionMode.Unique ],
            [2, SolutionMode.All ],
            [3, SolutionMode.All ]
        ];

    public static IEnumerable<object[]> SolverShouldGenerateOneSingleSolutionData =>
        [
            [1, SolutionMode.Single],
            [4, SolutionMode.Single],
            [5, SolutionMode.Single],
            [4, SolutionMode.Single],
            [5, SolutionMode.Single],
            [6, SolutionMode.Single],
            [7, SolutionMode.Single],
            [8, SolutionMode.Single],
            [9, SolutionMode.Single],
            [10, SolutionMode.Single],
            [11, SolutionMode.Single],
            [12, SolutionMode.Single],
            [13, SolutionMode.Single],
            [18, SolutionMode.Single],
            [19, SolutionMode.Single],
            [20, SolutionMode.Single],
            [21, SolutionMode.Single],
            [22, SolutionMode.Single],
            [23, SolutionMode.Single],
            [24, SolutionMode.Single],
            [25, SolutionMode.Single],
            [26, SolutionMode.Single],
            [27, SolutionMode.Single],
            [28, SolutionMode.Single],
            [1, SolutionMode.Unique],
            [1, SolutionMode.All],
        ];

    public static IEnumerable<object[]> SolverShouldGenerateCorrectListOfUniqueSolutions =>
        [
            [4, SolutionMode.Unique],
            [5, SolutionMode.Unique],
            [6, SolutionMode.Unique],
            [7, SolutionMode.Unique],
            [8, SolutionMode.Unique],
        ];

    public static IEnumerable<object[]> SolverShouldGenerateCorrectListOfAllSolutionsData =>
    [
        [4, SolutionMode.All],
        [5, SolutionMode.All],
        [6, SolutionMode.All],
        [7, SolutionMode.All],
        [8, SolutionMode.All],
    ];
}

