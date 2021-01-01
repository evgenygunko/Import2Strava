// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Reviewed>", Scope = "member", Target = "~M:Import2Strava.Models.WorkoutModel.FromFile(System.String,System.Boolean)~Import2Strava.Models.WorkoutModel")]
[assembly: SuppressMessage("Performance", "CA1835:Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'", Justification = "<An example of code by Google>", Scope = "member", Target = "~M:Import2Strava.Services.AuthenticationService.PerformCodeExchangeAsync(System.String,System.String,System.String)")]
[assembly: SuppressMessage("Reliability", "CA2008:Do not create tasks without passing a TaskScheduler", Justification = "<Pending>", Scope = "member", Target = "~M:Import2Strava.Services.AuthenticationService.AuthenticateAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<An example of code by Google>", Scope = "member", Target = "~M:Import2Strava.Services.ImportFile.ImportAsync(Import2Strava.Models.WorkoutModel,System.Boolean,System.Threading.CancellationToken)~System.Threading.Tasks.Task{System.Boolean}")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<An example of code by Google>", Scope = "member", Target = "~M:Import2Strava.Services.AuthenticationService.AuthenticateAsync~System.Threading.Tasks.Task")]
