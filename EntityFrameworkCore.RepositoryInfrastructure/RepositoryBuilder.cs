using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.RepositoryInfrastructure;

internal class RepositoryBuilder<TContext> : IRepositoryBuilder<TContext> where TContext : DbContext
{
    private readonly IServiceCollection _services;

    public RepositoryBuilder(IServiceCollection services) => _services = services;

    public IRepositoryBuilder<TContext> AddRepository<TEntity>() where TEntity : class, IEntity
    {
        _services.AddTransient<IRepository<TEntity>, Repository<TContext, TEntity>>();

        return this;
    }

    public IServiceCollection Build() => _services;
}