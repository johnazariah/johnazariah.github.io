# Aura: Architecture of an AI Accelerator

*How the pieces fit together*

---

Now that I've explained the methodology, let me show you what emerged from it.

Aura is a local-first AI development automation system. It takes GitHub issues, breaks them into executable workflows, and orchestrates multiple AI agents to make progress on them.

But describing what it *does* is less interesting than describing how it's *structured*. The architecture reflects hard lessons learned while building it.

## The Local-First Constraint

The first decision was non-negotiable: everything runs locally.

This might seem strange in 2024. Cloud APIs are convenient. They improve every few months without you doing anything.

But I work on proprietary code. Sending that code to cloud APIs raises questions—legal questions, compliance questions. I didn't want to answer those questions. I wanted to build something I could use on any codebase without needing approval from legal.

So: local-first. LLMs run via Ollama. Embeddings computed locally. Database is SQLite. No external dependencies during execution.

This constraint shaped everything that followed.

## The Component Map

```
┌─────────────────────────────────────────────────────────────┐
│                      VS Code Extension                      │
│  [Sidebar Tree View]  [Workflow Panels]  [Progress Tracking]│
└─────────────────────────────────────────────────────────────┘
                              │ HTTP
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   AgentOrchestrator.Api                     │
│  [Workflow Endpoints]  [Agent Endpoints]  [Step Execution]  │
└─────────────────────────────────────────────────────────────┘
                              │ Services
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  AgentOrchestrator.Core                     │
│  [Workflow Engine]  [Agent Registry]  [Provider Registry]   │
└─────────────────────────────────────────────────────────────┘
              │               │               │
              ▼               ▼               ▼
        [Ollama]         [SQLite]        [GitHub]
```

## The Agent System

Agents are the workers. Each has a name, capability set, model configuration, and prompt template.

Agents are defined in markdown files:

```markdown
# Coding Agent

## Capabilities
- code-generation
- refactoring

## Model
Provider: ollama
Model: qwen2.5-coder:7b

## System Prompt
You are an expert software engineer...
```

Why markdown? Human-readable, version-controllable, easy to edit. Tweak an agent's behavior by editing a text file.

## The Workflow Engine

The `WorkflowEngine` takes a work item, breaks it down into steps using the Orchestration Agent, then executes those steps using specialized agents.

```csharp
public record Workflow
{
    public required Guid Id { get; init; }
    public required string WorkItemId { get; init; }
    public required string WorkspacePath { get; init; }
    public WorkflowStatus Status { get; init; }
    public IReadOnlyList<WorkflowStep> Steps { get; init; } = [];
}
```

Workflows are declarative. The orchestration agent produces a plan, and the engine executes it. If a step fails, the engine can retry, adjust, or ask for help.

## The Provider Abstraction

Not everyone runs Ollama. The `ILlmProvider` interface abstracts this:

```csharp
public interface ILlmProvider
{
    Task<Result<string, LlmError>> GenerateAsync(string prompt, GenerationOptions options);
    Task<bool> IsModelAvailableAsync(string modelName);
}
```

The default is Ollama (local-first), but the architecture doesn't assume it.

## What's Missing

This architecture isn't complete. Notable gaps:

**RAG/Codebase Understanding**: Basic indexing exists, but sophisticated semantic search over code is work-in-progress.

**Parallel Execution**: Steps execute sequentially. Independent steps could run in parallel.

**Agent Memory**: Agents don't remember across sessions. Each execution starts fresh.

These aren't architectural limitations—they're implementation gaps. The foundations support them; the code doesn't exist yet.

## The Numbers

For the curious: 17 C# projects, 294 source files, 45 TypeScript files in the extension. All built in roughly three weeks of evening and weekend work.

Which brings me to the next post: how do you actually build something like this with AI assistance?

---

*Next: "Teaching AI to Understand Your Codebase" — A GraphRAG case study*

---

*This is the third post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
