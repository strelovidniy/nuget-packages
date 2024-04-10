using System.Reflection;
using BackgroundTaskExecutor.Constants;
using BackgroundTaskExecutor.Settings;

namespace BackgroundTaskExecutor.Types;

internal class ExecutionOptions
{
    public MethodInfo MethodInfo { get; set; } = null!;

    public Type ParentType { get; set; } = null!;

    public CoreExecutorSettings Settings { get; set; } = null!;

    public Timer? Timer { get; set; }

    public Task? ExecutingTask { get; set; }

    public string Profile { get; set; } = Profiles.Default;
}