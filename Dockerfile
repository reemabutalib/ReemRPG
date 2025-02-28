# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY *.sln ./
COPY . .
RUN dotnet restore

# Copy the rest of the application and build it
COPY . .
RUN dotnet publish -c Release -o /out

# Second stage: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Expose ports for API
EXPOSE 5000
EXPOSE 443

# Start the app
ENTRYPOINT ["dotnet", "ReemRPG.dll"]
