# --- Build Vue frontend ---
FROM oven/bun:latest AS frontend
WORKDIR /web
COPY src/TikkieTerug.Web/package.json src/TikkieTerug.Web/bun.lock ./
RUN bun install --frozen-lockfile
COPY src/TikkieTerug.Web/ .
RUN bun run build

# --- Build .NET API ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/TikkieTerug.Api/TikkieTerug.Api.csproj", "src/TikkieTerug.Api/"]
RUN dotnet restore "src/TikkieTerug.Api/TikkieTerug.Api.csproj"
COPY src/TikkieTerug.Api/ src/TikkieTerug.Api/
WORKDIR "/src/src/TikkieTerug.Api"
RUN dotnet publish "TikkieTerug.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend /web/dist ./wwwroot/
EXPOSE 8080
ENTRYPOINT ["dotnet", "TikkieTerug.Api.dll"]
