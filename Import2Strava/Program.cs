using Import2Strava.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Import2Strava
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddHttpClient<IProfileDataService, ProfileDataService>(client =>
            {
                client.BaseAddress = new Uri("https://www.strava.com");

                string accessToken = "123";
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            });

            builder.Services.AddHttpClient<IImportFile, ImportFile>(client =>
            {
                client.BaseAddress = new Uri("https://www.strava.com");

                string accessToken = "123";
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            });

            builder.Services.AddSingleton<IUploaderService, UploaderService>();

            builder.Services.AddMatBlazor();

            await builder.Build().RunAsync();
        }
    }
}
