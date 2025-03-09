# First stage: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Create the Data directory and ensure correct permissions
RUN mkdir -p /app/Data && chmod -R 777 /app/Data

# Copy project files and restore dependencies
COPY *.sln ./
COPY . .
RUN dotnet restore

# Copy the rest of the application and build it
COPY . .
RUN dotnet publish -c Release -o /out

# Second stage: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Ensure the Data directory exists and has correct permissions
RUN mkdir -p /app/Data && chmod -R 777 /app/Data

COPY --from=build /out .

# Expose ports for API
EXPOSE 5000
EXPOSE 443

# Start the app
ENTRYPOINT ["dotnet", "ReemRPG.dll"]
