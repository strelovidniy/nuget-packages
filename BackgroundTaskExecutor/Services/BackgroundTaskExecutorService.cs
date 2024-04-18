using System.Reflection;
using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Context;
using BackgroundTaskExecutor.Entities;
using BackgroundTaskExecutor.Settings;
using BackgroundTaskExecutor.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundTaskExecutor.Services;

internal class BackgroundTaskExecutorService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly IExecutorSettings _settings;
    private readonly Dictionary<Guid, ExecutionOptions> _methodsToRun = [];

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

                _methodsToRun.Add(Guid.NewGuid(), new ExecutionOptions
                {
                    MethodInfo = methodInfo,
                    ParentType = typeMethods.Type,
                    Settings = methodSettings
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

        await bgContext.Database.MigrateAsync(cancellationToken);
        await bgContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private void StartTimers()
    {
        foreach (var methodToRun in _methodsToRun)
        {
            methodToRun.Value.Timer = new Timer(
                _ => ExecuteTask(methodToRun.Key),
                null,
                TimeSpan.FromMinutes(methodToRun.Value.Settings.FirstRunAfterInMinutes),
                TimeSpan.FromMilliseconds(-1)
            );
        }
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
        try
        {
            await RunJobAsync(options, stoppingToken);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(new EventId(), exception, "Background task execution failed");
        }

        options.Timer?.Change(TimeSpan.FromMinutes(options.Settings.IntervalInMinutes), TimeSpan.FromMilliseconds(-1));
    }

    private async Task RunJobAsync(
        ExecutionOptions options,
        CancellationToken stoppingToken
    )
    {
        _logger.LogInformation(
            "{MethodName} started executing in background",
            options.MethodInfo.Name
        );

        await using var scope = _services.CreateAsyncScope();

        var serviceProvider = scope.ServiceProvider;

        var backgroundContext = serviceProvider.GetRequiredService<BackgroundContext>();

        var syncEntry = await backgroundContext
            .SyncEntries
            .OrderByDescending(syncEntry => syncEntry.LastRun)
            .Where(syncEntry => syncEntry.TaskName == options.MethodInfo.Name && syncEntry.Profile == options.Profile)
            .FirstOrDefaultAsync(stoppingToken);

        if (syncEntry is not null
            && DateTime.UtcNow - syncEntry.LastRun < TimeSpan.FromMinutes(options.Settings.IntervalInMinutes))
        {
            _logger.LogInformation(
                "Background task ({MethodName}) executed by another pod | Machine Name {MachineName}",
                options.MethodInfo.Name,
                syncEntry.MachineName
            );

            return;
        }

        await backgroundContext.AddAsync(
            new SyncEntry
            {
                Id = Guid.NewGuid(),
                MachineName = Environment.MachineName,
                TaskName = options.MethodInfo.Name,
                LastRun = DateTime.UtcNow,
                Profile = options.Profile
            },
            stoppingToken
        );

        await backgroundContext.SaveChangesAsync(stoppingToken);

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
                    options.MethodInfo.Invoke(instance, [stoppingToken]),
                _ => throw new InvalidOperationException("Method with parameters is not supported")
            };

            try
            {
                await (dynamic) result!;
            }
            catch (Exception)
            {
                // ignored
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
    }
}