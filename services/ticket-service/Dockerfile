FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/ticket-service/NextHappen.Ticket.csproj", "services/ticket-service/"]
COPY ["shared/NextHappen.Contracts/NextHappen.Contracts.csproj", "shared/NextHappen.Contracts/"]
RUN dotnet restore "services/ticket-service/NextHappen.Ticket.csproj"
COPY services/ticket-service/ services/ticket-service/
COPY shared/NextHappen.Contracts/ shared/NextHappen.Contracts/
RUN dotnet publish "services/ticket-service/NextHappen.Ticket.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "NextHappen.Ticket.dll"]
