# BackgroundExecutor

## Setting Up

 1. Add `.AddBackgroundTaskExecutor()`
 2. Specify EF Core provider for inner DbContext using `.WithDatabase(options => { })`
 3. Call `.Use` method to finish DI specification
 4. Add section to your appsettings.json

> "BackgroundTaskExecutor": {
> &nbsp;&nbsp;&nbsp;&nbsp;"Profiles": {
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"{profileName}": {
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"Interval": {interval},
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"IntervalTimeUnit": {"Millisecond" | "Second" | "Minute" | "Hour" | "Day" },
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"FirstRunAfter": {delay},
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"FirstRunAfterTimeUnit": {"Millisecond" | "Second" | "Minute" | "Hour" | "Day" },
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"LeaseDuration": {duration, optional, 2 minutes by default},
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"LeaseDurationTimeUnit": {"Millisecond" | "Second" | "Minute" | "Hour" | "Day" },
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;}
> &nbsp;&nbsp;&nbsp;&nbsp;} 
> }

*You can override Default profile as well both defining profile with name "Default" or define necessary parameters in "BackgroundTaskExecutor" section*

## Usage

 1. Add `[BackgroundExecution({profileName})]` attribute to method\
*Please note that BackgroundExecutor uses DI to get class instance for method running and can pass only CancellationToken to executing method*

## Several instances

Every instance of the application runs the timers, and each job runs on one of them at a time. A job's lease (the
`SyncLeases` table, one row per job and profile) is taken with a single conditional update: only when nobody holds
it and the interval has passed since the job last finished. The holder renews it every quarter of `LeaseDuration`
while the job runs and gives it back when the job ends, so:

- a job that runs longer than its interval never runs on two instances at once;
- an instance that dies mid-run blocks the job only until its lease expires (`LeaseDuration`), and an instance that
  is stopped gives it back at once;
- a run that loses its lease (it could not renew in time and another instance took over) is cancelled.

The interval is measured from the end of the last run on any instance. The job is identified by its type, method and
profile, so two jobs that only share a method name no longer block each other. Upgrading from an earlier version
starts each lease from the job's last run in `SyncEntries`, which is still written as the run log.