FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

RUN dotnet tool install Docker.HealthCheck --version 1.10.0 --tool-path /tools

COPY *.sln* ./
COPY Coaching/Coaching.csproj Coaching/
COPY Coaching/packages.lock.json Coaching/
COPY Coaching.Application/Coaching.Application.csproj Coaching.Application/
COPY Coaching.Application/packages.lock.json Coaching.Application/
COPY Coaching.Domain/Coaching.Domain.csproj Coaching.Domain/
COPY Coaching.Domain/packages.lock.json Coaching.Domain/
COPY Coaching.Infrastructure/Coaching.Infrastructure.csproj Coaching.Infrastructure/
COPY Coaching.Infrastructure/packages.lock.json Coaching.Infrastructure/
COPY Coaching.Tests.Unit/Coaching.Tests.Unit.csproj Coaching.Tests.Unit/
COPY Coaching.Tests.Unit/packages.lock.json Coaching.Tests.Unit/
COPY Coaching.Tests.Integration/Coaching.Tests.Integration.csproj Coaching.Tests.Integration/
COPY Coaching.Tests.Integration/packages.lock.json Coaching.Tests.Integration/
COPY shared/ shared/
RUN dotnet restore Coaching.slnx --locked-mode

COPY . .
RUN dotnet publish Coaching/Coaching.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /tools/.store/docker.healthcheck/*/*/tools/**/Docker.HealthCheck.dll /tools/Docker.HealthCheck.dll

EXPOSE 5060
EXPOSE 5061
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5060;http://+:5061

HEALTHCHECK --interval=30s --timeout=5s --retries=3 --start-period=10s \
  CMD ["dotnet", "/tools/Docker.HealthCheck.dll", "http://localhost:5060/health"]

ENTRYPOINT ["dotnet", "Coaching.dll"]
