# Deployment Guide

Three ways to run the platform, from laptop to cloud.

---

## A. Local (in-memory) — fastest

No database, no Azure AI Search. You only need an Azure OpenAI resource.

```powershell
cd src/ContractIntelligence.Api
dotnet user-secrets set "Ai:AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "Ai:AzureOpenAI:ApiKey"   "<key>"
dotnet run
```
→ http://localhost:5080

---

## B. Provision Azure OpenAI (if you don't have it yet)

The script [`scripts/provision-azure.ps1`](../scripts/provision-azure.ps1) creates a resource group,
an Azure OpenAI account, and deploys both models. It requires the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).

```powershell
az login
./scripts/provision-azure.ps1 -ResourceGroup rg-jobsdart-ai -Location swedencentral -OpenAiName jobsdart-openai
```

What it does (also runnable by hand):
```powershell
az group create -n rg-jobsdart-ai -l swedencentral

az cognitiveservices account create -n jobsdart-openai -g rg-jobsdart-ai -l swedencentral `
  --kind OpenAI --sku S0 --custom-domain jobsdart-openai

# Deploy the chat + embedding models
az cognitiveservices account deployment create -g rg-jobsdart-ai -n jobsdart-openai `
  --deployment-name gpt-4o --model-name gpt-4o --model-format OpenAI `
  --sku-name Standard --sku-capacity 10

az cognitiveservices account deployment create -g rg-jobsdart-ai -n jobsdart-openai `
  --deployment-name text-embedding-3-large --model-name text-embedding-3-large --model-format OpenAI `
  --sku-name Standard --sku-capacity 10

# Read the endpoint + key
az cognitiveservices account show -g rg-jobsdart-ai -n jobsdart-openai --query properties.endpoint -o tsv
az cognitiveservices account keys list -g rg-jobsdart-ai -n jobsdart-openai --query key1 -o tsv
```
> **Region note:** `gpt-4o` and `text-embedding-3-large` aren't in every region. Sweden Central and
> East US 2 are safe choices. If a model version is rejected, list what's available with
> `az cognitiveservices account list-models -g rg-jobsdart-ai -n jobsdart-openai -o table`.

### Optional: Azure AI Search
```powershell
az search service create -n jobsdart-search -g rg-jobsdart-ai -l swedencentral --sku Basic
az search admin-key show -g rg-jobsdart-ai --service-name jobsdart-search --query primaryKey -o tsv
```
Then set:
```
VectorStore__Provider=AzureAiSearch
VectorStore__AzureAiSearch__Endpoint=https://jobsdart-search.search.windows.net
VectorStore__AzureAiSearch__ApiKey=<admin key>
```
The `contracts` index is created automatically on first upload.

---

## C. Docker

```powershell
docker build -t contract-intelligence:latest .
docker run -p 8080:8080 `
  -e Ai__AzureOpenAI__Endpoint="https://<resource>.openai.azure.com/" `
  -e Ai__AzureOpenAI__ApiKey="<key>" `
  contract-intelligence:latest
```
→ http://localhost:8080

This same image is what gets deployed to **Azure Container Apps** or **AKS** (see the companion
Kubernetes platform project in the portfolio).

---

## Deploy options summary

| Target | Best for | Notes |
|--------|----------|-------|
| `dotnet run` | Development, demos | In-memory store, instant |
| Docker | Reproducible local / CI | Single container |
| Azure Container Apps | Simple cloud hosting | `az containerapp up --source .` |
| AKS | Enterprise scale | Image + Helm chart (separate repo) |

---

## Production checklist

- [ ] Move secrets to **Azure Key Vault** (the app already binds from configuration)
- [ ] Switch `VectorStore:Provider` to `AzureAiSearch`
- [ ] Replace `InMemoryContractStore` with an EF Core + Azure SQL implementation
- [ ] Put **Azure API Management** or **App Gateway** in front for rate limiting + WAF
- [ ] Enable **Application Insights** for tracing the RAG pipeline
- [ ] Replace header-based tenancy with **Entra ID** auth
