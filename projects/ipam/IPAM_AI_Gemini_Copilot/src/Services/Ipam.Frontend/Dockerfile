
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Ipam.Frontend/Ipam.Frontend.csproj", "src/Services/Ipam.Frontend/"]
COPY ["src/Services/Ipam.DataAccess/Ipam.DataAccess.csproj", "src/Services/Ipam.DataAccess/"]
COPY ["src/Shared/Ipam.Core/Ipam.Core.csproj", "src/Shared/Ipam.Core/"]
COPY ["src/Shared/Ipam.Dto/Ipam.Dto.csproj", "src/Shared/Ipam.Dto/"]
RUN dotnet restore "src/Services/Ipam.Frontend/Ipam.Frontend.csproj"
COPY .
WORKDIR "/src/src/Services/Ipam.Frontend"
RUN dotnet build "Ipam.Frontend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ipam.Frontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ipam.Frontend.dll"]
