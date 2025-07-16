# build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# копируем только csproj и восстанавливаем зависимости
COPY PhotoBank.Api/PhotoBank.Api.csproj PhotoBank.Api/
RUN dotnet restore PhotoBank.Api/PhotoBank.Api.csproj

# копируем остальной код
COPY . .

# билдим и публикуем
WORKDIR /src/PhotoBank.Api
RUN dotnet publish -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5066
EXPOSE 5066
ENTRYPOINT ["dotnet", "PhotoBank.Api.dll"]
