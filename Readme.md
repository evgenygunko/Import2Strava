# Import2Strava

## Description
This is a console tool for parsing workouts from [Endomondo](https://www.endomondo.com/) archive and uploading them to [Strava](https://www.strava.com/).

UnderArmour, which owned Endomondo, decided to close Endomondo tracker end of 2020 and offered all users an archive with their activities. AFter some research, I have decided to import my activities to Strava to continue tracking my runs there. Strava is a big social network for runners, so it is likely you have some of your friends there already.

An Endomondo archive has all data that a use entered into Endomondo: list of friends, photos, weights, workout data. We are only interested in workout data.

The workouts have 2 files: a json file with description and a tcx file with gps coordinates. Although tcx file has a tag for sport type, it is not accurate - it only has values of "running" and "other". The json file on the other hand has a sport type identifier that you selected when you were saving your workout: "running", "hiking", "weight training", etc. So we will parse sport type from json file and then save tcx file into appropriate folder.

The reason for saving workouts in different folders is that I also imported my activities to Garmin Connect. Garmin doesn't allow 3rd party developers to write any data via API, it only allows read-only access. You can only import activities manually via browser, and for that it is nice to have files in separate folders, so that you would know which sport type to select as a destination on Garmin upload page.

## Installation
1. Download binaries from Release page on GitHub
2. You need .NET 5 SDK to run the app: https://dotnet.microsoft.com/download/dotnet/5.0
3. The app is compiled on Ubuntu build agent in Azure Devops, so you will not see an "exe" file in the release folder. (But as a bonus you can run this app on Windows, Mac or Linux!)
To launch the app, you need to run this command:
```csharp
dotnet run "<path to the folder with binaries>\Import2Strava.dll"
```

## Settings
The tool requires the following settings (see appsettings.json)
1. **PathToArchive**: absolute path to the file with Endomondo archive
2. **PathToWorkouts**: absolute path to the folder, where the workouts will be extracted. If the folder does not exist, it will be created automatically.

In addition, you need to register a "3rd party app" on Strava portal so that you use their API, see below.

## Strava API and security keys
In order to import data into Strava, you need to register a "3rd party application". See more info in the official guide: https://developers.strava.com/docs/getting-started.

You will need Client Id and Client Secret in order to import data. As these value need to be protected, we do not save them in settings - instead you should save them in a secret storage.
Run these commands:
```csharp
dotnet user-secrets set "Strava:ClientId" "your_app_clientid"
dotnet user-secrets set "Strava:ClientSecret" "your_app_client_secret"
```

## Main window and options
![Main Window](https://github.com/evgenygunko/Import2Strava/blob/assets/MainWindow.png?raw=true)

1. **parse workouts**: parses the file with Endomondo archive and saves workouts (tcx files) in the "PathToWorkouts" folder. Each workout will be saved in a subfolder with a specific sport type.
2. **get profile**: get a public info for your user from Strava. It is useful for checking that you configured "3rd party app" correctly.
3. **test run**: emulates uploading to Strava, but doesn't actually import files. This is useful for checking the mapping between Endomondo workouts and Strava activities.
4. **upload activities**: uploads activities to Strava
0. **exit**: quits the application

## Strava API rate limits
Strava API usage is limited on a per-application basis using both a 15-minute and daily request limit. The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day.
If you have many workouts, you will likely hit the limit. In this case wait 15 minutes and then try again. We mark workouts which have been processed, so you can continue from the point where you got stopped.