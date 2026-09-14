# Use official Microsoft .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["SmartX.API.csproj", "./"]
RUN dotnet restore "SmartX.API.csproj"

# Copy remaining source files and build
COPY . .
RUN dotnet build "SmartX.API.csproj" -c Release -o /app/build
RUN dotnet publish "SmartX.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Production runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartX.API.dll"]
