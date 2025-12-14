# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore
COPY . .
RUN dotnet restore OnlineQuizSystem/OnlineQuizSystem.csproj

# Publish
RUN dotnet publish OnlineQuizSystem/OnlineQuizSystem.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Configure ASP.NET Core/Kestrel
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Gemini (do not bake secrets into image; use env vars at runtime)
# ENV GEMINI__APIKEY= # set via compose
# ENV GEMINI__ENDPOINT=https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent

# DB connection string via env var
# ENV ConnectionStrings__ConnectionString=Server=sqlhost;Database=OnlineQuizApp;User Id=sa;Password=your_password;TrustServerCertificate=True

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "OnlineQuizSystem.dll"]