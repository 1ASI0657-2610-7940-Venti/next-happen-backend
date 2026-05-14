FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/iam-service/NextHappen.IAM.csproj", "services/iam-service/"]
COPY ["shared/NextHappen.Contracts/NextHappen.Contracts.csproj", "shared/NextHappen.Contracts/"]
RUN dotnet restore "services/iam-service/NextHappen.IAM.csproj"
COPY services/iam-service/ services/iam-service/
COPY shared/NextHappen.Contracts/ shared/NextHappen.Contracts/
RUN dotnet publish "services/iam-service/NextHappen.IAM.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "NextHappen.IAM.dll"]
