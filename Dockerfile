# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER app
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
RUN mkdir -p /src/obj /src/bin /app && chown app:app /src/obj /src/bin /app
USER app
WORKDIR /src
COPY --chown=root:root --chmod=0444 ["UploadRecords.csproj", "."]
RUN dotnet restore "./UploadRecords.csproj"
# Source files stay root-owned and read-only; app writes only to obj, bin, and /app.
# Copy only build inputs; runtime configuration is mounted at deployment.
COPY --chown=root:root --chmod=0444 Program.cs ./
COPY --chown=root:root --chmod=0444 Configs/*.cs ./Configs/
COPY --chown=root:root --chmod=0444 Enums/*.cs ./Enums/
COPY --chown=root:root --chmod=0444 Models/*.cs ./Models/
COPY --chown=root:root --chmod=0444 Models/API/*.cs ./Models/API/
COPY --chown=root:root --chmod=0444 Models/Db/*.cs ./Models/Db/
COPY --chown=root:root --chmod=0444 Services/*.cs ./Services/
COPY --chown=root:root --chmod=0444 Utils/*.cs ./Utils/
WORKDIR "/src/."
RUN dotnet build "./UploadRecords.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
USER app
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UploadRecords.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
USER app
WORKDIR /app
# This directory contains only generated publish artifacts from the allowed build inputs.
COPY --from=publish --chown=root:root --chmod=0555 /app/publish .
ENTRYPOINT ["dotnet", "UploadRecords.dll"]
