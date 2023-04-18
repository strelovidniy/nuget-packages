using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.RepositoryInfrastructure;

internal class Repository<TContext, TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity where TContext : DbContext
{
    private readonly TContext _context;
    private readonly DbSet<TEntity> _dbEntities;
    private readonly ILogger<Repository<TContext, TEntity>> _logger;

    public Repository(
        TContext context,
        ILogger<Repository<TContext, TEntity>> logger
    )
    {
        _context = context;
        _logger = logger;
        _dbEntities = _context.Set<TEntity>();
    }

    /// <summary>
    ///     Gets all entity records with included entities.
    /// </summary>
    /// <param name="includes">Included entities.</param>
    /// <returns>IQueryable of all entity records with included entities, if includes is null this function is equal GetAll.</returns>
    public IQueryable<TEntity> Query(params Expression<Func<TEntity, object>>[] includes)
    {
        var dbSet = _context.Set<TEntity>();

        var query = includes
            .Aggregate<Expression<Func<TEntity, object>>, IQueryable<TEntity>>(
                dbSet,
                (current, include) => current.Include(include)
            );

        return query ?? dbSet;
    }

    /// <summary>
    ///     Gets entity by the keys.
    /// </summary>
    /// <param name="keys">Keys for the search.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Entity with such keys.</returns>
    public ValueTask<TEntity?> GetByIdAsync(CancellationToken cancellationToken = default, params Guid[] keys)
    {
        _logger.LogTrace("Finding entity with keys:\n\n{keys}\n", JsonSerializer.Serialize(keys));

        return _dbEntities.FindAsync(keys, cancellationToken);
    }

    /// <summary>
    ///     Async add entity into DBContext.
    /// </summary>
    /// <param name="entity">Entity.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <exception cref="ArgumentNullException">The entity to add cannot be <see langword="null" />.</exception>
    /// <returns>Added entity.</returns>
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        CheckEntityForNull(entity);

        _logger.LogTrace(
            "Adding entity\n\n{entity}\n",
            JsonSerializer.Serialize(
                entity,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                }
            )
        );

        return (await _dbEntities.AddAsync(entity, cancellationToken)).Entity;
    }

    /// <summary>
    ///     Adds a range of entities.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Task.</returns>
    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();

        _logger.LogTrace(
            "Adding entities\n\n{entitiesList}\n",
            JsonSerializer.Serialize(
                entitiesList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                }
            )
        );

        return _dbEntities.AddRangeAsync(entitiesList, cancellationToken);
    }

    /// <summary>
    ///     Updates entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to update.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Awaitable task with updated entity.</returns>
    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            _logger.LogTrace(
                "Updating entity\n\n{entity}\n",
                JsonSerializer.Serialize(
                    entity,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.Never
                    }
                )
            );

            return _dbEntities.Update(entity).Entity;
        }, cancellationToken);

    /// <summary>
    ///     Updates entities asynchronously.
    /// </summary>
    /// <param name="entities">Entities to update.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Awaitable task with updated entity.</returns>
    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            var entitiesList = entities.ToList();

            _logger.LogTrace(
                "Updating entities\n\n{entitiesList}\n",
                JsonSerializer.Serialize(
                    entitiesList,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.Never
                    }
                )
            );

            _dbEntities.UpdateRange(entitiesList);
        }, cancellationToken);

    /// <summary>
    ///     Deletes range.
    /// </summary>
    /// <param name="entities">Entities to delete.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Task.</returns>
    public Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            var entitiesList = entities.ToList();

            _logger.LogTrace(
                "Deleting entities\n\n{entitiesList}\n",
                JsonSerializer.Serialize(
                    entitiesList,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.Never
                    }
                )
            );

            entitiesList.ForEach(item => _context.Entry(item).State = EntityState.Deleted);
        }, cancellationToken);

    /// <summary>
    ///     Saves changes in the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Task.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving changes in the database");

            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.RollbackAsync(cancellationToken);
            }

            _logger.LogInformation("Transaction rolled back");

            throw;
        }
    }

    /// <summary>
    ///     Removes entity from DBContext.
    /// </summary>
    /// <param name="entity">Entity to delete.</param>
    /// <returns>Task.</returns>
    public void Delete(TEntity entity)
    {
        _logger.LogTrace(
            "Deleting entity\n\n{entity}\n",
            JsonSerializer.Serialize(
                entity,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    ReferenceHandler = ReferenceHandler.Preserve
                }
            )
        );

        _context.Entry(entity).State = EntityState.Deleted;
    }

    /// <summary>
    ///     Detaches entity.
    /// </summary>
    /// <param name="entity">Entity to detach.</param>
    /// <returns>Task.</returns>
    public void Detach(TEntity entity)
    {
        _logger.LogTrace(
            "Detaching entity\n\n{entity}\n",
            JsonSerializer.Serialize(
                entity,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                }
            )
        );

        _context.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    ///     Gets all entity records with included entities as no tracking list.
    /// </summary>
    /// <param name="includes">Included entities.</param>
    /// <returns>IQueryable of all entity records with included entities, if includes is null this function is equal GetAll.</returns>
    public IQueryable<TEntity> NoTrackingQuery(params Expression<Func<TEntity, object>>[] includes)
    {
        var dbSet = _context.Set<TEntity>();

        var query = includes
            .Aggregate<Expression<Func<TEntity, object>>, IQueryable<TEntity>>(
                dbSet,
                (current, include) => current.Include(include)
            );

        return (query ?? dbSet).AsNoTracking();
    }

    /// <summary>
    ///     Executes raw SQL query.
    /// </summary>
    /// <param name="sql">SQL query.</param>
    /// <param name="parameters">Parameters.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Number of affected rows.</returns>
    public Task<int> ExecuteSqlRawAsync(
        string sql,
        IEnumerable<object> parameters,
        CancellationToken cancellationToken = default
    ) => _context.Database.ExecuteSqlRawAsync(
        sql,
        parameters,
        cancellationToken
    );

    /// <summary>
    ///     Executes raw SQL query.
    /// </summary>
    /// <param name="sql">SQL query.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns>Number of affected rows.</returns>
    public Task<int> ExecuteSqlRawAsync(
        string sql,
        CancellationToken cancellationToken = default
    ) => _context.Database.ExecuteSqlRawAsync(
        sql,
        cancellationToken
    );

    /// <summary>
    ///     Executes raw SQL query with IQueryable collection return.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="sql"></param>
    /// <param name="parameters"></param>
    /// <returns>IQueryable</returns>
    public IQueryable<TResult> FromSqlRawAsync<TResult>(
        string sql,
        params object[] parameters
    ) => _context.Database.SqlQueryRaw<TResult>(
        sql,
        parameters
    );

    private static void CheckEntityForNull(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity), "The entity to add cannot be null.");
        }
    }
}