namespace NQueen.UnitTests.Setup;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Tests want full solution sets -> enableCap:false
        services.AddBitmaskSolverServices(enableCap: false);
        services.AddSingleton<ISolutionFormatter, TestSolutionFormatter>();
        return services;
    }

    public static IServiceCollection AddTestServices(this IServiceCollection services)
    {
        return services;
    }
}
