<#
.SYNOPSIS
    Deploy the Contract Intelligence Platform to Azure Container Apps and print a public
    HTTPS URL. Container Apps scales to zero when idle, so a demo costs almost nothing on
    the free $200 credit.
.PREREQUISITES
    Azure CLI installed + `az login`. Run this from the REPO ROOT (where the Dockerfile is).
.EXAMPLE
    ./scripts/deploy-containerapp.ps1 -OpenAiEndpoint "https://<res>.openai.azure.com/" -OpenAiKey "<key>"
#>
param(
    [Parameter(Mandatory = $true)] [string] $OpenAiEndpoint,
    [Parameter(Mandatory = $true)] [string] $OpenAiKey,
    [string] $ResourceGroup = "rg-jobsdart-ai",
    [string] $Environment   = "jobsdart-aca",
    [string] $AppName       = "contract-intelligence",
    [string] $Location      = "swedencentral"
)
$ErrorActionPreference = "Stop"

az extension add --name containerapp --upgrade --only-show-errors 2>$null

# `up` builds the image (via ACR), creates the environment, and deploys — one command.
az containerapp up `
    --name $AppName `
    --resource-group $ResourceGroup `
    --location $Location `
    --environment $Environment `
    --source . `
    --ingress external `
    --target-port 8080 `
    --env-vars "Ai__AzureOpenAI__Endpoint=$OpenAiEndpoint" "Ai__AzureOpenAI__ApiKey=$OpenAiKey"

$fqdn = az containerapp show -n $AppName -g $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
Write-Host "`n================ LIVE ================" -ForegroundColor Green
Write-Host "https://$fqdn" -ForegroundColor Green
Write-Host "Tear down later with: az group delete -n $ResourceGroup --yes --no-wait" -ForegroundColor Yellow
