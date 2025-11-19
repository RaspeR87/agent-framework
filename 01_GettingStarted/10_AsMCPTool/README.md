brew update && brew install azure-cli
az login
az account set --subscription "<your-subscription-id>"

npx @modelcontextprotocol/inspector dotnet run