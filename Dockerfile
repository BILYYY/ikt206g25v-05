FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Example.csproj", "./"]
RUN dotnet restore "Example.csproj"
COPY . .
# Add this line here to generate migration SQL
RUN dotnet ef migrations script -o /app/migrations.sql
RUN dotnet build "Example.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Example.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Add this line to copy the migrations SQL to the final image
COPY --from=build /app/migrations.sql .
ENTRYPOINT ["dotnet", "Example.dll"]