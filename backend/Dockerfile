FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# �������� ���� backend ���
COPY . .

# �������������� � ������
RUN dotnet restore PhotoBank.Api/PhotoBank.Api.csproj
RUN dotnet publish PhotoBank.Api/PhotoBank.Api.csproj -c Release -o /out

# Runtime ����
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5066
EXPOSE 5066
ENTRYPOINT ["dotnet", "PhotoBank.Api.dll"]
