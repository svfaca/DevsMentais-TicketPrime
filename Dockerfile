FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY TicketPrime.sln ./
COPY src/TicketPrime.Api/TicketPrime.Api.csproj ./src/TicketPrime.Api/

RUN dotnet restore ./src/TicketPrime.Api/TicketPrime.Api.csproj

COPY src/TicketPrime.Api/ ./src/TicketPrime.Api/

RUN dotnet publish ./src/TicketPrime.Api/TicketPrime.Api.csproj \
    -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "TicketPrime.Api.dll"]