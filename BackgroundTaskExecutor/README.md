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
> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;}
> &nbsp;&nbsp;&nbsp;&nbsp;} 
> }

*You can override Default profile as well both defining profile with name "Default" or define necessary parameters in "BackgroundTaskExecutor" section*

## Usage

 1. Add `[BackgroundExecution({profileName})]` attribute to method\
*Please note that BackgroundExecutor uses DI to get class instance for method running and can pass only CancellationToken to executing method*