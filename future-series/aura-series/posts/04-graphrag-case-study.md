# Teaching AI to Understand Your Codebase

*A GraphRAG case study*

---

Three weeks into building Aura, I hit a wall that surprised me.

The agents could generate code. Good code, technically. Syntactically correct, logically sound. But the code didn't *fit*.

I asked the coding agent to generate a new service class. It produced something perfectly reasonable. But it used different naming conventions than my existing services. It organized methods differently. It used a different logging pattern.

The code *worked*, but integrating it required translating it to match my codebase. The "assistance" was creating work.

The problem was obvious: the agents were generating code in a vacuum. They knew how to write C#. They didn't know how to write *my* C#.

## The Knowledge Gap

This is a fundamental limitation of LLMs. They're trained on public code. They know patterns in aggregate. They don't know *your* patterns.

When a human developer joins your team, they spend weeks absorbing the codebase. They read existing code. They notice conventions. Eventually, their new code looks like existing code.

AI agents skip this onboarding. Every generation starts fresh. No absorption of conventions.

I needed a way to give agents contextual knowledge. Not just "write a service class" but "write a service class *like the ones already in this codebase*."

## Enter GraphRAG

RAG—Retrieval Augmented Generation—is the standard solution. Before generating, retrieve relevant context and include it in the prompt.

But standard RAG uses vector similarity. "Find code that talks about authentication." It doesn't capture structural relationships. "Find code that calls AuthService."

For a codebase, structure matters as much as semantics.

**GraphRAG** combines both:
- **Semantic search**: "Find code similar to JWT validation"
- **Graph traversal**: "Show me what depends on AuthService"
- **Hybrid queries**: "Find authentication code and its architectural context"

## The Implementation

**Day 1**: Extract structure from code using Roslyn. Classes, interfaces, methods, properties. Who calls whom, who implements what.

**Day 2**: Vector embeddings with pgvector for semantic similarity.

**Day 3**: Graph expansion. Given seeds from vector search, traverse relationships to add structural context.

The key insight came on Day 3. The AI's first attempt at graph traversal made a database call for each level. Correct, but inefficient.

I knew PostgreSQL supports recursive CTEs. I pushed for that:

```sql
WITH RECURSIVE context AS (
    SELECT id, name, content, 0 as depth
    FROM code_symbols WHERE id IN (...seeds...)
    UNION
    SELECT s.id, s.name, s.content, c.depth + 1
    FROM code_symbols s
    JOIN code_relationships r ON s.id = r.target_id
    JOIN context c ON r.source_id = c.id
    WHERE c.depth < 2
)
SELECT DISTINCT * FROM context
```

This is the kind of optimization AI wouldn't suggest unprompted. It requires knowing PostgreSQL supports recursive CTEs. Human domain knowledge meeting AI implementation capability.

## The Results

Before GraphRAG:
- Generated code used generic patterns
- Naming conventions didn't match
- Required 2-3 iterations to integrate

After GraphRAG:
- Generated code follows existing conventions
- Naming matches the codebase
- Usually integrates on first attempt

The improvement was immediate. Not because the LLM got smarter, but because it had the right context.

## The Honest Caveats

The current implementation has limitations. Indexing is slow. The graph is static. Embedding quality varies with local models.

These are solvable problems, but they're not solved yet. GraphRAG improved agent output quality significantly. It's not magic.

## What I Learned

**AI excels at well-defined implementation.** Give it a clear spec and it produces good code quickly.

**Humans add optimization and architectural judgment.** The recursive CTE, the service decomposition—these came from human knowledge.

**The combination is powerful.** Neither could have built this as quickly alone.

That's the pattern I kept seeing throughout Aura's development.

---

*Next: "Code I Didn't Write" — The surprising results of story-driven development*

---

*This is the fourth post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
