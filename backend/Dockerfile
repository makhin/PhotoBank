FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Restore dependencies using solution and project files
COPY PhotoBank.sln ./
COPY Directory.Build.props ./
COPY PhotoBank.Api/PhotoBank.Api.csproj PhotoBank.Api/
COPY PhotoBank.DependencyInjection/PhotoBank.DependencyInjection.csproj PhotoBank.DependencyInjection/
COPY PhotoBank.Services/PhotoBank.Services.csproj PhotoBank.Services/
COPY PhotoBank.DbContext/PhotoBank.DbContext.csproj PhotoBank.DbContext/
COPY PhotoBank.InsightFace.Client/PhotoBank.InsightFaceApiClient.csproj PhotoBank.InsightFace.Client/
COPY PhotoBank.Repositories/PhotoBank.Repositories.csproj PhotoBank.Repositories/
COPY PhotoBank.ViewModel.Dto/PhotoBank.ViewModel.Dto.csproj PhotoBank.ViewModel.Dto/
RUN dotnet restore PhotoBank.sln

# Copy the remaining source files and publish
COPY . .

RUN dotnet publish PhotoBank.Api/PhotoBank.Api.csproj \
    -c Release \
    -o /out

# Runtime ńëîé
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5066
EXPOSE 5066
ENTRYPOINT ["dotnet", "PhotoBank.Api.dll"]
