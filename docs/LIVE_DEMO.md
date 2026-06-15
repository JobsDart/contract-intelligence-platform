# Live Demo — get a public URL recruiters can click

Three zero/low-cost ways to put this app on a public URL.

## Option 1 (recommended): Azure Container Apps — uses your free $200 credit
Container Apps scales to **zero** when idle, so an idle demo costs ~nothing.

```powershell
az login
# from the repo root:
./scripts/deploy-containerapp.ps1 `
  -OpenAiEndpoint "https://<your-resource>.openai.azure.com/" `
  -OpenAiKey "<your-key>"
```
The script builds the image (ACR), creates the environment, deploys, and prints
`https://contract-intelligence.<region>.azurecontainerapps.io`. Share that URL.

Tear down: `az group delete -n rg-jobsdart-ai --yes --no-wait`.

## Option 2: GitHub Codespaces (free 60 hrs/month)
Repo → **Code ▸ Codespaces ▸ Create**. In the terminal:
```bash
cd src/ContractIntelligence.Api
dotnet user-secrets set "Ai:AzureOpenAI:Endpoint" "https://<res>.openai.azure.com/"
dotnet user-secrets set "Ai:AzureOpenAI:ApiKey" "<key>"
dotnet run
```
Set the forwarded port (5080) visibility to **Public** → shareable URL.

## Option 3: Railway / Render (free tier)
Connect the GitHub repo, it builds the Dockerfile automatically. Set env vars
`Ai__AzureOpenAI__Endpoint` and `Ai__AzureOpenAI__ApiKey`. Deploy → public URL.

> Security note: the deploy script passes the key as a plain env var for simplicity. For anything
> long-lived, store it as a Container Apps secret (`--secrets`) and reference it with `secretref:`.
