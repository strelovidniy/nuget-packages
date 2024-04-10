using BackgroundTaskExecutor.Builders.Abstraction;
using BackgroundTaskExecutor.Builders.Realization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundTaskExecutor;

public static class BackgroundTaskExecutorDependencyInjection
{
    public static IExecutorBuilder AddBackgroundTaskExecutor(
        this IServiceCollection services,
        IConfiguration configuration
    ) => new ExecutorBuilder(services, configuration);
}