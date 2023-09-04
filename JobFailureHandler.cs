using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace QuartzRetryMechanism
{
    /// <summary>
    /// JobFailureHandler serves as a listener for jobs in Quartz.NET to implement a retry mechanism on job failures.
    /// </summary>
    public class JobFailureHandler : IJobListener
    {
        public string Name => _configuration.Name;
        private readonly ILogger<JobFailureHandler>? _logger;
        private readonly JobFailureConfiguration _configuration;
        private bool mainSchedule = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFailureHandler"/> class.
        /// </summary>
        /// <param name="configuration">Configuration related to job failure handling.</param>
        /// <param name="logger">The logger to use for logging errors and messages.</param>
        public JobFailureHandler(JobFailureConfiguration configuration, ILogger<JobFailureHandler>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (!context.JobDetail.JobDataMap.Contains(context.JobDetail.Key.Name))
                context.JobDetail.JobDataMap.Put(context.JobDetail.Key.Name, 0);

            var numberTries = context.JobDetail.JobDataMap.GetIntValue(context.JobDetail.Key.Name);
            context.JobDetail.JobDataMap.Put(context.JobDetail.Key.Name, ++numberTries);

            return Task.CompletedTask;
        }

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            if (jobException == null)
            {
                ResetRetryCount(context);

                if (!mainSchedule)
                    await RescheduleJobWithMainCron(context, cancellationToken);

                return;
            }

            await HandleJobFailure(context, jobException, cancellationToken);
        }

        private void ResetRetryCount(IJobExecutionContext context)
        {
            context.JobDetail.JobDataMap.Put(context.JobDetail.Key.Name, 0);
        }

        private async Task RescheduleJobWithMainCron(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            var periodicallyTrigger = _configuration.TimeZoneInfo == null ?
                   TriggerBuilder.Create().WithCronSchedule(_configuration.CronSchedule).Build() :
                   TriggerBuilder.Create().WithCronSchedule(_configuration.CronSchedule, x => x.InTimeZone(_configuration.TimeZoneInfo)).Build();

            await context.Scheduler.RescheduleJob(context.Trigger.Key, periodicallyTrigger, cancellationToken).ConfigureAwait(false);
            mainSchedule = true;
        }

        private async Task HandleJobFailure(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var numTries = dataMap.GetIntValue(context.JobDetail.Key.Name);
            var maxRetries = dataMap.GetInt("MaxRetries");
            var waitInterval = dataMap.GetInt("WaitInterval");

            if (numTries > maxRetries)
            {
                _logger?.LogWarning($"Job with ID and type: {context.JobDetail.Key}, {context.JobDetail.JobType} has run {maxRetries} times and has failed each time.");
                ResetRetryCount(context);
                await RescheduleJobWithMainCron(context, cancellationToken);
                return;
            }

            var retryTrigger = TriggerBuilder
                    .Create()
                    .WithIdentity(Guid.NewGuid().ToString(), context.JobDetail.Key.Group)
                    .StartAt(DateTime.Now.AddSeconds(waitInterval))
                    .Build();

            _logger?.LogError($"Job with ID and type: {context.JobDetail.Key}, {context.JobDetail.JobType} has thrown the exception: {jobException.InnerException?.Message}. Running again in {waitInterval} seconds. Retry count: {numTries}");

            await context.Scheduler.RescheduleJob(context.Trigger.Key, retryTrigger, cancellationToken).ConfigureAwait(false);
            mainSchedule = false;
        }
    }
}