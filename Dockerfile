﻿FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# copy everything else and build app
COPY . .
RUN dotnet build -c release -o /app
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0
COPY --from=build /app /app/JenkinsRemoteBuilder/
ENTRYPOINT ["dotnet", "/app/JenkinsRemoteBuilder/JenkinsRemoteBuilder.dll"]