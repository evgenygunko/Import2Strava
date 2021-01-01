using Import2Strava.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Import2Strava.Services
{
    public interface IUserProfileService
    {
        Task<AthleteModel> GetProfileAsync(CancellationToken cancellationToken);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserProfileService> _logger;
        private readonly IAuthenticationService _authenticationService;

        public UserProfileService(
            HttpClient httpClient,
            ILogger<UserProfileService> logger,
            IAuthenticationService authenticationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authenticationService = authenticationService;
        }

        public async Task<AthleteModel> GetProfileAsync(CancellationToken cancellationToken)
        {
            string accessToken = await _authenticationService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Could not get access token, the operation is canceled.");
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var responseString = await _httpClient.GetStringAsync("/api/v3/athlete", cancellationToken);

            AthleteModel athleteModel = JsonConvert.DeserializeObject<AthleteModel>(responseString);
            return athleteModel;
        }
    }
}
