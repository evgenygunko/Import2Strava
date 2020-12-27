using Import2Strava.Models;
using Import2Strava.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Import2Strava.Pages
{
    public partial class Index
    {
        [Inject]
        private IProfileDataService ProfileDataService { get; set; }

        public AthleteModel Athlete { get; set; }

        public string Summary { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Athlete = await ProfileDataService.GetProfileAsync();
        }
    }
}
