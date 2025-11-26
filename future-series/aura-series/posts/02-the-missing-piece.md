# The Missing Piece

*Why LLMs need structure, and how stories provide it*

---

Let me tell you about the first few prompts I tried with Claude.

"Build me a workflow orchestration system that can execute multi-step AI-assisted development workflows."

What I got back was technically correct. A class called `WorkflowOrchestrator` with methods like `ExecuteWorkflow`. Generic, abstract, and utterly useless for my actual needs. Tutorial code, not shipping code.

So I tried being more specific. Added .NET 8, Entity Framework, GitHub integration, iterative refinement cycles.

Better. But it still felt like code written by someone who'd read the requirements but didn't understand the *problem*. The data model made assumptions I wouldn't have made. The abstractions didn't map to how I was thinking.

I could have started fixing the code. That's what most people do—generate a starting point, then spend hours reshaping it. But something about this felt wrong.

## The Bias for Action

Here's a pattern I've observed in myself and other developers: when faced with a coding task, we want to start coding. We have a bias for action. The keyboard calls to us. The dopamine hit of seeing code compile is more immediate than the satisfaction of thinking through a problem.

LLMs have inherited this bias. When you prompt them with a coding task, they want to generate code. Immediately. Whether or not they have sufficient context to generate *good* code.

This is a feature, not a bug. These models were trained on mountains of code and conversations about code. They're optimized to be helpful, and "helpful" usually means "producing something tangible quickly."

But here's what I realized: the bias for action is exactly wrong for complex software development.

When I build a substantial system, I don't start by coding. I start by understanding. What problem am I solving? What are the constraints? What patterns exist in my codebase? What decisions will be hard to reverse?

The best code I've ever written came after significant thinking. The worst code I've ever written came from diving in before I understood the problem.

If I wouldn't start a substantial project by immediately typing code, why would I expect an LLM to succeed that way?

## The Story Format

I started experimenting with a different approach. Instead of prompting for code, I wrote "stories"—structured documents capturing everything I knew about a piece of work:

```markdown
# Story Title

## Context
Why does this work need to happen? What's the background?

## Goals
What are we trying to achieve? (Specific, measurable outcomes)

## Non-Goals
What are we explicitly NOT trying to achieve?

## Design Sketch
How might we approach this? (Directional, not prescriptive)

## Acceptance Criteria
How will we know this is complete?
```

The first time I wrote a story properly, I noticed something. The act of writing forced me to think through the problem. I couldn't write "Non-Goals" without explicitly deciding what I *wouldn't* build. I couldn't write "Acceptance Criteria" without picturing what "done" looked like.

By the time I finished writing the story, I understood the problem better than I would have after an hour of exploratory coding.

## The 15-30 Minute Investment

Here's the counterintuitive part: investing 15-30 minutes writing a proper story *before* engaging with the LLM dramatically reduces total time spent.

Without the story, the cycle looks like this:
1. Prompt for code (2 minutes)
2. Review generated code (5 minutes)
3. Realize it doesn't match your mental model (immediate)
4. Re-prompt with corrections (3 minutes)
5. Start integrating, discover architectural issues (20 minutes)
6. Throw away half the code, re-prompt (3 minutes)
7. Repeat several times (60+ minutes)

With the story:
1. Write story (20 minutes)
2. Share with LLM, prompt for implementation (2 minutes)
3. Review generated code that actually matches your mental model (5 minutes)
4. Minor refinements (10 minutes)

The story acts as a contract between you and the LLM. It eliminates ambiguity. It provides context. It sets boundaries.

## Stories as Knowledge Transfer

When I shared a story with Claude, something changed in the quality of responses. Generated code referenced patterns from my existing codebase (which I'd described). Error handling matched my conventions. Abstractions fit my mental model.

The story wasn't just telling the LLM *what* to build. It was teaching it *how I think about* building.

Code generated from a proper story feels like it was written by someone who understands your project. Because, in a sense, it was. You transferred that understanding through the story.

## Try It Yourself

I know this sounds like overhead. It feels like overhead when you start. But I'd encourage you to try it once—really try it.

Pick a substantial task. Something that would take you a few hours to implement. Before you write any code, before you prompt any AI, write a story.

Force yourself to articulate: Why this work matters. What success looks like. What you're NOT building. How it fits into the larger system.

Then share that story with your AI assistant and ask for implementation.

Compare the result to what you'd have gotten from a quick prompt.

I think you'll be surprised.

---

*Next: "Aura: Architecture of an AI Accelerator" — How the pieces fit together*

---

*This is the second post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
