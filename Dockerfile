FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AppointmentService.Api/AppointmentService.Api.csproj", "AppointmentService.Api/"]
RUN dotnet restore "AppointmentService.Api/AppointmentService.Api.csproj"

COPY . .
RUN dotnet publish "AppointmentService.Api/AppointmentService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5263
EXPOSE 5263

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AppointmentService.Api.dll"]
