FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/backend/Imova.Domain/Imova.Domain.csproj src/backend/Imova.Domain/
COPY src/backend/Imova.Contracts/Imova.Contracts.csproj src/backend/Imova.Contracts/
COPY src/backend/Imova.Infrastructure/Imova.Infrastructure.csproj src/backend/Imova.Infrastructure/
COPY src/backend/Imova.Application/Imova.Application.csproj src/backend/Imova.Application/
COPY src/backend/Imova.Api/Imova.Api.csproj src/backend/Imova.Api/
# linux-x64 only: Magick.NET (photo sizes) ships ImageMagick for eight platforms, ~240 MB that
# a portable publish would copy into the image.
RUN dotnet restore src/backend/Imova.Api/Imova.Api.csproj -r linux-x64

COPY src/backend/Imova.Domain src/backend/Imova.Domain
COPY src/backend/Imova.Contracts src/backend/Imova.Contracts
COPY src/backend/Imova.Infrastructure src/backend/Imova.Infrastructure
COPY src/backend/Imova.Application src/backend/Imova.Application
COPY src/backend/Imova.Api src/backend/Imova.Api
RUN dotnet publish src/backend/Imova.Api/Imova.Api.csproj -c Release -r linux-x64 --self-contained false -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Imova.Api.dll"]
