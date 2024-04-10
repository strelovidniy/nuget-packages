using BackgroundTaskExecutor.Builders.Abstraction;
using BackgroundTaskExecutor.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundTaskExecutor.Builders.Realization;

internal class ExecutorBuilder(
    IServiceCollection services,
    IConfiguration configuration
) : IExecutorBuilder
{
    public IExecutorContextBuilder WithDatabase(Action<DbContextOptionsBuilder> options)
    {
        services.AddDbContext<BackgroundContext>(options);

        return new ExecutorContextBuilder(services, configuration);
    }
}