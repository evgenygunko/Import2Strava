<#
.SYNOPSIS
    Script to updating project version.
.DESCRIPTION
    Script will update version for all csharp projects.
.PARAMETER mode
    Specify a value for the version
.EXAMPLE
    UpdateVersion.ps1 "1.2.3.4"
#>

[cmdletbinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$version
)

$file = Get-ChildItem -Path $PSScriptRoot/../Directory.Build.props

if ($file) {
    Write-Host "Found Directory.Build.props file"

    $xml = [xml](Get-Content $file)
    [bool]$updated = $false

    $xml.GetElementsByTagName("Version") | ForEach-Object{
        Write-Host "Updating Version to:" $version
        $_."#text" = $version

        $updated = $true
    }

    if ($updated) {
        $xml.Save($file.FullName)
        Write-Host "Directory.Build.props file has been updated"
    } else {
        Write-Host "Version property not found in the Directory.Build.props file"
    }
}