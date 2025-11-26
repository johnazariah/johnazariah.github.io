# The Challenges

*What went wrong, what remains unsolved, and honest lessons learned*

---

I've spent five posts telling you what worked. Now let me tell you what didn't.

Every project has rough edges. Story-driven development isn't magic. The real test of any methodology is whether you'd use it again knowing everything you know now.

I would. But I'd go in with different expectations.

## The Solved Problems

Some challenges had solutions. Not always elegant ones, but solutions.

**Context window limits.** LLMs have limited context windows. Even with 200K tokens, you can't dump an entire codebase into a prompt. Early on, conversations would truncate, context would be lost, responses became inconsistent.

*Solution*: Structured context management. Each story includes just enough context for that piece of work. GraphRAG retrieves relevant code snippets rather than everything. Conversations reset between stories rather than accumulating indefinitely. This requires discipline—you have to explicitly specify what context matters for each task.

**Inconsistent output quality.** The same prompt doesn't always produce the same quality output. Sometimes AI nails it first try. Sometimes it produces something subtly wrong that takes longer to debug than writing it yourself.

*Partial solution*: More detailed stories reduce variance. The more precisely you specify what you want, the more consistently you get it. But variance doesn't disappear entirely. About 80% good-on-first-try, 15% minor corrections, 5% substantial rework. That 5% is frustrating because you don't know in advance which features will fall into it.

**Integration testing.** Unit tests are easy for AI. You specify the function, you specify the test cases, AI generates tests. Integration tests are harder because they require understanding how components interact.

*Solution*: Explicit integration scenarios in stories. Instead of asking for "integration tests," describe specific scenarios: "User creates workflow, workflow executes three steps, user sees completion status." The scenario becomes the test specification.

**Refactoring coordination.** If you rename a concept, you need to update stories, regenerate affected code, verify nothing broke. The AI doesn't automatically propagate changes.

*Solution*: Treat refactoring as its own story. Write a specification for the refactoring: what's changing, why, what needs to update. More ceremony than traditional refactoring but more reliable.

## The Unsolved Problems

Some challenges remain. I don't have good solutions. Maybe you will.

**Long-term memory.** Every conversation with the AI starts fresh. It doesn't remember that three days ago we decided to use a particular pattern. It doesn't know that we tried approach X and it didn't work.

I tried various workarounds: maintaining "decisions.md" files, adding "previously decided" sections to stories, referencing earlier conversations. None really solve it. The AI doesn't have persistent memory. Everything needs re-specification every time.

This matters more for long-running projects than for the three-week sprint I did. Teams doing sustained development will feel this pain more acutely.

**Architectural drift.** Each story is implemented somewhat independently. Over time, small inconsistencies accumulate. Different stories make slightly different assumptions. Authentication handling in one module doesn't quite match another. Error handling patterns vary. Logging approaches differ.

Periodic reviews help catch these inconsistencies, but catching isn't preventing. I don't have a good way to ensure architectural consistency across stories except vigilance.

**The 5% problem.** That 5% of generations needing substantial rework? I can't predict which ones they'll be. Sometimes it's complex features—fair enough. But sometimes it's seemingly simple features that just don't work. The AI confidently generates something wrong. Debugging it takes longer than implementing from scratch.

The best mitigation: fast feedback cycles. Generate, test, iterate. Don't let bad generations sit. But that's management, not prevention.

**Code review depth.** When you write code yourself, you understand it deeply. When you review AI-generated code, your understanding is shallower. You verify it works, check patterns, spot-check implementation. But do you really understand every line?

Is this a problem? Maybe. If something breaks six months from now, will I understand the code well enough to fix it? I think so—but I'm not certain.

**Model dependency.** This methodology works with Claude Opus. It works less well with lesser models. What happens when Opus is deprecated? Or becomes too expensive? I'm dependent on a capability I don't control.

The mitigation is that local models keep improving. Qwen2.5-Coder is surprisingly capable for a 7B model. But today, there's a quality gap.

## Lessons Learned

Beyond specific problems, some broader lessons:

**Thinking time isn't optional.** The 15-30 minutes spent writing a story isn't overhead—it's the work. When I got sloppy with stories, output quality dropped. When I invested in thorough specifications, output quality rose. The correlation was obvious.

**AI has opinions (and they're not always right).** LLMs have preferences. They default to certain patterns, libraries, approaches. I learned to be explicit: "Use X pattern, not Y." "Follow the existing approach in file Z." Without guidance, AI makes reasonable but non-optimal choices.

**Fast iteration beats careful specification (sometimes).** Well-understood features benefit from thorough specification. Exploratory features benefit from fast generation and refinement. One size doesn't fit all. The methodology is a framework, not a straitjacket.

**Human judgment remains critical.** AI amplifies human judgment. It doesn't replace it. Every significant decision in this project was mine. If I'd made bad decisions, AI would have faithfully implemented them. Fast, correct, and wrong.

## Would I Do It Again?

Yes. Without hesitation.

The productivity gain was real. The code quality was acceptable. The learning was valuable.

But I'd do it with clear eyes. The methodology works best when you know what to build, can specify it clearly, can review output effectively, and accept iteration as normal.

It works worst when requirements are genuinely uncertain, the domain is unusual, you can't evaluate output quality, or you expect first-try perfection.

I hit the happy path most of the time. Your experience may vary.

---

*Next: "Aura is Eating Itself" — When your tool improves itself*

---

*This is the sixth post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
