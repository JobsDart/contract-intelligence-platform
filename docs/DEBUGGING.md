# Debugging Guide

Real errors you may hit, what they mean, and how to fix them.

---

### `InvalidOperationException: Missing required configuration 'Ai:AzureOpenAI:Endpoint'`
The app starts only when Azure OpenAI is configured. Set the endpoint and key:
```powershell
cd src/ContractIntelligence.Api
dotnet user-secrets set "Ai:AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "Ai:AzureOpenAI:ApiKey"   "<key>"
```
Check what's set: `dotnet user-secrets list`.

---

### `401 Unauthorized` / `Access denied` from Azure OpenAI
- The API key is wrong or from a different resource. Re-read it:
  `az cognitiveservices account keys list -g <rg> -n <account> --query key1 -o tsv`
- The endpoint and key belong to **different** resources. They must match.

---

### `DeploymentNotFound` (404) when asking a question
The deployment **names** in config must match what exists in Azure (these are *your* deployment
names, not the model names):
```powershell
az cognitiveservices account deployment list -g <rg> -n <account> -o table
```
Align `Ai:AzureOpenAI:ChatDeployment` / `EmbeddingDeployment` with that list.

---

### `429 Too Many Requests`
You hit the tokens-per-minute quota on a deployment. Either raise `--sku-capacity`, or reduce load.
Uploading a very large contract triggers many embedding calls at once — start with the sample file.

---

### Empty or "I could not find that information" answers
- **Did ingestion succeed?** `GET /api/contracts` — the contract should be `Indexed` or `Analyzed`,
  not `Failed` (check the `error` field).
- **Scanned PDF?** PdfPig extracts *text*, not images. A scanned/image-only PDF yields no text and
  therefore nothing to retrieve. Use a text-based PDF or add OCR.
- **Wrong tenant?** If you uploaded with an `X-Tenant-Id` header, you must query with the same one.

---

### Vector dimension mismatch (Azure AI Search)
```
The vector field 'embedding' dimensions '1536' do not match the index '3072'.
```
`VectorStore:AzureAiSearch:Dimensions` must equal the embedding model's output:
`text-embedding-3-large` = **3072**, `text-embedding-3-small` / `ada-002` = **1536**. If you change
models, delete the index (or use a new `IndexName`) so it is recreated with the right dimension.

---

### Clauses table is empty after upload
Clause extraction is best-effort. If the model returns non-JSON, it's swallowed (indexing still
worked). Re-upload, or inspect by setting logging to `Debug` in `appsettings.Development.json`.
Very large contracts are truncated to 24 000 characters for the demo (see `ClauseAnalysisService`).

---

### Upload returns `400 No file uploaded`
The browser UI sends multipart form field `file`. With `curl`, use:
```bash
curl -X POST http://localhost:5080/api/contracts -F "file=@samples/sample-services-agreement.txt"
```

---

### Useful diagnostics
```powershell
# Verbose logs
$env:Logging__LogLevel__Default="Debug"; dotnet run --project src/ContractIntelligence.Api

# Confirm the API is up and see the contract list
curl http://localhost:5080/api/contracts

# Inspect the OpenAPI surface
curl http://localhost:5080/openapi/v1.json
```
