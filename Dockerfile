# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy csproj files first for layer caching, then restore
COPY CategorizeIt/CategorizeIt.sln ./CategorizeIt/
COPY CategorizeIt/src/CategorizeIt.API/CategorizeIt.API.csproj                     ./CategorizeIt/src/CategorizeIt.API/
COPY CategorizeIt/src/CategorizeIt.Application/CategorizeIt.Application.csproj     ./CategorizeIt/src/CategorizeIt.Application/
COPY CategorizeIt/src/CategorizeIt.Domain/CategorizeIt.Domain.csproj              ./CategorizeIt/src/CategorizeIt.Domain/
COPY CategorizeIt/src/CategorizeIt.Infrastructure/CategorizeIt.Infrastructure.csproj ./CategorizeIt/src/CategorizeIt.Infrastructure/

RUN dotnet restore CategorizeIt/src/CategorizeIt.API/CategorizeIt.API.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish CategorizeIt/src/CategorizeIt.API/CategorizeIt.API.csproj -c Release -o /app /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Render provides PORT; bind to it
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CategorizeIt.API.dll"]