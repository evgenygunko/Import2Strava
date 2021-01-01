using Import2Strava.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Import2Strava.Services
{
    public class WorkerService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private ILogger<WorkerService> _logger;
        private IUploaderService _uploaderService;
        private IUserProfileService _userProfileService;
        private IAuthenticationService _authenticationService;

        public WorkerService(
            IHostApplicationLifetime applicationLifetime,
            ILogger<WorkerService> logger,
            IUploaderService uploaderService,
            IUserProfileService userProfileService,
            IAuthenticationService authenticationService)
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
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
                Console.WriteLine("    1: get profile");
                Console.WriteLine("    2: test run");
                Console.WriteLine("    3: upload activities");

                ConsoleKeyInfo cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.D1:
                        await GetProfileAsync(cancellationToken);
                        break;

                    case ConsoleKey.D2:
                        await UploadWorkoutsAsync(true, cancellationToken);
                        break;

                    case ConsoleKey.D3:
                        await UploadWorkoutsAsync(false, cancellationToken);
                        break;

                    case ConsoleKey.D0:
                    case ConsoleKey.Escape:
                        _applicationLifetime.StopApplication();
                        return;
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task GetProfileAsync(CancellationToken cancellationToken)
        {
            try
            {
                AthleteModel athleteModel = await _userProfileService.GetProfileAsync(cancellationToken);

                Console.WriteLine("Found athlete profile:");
                Console.WriteLine($"Name: {athleteModel.FirstName} {athleteModel.LastName}");
                Console.WriteLine($"Id: {athleteModel.Id}");
                Console.WriteLine($"Country: {athleteModel.Country}");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: " + ex);
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
                _logger.LogError("An error occurred: " + ex);
            }
        }

        #endregion
    }
}
