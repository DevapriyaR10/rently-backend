# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore
COPY . .
RUN dotnet restore "rently-backend.csproj"
RUN dotnet publish "rently-backend.csproj" -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render provides a PORT environment variable, so listen on it
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 10000

ENTRYPOINT ["dotnet", "rently-backend.dll"]
