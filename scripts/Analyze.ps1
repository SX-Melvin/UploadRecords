param(
    [string]$ServerUrl = "http://localhost:9000",
    [string]$ProjectKey = "upload-records"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($env:SONAR_TOKEN)) {
    throw "Set SONAR_TOKEN to a SonarQube analysis token before running this script."
}

$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet sonarscanner begin "/k:$ProjectKey" "/d:sonar.host.url=$ServerUrl" "/d:sonar.token=$env:SONAR_TOKEN" /d:sonar.exclusions=appsettings.json /d:sonar.cs.opencover.reportsPaths=TestResults/coverage.opencover.xml /d:sonar.qualitygate.wait=true
    if ($LASTEXITCODE -ne 0) { throw "SonarQube initialization failed." }

    dotnet build UploadRecords.sln --no-incremental
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    dotnet test Tests/UploadRecords.Tests/UploadRecords.Tests.csproj --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=../../TestResults/coverage
    if ($LASTEXITCODE -ne 0) { throw "Tests or coverage collection failed." }

    dotnet sonarscanner end "/d:sonar.token=$env:SONAR_TOKEN"
    if ($LASTEXITCODE -ne 0) { throw "SonarQube analysis or quality gate failed." }
}
finally {
    Pop-Location
}
