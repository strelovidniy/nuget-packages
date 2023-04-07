using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.RepositoryInfrastructure.DependencyInjection;

public static class RepositoryInfrastructureDependencyInjectionExtension
{
    private static IServiceCollection AddRepository<TEntity>(
        this IServiceCollection services
    ) where TEntity : class, IEntity =>
        services.AddTransient<IRepository<TEntity>, Repository<TEntity>>();
}