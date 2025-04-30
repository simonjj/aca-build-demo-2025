FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ChaosDragonApi.csproj", "./"]
RUN dotnet restore

# Copy the rest of the files and build
COPY . .
RUN dotnet publish -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published output
COPY --from=build /app .

# Configure environment variables
ENV ASPNETCORE_URLS=http://+:3002
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check with proper interval for the chaotic nature of the dragon
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget -qO- http://localhost:3002/healthz || exit 1

EXPOSE 3002

ENTRYPOINT ["dotnet", "ChaosDragonApi.dll"]