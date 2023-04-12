using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.RepositoryInfrastructure;

public interface IRepositoryBuilder<TContext> where TContext : DbContext
{
    public IRepositoryBuilder<TContext> AddRepository<TEntity>() where TEntity : class, IEntity;

    public IServiceCollection Build();
}