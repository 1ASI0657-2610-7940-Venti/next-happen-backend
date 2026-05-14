FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["gateway/api-gateway/NextHappen.Gateway.csproj", "gateway/api-gateway/"]
RUN dotnet restore "gateway/api-gateway/NextHappen.Gateway.csproj"
COPY gateway/api-gateway/ gateway/api-gateway/
RUN dotnet publish "gateway/api-gateway/NextHappen.Gateway.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "NextHappen.Gateway.dll"]
