FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AI Readiness Hub.csproj", "./"]
RUN dotnet restore "AI Readiness Hub.csproj"

COPY . .
RUN dotnet publish "AI Readiness Hub.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AI Readiness Hub.dll"]
