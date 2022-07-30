FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Otakurin.Application/Otakurin.Application.csproj", "Otakurin.Application/"]
RUN dotnet restore "Otakurin.Application/Otakurin.Application.csproj"
COPY . .
WORKDIR "/src/Otakurin.Application"
RUN dotnet build "Otakurin.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Otakurin.Application.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Otakurin.Application.dll"]