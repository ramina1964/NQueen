namespace NQueen.ViewModelTests.Tests.Main;

[Trait("Category", "ViewModel")]
public class StorageModeTests
{
    // ── Single mode: storage locked to Materialize, ComboBox disabled ──────

    [Theory]
    [InlineData(SolutionMode.Single)]
    public void SelectedStorageMode_ShouldBeMaterialize_WhenSolutionModeIsSingle(
        SolutionMode solutionMode)
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: solutionMode);

        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.Materialize,
            "Single mode always materializes the one solution found.");
    }

    [Fact]
    public void CanChangeStorageMode_ShouldBeFalse_WhenSolutionModeIsSingle()
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: SolutionMode.Single);

        vm.CanChangeStorageMode.ShouldBeFalse(
            "Solution Storage Mode ComboBox must be disabled for Single mode.");
    }

    // ── Unique / All: storage defaults to CountOnly, ComboBox enabled ───────

    [Theory]
    [InlineData(SolutionMode.Unique)]
    [InlineData(SolutionMode.All)]
    public void SelectedStorageMode_ShouldBeCountOnly_WhenSolutionModeIsUniqueOrAll(
        SolutionMode solutionMode)
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: solutionMode);

        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.CountOnly,
            "Switching to Unique/All should restore CountOnly as the default.");
    }

    [Theory]
    [InlineData(SolutionMode.Unique)]
    [InlineData(SolutionMode.All)]
    public void CanChangeStorageMode_ShouldBeTrue_WhenSolutionModeIsUniqueOrAll(
        SolutionMode solutionMode)
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: solutionMode);

        vm.CanChangeStorageMode.ShouldBeTrue(
            "Solution Storage Mode ComboBox must be enabled for Unique and All modes.");
    }

    // ── Switching from Single → Unique/All restores CountOnly ───────────────

    [Theory]
    [InlineData(SolutionMode.Unique)]
    [InlineData(SolutionMode.All)]
    public void SelectedStorageMode_ShouldRestoreCountOnly_WhenSwitchingFromSingleToUniqueOrAll(
        SolutionMode targetMode)
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: SolutionMode.Single);
        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.Materialize);

        vm.SolutionMode = targetMode;

        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.CountOnly,
            "Storage mode should revert to CountOnly when leaving Single mode.");
        vm.CanChangeStorageMode.ShouldBeTrue();
    }

    // ── Switching from Unique/All → Single forces Materialize and disables ──

    [Theory]
    [InlineData(SolutionMode.Unique)]
    [InlineData(SolutionMode.All)]
    public void SelectedStorageMode_ShouldForceMaterialize_WhenSwitchingToSingle(
        SolutionMode sourceMode)
    {
        var vm = TestHelpers.CreateMainViewModel(solutionMode: sourceMode);
        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.CountOnly);

        vm.SolutionMode = SolutionMode.Single;

        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.Materialize,
            "Switching to Single must force Materialize.");
        vm.CanChangeStorageMode.ShouldBeFalse(
            "ComboBox must be disabled when mode is Single.");
    }

    // ── Visualize mode: always Materialize regardless of solution mode ───────

    [Theory]
    [InlineData(SolutionMode.Single)]
    [InlineData(SolutionMode.Unique)]
    [InlineData(SolutionMode.All)]
    public void SelectedStorageMode_ShouldBeMaterialize_WhenDisplayModeIsVisualize(
        SolutionMode solutionMode)
    {
        var vm = TestHelpers.CreateMainViewModel(
            boardSize: 4,
            solutionMode: solutionMode,
            displayMode: DisplayMode.Visualize);

        vm.SelectedStorageMode.ShouldBe(ResultStorageMode.Materialize,
            "Visualize mode requires solutions to be materialized for rendering.");
        vm.CanChangeStorageMode.ShouldBeFalse(
            "ComboBox must be disabled while visualizing.");
    }
}
