using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Import2Strava.Services
{
    public interface IArchiveParser
    {
        bool ParseWorkouts();
    }

    public class ArchiveParser : IArchiveParser
    {
        private record WorkoutEntry(string sportType, string fileName);

        private readonly ILogger<ArchiveParser> _logger;
        private readonly IOptions<AppConfiguration> _appConfiguration;

        public ArchiveParser(
            ILogger<ArchiveParser> logger,
            IOptions<AppConfiguration> options)
        {
            _logger = logger;
            _appConfiguration = options;
        }

        public bool ParseWorkouts()
        {
            string pathToArchive = _appConfiguration.Value.PathToArchive;
            string pathToWorkouts = _appConfiguration.Value.PathToWorkouts;

            if (!File.Exists(pathToArchive))
            {
                _logger.LogError($"Cannot find file with archive: {pathToArchive}");
            }

            if (string.IsNullOrEmpty(pathToWorkouts))
            {
                _logger.LogError($"Path to workouts folder cannot be empty: {pathToWorkouts}.");
                return false;
            }

            WriteMessage($"Staring parsing archive file {pathToArchive}...");

            var workoutEntries = new List<WorkoutEntry>();
            int i = 0;

            using (ZipArchive archive = ZipFile.OpenRead(pathToArchive))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(Path.GetDirectoryName(entry.FullName), "Workouts", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogInformation($"Trying to find a sport type in {entry.FullName}");
                        string sportType = GetSportType(entry);
                        _logger.LogInformation($"Found '{sportType}'");

                        workoutEntries.Add(new WorkoutEntry(sportType, Path.GetFileNameWithoutExtension(entry.FullName)));
                    }
                }

                if (workoutEntries.Count > 0)
                {
                    WriteMessage($"Found {workoutEntries.Count} json files");

                    if (Directory.Exists(pathToWorkouts))
                    {
                        WriteMessage($"Deleting directory '{pathToWorkouts}'");
                        Directory.Delete(pathToWorkouts, true);
                    }

                    WriteMessage($"Creating directory '{pathToWorkouts}'");
                    Directory.CreateDirectory(pathToWorkouts);
                }

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".tcx", StringComparison.InvariantCultureIgnoreCase) &&
                        string.Equals(Path.GetDirectoryName(entry.FullName), "Workouts", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(entry.FullName);
                        WorkoutEntry workoutEntry = workoutEntries.FirstOrDefault(x => x.fileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));

                        if (workoutEntry != null)
                        {
                            string destinationPath = Path.Combine(pathToWorkouts, workoutEntry.sportType, fileName.Replace(':', '_') + $".tcx");
                            WriteMessage($"{++i}: Saving workout '{entry.FullName}' to {destinationPath}");

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                            entry.ExtractToFile(destinationPath);
                        }
                    }
                }
            }

            WriteMessage("DONE");

            return true;
        }

        private string GetSportType(ZipArchiveEntry jsonArchiveEntry)
        {
            using (StreamReader sr = new StreamReader(jsonArchiveEntry.Open()))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JObject jObject = JObject.Load(reader);
                        if (jObject.ContainsKey("sport"))
                        {
                            return jObject.Value<string>("sport");
                        }
                    }
                }
            }

            _logger.LogWarning($"Could not find 'sport' token in {jsonArchiveEntry.FullName}");

            return null;
        }

        private void WriteMessage(string message)
        {
            Console.WriteLine(message);
            _logger.LogInformation(message);
        }
    }
}
