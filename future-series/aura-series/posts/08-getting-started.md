# Getting Started

*A practical guide to story-driven development*

---

You've read seven posts about how I built Aura. Now the obvious question: how do you try this yourself?

No philosophy here. Just steps.

## Option 1: Try Story-Driven Development (Without Aura)

You don't need Aura. The methodology works with just Claude, ChatGPT, or any capable LLM.

### Step 1: Pick a Feature

Something real but bounded. Not "rewrite authentication"—too big. Not "add a log statement"—too small. Something like "add a user preferences endpoint."

### Step 2: Write the Story

```markdown
# [Feature Name]

## Context
Why does this feature need to exist?

## Goals
What specifically should this accomplish?

## Non-Goals
What are you explicitly NOT building?

## Design Sketch
How might this work?

## Acceptance Criteria
How will you know this is done?
```

Spend 15-30 minutes. It's not overhead—it's the work.

### Step 3: Share Context

When you prompt the AI, include:
1. The story you wrote
2. Relevant existing code (files, patterns)
3. Your project's conventions

### Step 4: Review and Iterate

The AI generates code. Review it. Does it match your specification? Follow your patterns? Have obvious bugs?

If wrong, be specific: "the error handling doesn't match our pattern—we use Result<T> not exceptions."

### Step 5: Test and Integrate

Generate tests. Run them. Fix issues. Integrate.

Repeat for the next feature.

## Option 2: Try Aura

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- VS Code
- Ollama

### Setup

```bash
git clone https://github.com/johnazariah/aura.git
cd aura

# Install Ollama from ollama.ai, then:
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text

# Start backend
cd src/AgentOrchestrator.Api
dotnet run

# In another terminal, install extension
cd extension
npm install
npm run compile
# Press F5 in VS Code to launch Extension Development Host
```

### Using Aura

1. Select a GitHub issue or create manual workflow
2. Click "Digest" to have Aura extract and formalize requirements
3. Review the structured story—edit until you're happy with it
4. Click "Orchestrate" to break down into steps
5. Execute steps individually or let them flow
6. Review output in the workflow panel

Here's the thing: you don't have to get the story perfect. Aura's first step is the `issue-digester` agent, which uses an LLM to extract the semantics from whatever you wrote and format it into a proper structured story. Write a rough description, let the digester formalize it, then refine until the structured version matches what you actually want.

This is important. The barrier to entry isn't "write a perfect specification." It's "write something close enough that an LLM can understand your intent." Much lower bar.

## Writing Effective Stories

**Be specific about patterns:**
```markdown
## Design Sketch
Follow the Repository pattern in `UserRepository.cs`.
Use Result<T> for error handling.
```

**Define non-goals explicitly:**
```markdown
## Non-Goals
- Not handling authentication (assume user authenticated)
- Not implementing pagination (separate story)
```

**Make acceptance criteria testable:**
```markdown
## Acceptance Criteria
- [ ] Returns 200 with valid user ID
- [ ] Returns 404 with invalid user ID
- [ ] Response matches UserDto schema
```

## Common Pitfalls

**Too vague**: "Make the code better" produces random changes.

**Too big**: Stories taking more than a few hours are too big. Break them down.

**Missing context**: AI doesn't know your codebase unless you show it.

**Not reviewing**: AI-generated code needs review. Always.

**Expecting perfection**: First-try success is ~80%. Plan for iteration.

## Series Summary

Over eight posts:

1. **The skeptic's question**: Can AI actually accelerate development?
2. **The methodology**: Story-driven development
3. **The architecture**: How Aura's pieces fit
4. **The case study**: Building GraphRAG
5. **The results**: Building without writing implementation code
6. **The challenges**: What went wrong
7. **The recursion**: Using Aura to improve Aura
8. **The practice**: How to try it yourself

The bottom line: AI can significantly accelerate development, but methodology matters. Unstructured prompting produces mediocre results. Story-driven development produces useful output.

The investment is 15-30 minutes of specification per feature. The return is dramatically reduced implementation time.

Try it once. See what happens.

---

*This is the final post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
