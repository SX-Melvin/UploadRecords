# SonarQube
```
$env:SONAR_TOKEN="YOUR_TOKEN"
```

```
dotnet sonarscanner begin /k:"upload-records" /d:sonar.host.url="http://localhost:9000" /d:sonar.exclusions="appsettings.json" /d:sonar.token="$env:SONAR_TOKEN"; dotnet build --no-incremental; dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
```