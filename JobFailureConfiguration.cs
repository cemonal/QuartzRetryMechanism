using System;

namespace QuartzRetryMechanism
{
    /// <summary>
    /// Contains configuration related to the job failure handler.
    /// </summary>
    public class JobFailureConfiguration
    {
        /// <summary>
        /// Gets or sets the time zone information to be used for scheduling. 
        /// This can be null if the default time zone is to be used.
        /// </summary>
        public TimeZoneInfo TimeZoneInfo { get; set; }

        /// <summary>
        /// Gets or sets the cron schedule expression representing the main job schedule.
        /// The cron schedule is a string expression that represents a schedule in a time-based job-scheduling format.
        /// </summary>
        public string CronSchedule { get; set; }

        /// <summary>
        /// Gets or sets the unique name for the job failure handler. 
        /// This name is used to identify the job failure handler instance within the Quartz scheduler.
        /// </summary>
        public string Name { get; set; }
    }
}
