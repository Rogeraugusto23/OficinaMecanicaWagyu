# Etapa 1 — build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OficinaMecanicaWagyu/OficinaMecanicaWagyu.csproj OficinaMecanicaWagyu/
RUN dotnet restore OficinaMecanicaWagyu/OficinaMecanicaWagyu.csproj

COPY OficinaMecanicaWagyu/ OficinaMecanicaWagyu/
WORKDIR /src/OficinaMecanicaWagyu
RUN dotnet publish -c Release -o /app/publish

# Etapa 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OficinaMecanicaWagyu.dll"]