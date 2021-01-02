using System;
using System.IO;

namespace Import2Strava.Models
{
    public class WorkoutModel
    {
        public string ActivityType { get; private set; }

        public string Name { get; private set; }

        public string DataType { get; private set; }

        public string FilePath { get; private set; }

        public static WorkoutModel FromFile(string filePath, bool dryRun)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                throw new Exception($"Cannot find file {filePath}");
            }

            WorkoutModel workoutModel = new WorkoutModel();

            string workoutType = fileInfo.Directory.Name;

            // See mapping https://developers.strava.com/docs/reference/#api-models-ActivityType
            switch (workoutType)
            {
                case "CYCLING_SPORT":
                case "CYCLING_TRANSPORTATION":
                case "MOUNTAIN_BIKING":
                    workoutModel.ActivityType = "ride";
                    break;

                case "RUNNING":
                case "TREADMILL_RUNNING":
                    workoutModel.ActivityType = "run";
                    break;

                case "SWIMMING":
                    workoutModel.ActivityType = "swim";
                    break;

                case "HIKING":
                    workoutModel.ActivityType = "hike";
                    break;

                case "WALKING":
                    workoutModel.ActivityType = "walk";
                    break;

                case "SKIING_CROSS_COUNTRY":
                    workoutModel.ActivityType = "nordicski";
                    break;

                case "SKIING_DOWNHILL":
                    workoutModel.ActivityType = "alpineski";
                    break;

                case "AEROBICS":
                case "GYMNASTICS":
                case "WEIGHT_TRAINING":
                    workoutModel.ActivityType = "weighttraining";
                    break;

                default:
                    if (dryRun)
                    {
                        throw new Exception($"Unknown workout type: {workoutType}. Please verify that you have correct mapping.");
                    }

                    workoutModel.ActivityType = "workout";
                    break;
            }

            workoutModel.Name = (char.ToUpperInvariant(workoutType[0]) + workoutType.Substring(1).ToLowerInvariant()).Replace('_', ' ');
            workoutModel.DataType = "tcx";
            workoutModel.FilePath = filePath;

            return workoutModel;
        }
    }
}
