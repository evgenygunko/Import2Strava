﻿using Import2Strava.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Import2Strava
{
    public class UploaderService : IHostedService
    {
        private ILogger<UploaderService> _logger;
        private IImportFile _importFile;
        private string _pathToWorkouts;
        private bool _dryRun;

        public UploaderService(ILogger<UploaderService> logger,
            IOptions<AppConfiguration> options,
            IImportFile importFile)
        {
            _logger = logger;
            _importFile = importFile;

            _pathToWorkouts = options.Value.PathToWorkouts;
            _dryRun = options.Value.DryRun;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_pathToWorkouts))
            {
                _logger.LogError($"Cannot find path to the workouts data: {_pathToWorkouts}. Please configure it in the appsetting.json and restart the application.");
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
                        WorkoutModel workoutModel = WorkoutModel.FromFile(fileInfo.FullName, _dryRun);

                        bool result = await _importFile.ImportAsync(workoutModel, _dryRun, cancellationToken);
                        if (!result)
                        {
                            _logger.LogError($"Could not upload workout '{_pathToWorkouts}'. Please investigate the error and restart the application.");
                            return;
                        }

                        if (!_dryRun)
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
                _logger.LogError("Please investigate the error and restart the application.");
                return;
            }

            _logger.LogInformation("All done.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}