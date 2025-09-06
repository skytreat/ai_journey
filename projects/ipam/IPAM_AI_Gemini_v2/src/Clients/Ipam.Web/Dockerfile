
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Clients/Ipam.Web/Ipam.Web.csproj", "src/Clients/Ipam.Web/"]
COPY ["src/Clients/Ipam.Client/Ipam.Client.csproj", "src/Clients/Ipam.Client/"]
COPY ["src/Shared/Ipam.Dto/Ipam.Dto.csproj", "src/Shared/Ipam.Dto/"]
RUN dotnet restore "src/Clients/Ipam.Web/Ipam.Web.csproj"
COPY .
WORKDIR "/src/src/Clients/Ipam.Web"
RUN dotnet build "Ipam.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ipam.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ipam.Web.dll"]
