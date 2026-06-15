# ADR-0002: Semantic Kernel as the AI orchestration layer

- **Status:** Accepted
- **Date:** 2026-06-14

## Context
We need chat completion and embeddings against Azure OpenAI from .NET. Options considered:
1. Call the **Azure OpenAI SDK** (`Azure.AI.OpenAI`) directly.
2. Use **Semantic Kernel** (Microsoft's AI orchestration framework).
3. Use a third-party framework.

## Decision
Use **Semantic Kernel** for chat + embeddings, but hide it behind our own `IChatService` and
`IEmbeddingGenerator` interfaces in Core.

## Rationale
- Semantic Kernel is Microsoft's strategic, production-grade AI SDK for .NET and the natural growth
  path toward plugins, function calling, planners and agents (used by sibling portfolio projects).
- Wrapping it in our own interfaces means Core never references SK types — if we later move to the
  Microsoft Agent Framework or call the OpenAI SDK directly, only two adapter classes change.
- We deliberately migrated embeddings to the modern `Microsoft.Extensions.AI.IEmbeddingGenerator`
  surface (SK's older `ITextEmbeddingGenerationService` is obsolete), keeping the build warning-free.

## Consequences
- ✅ Clean upgrade path to richer SK / Agent Framework features.
- ✅ No leakage of SK types into business logic.
- ⚠️ One extra abstraction layer; accepted for the isolation it provides.
