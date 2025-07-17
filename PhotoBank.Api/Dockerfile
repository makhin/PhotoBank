FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY PhotoBank.Api.csproj ./
RUN dotnet restore PhotoBank.Api.csproj

# Копируем всё остальное и билдим
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime слой
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5066
EXPOSE 5066
ENTRYPOINT ["dotnet", "PhotoBank.Api.dll"]
