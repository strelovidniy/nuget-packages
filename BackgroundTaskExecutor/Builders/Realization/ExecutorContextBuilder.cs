using BackgroundTaskExecutor.Builders.Abstraction;
using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Services;
using BackgroundTaskExecutor.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundTaskExecutor.Builders.Realization;

internal class ExecutorContextBuilder(
    IServiceCollection services,
    IConfiguration configuration
) : IExecutorContextBuilder
{
    public IServiceCollection Use()
    {
        var settings = new ExecutorSettings();

        configuration
            .GetSection(nameof(BackgroundTaskExecutor))
            .Bind(settings);

        settings.Profiles.TryAdd(Profiles.Default, settings);

        return services
            .AddHostedService<BackgroundTaskExecutorService>()
            .AddSingleton<IExecutorSettings>(_ => settings);
    }
}