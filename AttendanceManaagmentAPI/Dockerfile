FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AttendanceManaagmentAPI.csproj", "./"]

RUN dotnet restore "AttendanceManaagmentAPI.csproj"

COPY . .

RUN dotnet publish "AttendanceManaagmentAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "AttendanceManaagmentAPI.dll"]