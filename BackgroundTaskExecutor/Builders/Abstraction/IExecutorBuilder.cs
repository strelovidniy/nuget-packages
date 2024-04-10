using Microsoft.EntityFrameworkCore;

namespace BackgroundTaskExecutor.Builders.Abstraction;

public interface IExecutorBuilder
{
    public IExecutorContextBuilder WithDatabase(
        Action<DbContextOptionsBuilder> options
    );
}