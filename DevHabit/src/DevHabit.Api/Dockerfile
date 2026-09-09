FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy Directory.*.props from DevHabit root (build context is DevHabit/ via docker-compose)
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
# Copy project file
COPY ["src/DevHabit.Api/DevHabit.Api.csproj", "src/DevHabit.Api/"]
RUN dotnet restore "src/DevHabit.Api/DevHabit.Api.csproj"
# Copy all source files
COPY . .
# Build from the project directory
WORKDIR "/src/src/DevHabit.Api"
RUN dotnet build "DevHabit.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DevHabit.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevHabit.Api.dll"]
