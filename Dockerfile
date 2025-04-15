FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src
COPY ["src/CurrencyConverter.Api/CurrencyConverter.Api.csproj", "src/CurrencyConverter.Api/"]
COPY ["src/CurrencyConverter.Core/CurrencyConverter.Core.csproj", "src/CurrencyConverter.Core/"]
COPY ["src/CurrencyConverter.Infrastructure/CurrencyConverter.Infrastructure.csproj", "src/CurrencyConverter.Infrastructure/"]
RUN dotnet restore "src/CurrencyConverter.Api/CurrencyConverter.Api.csproj"
COPY . .
WORKDIR "/src/src/CurrencyConverter.Api"
RUN dotnet build "CurrencyConverter.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CurrencyConverter.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p logs

# Default environment variables
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "CurrencyConverter.Api.dll"]