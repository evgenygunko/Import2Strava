using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Import2Strava.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Import2Strava.Services
{
    public class WorkerService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ILogger<WorkerService> _logger;
        private IArchiveParser _archiveParser;
        private IUploaderService _uploaderService;
        private IUserProfileService _userProfileService;
        private IAuthenticationService _authenticationService;

        public WorkerService(
            IHostApplicationLifetime applicationLifetime,
            ILogger<WorkerService> logger,
            IArchiveParser archiveParser,
            IUploaderService uploaderService,
            IUserProfileService userProfileService,
            IAuthenticationService authenticationService)
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _archiveParser = archiveParser;
            _uploaderService = uploaderService;
            _userProfileService = userProfileService;
            _authenticationService = authenticationService;
        }

        #region Public Methods

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancellation requested");
                    break;
                }

                Console.WriteLine("Please select an action:");
                Console.WriteLine("    0: exit");
                Console.WriteLine("    1: parse workouts");
                Console.WriteLine("    2: get profile");
                Console.WriteLine("    3: test run");
                Console.WriteLine("    4: upload activities");

                ConsoleKeyInfo cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.D1:
                        ParseWorkouts();
                        break;

                    case ConsoleKey.D2:
                        await GetProfileAsync(cancellationToken);
                        break;

                    case ConsoleKey.D3:
                        await UploadWorkoutsAsync(true, cancellationToken);
                        break;

                    case ConsoleKey.D4:
                        await UploadWorkoutsAsync(false, cancellationToken);
                        break;

                    case ConsoleKey.D0:
                    case ConsoleKey.Escape:
                        _applicationLifetime.StopApplication();
                        return;
                }

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}{0}Press any key to continue...", Environment.NewLine));
                Console.ReadKey(true);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void ParseWorkouts()
        {
            try
            {
                bool result = _archiveParser.ParseWorkouts();
                if (result)
                {
                    Console.WriteLine("Finished parsing.");
                }
                else
                {
                    Console.WriteLine("Parsing ended with errors.");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "An error occurred: " + ex;
                Console.WriteLine(errorMessage);
                _logger.LogError(errorMessage);
            }
        }

        private async Task GetProfileAsync(CancellationToken cancellationToken)
        {
            try
            {
                AthleteModel athleteModel = await _userProfileService.GetProfileAsync(cancellationToken);

                if (athleteModel != null)
                {
                    Console.WriteLine("Found athlete profile:");
                    Console.WriteLine($"Name: {athleteModel.FirstName} {athleteModel.LastName}");
                    Console.WriteLine($"Id: {athleteModel.Id}");
                    Console.WriteLine($"Country: {athleteModel.Country}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "An error occurred: " + ex;
                Console.WriteLine(errorMessage);
                _logger.LogError(errorMessage);
            }
        }

        private async Task UploadWorkoutsAsync(bool dryRun, CancellationToken cancellationToken)
        {
            try
            {
                bool result = await _uploaderService.UploadWorkoutsAsync(dryRun, cancellationToken);
                if (result)
                {
                    Console.WriteLine("Upload has finished.");
                }
                else
                {
                    Console.WriteLine("Upload was not successful.");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "An error occurred: " + ex;
                Console.WriteLine(errorMessage);
                _logger.LogError(errorMessage);
            }
        }

        #endregion
    }
}
