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
        Task UploadWorkoutsAsync(bool dryRun, CancellationToken cancellationToken);
    }

    public class UploaderService : IUploaderService
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly IOptions<AppConfiguration> _appConfiguration;
        private readonly IImportFile _importFile;
        private readonly IAuthenticationService _authenticationService;

        public UploaderService(
            ILogger<UploaderService> logger,
            IOptions<AppConfiguration> options,
            IImportFile importFile,
            IAuthenticationService authenticationService)
        {
            _logger = logger;
            _appConfiguration = options;
            _importFile = importFile;
            _authenticationService = authenticationService;
        }

        public async Task UploadWorkoutsAsync(bool dryRun, CancellationToken cancellationToken)
        {
            string _pathToWorkouts = _appConfiguration.Value.PathToWorkouts;

            if (!Directory.Exists(_pathToWorkouts))
            {
                _logger.LogError($"Cannot find path to the workouts data: {_pathToWorkouts}.");
                return;
            }

            DirectoryInfo workoutsDir = new DirectoryInfo(_pathToWorkouts);

            try
            {
                int i = 0;

                foreach (DirectoryInfo workoutTypeDir in workoutsDir.EnumerateDirectories())
                {
                    _logger.LogInformation($"Found activities {workoutTypeDir.Name}");

                    foreach (FileInfo fileInfo in workoutTypeDir.GetFiles("*.tcx"))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Cancellation requested");
                            return;
                        }

                        _logger.LogInformation($"{i++}: Will upload {fileInfo.Name}");
                        WorkoutModel workoutModel = WorkoutModel.FromFile(fileInfo.FullName, dryRun);

                        bool result = await _importFile.ImportAsync(workoutModel, dryRun, cancellationToken);
                        if (!result)
                        {
                            _logger.LogError($"Could not upload workout '{_pathToWorkouts}'.");
                            return;
                        }

                        if (!dryRun)
                        {
                            // rename the file so that we wouldn't process it again in case we run the program several times
                            var destinationPath = Path.Combine(fileInfo.Directory.FullName, fileInfo.Name + ".processed");
                            File.Move(fileInfo.FullName, destinationPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: " + ex);
                return;
            }

            _logger.LogInformation("All done.");
        }
    }
}
