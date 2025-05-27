namespace NQueen.ViewModelTests.Helpers;

public static class AssertionHelpers
{
    public static void AssertChessboardState(MainViewModel mainVm, int expectedQueenCount)
    {
        mainVm.ChessboardVm.Squares.Should().NotBeEmpty(TestConst.ChessboardNotPopulatedError);
        mainVm.ChessboardVm.Squares.Count(sq => string.IsNullOrEmpty(sq.ImagePath) == false)
            .Should().Be(expectedQueenCount, TestConst.IncorrectQueenPlacementError);
    }

    public static void AssertSolutionsState(MainViewModel mainVm)
    {
        mainVm.ObservableSolutions.Should().NotBeEmpty(TestConst.NoOfSolsValueError);
        mainVm.SelectedSolution.Should().NotBeNull(TestConst.SolutionNotSelectedError);
        mainVm.NoOfSolutions.Should().NotBe("0", TestConst.SolutionNumberZeroError);
    }

    public static void AssertSavedContentProperties(
        string savedContent,
        IDictionary<string, string> expectedProperties,
        params string[] requiredKeysWithDynamicValues)
    {
        // Parse lines into a dictionary
        var lines = savedContent.Split(['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);

        var dict = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                var key = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1).Trim();
                dict[key] = value;
            }
        }

        // Assert all expected properties
        foreach (var kvp in expectedProperties)
        {
            dict.Should().ContainKey(kvp.Key, $"Missing property: {kvp.Key}");
            dict[kvp.Key].Should().Be(kvp.Value, $"Property '{kvp.Key}' should have value '{kvp.Value}'");
        }

        // Assert required keys with dynamic values exist
        foreach (var key in requiredKeysWithDynamicValues)
        {
            dict.Should().ContainKey(key, $"Missing property: {key}");
            dict[key].Should().NotBeNullOrWhiteSpace($"Property '{key}' should have a value");
        }
    }
}
