using Import2Strava.Models;
using Import2Strava.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
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
                    // To save the secrets in the secret storage, run this command in the directory with your project file:
                    // dotnet user-secrets set "Strava:ClientId" "YOURCLIENTID".
                    // dotnet user-secrets set "Strava:ClientSecret" "YOURCLIENTSECRET".
                    // Where "YOURCLIENTID" and "YOURCLIENTSECRET" you should copy from your app configuration on STrava portal: https://www.strava.com/settings/api
                    // The command below loads configuration for Secret Storage and names it available with a call: configuration["Strava:ClientId"]
                    configuration.AddUserSecrets<WorkerService>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.Configure<AppConfiguration>(hostContext.Configuration.GetSection("Application"));

                    services.AddSingleton<IUploaderService, UploaderService>();
                    services.AddSingleton<IAuthenticationService, AuthenticationService>();

                    services.AddHttpClient<IImportFile, ImportFile>(client =>
                    {
                        client.BaseAddress = new Uri("https://www.strava.com");
                    });
                    services.AddHttpClient<IUserProfileService, UserProfileService>(client =>
                    {
                        client.BaseAddress = new Uri("https://www.strava.com");
                    });

                    services.AddHostedService<WorkerService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.ClearProviders();

                    configLogging.AddDebug();
                    configLogging.AddNLog(hostContext.Configuration);
                })
                .UseConsoleLifetime();
    }
}
