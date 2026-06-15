# ADR-0003: Swappable in-memory / Azure AI Search vector store

- **Status:** Accepted
- **Date:** 2026-06-14

## Context
RAG needs a vector store. For a public demo we want it to run with **zero setup** so a reviewer can
try it in one minute. For production we need a managed, scalable, tenant-filtered store.

## Decision
Define a single `IVectorStore` interface with two implementations selected by configuration
(`VectorStore:Provider`):
- **InMemoryVectorStore** — cosine similarity over an in-process list. Default. Zero dependencies.
- **AzureAiSearchVectorStore** — HNSW vector search on Azure AI Search, with a `tenantId` filter and
  automatic index creation.

## Consequences
- ✅ `dotnet run` works with no database — ideal for demos, tests and CI.
- ✅ Production turns on Azure AI Search with one config value; no code change elsewhere.
- ✅ Tenant isolation is enforced inside **both** implementations, not bolted on.
- ⚠️ In-memory data is volatile (lost on restart) — acceptable and intended for non-production use.
- ⚠️ The two stores must keep behaviour aligned (e.g. tenant filtering); covered by the shared
  interface contract and tests.
