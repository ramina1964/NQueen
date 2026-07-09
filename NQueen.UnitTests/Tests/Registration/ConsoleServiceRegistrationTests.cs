using NQueen.ConsoleApp;
using NQueen.ConsoleApp.Services;

namespace NQueen.UnitTests.Tests.Registration;

[Trait("Category", "Registration")]
public class ConsoleServiceRegistrationTests
{
    private static ServiceProvider BuildProvider(bool enableCap = true)
    {
        var services = new ServiceCollection();
        services.AddNQueenServices(enableCap: enableCap);
        services.AddSingleton<App>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddNQueenServices_RegistersFormatter()
    {
        using var provider = BuildProvider();
        provider.GetService<ISolutionFormatter>().ShouldBeOfType<SolutionFormatter>();
    }

    [Fact]
    public void AddNQueenServices_RegistersSolverConcreteType()
    {
        using var provider = BuildProvider();
        provider.GetService<BitmaskSolver>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(ISolver))]
    [InlineData(typeof(ISolverBackEnd))]
    [InlineData(typeof(ISolverFrontEnd))]
    public void AddNQueenServices_MapsSolverInterfaces(Type serviceType)
    {
        using var provider = BuildProvider();
        provider.GetService(serviceType).ShouldBeAssignableTo<BitmaskSolver>();
    }

    [Fact]
    public void AddNQueenServices_ConfiguresCountOnlyStorageModes()
    {
        using var provider = BuildProvider();
        var solver = provider.GetRequiredService<BitmaskSolver>();
        solver.AllStorageMode.ShouldBe(ResultStorageMode.CountOnly);
        solver.UniqueStorageMode.ShouldBe(ResultStorageMode.CountOnly);
    }

    [Fact]
    public void AddNQueenServices_SolverIsTransient_YieldsDistinctInstances()
    {
        using var provider = BuildProvider();
        var first = provider.GetRequiredService<BitmaskSolver>();
        var second = provider.GetRequiredService<BitmaskSolver>();
        first.ShouldNotBeSameAs(second);
    }

    [Fact]
    public void AddNQueenServices_DoesNotOverridePreRegisteredFormatter()
    {
        var custom = new SolutionFormatter();
        var services = new ServiceCollection();
        services.AddSingleton<ISolutionFormatter>(custom);

        services.AddNQueenServices();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISolutionFormatter>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void AddNQueenServices_ReturnsSameCollectionForChaining()
    {
        var services = new ServiceCollection();
        services.AddNQueenServices().ShouldBeSameAs(services);
    }

    [Fact]
    public void App_ResolvesFromContainer()
    {
        using var provider = BuildProvider();
        provider.GetService<App>().ShouldNotBeNull();
    }

    [Fact]
    public void App_Constructor_AcceptsServiceProvider()
    {
        using var provider = BuildProvider();
        var app = new App(provider);
        app.ShouldNotBeNull();
    }
}
