# Use official .NET 8 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source files
COPY . ./
RUN dotnet publish -c Release -o /out

# Use lightweight .NET 8 runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy compiled DLL from build step
COPY --from=build /out ./

# Expose port 5000 (ensure your app listens on this port)
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Run the application
ENTRYPOINT ["dotnet", "TravelAd_Api.dll"]
