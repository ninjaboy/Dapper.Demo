Param(
    [ValidateNotNullOrEmpty()]
    [string]$Target="Default",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration="Release",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("linux-x64", "win-x64")]
    [string]$Runtime="win-x64"
)

$startDir=Get-Location
$buildDir=$PSScriptRoot
$solutionDir=$buildDir
$srcDir=[System.IO.Path]::Combine($solutionDir, "src")

Write-Host -ForegroundColor Green "*** Building $Configuration in $solutionDir"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Build"
dotnet build "$srcDir\Dapper.Demo.sln" --configuration $Configuration

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Unit tests"
dotnet test "$srcDir\Dapper.Demo.Test.Unit\Dapper.Demo.Test.Unit.csproj" --configuration $Configuration

Set-Location $startDir