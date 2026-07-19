FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Customers/Customers.Api/Customers.Api.csproj", "Customers/Customers.Api/"]
COPY ["Customers/Customers.Core/Customers.Core.csproj", "Customers/Customers.Core/"]
RUN dotnet restore "Customers/Customers.Api/Customers.Api.csproj"
COPY . .
WORKDIR "/src/Customers/Customers.Api"
RUN dotnet build "Customers.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Customers.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Customers.Api.dll"]
