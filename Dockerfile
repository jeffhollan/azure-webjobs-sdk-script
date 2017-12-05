FROM microsoft/aspnetcore-build:2.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
# TODO - actually 
COPY . ./
RUN dotnet restore WebJobs.Script.sln

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out WebJobs.Script.sln

# Build runtime image
FROM microsoft/aspnetcore:2.0
WORKDIR /app
COPY --from=build-env /app/src/WebJobs.Script.K8Host/out .
ENTRYPOINT ["dotnet", "WebJobs.Script.K8Host.dll"]
