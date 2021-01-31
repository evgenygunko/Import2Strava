using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Import2Strava.Services
{
    public interface IImportFile
    {
        Task<bool> ImportAsync(WorkoutModel workoutModel, bool dryRun, CancellationToken cancellationToken);
    }

    public class ImportFile : IImportFile
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImportFile> _logger;
        private readonly IAuthenticationService _authenticationService;

        public ImportFile(
            HttpClient httpClient,
            ILogger<ImportFile> logger,
            IAuthenticationService authenticationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authenticationService = authenticationService;
        }

        public async Task<bool> ImportAsync(WorkoutModel workoutModel, bool dryRun, CancellationToken cancellationToken)
        {
            if (workoutModel == null)
            {
                throw new ArgumentNullException(nameof(workoutModel));
            }

            if (dryRun)
            {
                return true;
            }

            string accessToken = await _authenticationService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Could not get access token, the operation is canceled.");
                _logger.LogWarning("Could not get access token, the operation is canceled.");
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            UploadStatusResponse uploadStatusResponse = null;

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(workoutModel.ActivityType), "activity_type");
                form.Add(new StringContent(workoutModel.Name), "name");
                form.Add(new StringContent(workoutModel.DataType), "data_type");

                byte[] fileContent = await File.ReadAllBytesAsync(workoutModel.FilePath, cancellationToken);
                form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), "file", new FileInfo(workoutModel.FilePath).Name);

                HttpResponseMessage response = await _httpClient.PostAsync(new Uri("/api/v3/uploads", UriKind.Relative), form, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await PauseForStravaTimeoutAsync(cancellationToken);
                    response = await _httpClient.PostAsync(new Uri("/api/v3/uploads", UriKind.Relative), form, cancellationToken);
                }

                if (!CheckSuccessStatusCode(response))
                {
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug(json);
                uploadStatusResponse = JsonConvert.DeserializeObject<UploadStatusResponse>(json);

                if (!string.IsNullOrEmpty(uploadStatusResponse.Error))
                {
                    _logger.LogError($"The API returned an error: '{uploadStatusResponse.Error}'");
                    return false;
                }
            }

            return await CheckStatusAsync(uploadStatusResponse.Id, cancellationToken);
        }

        private async Task<bool> CheckStatusAsync(long id, CancellationToken cancellationToken)
        {
            UploadStatusResponse uploadStatusResponse = null;
            string errorMessage;

            for (int i = 0; i < 30; i++)
            {
                HttpResponseMessage response = await _httpClient.GetAsync(new Uri($"api/v3/uploads/{id}", UriKind.Relative), cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await PauseForStravaTimeoutAsync(cancellationToken);
                    response = await _httpClient.GetAsync(new Uri($"api/v3/uploads/{id}", UriKind.Relative), cancellationToken);
                }

                if (!CheckSuccessStatusCode(response))
                {
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug(json);
                uploadStatusResponse = JsonConvert.DeserializeObject<UploadStatusResponse>(json);

                if (!string.IsNullOrEmpty(uploadStatusResponse.Error))
                {
                    errorMessage = "The API returned an error: " + uploadStatusResponse.Error;
                    Console.WriteLine(errorMessage);
                    _logger.LogError(errorMessage);
                    return false;
                }

                if (uploadStatusResponse.ActivityId.HasValue || uploadStatusResponse.Status.Contains("Your activity is ready", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                // From documentation: https://developers.strava.com/docs/uploads/
                // "Strava recommends polling no more than once a second. The mean processing time is around 8 seconds."
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            errorMessage = "The API did not return success status for activity.The last response was:" + Environment.NewLine +
                "Status=" + uploadStatusResponse.Status + Environment.NewLine +
                "Error=" + uploadStatusResponse.Error;
            Console.WriteLine(errorMessage);
            _logger.LogError(errorMessage);

            return false;
        }

        private bool CheckSuccessStatusCode(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                string errorMessage = "The server returned an error: 'TooManyRequests'." + Environment.NewLine +
                    "Strava API usage is limited on a per-application basis using both a 15-minute and daily request limit. The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day." + Environment.NewLine +
                    "Please try again later.";
                Console.WriteLine(errorMessage);
                _logger.LogWarning(errorMessage);
                return false;
            }

            response.EnsureSuccessStatusCode();

            return true;
        }

        private async Task PauseForStravaTimeoutAsync(CancellationToken cancellationToken)
        {
            string errorMessage = "The server returned an error: 'TooManyRequests'." + Environment.NewLine +
                        "Strava API usage is limited on a per-application basis using both a 15-minute and daily request limit. The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day." + Environment.NewLine;
            Console.WriteLine(errorMessage);
            _logger.LogWarning(errorMessage);

            for (int i = 16; i > 0; i--)
            {
                errorMessage = $"Waiting {i} minutes before continue...";
                Console.WriteLine(errorMessage);
                _logger.LogWarning(errorMessage);

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }

            errorMessage = "Repeating the last operation...";

            Console.WriteLine(errorMessage);
            _logger.LogWarning(errorMessage);
        }
    }
}
