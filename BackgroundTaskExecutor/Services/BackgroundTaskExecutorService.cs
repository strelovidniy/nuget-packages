using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Context;
using BackgroundTaskExecutor.Entities;
using BackgroundTaskExecutor.Enums;
using BackgroundTaskExecutor.Settings;
using BackgroundTaskExecutor.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundTaskExecutor.Services;

// Every instance of the application runs these timers, and a job runs on exactly one of them at a time: the one that
// took the job's lease (SyncLeases). Taking it is one conditional update - free, or held by nobody alive, and the
// interval passed since the job last finished - so two instances can never both win it. The holder renews it while
// the job runs and gives it back when the job ends; an instance that dies holding it loses it when it expires.
internal class BackgroundTaskExecutorService : IHostedService, IDisposable
{
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger _logger;
    private readonly Dictionary<Guid, ExecutionOptions> _methodsToRun = [];
    private readonly IServiceProvider _services;
    private readonly IExecutorSettings _settings;
    private readonly CancellationTokenSource _stoppingCts = new();

    public BackgroundTaskExecutorService(IServiceProvider services)
    {
        _services = services;
        _logger = _services.GetRequiredService<ILogger<BackgroundTaskExecutorService>>();
        _settings = _services.GetRequiredService<IExecutorSettings>();
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();

        foreach (var methodToRun in _methodsToRun)
        {
            methodToRun.Value.Timer?.Dispose();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CollectMethodsToExecute();

        await ApplyMigrationsAsync(cancellationToken);

        StartTimers();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var methodToRun in _methodsToRun)
        {
            methodToRun.Value.Timer?.Change(Timeout.Infinite, 0);

            if (methodToRun.Value.ExecutingTask is null)
            {
                continue;
            }

            try
            {
                await _stoppingCts.CancelAsync();
            }
            finally
            {
                await Task.WhenAny(methodToRun.Value.ExecutingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }
    }

    // A job's lease is keyed by its type, method and profile, hashed into the row's id, so it is the same on every
    // instance and every restart and two jobs that merely share a method name never block each other.
    internal static Guid LeaseIdOf(string leaseName, string profile)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{leaseName}|{profile}"));

        return new Guid(hash.AsSpan(0, 16));
    }

    private void CollectMethodsToExecute()
    {
        var typesMethods = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => (Type: type, Methods: type
                .GetMethods()
                .Where(methodInfo =>
                    methodInfo.GetCustomAttributes(typeof(BackgroundExecutionAttribute), false).Length > 0)))
            .Where(typesMethods => typesMethods.Methods.Any());

        var defaultSettings = _settings.Profiles.GetValueOrDefault(Profiles.Default, new CoreExecutorSettings());

        foreach (var typeMethods in typesMethods)
        {
            foreach (var methodInfo in typeMethods.Methods)
            {
                var attribute
                    = methodInfo.GetCustomAttributes(typeof(BackgroundExecutionAttribute)).FirstOrDefault() as
                        BackgroundExecutionAttribute;

                var profileName = Profiles.Default;

                if (attribute is not null)
                {
                    profileName = attribute.Profile;
                }

                var methodSettings = _settings.Profiles.GetValueOrDefault(profileName, defaultSettings);
                var leaseName = $"{typeMethods.Type.FullName}.{methodInfo.Name}";

                _methodsToRun.Add(Guid.NewGuid(), new ExecutionOptions
                {
                    MethodInfo = methodInfo,
                    ParentType = typeMethods.Type,
                    Settings = methodSettings,
                    Profile = profileName,
                    LeaseId = LeaseIdOf(leaseName, profileName),
                    LeaseName = leaseName.Length > 400 ? leaseName[..400] : leaseName
                });
            }
        }
    }

    private async Task ApplyMigrationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = _services.CreateAsyncScope();

        var bgContext = scope.ServiceProvider.GetRequiredService<BackgroundContext>();

        try
        {
            await bgContext.Database.MigrateAsync(cancellationToken);
            await bgContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
    }

    private void StartTimers()
    {
        foreach (var methodToRun in _methodsToRun)
        {
            methodToRun.Value.Timer = new Timer(
                _ => ExecuteTask(methodToRun.Key),
                null,
                CalculateTimeSpan(
                    methodToRun.Value.Settings.FirstRunAfter,
                    methodToRun.Value.Settings.FirstRunAfterTimeUnit
                ),
                TimeSpan.FromMilliseconds(-1)
            );
        }
    }

    private static TimeSpan CalculateTimeSpan(
        double value,
        TimeUnit timeUnit
    ) => timeUnit switch
    {
        TimeUnit.Millisecond => TimeSpan.FromMilliseconds(value),
        TimeUnit.Second => TimeSpan.FromSeconds(value),
        TimeUnit.Minute => TimeSpan.FromMinutes(value),
        TimeUnit.Hour => TimeSpan.FromHours(value),
        TimeUnit.Day => TimeSpan.FromDays(value),
        _ => TimeSpan.FromMinutes(value)
    };

    private static TimeSpan IntervalOf(ExecutionOptions options) =>
        CalculateTimeSpan(options.Settings.Interval, options.Settings.IntervalTimeUnit);

    private static TimeSpan LeaseDurationOf(ExecutionOptions options)
    {
        var duration = CalculateTimeSpan(options.Settings.LeaseDuration, options.Settings.LeaseDurationTimeUnit);

        // Renewed every quarter of it, so anything shorter than a few seconds would spend the run renewing.
        return duration < TimeSpan.FromSeconds(4) ? TimeSpan.FromSeconds(4) : duration;
    }

    private void ExecuteTask(Guid id)
    {
        if (!_methodsToRun.TryGetValue(id, out var methodToRun))
        {
            return;
        }

        methodToRun.Timer?.Change(Timeout.Infinite, 0);

        methodToRun.ExecutingTask = ExecuteTaskAsync(methodToRun, _stoppingCts.Token);
    }

    private async Task ExecuteTaskAsync(
        ExecutionOptions options,
        CancellationToken stoppingToken
    )
    {
        var next = IntervalOf(options);

        try
        {
            next = await RunJobAsync(options, stoppingToken);
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(new EventId(), exception, "Background task execution failed");
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            options.Timer?.Change(next, TimeSpan.FromMilliseconds(-1));
        }
        catch (ObjectDisposedException)
        {
            // The host is stopping.
        }
    }

    // Runs the job if this instance takes its lease, and answers when to try again: a full interval after a run, or
    // after a job another instance holds, and the moment the job falls due when it was simply not due yet.
    private async Task<TimeSpan> RunJobAsync(
        ExecutionOptions options,
        CancellationToken stoppingToken
    )
    {
        var interval = IntervalOf(options);
        var leaseDuration = LeaseDurationOf(options);

        await using var scope = _services.CreateAsyncScope();

        var serviceProvider = scope.ServiceProvider;

        var backgroundContext = serviceProvider.GetRequiredService<BackgroundContext>();

        await EnsureLeaseAsync(backgroundContext, options, stoppingToken);

        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var leasedUntil = now + leaseDuration;
        var dueBefore = now - interval;

        var acquired = await backgroundContext
            .SyncLeases
            .Where(lease => lease.Id == options.LeaseId
                && (lease.LeasedUntil == null || lease.LeasedUntil < now)
                && (lease.FinishedAt == null || lease.FinishedAt <= dueBefore))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(lease => lease.Token, token)
                    .SetProperty(lease => lease.Owner, Environment.MachineName)
                    .SetProperty(lease => lease.LeasedUntil, leasedUntil)
                    .SetProperty(lease => lease.StartedAt, now),
                stoppingToken
            ) == 1;

        if (!acquired)
        {
            return await RetryDelayAsync(backgroundContext, options, interval, stoppingToken);
        }

        _logger.LogInformation(
            "{MethodName} started executing in background",
            options.MethodInfo.Name
        );

        // The run log, and what an instance on an older version of this package checks before it runs the same job.
        await backgroundContext.AddAsync(
            new SyncEntry
            {
                Id = Guid.NewGuid(),
                MachineName = Environment.MachineName,
                TaskName = options.MethodInfo.Name,
                LastRun = now,
                Profile = options.Profile
            },
            stoppingToken
        );

        await backgroundContext.SaveChangesAsync(stoppingToken);

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        using var heartbeatCts = new CancellationTokenSource();

        var heartbeat = RenewWhileRunningAsync(options, token, leaseDuration, runCts, heartbeatCts.Token);
        var completed = false;

        try
        {
            completed = await InvokeAsync(serviceProvider, options, runCts.Token);
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            await heartbeat;

            // Shutting down before the job finished: the lease is given back without a finish, so the next instance
            // runs the job at once rather than an interval after a run that never ended.
            await ReleaseAsync(options, token, completed && !stoppingToken.IsCancellationRequested);
        }

        return interval;
    }

    // The lease row exists before anybody can take it. Created by the first instance to run the job; another
    // creating it at the same moment loses on the key and simply uses that one. A job that already ran under an
    // older version of this package starts its lease from that run, so upgrading does not run every job at once.
    private async Task EnsureLeaseAsync(
        BackgroundContext context,
        ExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        if (await context.SyncLeases.AnyAsync(lease => lease.Id == options.LeaseId, cancellationToken))
        {
            return;
        }

        var lastRun = await context
            .SyncEntries
            .Where(entry => entry.TaskName == options.MethodInfo.Name && entry.Profile == options.Profile)
            .MaxAsync(entry => (DateTime?)entry.LastRun, cancellationToken);

        context.SyncLeases.Add(new SyncLease
        {
            Id = options.LeaseId,
            TaskName = options.LeaseName,
            Profile = options.Profile,
            FinishedAt = lastRun is null ? null : DateTime.SpecifyKind(lastRun.Value, DateTimeKind.Utc)
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another instance created it first.
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    private async Task<TimeSpan> RetryDelayAsync(
        BackgroundContext context,
        ExecutionOptions options,
        TimeSpan interval,
        CancellationToken cancellationToken
    )
    {
        var lease = await context
            .SyncLeases
            .AsNoTracking()
            .FirstOrDefaultAsync(lease => lease.Id == options.LeaseId, cancellationToken);

        var now = DateTime.UtcNow;

        if (lease?.LeasedUntil is { } leasedUntil && leasedUntil >= now)
        {
            _logger.LogInformation(
                "Background task ({MethodName}) is running on another instance | Machine Name {MachineName}",
                options.MethodInfo.Name,
                lease.Owner
            );

            return interval;
        }

        _logger.LogInformation(
            "Background task ({MethodName}) executed by another instance | Machine Name {MachineName}",
            options.MethodInfo.Name,
            lease?.Owner
        );

        if (lease?.FinishedAt is not { } finishedAt)
        {
            return interval;
        }

        var due = finishedAt + interval - now;

        return due < MinRetryDelay ? MinRetryDelay : due > interval ? interval : due;
    }

    // Keeps the lease while the job runs. A renewal that finds the lease taken means this instance was too slow to
    // renew and another runs the job now, so this run is cancelled rather than carried on beside it.
    private async Task RenewWhileRunningAsync(
        ExecutionOptions options,
        Guid token,
        TimeSpan leaseDuration,
        CancellationTokenSource runCts,
        CancellationToken stopRenewing
    )
    {
        var every = leaseDuration / 4;

        while (!stopRenewing.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(every, stopRenewing);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await using var scope = _services.CreateAsyncScope();

                var context = scope.ServiceProvider.GetRequiredService<BackgroundContext>();
                var leasedUntil = DateTime.UtcNow + leaseDuration;

                var renewed = await context
                    .SyncLeases
                    .Where(lease => lease.Id == options.LeaseId && lease.Token == token)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(lease => lease.LeasedUntil, leasedUntil),
                        stopRenewing) == 1;

                if (renewed)
                {
                    continue;
                }

                _logger.LogWarning(
                    "Background task ({MethodName}) lost its lease to another instance; this run is cancelled",
                    options.MethodInfo.Name
                );

                await runCts.CancelAsync();

                return;
            }
            catch (OperationCanceledException) when (stopRenewing.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The database is out of reach for a moment; the lease stands until it expires, and the next
                // renewal tries again.
                _logger.LogWarning(exception, "Background task ({MethodName}) could not renew its lease",
                    options.MethodInfo.Name);
            }
        }
    }

    private async Task ReleaseAsync(
        ExecutionOptions options,
        Guid token,
        bool finished
    )
    {
        using var timeout = new CancellationTokenSource(ReleaseTimeout);

        try
        {
            await using var scope = _services.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<BackgroundContext>();
            var held = context.SyncLeases.Where(lease => lease.Id == options.LeaseId && lease.Token == token);

            if (finished)
            {
                var finishedAt = DateTime.UtcNow;

                await held.ExecuteUpdateAsync(setters => setters
                        .SetProperty(lease => lease.Token, (Guid?)null)
                        .SetProperty(lease => lease.LeasedUntil, (DateTime?)null)
                        .SetProperty(lease => lease.FinishedAt, finishedAt),
                    timeout.Token
                );
            }
            else
            {
                await held.ExecuteUpdateAsync(setters => setters
                        .SetProperty(lease => lease.Token, (Guid?)null)
                        .SetProperty(lease => lease.LeasedUntil, (DateTime?)null),
                    timeout.Token
                );
            }
        }
        catch (Exception exception)
        {
            // Not given back: it expires on its own and the job waits that long.
            _logger.LogWarning(exception, "Background task ({MethodName}) could not release its lease",
                options.MethodInfo.Name);
        }
    }

    // Whether the job ran to its end, whatever it answered. A job that throws has still finished: it waits its
    // interval like any other run rather than being retried at once by every instance in turn.
    private async Task<bool> InvokeAsync(
        IServiceProvider serviceProvider,
        ExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        object? instance = null;

        foreach (var interfaceType in options.ParentType.GetInterfaces())
        {
            try
            {
                instance = serviceProvider.GetRequiredService(interfaceType);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        try
        {
            instance ??= serviceProvider.GetRequiredService(options.ParentType);

            var result = options.MethodInfo.GetParameters().Length switch
            {
                0 => options.MethodInfo.Invoke(instance, null),
                1 when options.MethodInfo.GetParameters()[0].ParameterType == typeof(CancellationToken) =>
                    options.MethodInfo.Invoke(instance, [cancellationToken]),
                _ => throw new InvalidOperationException("Method with parameters is not supported")
            };

            try
            {
                await (dynamic)result!;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // ignored
            }
            catch (Exception)
            {
                return false;
            }

            _logger.LogInformation(
                "{MethodName} successfully executed",
                options.MethodInfo.Name
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while executing background task"
            );
        }

        return true;
    }
}
