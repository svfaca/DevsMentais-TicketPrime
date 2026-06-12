FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY TicketPrime.sln ./
COPY src/Backend/Backend.csproj ./src/Backend/

RUN dotnet restore ./src/Backend/Backend.csproj

COPY src/Backend/ ./src/Backend/
COPY src/frontend/imagens/ ./src/Backend/wwwroot/imagens/

RUN dotnet publish ./src/Backend/Backend.csproj \
    -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# ✅ ASPNETCORE_ENVIRONMENT definido para produção
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Backend.dll"]