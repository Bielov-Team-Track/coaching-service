FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN dotnet tool install --tool-path /tools Docker.HealthCheck --version 1.10.0

COPY Directory.Build.props ./
COPY Coaching/Coaching.csproj Coaching/
COPY Coaching/packages.lock.json Coaching/
COPY Coaching.Application/Coaching.Application.csproj Coaching.Application/
COPY Coaching.Application/packages.lock.json Coaching.Application/
COPY Coaching.Domain/Coaching.Domain.csproj Coaching.Domain/
COPY Coaching.Domain/packages.lock.json Coaching.Domain/
COPY Coaching.Infrastructure/Coaching.Infrastructure.csproj Coaching.Infrastructure/
COPY Coaching.Infrastructure/packages.lock.json Coaching.Infrastructure/
COPY shared/ shared/
RUN dotnet restore Coaching/Coaching.csproj

COPY . .
RUN dotnet publish Coaching/Coaching.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /tools /tools

EXPOSE 5060
EXPOSE 5061
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5060;http://+:5061

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=30s \
  CMD ["/tools/Docker.HealthCheck", "http://localhost:5060/health"]

ENTRYPOINT ["dotnet", "Coaching.dll"]
