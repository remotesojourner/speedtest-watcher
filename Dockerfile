FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SpeedtestWatcher.slnx ./
COPY src/SpeedtestWatcher.Core/SpeedtestWatcher.Core.csproj src/SpeedtestWatcher.Core/
COPY src/SpeedtestWatcher.Infrastructure/SpeedtestWatcher.Infrastructure.csproj src/SpeedtestWatcher.Infrastructure/
COPY src/SpeedtestWatcher.Web/SpeedtestWatcher.Web.csproj src/SpeedtestWatcher.Web/
COPY tests/SpeedtestWatcher.Tests/SpeedtestWatcher.Tests.csproj tests/SpeedtestWatcher.Tests/

RUN dotnet restore src/SpeedtestWatcher.Web/SpeedtestWatcher.Web.csproj

COPY . .
WORKDIR /src/src/SpeedtestWatcher.Web
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    fontconfig \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/data /app/bin

ARG PORT=2003
ENV PORT=${PORT}
ENV DOTNET_RUNNING_IN_CONTAINER=true

VOLUME ["/app/data", "/app/bin"]
EXPOSE ${PORT}

ENTRYPOINT ["dotnet", "SpeedtestWatcher.Web.dll"]
