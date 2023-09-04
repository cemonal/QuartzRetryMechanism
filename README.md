# QuartzRetryMechanism

QuartzRetryMechanism is a library designed to provide a retry mechanism for Quartz.NET jobs in case they fail during execution. It's developed to be compatible with .NET Standard 2.1 and leverages the power of the [Quartz.NET](https://www.quartz-scheduler.net/) scheduling library.

## 📦 Dependencies

- [Quartz.NET](https://www.quartz-scheduler.net/)

## 🚀 Installation

Integrate this library into your project either by directly referencing it or by adding it through NuGet once it's available.

## 🛠 Usage

Here's a simple guide to get you started with `QuartzRetryMechanism`:

```csharp
// Firstly, define your job that implements the IJob interface
public class SampleJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // Your job's execution logic goes here
    }
}

// Next, configure your job
var jobName = "MyCustomJob";
var jobGroup = "DefaultGroup";

var job = JobBuilder.Create<SampleJob>()
    .WithIdentity(jobName, jobGroup)
    .UsingJobData("MaxRetries", 3)
    .UsingJobData("WaitInterval", 5)
    .Build();

var periodicallyTrigger = TriggerBuilder.Create()
    .WithCronSchedule("0 0/1 * 1/1 * ? *") // Example cron expression
    .Build();

var jobFailureHandlerName = "RetryHandlerForMyCustomJob";

_scheduler.ScheduleJob(job, periodicallyTrigger);
_scheduler.ListenerManager.AddJobListener(
    new JobFailureHandler(
        new JobFailureConfiguration 
        { 
            Name = jobFailureHandlerName, 
            CronSchedule = "0 0/1 * 1/1 * ? *" // Again, adjust this to your needs
        }
    ), 
    KeyMatcher<JobKey>.KeyEquals(new JobKey(jobName, jobGroup))
);
```
Remember to adjust the cron expression ("0 0/1 * 1/1 * ? *") to your specific needs.

## 🔍 Understanding Parameters

### MaxRetries

`MaxRetries` represents the maximum number of times the job will attempt to retry after a failure. For instance, if a job encounters an error and `MaxRetries` is set to 3, the job will attempt to run three more times (if it continues to fail) before giving up.

### WaitInterval

`WaitInterval` specifies the amount of time (in seconds) the scheduler will wait before attempting to retry a failed job. For instance, if a job fails and `WaitInterval` is set to 60, the scheduler will wait for 60 seconds before trying to execute the job again.

It's essential to adjust these parameters according to the nature and requirements of your specific job to ensure the most efficient recovery strategy.

## 🔍 Understanding `JobFailureConfiguration` Parameters

### TimeZoneInfo

`TimeZoneInfo` represents the time zone information for the job's schedule. It helps ensure that the job triggers according to the specified time zone, which can be crucial for jobs that cater to different global regions.

### CronSchedule

`CronSchedule` is a string representation of a cron expression that dictates when and how often the job should run. Adjust this to set the schedule for your specific job. For example, a value of `"0 0/1 * 1/1 * ? *"` would mean the job triggers every minute.

### Name

`Name` is the identifier for the `JobFailureHandler` instance. It provides a means to uniquely identify different instances of job failure handlers, especially when dealing with multiple jobs or different configurations.

It's crucial to correctly configure these parameters to ensure the smooth operation of your job scheduling and failure handling mechanisms.

## 🙌 Contributing

Contributions are welcome! If you'd like to help improve QuartzRetryMechanism, feel free to fork the repo and submit a pull request!
