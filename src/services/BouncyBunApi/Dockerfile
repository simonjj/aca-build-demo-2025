FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["*.csproj", "./"]
RUN dotnet restore

# Copy & publish
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser \
    && chown -R appuser /app
USER appuser

# Copy published output
COPY --from=build /app .

# Tell Kestrel to bind to port 3000
ENV ASPNETCORE_URLS=http://+:3000  
ENV ASPNETCORE_ENVIRONMENT=Production

# Docker health check against /healthz
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
    CMD wget -qO- http://localhost:3000/healthz || exit 1

# Expose the actual listening port
EXPOSE 3000

ENTRYPOINT ["dotnet", "BouncyBunApi.dll"]