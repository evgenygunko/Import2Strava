using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Import2Strava
{
    public interface IImportFile
    {
        Task<bool> ImportAsync(WorkoutModel workoutModel, bool dryRun, CancellationToken cancellationToken);
    }

    public class ImportFile : IImportFile
    {
        private ILogger<ImportFile> _logger;
        private HttpClient _httpClient;

        public ImportFile(HttpClient httpClient, ILogger<ImportFile> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> ImportAsync(WorkoutModel workoutModel, bool dryRun, CancellationToken cancellationToken)
        {
            if (dryRun)
            {
                return true;
            }

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(workoutModel.ActivityType), "activity_type");
                form.Add(new StringContent(workoutModel.Name), "name");
                form.Add(new StringContent(workoutModel.DataType), "data_type");

                byte[] fileContent = await File.ReadAllBytesAsync(workoutModel.FilePath, cancellationToken);
                form.Add(new ByteArrayContent(fileContent, 0, fileContent.Length), "file", new FileInfo(workoutModel.FilePath).Name);

                HttpResponseMessage response = await _httpClient.PostAsync("/api/v3/uploads", form, cancellationToken);

                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync(cancellationToken).Result;
                UploadStatusResponse uploadStatusResponse = JsonConvert.DeserializeObject<UploadStatusResponse>(json);

                if (!string.IsNullOrEmpty(uploadStatusResponse.Error))
                {
                    _logger.LogError("The API returned an error: " + uploadStatusResponse.Error);
                    return false;
                }

                await CheckStatusAsync(uploadStatusResponse.Id, cancellationToken);
            }

            return true;
        }

        private async Task<bool> CheckStatusAsync(int id, CancellationToken cancellationToken)
        {
            UploadStatusResponse uploadStatusResponse = null;

            for (int i = 0; i < 30; i++)
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"api/v3/uploads/{id}", cancellationToken);

                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync(cancellationToken).Result;
                uploadStatusResponse = JsonConvert.DeserializeObject<UploadStatusResponse>(json);

                if (!string.IsNullOrEmpty(uploadStatusResponse.Error))
                {
                    _logger.LogError("The API returned an error: " + uploadStatusResponse.Error);
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

            _logger.LogError("The API did not return success status for activity. The last response was:" + Environment.NewLine +
                "Status=" + uploadStatusResponse.Status + Environment.NewLine +
                "Error=" + uploadStatusResponse.Error + Environment.NewLine);

            return false;
        }
    }
}
