using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Import2Strava.Services
{
    public interface IUploaderService
    {
        Task<bool> UploadWorkoutsAsync(bool dryRun, CancellationToken cancellationToken);
    }

    public class UploaderService : IUploaderService
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly IOptions<AppConfiguration> _appConfiguration;
        private readonly IImportFile _importFile;

        public UploaderService(
            ILogger<UploaderService> logger,
            IOptions<AppConfiguration> options,
            IImportFile importFile)
        {
            _logger = logger;
            _appConfiguration = options;
            _importFile = importFile;
        }

        public async Task<bool> UploadWorkoutsAsync(bool dryRun, CancellationToken cancellationToken)
        {
            string _pathToWorkouts = _appConfiguration.Value.PathToWorkouts;

            if (!Directory.Exists(_pathToWorkouts))
            {
                _logger.LogError($"Cannot find path to the workouts data: {_pathToWorkouts}.");
                return false;
            }

            DirectoryInfo workoutsDir = new DirectoryInfo(_pathToWorkouts);

            try
            {
                _logger.LogInformation("Starting upload...");
                int i = 0;

                foreach (DirectoryInfo workoutTypeDir in workoutsDir.EnumerateDirectories())
                {
                    string message = $"Found activities {workoutTypeDir.Name}";
                    Console.WriteLine(message);
                    _logger.LogInformation(message);

                    foreach (FileInfo fileInfo in workoutTypeDir.GetFiles("*.tcx"))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Cancellation requested");
                            return false;
                        }

                        message = $"{i++}: Will upload {fileInfo.Name}";
                        Console.WriteLine(message);
                        _logger.LogInformation(message);

                        WorkoutModel workoutModel = WorkoutModel.FromFile(fileInfo.FullName, dryRun);

                        bool result = await _importFile.ImportAsync(workoutModel, dryRun, cancellationToken);
                        if (!result)
                        {
                            Console.WriteLine($"Could not upload workout '{fileInfo.FullName}'. Please check the log for details.");
                            _logger.LogError($"Could not upload workout '{fileInfo.FullName}'.");
                            return false;
                        }

                        Console.WriteLine($"The workout has been uploaded successfully.");

                        if (!dryRun)
                        {
                            // rename the file so that we wouldn't process it again in case we run the program several times
                            var destinationPath = Path.Combine(fileInfo.Directory.FullName, fileInfo.Name + ".processed");

                            _logger.LogInformation($"Renaming workout file to destinationPath");
                            File.Move(fileInfo.FullName, destinationPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: " + ex);
                return false;
            }

            _logger.LogInformation("Upload has finished.");

            return true;
        }
    }
}
