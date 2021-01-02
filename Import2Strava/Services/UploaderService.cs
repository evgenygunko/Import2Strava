using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            string pathToWorkouts = _appConfiguration.Value.PathToWorkouts;

            if (!Directory.Exists(pathToWorkouts))
            {
                _logger.LogError($"Cannot find path to the workouts data: {pathToWorkouts}.");
                return false;
            }

            DirectoryInfo workoutsDir = new DirectoryInfo(pathToWorkouts);

            try
            {
                Console.WriteLine($"Starting uploading workouts from {pathToWorkouts}...");
                _logger.LogInformation($"Starting uploading workouts from {pathToWorkouts}...");
                int i = 0;

                foreach (DirectoryInfo workoutTypeDir in workoutsDir.EnumerateDirectories())
                {
                    FileInfo[] fileInfos = workoutTypeDir.GetFiles("*.tcx");
                    if (fileInfos.Length > 0)
                    {
                        string message = $"Found activities of type '{workoutTypeDir.Name}'";
                        _logger.LogInformation(message);
                        Console.WriteLine("===============================================");
                        Console.WriteLine(message);
                        Console.WriteLine("===============================================");
                    }

                    for (int j = 0; j < fileInfos.Length; j++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Cancellation requested");
                            return false;
                        }

                        FileInfo fileInfo = fileInfos[j];

                        string message = $"{++i}: Will upload {fileInfo.Name}";
                        Console.WriteLine(message);
                        _logger.LogInformation(message);

                        WorkoutModel workoutModel = WorkoutModel.FromFile(fileInfo.FullName, dryRun);

                        bool result = false;
                        try
                        {
                            result = await _importFile.ImportAsync(workoutModel, dryRun, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not upload workout '{fileInfo.FullName}'. Please check the log for details. Error message: " + ex.ToString());
                            return false;
                        }

                        if (!result)
                        {
                            _logger.LogError($"Could not upload workout '{fileInfo.FullName}'.");
                            Console.WriteLine($"Could not upload workout '{fileInfo.FullName}'. Please check the log for details.");
                            Console.WriteLine("Do you want to mark this workout skipped and continue? Y/n");
                            ConsoleKeyInfo cki = Console.ReadKey(true);
                            if (cki.Key != ConsoleKey.Y)
                            {
                                return false;
                            }

                            if (!dryRun)
                            {
                                MarkFileSkipped(fileInfo);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"The workout has been uploaded successfully.");

                            if (!dryRun)
                            {
                                MarkFileProcessed(fileInfo);
                            }
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

        private void MarkFileSkipped(FileInfo fileInfo)
        {
            var destinationPath = Path.Combine(fileInfo.Directory.FullName, fileInfo.Name + ".skipped");
            RenameFile(fileInfo.FullName, destinationPath);
        }

        private void MarkFileProcessed(FileInfo fileInfo)
        {
            var destinationPath = Path.Combine(fileInfo.Directory.FullName, fileInfo.Name + ".processed");
            RenameFile(fileInfo.FullName, destinationPath);
        }

        private void RenameFile(string path, string newPath)
        {
            // rename the file so that we wouldn't process it again in case we run the program several times
            _logger.LogInformation($"Renaming workout file to: '{newPath}'");
            File.Move(path, newPath);
        }
    }
}
