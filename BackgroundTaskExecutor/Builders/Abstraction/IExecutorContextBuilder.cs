using Microsoft.Extensions.DependencyInjection;

namespace BackgroundTaskExecutor.Builders.Abstraction;

public interface IExecutorContextBuilder
{
    public IServiceCollection Use();
}