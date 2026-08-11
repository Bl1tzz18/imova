FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Imova.Domain/Imova.Domain.csproj Imova.Domain/
COPY Imova.Infrastructure/Imova.Infrastructure.csproj Imova.Infrastructure/
COPY Imova.Api/Imova.Api.csproj Imova.Api/
RUN dotnet restore Imova.Api/Imova.Api.csproj

COPY . .
RUN dotnet publish Imova.Api/Imova.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Imova.Api.dll"]
