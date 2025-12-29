# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Create data directory for SQLite database
RUN mkdir -p /data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE ${PORT:-8080}

# Volume for persistent SQLite database
VOLUME ["/data"]

# Run the application
ENTRYPOINT ["dotnet", "Inventory_and_Sales_Tracker.dll"]
