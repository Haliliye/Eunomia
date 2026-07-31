# Multi-stage build: the SDK image (large, has the compiler) only exists to
# produce a published output; the final image is the much smaller ASP.NET
# runtime image with just that output — this is what actually gets deployed
# and run in production (Render, or anywhere else that builds from a
# Dockerfile). The docker-compose.yml at the repo root is a *different*,
# dev-only setup (mounts source, runs `dotnet run`) — this file is the one
# real hosting platforms use.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the .csproj files first so Docker can cache the `dotnet restore`
# layer — it only re-runs when a project actually adds/removes a package
# reference, not on every source code change.
COPY src/TodoApp.Domain/TodoApp.Domain.csproj src/TodoApp.Domain/
COPY src/TodoApp.Application/TodoApp.Application.csproj src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj src/TodoApp.Infrastructure/
COPY src/TodoApp.Api/TodoApp.Api.csproj src/TodoApp.Api/
RUN dotnet restore src/TodoApp.Api/TodoApp.Api.csproj

# Now copy everything else and publish.
COPY src/ src/
RUN dotnet publish src/TodoApp.Api/TodoApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Fixed container-internal port — Render (and most container platforms)
# detect this via EXPOSE and route external traffic to it, regardless of
# whatever public port the platform itself uses. ASPNETCORE_HTTP_PORTS is
# the .NET 8 shorthand for "listen on this port over plain HTTP" — TLS
# termination happens at the platform's edge (Render, etc.), not here.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]
