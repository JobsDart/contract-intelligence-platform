# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first to leverage Docker layer caching on restore.
COPY ContractIntelligence.sln .
COPY src/ContractIntelligence.Core/ContractIntelligence.Core.csproj src/ContractIntelligence.Core/
COPY src/ContractIntelligence.Infrastructure/ContractIntelligence.Infrastructure.csproj src/ContractIntelligence.Infrastructure/
COPY src/ContractIntelligence.Api/ContractIntelligence.Api.csproj src/ContractIntelligence.Api/
RUN dotnet restore src/ContractIntelligence.Api/ContractIntelligence.Api.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish src/ContractIntelligence.Api/ContractIntelligence.Api.csproj -c Release -o /app /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContractIntelligence.Api.dll"]
