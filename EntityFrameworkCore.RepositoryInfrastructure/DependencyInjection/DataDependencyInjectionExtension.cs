using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.RepositoryInfrastructure.DependencyInjection;

public static class RepositoryInfrastructureDependencyInjectionExtension
{
    public static IRepositoryBuilder<TContext> CreateRepositoryBuilderWithContext<TContext>(
        this IServiceCollection services
    ) where TContext : DbContext => new RepositoryBuilder<TContext>(services);
}