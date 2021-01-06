namespace Import2Strava.Models
{
    public class AppConfiguration
    {
        /// <summary>
        /// Gets or sets path to archive with workouts.
        /// </summary>
        public string PathToArchive { get; set; }

        /// <summary>
        /// Gets or sets path to the folder where the workouts will be extracted.
        /// </summary>
        public string PathToWorkouts { get; set; }
    }
}
