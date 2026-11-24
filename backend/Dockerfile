FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CorporateResponsibility.Api/CorporateResponsibility.Api.csproj CorporateResponsibility.Api/
COPY CorporateResponsibility.Api.Tests/CorporateResponsibility.Api.Tests.csproj CorporateResponsibility.Api.Tests/
RUN dotnet restore CorporateResponsibility.Api.Tests/CorporateResponsibility.Api.Tests.csproj

COPY . .
RUN dotnet publish CorporateResponsibility.Api/CorporateResponsibility.Api.csproj -c Release -o /app/publish --no-restore

FROM build AS test
RUN dotnet test CorporateResponsibility.Api.Tests/CorporateResponsibility.Api.Tests.csproj --no-restore --verbosity normal

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV PORT=5080
EXPOSE 5080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CorporateResponsibility.Api.dll"]
