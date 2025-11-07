$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet swagger tofile --yaml --output "..\..\openapi.yaml" "bin\Debug\net9.0\PhotoBank.Api.dll" v1
