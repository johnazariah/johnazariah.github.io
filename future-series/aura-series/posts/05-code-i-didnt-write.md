# Code I Didn't Write

*The surprising results of three weeks of AI collaboration*

---

Let me tell you the part of this story that still surprises me.

Over three weeks of evening and weekend work, I built a production-quality AI development accelerator. 17 C# projects, 294 source files, a VS Code extension with 45 TypeScript files, 9 specialized AI agents, 266 automated tests.

I didn't write any of the implementation code.

Not a line. Not a function. Not a class.

I wrote specifications. I wrote stories. I reviewed output. I made architectural decisions. I debugged problems.

But the actual code—the syntax, the logic, the implementation details—came from Claude.

## What I Actually Did

If I didn't write code, what did I do for three weeks?

**Writing stories** (30% of time). Each feature started as a markdown document. 15-30 minutes articulating: Why does this exist? What does success look like? What are we NOT building?

**Reviewing output** (25%). AI generates code quickly. Reviewing takes time. Check for issues, verify it matches spec, spot-check implementation.

**Debugging** (20%). When tests failed or integration broke, I diagnosed problems and specified solutions.

**Making decisions** (15%). "Should we use this pattern or that one?" Judgment calls that AI can inform but not make.

**Orchestrating** (10%). Which stories next? How do features depend on each other? When is something "done enough"?

Zero percent writing implementation code. One hundred percent thinking about what code to write.

## The Productivity Math

The codebase has approximately 15,000 lines of C# and 3,000 lines of TypeScript. At normal pace—200-400 lines of quality code per day—that's 45-90 days of implementation work.

I did it in about 60 hours over three weeks.

The obvious factor: AI writes code faster than humans.

But that's not the real story. The real story is *reduced rework*. Story-driven development compressed the time normally spent writing wrong code, debugging wrong code, refactoring wrong code.

When I did encounter issues, they were usually specification issues, not implementation issues. Fix the story, regenerate the code. Faster than debugging implementation details.

## What It Felt Like

At first, not writing code felt wrong. I'm a developer. Code is what I *do*. Writing prose while AI did the "real work"—it felt like cheating.

But over time, something shifted. The stories *were* the development. The code was just an artifact—a necessary byproduct of having specified something clearly enough to implement.

It's like being an architect who never touches the construction site. You're not building, exactly. You're specifying what should be built, then verifying it was built correctly.

Some developers will find this satisfying. Others will hate it.

## The Quality Question

"But is the code any good?"

**Architecture quality**: High. Stories included design sketches, so generated code followed those designs.

**Code style quality**: Improving. Once GraphRAG provided codebase context, new code matched existing patterns.

**Test quality**: Mixed. AI generates good happy-path tests. Edge cases need human attention.

**Naming quality**: Surprisingly good when stories included terminology.

Overall: "production acceptable with review." Not world-class. Not embarrassing. Solid, workmanlike code.

## The Honest Caveats

**Model quality matters.** I used Claude Opus. Lesser models produce worse results.

**Domain matters.** This was greenfield development. Legacy systems are harder.

**Expertise matters.** I can spot bad code quickly. The methodology doesn't replace expertise—it amplifies it.

## The Uncomfortable Implication

If this methodology works, what does that mean for developers?

The skill that mattered most was *specification*. Articulating clearly what should be built. Decomposing complex problems into tractable pieces.

Implementation skill—the ability to write correct code—mattered less. Not zero. But less.

If AI continues improving, the premium skill becomes specification.

This is either terrifying or liberating depending on your perspective.

---

*Next: "The Challenges" — What went wrong and what remains unsolved*

---

*This is the fifth post in a series about building Aura. The code is open source at [github.com/johnazariah/aura](https://github.com/johnazariah/aura).*
