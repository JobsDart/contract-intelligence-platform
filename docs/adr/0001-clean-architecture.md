# ADR-0001: Clean Architecture with a dependency-free Core

- **Status:** Accepted
- **Date:** 2026-06-14

## Context
This is an AI application whose infrastructure (LLM provider, vector database, document parser) is
expected to change — Azure OpenAI today, possibly a local model tomorrow; in-memory now, Azure AI
Search in production. We need the business logic to survive those changes and to be testable without
provisioning cloud resources.

## Decision
Adopt Clean Architecture with three projects:
- **Core** — domain model, use-case services, and interfaces. **No external NuGet dependencies.**
- **Infrastructure** — implementations of the Core interfaces (Semantic Kernel, PdfPig, Azure).
- **Api** — the ASP.NET Core host.

Dependencies point inward only. The composition root (`DependencyInjection.AddContractIntelligence`)
wires concrete implementations chosen from configuration.

## Consequences
- ✅ Business logic is unit-testable with in-memory fakes — no Azure account required.
- ✅ Swapping a vector store or LLM is an Infrastructure-only change plus one config value.
- ✅ The dependency rule is enforced by project references (Core literally cannot reference Azure).
- ⚠️ Slightly more ceremony (interfaces + adapters) than a single-project app — justified here by the
  high rate of expected infrastructure change.
