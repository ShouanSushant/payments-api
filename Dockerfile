# Three stages: build the React dashboard with Node, build the API with the
# .NET SDK, then copy only the published app into a small runtime image.

# ---- 1. Dashboard ----
FROM node:22-alpine AS client
WORKDIR /app/client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
# Vite writes the build to ../src/PaymentsApi/wwwroot (/app/src/PaymentsApi/wwwroot).
RUN npm run build

# ---- 2. API ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/PaymentsApi/PaymentsApi.csproj src/PaymentsApi/
RUN dotnet restore src/PaymentsApi/PaymentsApi.csproj
COPY src/ src/
COPY --from=client /app/src/PaymentsApi/wwwroot src/PaymentsApi/wwwroot
RUN dotnet publish src/PaymentsApi/PaymentsApi.csproj -c Release -o /out --no-restore

# ---- 3. Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# The SQLite file lives in /data so it can be kept in a volume between runs.
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Payments="Data Source=/data/payments.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentsApi.dll"]
