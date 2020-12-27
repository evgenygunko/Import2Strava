﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Reviewed>", Scope = "member", Target = "~M:Import2Strava.UploaderService.StartAsync(System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Reviewed>", Scope = "member", Target = "~M:Import2Strava.ImportFile.ImportAsync(Import2Strava.Models.WorkoutModel,System.Boolean,System.Threading.CancellationToken)~System.Threading.Tasks.Task{System.Boolean}")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Reviewed>", Scope = "member", Target = "~M:Import2Strava.Models.WorkoutModel.FromFile(System.String,System.Boolean)~Import2Strava.Models.WorkoutModel")]