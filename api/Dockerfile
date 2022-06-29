FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /usr/local/src
COPY . .
RUN dotnet test ./tests/tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
RUN dotnet build ./src/api.csproj --configuration Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS api
WORKDIR /usr/local/bin
COPY --from=BUILD /usr/local/src/src/bin/Release/net6.0/. .
CMD ["dotnet", "api.dll", "--urls", "http://0.0.0.0:9090" ]