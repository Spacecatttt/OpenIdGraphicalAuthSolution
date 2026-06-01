FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["OpenIdProvider.Blazor/OpenIdProvider.Blazor.csproj", "OpenIdProvider.Blazor/"]
COPY ["OpenIdProvider.Data/OpenIdProvider.Data.csproj", "OpenIdProvider.Data/"]
RUN dotnet restore "OpenIdProvider.Blazor/OpenIdProvider.Blazor.csproj"

COPY ["OpenIdProvider.Blazor/", "OpenIdProvider.Blazor/"]
COPY ["OpenIdProvider.Data/", "OpenIdProvider.Data/"]
WORKDIR "/src/OpenIdProvider.Blazor"

RUN dotnet publish "OpenIdProvider.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R app:app /app
USER app

ENTRYPOINT ["dotnet", "OpenIdProvider.Blazor.dll"]