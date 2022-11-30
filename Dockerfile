FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/Hosts/GenericHost/GenericHost.csproj" -c Release -o /app/

FROM base AS final
WORKDIR /app
COPY --from=build /app/ .

