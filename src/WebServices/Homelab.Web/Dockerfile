# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the web project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebServices/Homelab.Web/Homelab.Web.csproj", "WebServices/Homelab.Web/"]
COPY ["Common/Homelab.Domain/Homelab.Domain.csproj", "Common/Homelab.Domain/"]
COPY ["Common/Homelab.ServiceDefaults/Homelab.ServiceDefaults.csproj", "Common/Homelab.ServiceDefaults/"]
COPY ["WebServices/Homelab.Web.Services/Homelab.Web.Services.csproj", "WebServices/Homelab.Web.Services/"]
COPY ["WebServices/Homelab.Web.Gateway/Homelab.Web.Gateway.csproj", "WebServices/Homelab.Web.Gateway/"]
RUN dotnet restore "./WebServices/Homelab.Web/Homelab.Web.csproj"
COPY . .
WORKDIR "/src/WebServices/Homelab.Web"
RUN dotnet build "./Homelab.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the web project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Homelab.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Homelab.Web.dll"]
