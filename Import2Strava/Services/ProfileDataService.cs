using Import2Strava.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Import2Strava.Services
{
    public interface IProfileDataService
    {
        Task<AthleteModel> GetProfileAsync();
    }

    public class ProfileDataService : IProfileDataService
    {
        private HttpClient _httpClient;

        public ProfileDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AthleteModel> GetProfileAsync()
        {
            var responseString = await _httpClient.GetStringAsync("/api/v3/athlete");

            AthleteModel athleteModel = JsonConvert.DeserializeObject<AthleteModel>(responseString);
            return athleteModel;
        }
    }
}
