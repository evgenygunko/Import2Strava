using Import2Strava.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Import2Strava
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    configuration
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                    // Save access token in secret storage, see https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows
                    // To save the key in the secret storage, run this command in the directory with your project file: dotnet user-secrets set "Strava:AccessToken" "your_token".
                    // The command below loads configuration for Secret Storage and names it available with a call: configuration["Strava:AccessToken"]
                    configuration.AddUserSecrets<UploaderService>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.Configure<AppConfiguration>(hostContext.Configuration.GetSection("Application"));

                    services.AddHttpClient<IImportFile, ImportFile>(client =>
                    {
                        client.BaseAddress = new Uri("https://www.strava.com");

                        IConfiguration configuration = hostContext.Configuration;
                        string accessToken = configuration["Strava:AccessToken"];

                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    });

                    services.AddHostedService<UploaderService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();

                })
                .UseConsoleLifetime();
    }
}
