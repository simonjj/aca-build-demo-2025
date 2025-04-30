FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["EmoOctoApi.csproj", "./"]
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
ENV ASPNETCORE_URLS=http://+:3004
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DAPR_HTTP_PORT=3500
ENV DAPR_GRPC_PORT=60001

# Health check - explicitly using /healthz endpoint
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:3004/healthz || exit 1

EXPOSE 3004

ENTRYPOINT ["dotnet", "EmoOctoApi.dll"]