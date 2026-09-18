FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["PortfolioContactApi.csproj", "./"]

RUN dotnet restore "PortfolioContactApi.csproj"

COPY . .

RUN dotnet publish "PortfolioContactApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "PortfolioContactApi.dll"]