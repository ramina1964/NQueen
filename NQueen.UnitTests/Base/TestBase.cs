namespace NQueen.UnitTests.Base;

public class TestBase(ISolverBackEnd sut)
{
    public List<byte[]> ExpectedSolutions { get; set; } = [];

    public List<byte[]> ActualSolutions { get; set; } = [];

    public static List<byte[]> GetExpectedSolutions(byte boardSize, SolutionMode solutionMode)
    {
        return solutionMode == SolutionMode.Single
               ? new List<byte[]>(GetExpectedSingleSolution(boardSize))
               : solutionMode == SolutionMode.Unique
               ? new List<byte[]>(GetExpectedUniqueSolutions(boardSize))
               : new List<byte[]>(GetExpectedAllSolutions(boardSize));
    }

    public List<byte[]> GetActualSolutions(byte boardSize, SolutionMode solutionMode)
    {
        return Sut
               .GetResultsAsync(boardSize, solutionMode)
               .Result
               .Solutions
               .Select(sol => sol.QueenPositions)
               .ToList();
    }

    protected readonly ISolverBackEnd Sut = sut
        ?? throw new ArgumentNullException(nameof(sut));

    #region PrivateAttributes
    private static List<byte[]> GetExpectedSingleSolution(byte boardSize) => _expectedSingleSolutions[boardSize];

    private static List<byte[]> GetExpectedUniqueSolutions(byte boardSize) => _expectedUniqueSolutions[boardSize];

    private static List<byte[]> GetExpectedAllSolutions(byte boardSize) => _expectedAllSolutions[boardSize];

    private static readonly Dictionary<byte, List<byte[]>> _expectedSingleSolutions = new()
    {
        { 1, new List<byte[]> { new byte[] { 0 } } },
        { 2, new List<byte[]> { } },
        { 3, new List<byte[]> { } },
        { 4, new List<byte[]> { new byte[] { 1, 3, 0, 2 } } },
        { 5, new List<byte[]> { new byte[] { 0, 2, 4, 1, 3 } } },
        { 6, new List<byte[]> { new byte[] { 1, 3, 5, 0, 2, 4 } } },
        { 7, new List<byte[]> { new byte[] { 0, 2, 4, 6, 1, 3, 5 } } },
        { 8, new List<byte[]> { new byte[] { 0, 4, 7, 5, 2, 6, 1, 3 } } },
        { 9, new List<byte[]> { new byte[] { 0, 2, 5, 7, 1, 3, 8, 6, 4 } } },
        { 10, new List<byte[]> { new byte[] { 0, 2, 5, 7, 9, 4, 8, 1, 3, 6 } } },
        { 11, new List<byte[]> { new byte[] { 0, 2, 4, 6, 8, 10, 1, 3, 5, 7, 9 } } },
        { 12, new List<byte[]> { new byte[] { 0, 2, 4, 7, 9, 11, 5, 10, 1, 6, 8, 3 } } },
        { 13, new List<byte[]> { new byte[] { 0, 2, 4, 1, 8, 11, 9, 12, 3, 5, 7, 10, 6 } } },
        { 18, new List<byte[]> { new byte[] { 0, 2, 4, 1, 7, 14, 11, 15, 12, 16, 5, 17, 6, 3, 10, 8, 13, 9 } } },
        { 19, new List<byte[]> { new byte[] { 0, 2, 4, 1, 3, 8, 12, 14, 16, 18, 6, 15, 17, 10, 5, 7, 9, 11, 13 } } },
        { 20, new List<byte[]> { new byte[] { 0, 2, 4, 1, 3, 12, 14, 11, 17, 19, 16, 8, 15, 18, 7, 9, 6, 13, 5, 10 } } },
        { 21, new List<byte[]> { new byte[] { 0, 2, 4, 1, 3, 8, 10, 14, 20, 17, 19, 16, 18, 6, 11, 9, 7, 5, 13, 15, 12 } } },

        {
            22,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 9, 13, 16, 19, 12, 18, 21, 17, 7, 20, 11, 8, 5, 15, 6, 10, 14 } }
        },

        {
            23,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 8, 10, 12, 17, 19, 21, 18, 20, 9, 7, 5, 22, 6, 15, 11, 14, 16, 13 } }
        },

        {
            24,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 8, 10, 13, 17, 21, 18, 22, 19, 23, 9, 20, 5, 7, 11, 15, 12, 6, 16, 14 } }
        },

        {
            25,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 8, 10, 12, 14, 18, 20, 23, 19, 24, 22, 5, 7, 9, 6, 13, 15, 17, 11, 16, 21} }
        },

        {
            26,
            new List<byte[]> { new byte[] {0, 2, 4, 1, 3, 8, 10, 12, 14, 20, 22, 24, 19, 21, 23, 25, 9, 6, 15, 11, 7,
                5, 17, 13, 18, 16 } }
        },

        {
            27,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 8, 10, 12, 14, 16, 18, 22, 24, 26, 23, 25, 5, 9, 6, 15, 7, 11, 13, 20, 17, 19, 21 } }
        },

        {
            28,
            new List<byte[]>
                { new byte[] {0, 2, 4, 1, 3, 8, 10, 12, 14, 16, 22, 24, 21, 27, 25, 23, 26, 6, 11, 15, 17, 7, 9, 13, 19, 5, 20, 18 } }
        }
    };

    private static readonly Dictionary<byte, List<byte[]>> _expectedUniqueSolutions = new()
    {
        { 1, new List<byte[]> { new byte[] { 0 } } },
        { 2, new List<byte[]> { } },
        { 3, new List<byte[]> { } },

        { 4, new List<byte[]> { new byte[] { 1, 3, 0, 2 } } },
        {
            5,
            new List<byte[]>
                {
                    { new byte[] { 0, 2, 4, 1, 3 } },
                    { new byte[] { 1, 4, 2, 0, 3 } },
                }
        },

        { 6, new List<byte[]> { new byte[] { 1, 3, 5, 0, 2, 4 } } },
        {
            7,
            new List<byte[]>
                {
                    { new byte[] {0, 2, 4, 6, 1, 3, 5} },
                    { new byte[] {0, 3, 6, 2, 5, 1, 4} },
                    { new byte[] {1, 3, 0, 6, 4, 2, 5} },
                    { new byte[] {1, 4, 0, 3, 6, 2, 5} },
                    { new byte[] {1, 4, 6, 3, 0, 2, 5} },
                    { new byte[] {1, 5, 2, 6, 3, 0, 4} },
                }
        },

        {
            8,
            new List<byte[]>
                {
                    { new byte[]  {0, 4, 7, 5, 2, 6, 1, 3} },
                    { new byte[]  {0, 5, 7, 2, 6, 3, 1, 4} },
                    { new byte[]  {1, 3, 5, 7, 2, 0, 6, 4} },
                    { new byte[]  {1, 4, 6, 0, 2, 7, 5, 3} },
                    { new byte[]  {1, 4, 6, 3, 0, 7, 5, 2} },
                    { new byte[]  {1, 5, 0, 6, 3, 7, 2, 4} },
                    { new byte[]  {1, 5, 7, 2, 0, 3, 6, 4} },
                    { new byte[]  {1, 6, 2, 5, 7, 4, 0, 3} },
                    { new byte[]  {1, 6, 4, 7, 0, 3, 5, 2} },
                    { new byte[]  {2, 4, 1, 7, 0, 6, 3, 5} },
                    { new byte[]  {2, 4, 7, 3, 0, 6, 1, 5} },
                    { new byte[]  {2, 5, 1, 4, 7, 0, 6, 3} }
                }
        },

        {
            9,
            new List<byte[]>
                {
                    { new byte[]  {0, 2, 5, 7, 1, 3, 8, 6, 4} },
                    { new byte[]  {0, 2, 6, 1, 7, 4, 8, 3, 5} },
                    { new byte[]  {0, 2, 7, 5, 8, 1, 4, 6, 3} },
                    { new byte[]  {0, 3, 5, 2, 8, 1, 7, 4, 6} },
                    { new byte[]  {0, 3, 5, 7, 1, 4, 2, 8, 6} },
                    { new byte[]  {0, 3, 6, 2, 7, 1, 4, 8, 5} },
                    { new byte[]  {0, 3, 6, 8, 1, 4, 7, 5, 2} },
                    { new byte[]  {0, 3, 7, 2, 8, 6, 4, 1, 5} },
                    { new byte[]  {0, 4, 6, 8, 2, 7, 1, 3, 5} },
                    { new byte[]  {0, 4, 6, 8, 3, 1, 7, 5, 2} },
                    { new byte[]  {0, 4, 8, 5, 3, 1, 7, 2, 6} },
                    { new byte[]  {0, 5, 7, 2, 6, 3, 1, 8, 4} },
                    { new byte[]  {0, 6, 3, 7, 2, 4, 8, 1, 5} },
                    { new byte[]  {0, 6, 3, 7, 2, 8, 5, 1, 4} },
                    { new byte[]  {1, 3, 0, 6, 8, 5, 2, 4, 7} },
                    { new byte[]  {1, 3, 6, 0, 2, 8, 5, 7, 4} },
                    { new byte[]  {1, 4, 7, 0, 2, 5, 8, 6, 3} },
                    { new byte[]  {1, 4, 7, 0, 8, 5, 2, 6, 3} },
                    { new byte[]  {1, 4, 8, 3, 0, 7, 5, 2, 6} },
                    { new byte[]  {1, 5, 0, 2, 6, 8, 3, 7, 4} },
                    { new byte[]  {1, 5, 0, 6, 4, 2, 8, 3, 7} },
                    { new byte[]  {1, 5, 0, 8, 4, 7, 3, 6, 2} },
                    { new byte[]  {1, 5, 2, 0, 7, 3, 8, 6, 4} },
                    { new byte[]  {1, 6, 4, 0, 8, 3, 5, 7, 2} },
                    { new byte[]  {1, 7, 0, 3, 6, 8, 5, 2, 4} },
                    { new byte[]  {2, 4, 8, 3, 0, 6, 1, 5, 7} },
                    { new byte[]  {2, 5, 7, 0, 4, 8, 1, 3, 6} },
                    { new byte[]  {2, 5, 7, 4, 0, 8, 6, 1, 3} },
                    { new byte[]  {2, 5, 8, 0, 7, 3, 1, 6, 4} },
                    { new byte[]  {2, 5, 8, 6, 0, 3, 1, 4, 7} },
                    { new byte[]  {2, 6, 8, 0, 4, 1, 7, 5, 3} },
                    { new byte[]  {2, 7, 5, 0, 8, 1, 4, 6, 3} },
                    { new byte[]  {2, 8, 3, 0, 7, 5, 1, 6, 4} },
                    { new byte[]  {2, 8, 5, 3, 0, 6, 4, 1, 7} },
                    { new byte[]  {3, 1, 6, 8, 0, 7, 4, 2, 5} },
                    { new byte[]  {3, 1, 8, 4, 0, 7, 5, 2, 6} },
                    { new byte[]  {3, 5, 8, 2, 0, 7, 1, 4, 6} },
                    { new byte[]  {3, 8, 4, 2, 0, 5, 7, 1, 6} },
                    { new byte[]  {3, 8, 4, 2, 0, 6, 1, 7, 5} },
                    { new byte[]  {3, 8, 4, 7, 0, 2, 5, 1, 6} },
                    { new byte[]  {5, 0, 6, 3, 7, 2, 4, 8, 1} },
                    { new byte[]  {5, 2, 0, 7, 3, 8, 6, 4, 1} },
                    { new byte[]  {5, 2, 0, 7, 4, 1, 8, 6, 3} },
                    { new byte[]  {5, 7, 0, 6, 3, 1, 8, 4, 2} },
                    { new byte[]  {6, 0, 3, 7, 4, 2, 8, 5, 1} },
                    { new byte[]  {6, 0, 5, 1, 4, 7, 3, 8, 2} },
                }
        },
    };

    private static readonly Dictionary<byte, List<byte[]>> _expectedAllSolutions = new()
    {
        { 1, new List<byte[]> { new byte[] { 0 } } },
        { 2, new List<byte[]> { } },
        { 3, new List<byte[]> { } },
        {
            4,
            new List<byte[]>
                {
                   { new byte[] { 1, 3, 0, 2 } },
                   { new byte[] { 2, 0, 3, 1 } }
                }
        },

        {
            5,
            new List<byte[]>()
                {
                    { new byte[] {0, 2, 4, 1, 3} },
                    { new byte[] {4, 2, 0, 3, 1} },
                    { new byte[] {3, 1, 4, 2, 0} },
                    { new byte[] {0, 3, 1, 4, 2} },
                    { new byte[] {2, 0, 3, 1, 4} },
                    { new byte[] {2, 4, 1, 3, 0} },
                    { new byte[] {1, 3, 0, 2, 4} },
                    { new byte[] {4, 1, 3, 0, 2} },
                    { new byte[] {1, 4, 2, 0, 3} },
                    { new byte[] {3, 0, 2, 4, 1} },
                }
        },

        {
            6,
            new List<byte[]>
                {
                    { new byte[] {1, 3, 5, 0, 2, 4} },
                    { new byte[] {4, 2, 0, 5, 3, 1} },
                    { new byte[] {3, 0, 4, 1, 5, 2} },
                    { new byte[] {2, 5, 1, 4, 0, 3} }
                }
        },

        {
            7,
            new List<byte[]>
                {
                    {  new byte[] {0, 2, 4, 6, 1, 3, 5} },
                    {  new byte[] {6, 4, 2, 0, 5, 3, 1} },
                    {  new byte[] {5, 3, 1, 6, 4, 2, 0} },
                    {  new byte[] {0, 4, 1, 5, 2, 6, 3} },
                    {  new byte[] {3, 0, 4, 1, 5, 2, 6} },
                    {  new byte[] {3, 6, 2, 5, 1, 4, 0} },
                    {  new byte[] {1, 3, 5, 0, 2, 4, 6} },
                    {  new byte[] {6, 2, 5, 1, 4, 0, 3} },
                    {  new byte[] {0, 3, 6, 2, 5, 1, 4} },
                    {  new byte[] {6, 3, 0, 4, 1, 5, 2} },
                    {  new byte[] {4, 1, 5, 2, 6, 3, 0} },
                    {  new byte[] {0, 5, 3, 1, 6, 4, 2} },
                    {  new byte[] {4, 2, 0, 5, 3, 1, 6} },
                    {  new byte[] {2, 4, 6, 1, 3, 5, 0} },
                    {  new byte[] {2, 5, 1, 4, 0, 3, 6} },
                    {  new byte[] {6, 1, 3, 5, 0, 2, 4} },
                    {  new byte[] {1, 3, 0, 6, 4, 2, 5} },
                    {  new byte[] {5, 3, 6, 0, 2, 4, 1} },
                    {  new byte[] {5, 2, 4, 6, 0, 3, 1} },
                    {  new byte[] {2, 0, 5, 1, 4, 6, 3} },
                    {  new byte[] {3, 0, 2, 5, 1, 6, 4} },
                    {  new byte[] {3, 6, 4, 1, 5, 0, 2} },
                    {  new byte[] {1, 4, 2, 0, 6, 3, 5} },
                    {  new byte[] {4, 6, 1, 5, 2, 0, 3} },
                    {  new byte[] {1, 4, 0, 3, 6, 2, 5} },
                    {  new byte[] {5, 2, 6, 3, 0, 4, 1} },
                    {  new byte[] {2, 0, 5, 3, 1, 6, 4} },
                    {  new byte[] {4, 6, 1, 3, 5, 0, 2} },
                    {  new byte[] {1, 4, 6, 3, 0, 2, 5} },
                    {  new byte[] {5, 2, 0, 3, 6, 4, 1} },
                    {  new byte[] {4, 0, 5, 3, 1, 6, 2} },
                    {  new byte[] {2, 6, 1, 3, 5, 0, 4} },
                    {  new byte[] {1, 5, 2, 6, 3, 0, 4} },
                    {  new byte[] {5, 1, 4, 0, 3, 6, 2} },
                    {  new byte[] {4, 0, 3, 6, 2, 5, 1} },
                    {  new byte[] {5, 0, 2, 4, 6, 1, 3} },
                    {  new byte[] {3, 5, 0, 2, 4, 6, 1} },
                    {  new byte[] {3, 1, 6, 4, 2, 0, 5} },
                    {  new byte[] {2, 6, 3, 0, 4, 1, 5} },
                    {  new byte[] {1, 6, 4, 2, 0, 5, 3 } },
                }
        },

        {
            8,
            new List<byte[]>
                {
                    {  new byte[] {0, 4, 7, 5, 2, 6, 1, 3} },
                    {  new byte[] {7, 3, 0, 2, 5, 1, 6, 4} },
                    {  new byte[] {3, 1, 6, 2, 5, 7, 4, 0} },
                    {  new byte[] {0, 6, 4, 7, 1, 3, 5, 2} },
                    {  new byte[] {5, 2, 4, 6, 0, 3, 1, 7} },
                    {  new byte[] {2, 5, 3, 1, 7, 4, 6, 0} },
                    {  new byte[] {4, 6, 1, 5, 2, 0, 3, 7} },
                    {  new byte[] {7, 1, 3, 0, 6, 4, 2, 5} },
                    {  new byte[] {0, 5, 7, 2, 6, 3, 1, 4} },
                    {  new byte[] {7, 2, 0, 5, 1, 4, 6, 3} },
                    {  new byte[] {4, 1, 3, 6, 2, 7, 5, 0} },
                    {  new byte[] {0, 6, 3, 5, 7, 1, 4, 2} },
                    {  new byte[] {5, 3, 6, 0, 2, 4, 1, 7} },
                    {  new byte[] {2, 4, 1, 7, 5, 3, 6, 0} },
                    {  new byte[] {3, 6, 4, 1, 5, 0, 2, 7} },
                    {  new byte[] {7, 1, 4, 2, 0, 6, 3, 5} },
                    {  new byte[] {1, 3, 5, 7, 2, 0, 6, 4} },
                    {  new byte[] {6, 4, 2, 0, 5, 7, 1, 3} },
                    {  new byte[] {4, 6, 0, 2, 7, 5, 3, 1} },
                    {  new byte[] {5, 0, 4, 1, 7, 2, 6, 3} },
                    {  new byte[] {4, 1, 5, 0, 6, 3, 7, 2} },
                    {  new byte[] {3, 6, 2, 7, 1, 4, 0, 5} },
                    {  new byte[] {3, 1, 7, 5, 0, 2, 4, 6} },
                    {  new byte[] {2, 7, 3, 6, 0, 5, 1, 4} },
                    {  new byte[] {1, 4, 6, 0, 2, 7, 5, 3} },
                    {  new byte[] {6, 3, 1, 7, 5, 0, 2, 4} },
                    {  new byte[] {3, 5, 7, 2, 0, 6, 4, 1} },
                    {  new byte[] {3, 0, 4, 7, 1, 6, 2, 5} },
                    {  new byte[] {2, 5, 1, 6, 0, 3, 7, 4} },
                    {  new byte[] {5, 2, 6, 1, 7, 4, 0, 3} },
                    {  new byte[] {4, 2, 0, 5, 7, 1, 3, 6} },
                    {  new byte[] {4, 7, 3, 0, 6, 1, 5, 2} },
                    {  new byte[] {1, 4, 6, 3, 0, 7, 5, 2} },
                    {  new byte[] {6, 3, 1, 4, 7, 0, 2, 5} },
                    {  new byte[] {2, 5, 7, 0, 3, 6, 4, 1} },
                    {  new byte[] {4, 0, 7, 3, 1, 6, 2, 5} },
                    {  new byte[] {2, 5, 1, 6, 4, 0, 7, 3} },
                    {  new byte[] {5, 2, 6, 1, 3, 7, 0, 4} },
                    {  new byte[] {5, 2, 0, 7, 4, 1, 3, 6} },
                    {  new byte[] {3, 7, 0, 4, 6, 1, 5, 2} },
                    {  new byte[] {1, 5, 0, 6, 3, 7, 2, 4} },
                    {  new byte[] {6, 2, 7, 1, 4, 0, 5, 3} },
                    {  new byte[] {4, 2, 7, 3, 6, 0, 5, 1} },
                    {  new byte[] {2, 0, 6, 4, 7, 1, 3, 5} },
                    {  new byte[] {2, 4, 6, 0, 3, 1, 7, 5} },
                    {  new byte[] {5, 3, 1, 7, 4, 6, 0, 2} },
                    {  new byte[] {3, 5, 0, 4, 1, 7, 2, 6} },
                    {  new byte[] {5, 7, 1, 3, 0, 6, 4, 2} },
                    {  new byte[] {1, 5, 7, 2, 0, 3, 6, 4} },
                    {  new byte[] {6, 2, 0, 5, 7, 4, 1, 3} },
                    {  new byte[] {4, 6, 3, 0, 2, 7, 5, 1} },
                    {  new byte[] {4, 0, 3, 5, 7, 1, 6, 2} },
                    {  new byte[] {5, 1, 6, 0, 2, 4, 7, 3} },
                    {  new byte[] {2, 6, 1, 7, 5, 3, 0, 4} },
                    {  new byte[] {3, 1, 4, 7, 5, 0, 2, 6} },
                    {  new byte[] {3, 7, 4, 2, 0, 6, 1, 5} },
                    {  new byte[] {1, 6, 2, 5, 7, 4, 0, 3} },
                    {  new byte[] {6, 1, 5, 2, 0, 3, 7, 4} },
                    {  new byte[] {3, 0, 4, 7, 5, 2, 6, 1} },
                    {  new byte[] {6, 0, 2, 7, 5, 3, 1, 4} },
                    {  new byte[] {3, 6, 4, 2, 0, 5, 7, 1} },
                    {  new byte[] {4, 1, 3, 5, 7, 2, 0, 6} },
                    {  new byte[] {4, 7, 3, 0, 2, 5, 1, 6} },
                    {  new byte[] {1, 7, 5, 0, 2, 4, 6, 3} },
                    {  new byte[] {1, 6, 4, 7, 0, 3, 5, 2} },
                    {  new byte[] {6, 1, 3, 0, 7, 4, 2, 5} },
                    {  new byte[] {2, 5, 3, 0, 7, 4, 6, 1} },
                    {  new byte[] {4, 0, 7, 5, 2, 6, 1, 3} },
                    {  new byte[] {4, 6, 1, 5, 2, 0, 7, 3} },
                    {  new byte[] {3, 1, 6, 2, 5, 7, 0, 4} },
                    {  new byte[] {5, 2, 4, 7, 0, 3, 1, 6} },
                    {  new byte[] {3, 7, 0, 2, 5, 1, 6, 4} },
                    {  new byte[] {2, 4, 1, 7, 0, 6, 3, 5} },
                    {  new byte[] {5, 3, 6, 0, 7, 1, 4, 2} },
                    {  new byte[] {4, 2, 0, 6, 1, 7, 5, 3} },
                    {  new byte[] {3, 5, 7, 1, 6, 0, 2, 4} },
                    {  new byte[] {2, 4, 7, 3, 0, 6, 1, 5} },
                    {  new byte[] {5, 3, 0, 4, 7, 1, 6, 2} },
                    {  new byte[] {5, 1, 6, 0, 3, 7, 4, 2} },
                    {  new byte[] {4, 6, 0, 3, 1, 7, 5, 2} },
                    {  new byte[] {5, 2, 0, 6, 4, 7, 1, 3} },
                    {  new byte[] {2, 5, 7, 1, 3, 0, 6, 4} },
                    {  new byte[] {2, 6, 1, 7, 4, 0, 3, 5} },
                    {  new byte[] {3, 1, 7, 4, 6, 0, 2, 5} },
                    {  new byte[] {2, 5, 1, 4, 7, 0, 6, 3} },
                    {  new byte[] {5, 2, 6, 3, 0, 7, 1, 4} },
                    {  new byte[] {3, 6, 0, 7, 4, 1, 5, 2} },
                    {  new byte[] {5, 2, 0, 7, 3, 1, 6, 4} },
                    {  new byte[] {3, 1, 6, 4, 0, 7, 5, 2} },
                    {  new byte[] {4, 6, 1, 3, 7, 0, 2, 5} },
                    {  new byte[] {4, 1, 7, 0, 3, 6, 2, 5} },
                    {  new byte[] {2, 5, 7, 0, 4, 6, 1, 3} },
                }
        }
    };
    #endregion PrivateAttributes
}
