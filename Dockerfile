# Build stage: restore first (cached while the .csproj files don't change), then publish.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Helsi.TodoApi.Domain/Helsi.TodoApi.Domain.csproj src/Helsi.TodoApi.Domain/
COPY src/Helsi.TodoApi.Application/Helsi.TodoApi.Application.csproj src/Helsi.TodoApi.Application/
COPY src/Helsi.TodoApi.Infrastructure/Helsi.TodoApi.Infrastructure.csproj src/Helsi.TodoApi.Infrastructure/
COPY src/Helsi.TodoApi.Api/Helsi.TodoApi.Api.csproj src/Helsi.TodoApi.Api/
RUN dotnet restore src/Helsi.TodoApi.Api/Helsi.TodoApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/Helsi.TodoApi.Api/Helsi.TodoApi.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage: only the ASP.NET runtime and the published app, running as a non-root user.
# The aspnet:8.0 image already listens on port 8080 (ASPNETCORE_HTTP_PORTS).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Helsi.TodoApi.Api.dll"]
