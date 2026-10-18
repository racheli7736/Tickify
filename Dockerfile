# Build the API image (used by Render). Build context = repository root.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Tickify.sln ./
COPY Tickify.Core/*.csproj Tickify.Core/
COPY Tickify.Data/*.csproj Tickify.Data/
COPY Tickify.Service/*.csproj Tickify.Service/
COPY Tickify.API/*.csproj Tickify.API/
COPY Tickify.Tests/*.csproj Tickify.Tests/
RUN dotnet restore Tickify.API/Tickify.API.csproj
COPY . .
RUN dotnet publish Tickify.API/Tickify.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Render injects PORT; locally it falls back to 8080.
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Tickify.API.dll"]
