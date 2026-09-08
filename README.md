# Tests and SonarQube

Run the tests and generate OpenCover coverage:

```powershell
dotnet test Tests/UploadRecords.Tests/UploadRecords.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=../../TestResults/coverage
```

The tests use simulated HTTP responses and temporary files. They do not connect to
OTCS, SQL Server, or SMTP, and do not read production registry credentials.

To build, test, upload coverage, and wait for the quality gate:

```powershell
$env:SONAR_TOKEN = "YOUR_TOKEN"
./scripts/Analyze.ps1
```

The script requires the `dotnet-sonarscanner` tool and defaults to
`http://localhost:9000` with project key `upload-records`. Coverage is written to
`TestResults/coverage.opencover.xml`. The gate and coverage thresholds remain unchanged.

# Container build

The Dockerfile runs build, publish, and runtime stages as the non-root `app` user.
The build context allows only the project file and C# sources. Local configuration,
credentials, logs, Git data, and SonarQube output are excluded from the image.
Supply `appsettings.json` at `/app/appsettings.json` using a read-only runtime mount,
and provide writable mounts for the configured log and audit paths.

The application currently loads credentials from the Windows registry using DPAPI.
The Linux image can be built, but running the batch job requires replacing that
Windows-only credential source with a Linux-compatible provider first.
