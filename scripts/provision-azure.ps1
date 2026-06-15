<#
.SYNOPSIS
    Provisions Azure OpenAI (and optionally Azure AI Search) for the Contract
    Intelligence Platform, deploys the chat + embedding models, and prints the
    settings you need.

.PREREQUISITES
    - Azure CLI:  https://learn.microsoft.com/cli/azure/install-azure-cli
    - Run `az login` first.

.EXAMPLE
    ./provision-azure.ps1 -ResourceGroup rg-jobsdart-ai -Location swedencentral -OpenAiName jobsdart-openai

.EXAMPLE
    ./provision-azure.ps1 -ResourceGroup rg-jobsdart-ai -Location swedencentral -OpenAiName jobsdart-openai -WithSearch
#>
param(
    [Parameter(Mandatory = $true)] [string] $ResourceGroup,
    [string] $Location   = "swedencentral",
    [Parameter(Mandatory = $true)] [string] $OpenAiName,
    [string] $ChatModel  = "gpt-4o",
    [string] $EmbedModel = "text-embedding-3-large",
    [switch] $WithSearch,
    [string] $SearchName = "jobsdart-search"
)

$ErrorActionPreference = "Stop"

Write-Host "==> Resource group '$ResourceGroup' in '$Location'" -ForegroundColor Cyan
az group create -n $ResourceGroup -l $Location | Out-Null

Write-Host "==> Azure OpenAI account '$OpenAiName'" -ForegroundColor Cyan
az cognitiveservices account create -n $OpenAiName -g $ResourceGroup -l $Location `
    --kind OpenAI --sku S0 --custom-domain $OpenAiName --yes | Out-Null

Write-Host "==> Deploying chat model '$ChatModel'" -ForegroundColor Cyan
az cognitiveservices account deployment create -g $ResourceGroup -n $OpenAiName `
    --deployment-name $ChatModel --model-name $ChatModel --model-format OpenAI `
    --sku-name Standard --sku-capacity 10 | Out-Null

Write-Host "==> Deploying embedding model '$EmbedModel'" -ForegroundColor Cyan
az cognitiveservices account deployment create -g $ResourceGroup -n $OpenAiName `
    --deployment-name $EmbedModel --model-name $EmbedModel --model-format OpenAI `
    --sku-name Standard --sku-capacity 10 | Out-Null

$endpoint = az cognitiveservices account show -g $ResourceGroup -n $OpenAiName --query properties.endpoint -o tsv
$key      = az cognitiveservices account keys list -g $ResourceGroup -n $OpenAiName --query key1 -o tsv

$searchEndpoint = ""
$searchKey      = ""
if ($WithSearch) {
    Write-Host "==> Azure AI Search '$SearchName' (Basic)" -ForegroundColor Cyan
    az search service create -n $SearchName -g $ResourceGroup -l $Location --sku Basic | Out-Null
    $searchEndpoint = "https://$SearchName.search.windows.net"
    $searchKey = az search admin-key show -g $ResourceGroup --service-name $SearchName --query primaryKey -o tsv
}

Write-Host ""
Write-Host "================ DONE ================" -ForegroundColor Green
Write-Host "Set these (from src/ContractIntelligence.Api):" -ForegroundColor Green
Write-Host ""
Write-Host "  dotnet user-secrets set `"Ai:AzureOpenAI:Endpoint`" `"$endpoint`""
Write-Host "  dotnet user-secrets set `"Ai:AzureOpenAI:ApiKey`"   `"$key`""
Write-Host "  dotnet user-secrets set `"Ai:AzureOpenAI:ChatDeployment`" `"$ChatModel`""
Write-Host "  dotnet user-secrets set `"Ai:AzureOpenAI:EmbeddingDeployment`" `"$EmbedModel`""
if ($WithSearch) {
    Write-Host "  dotnet user-secrets set `"VectorStore:Provider`" `"AzureAiSearch`""
    Write-Host "  dotnet user-secrets set `"VectorStore:AzureAiSearch:Endpoint`" `"$searchEndpoint`""
    Write-Host "  dotnet user-secrets set `"VectorStore:AzureAiSearch:ApiKey`"   `"$searchKey`""
}
Write-Host ""
Write-Host "If a model/version is unavailable in your region, list options with:" -ForegroundColor Yellow
Write-Host "  az cognitiveservices account list-models -g $ResourceGroup -n $OpenAiName -o table"
