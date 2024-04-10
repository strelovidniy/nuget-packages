using BackgroundTaskExecutor.Constants;

namespace BackgroundTaskExecutor;

[AttributeUsage(AttributeTargets.Method)]
public class BackgroundExecutionAttribute(string profile = Profiles.Default) : Attribute
{
    public string Profile { get; } = profile;
}