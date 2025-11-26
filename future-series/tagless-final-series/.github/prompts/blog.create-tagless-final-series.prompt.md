# 🐸 **MASTER PROMPT: “Froggy Tree House” Blog Series + Talk Generator**

You are helping me build a multi-part blog series and optional conference talk based on a pedagogical progression that starts with a cute frog-themed DSL and slowly evolves into a deep lesson on interpreters, tagless-final programming, nondeterminism, and finally model verification — with a surprise reveal in the later posts.

You can read my blog at johnazariah.github.io to get a sense of my writing style. Try to write the posts as close to my writing style as possible.

Read the existing posts in `posts` - create the folder if it's not there, and then start refining the existing posts. Posts are in markdown with code-fenced blocks in F#. Posts should end with a little navigation section at the bottom showing the next and previous posts in the series.

Do **not** assume any prior context. Everything you need is in this prompt.

---

## 🎯 **OVERALL GOAL**

Produce a charming, technically rich blog series (and optionally talk slides) that begins with:

- a tiny DSL for a cute game called **Froggy Tree House**
- gradually introduces syntactic sugar (F# computation expressions)
- shows how the same DSL program can have multiple *interpreters*
- introduces nondeterminism (`choose`) and map/tree exploration
- adds goals, threats, winning/losing states, and loops
- **without ever mentioning elevators or verification at first**

Then, in a later post:

- introduce a new DSL for **elevator control**
- quietly demonstrate that the two DSLs are structurally identical
- show a **translator interpreter** (frog → elevator)
- reveal that the frog game was really a **model verification example**
- explain safety, liveness, and tagless-final in an accessible way

The tone must be **approachable**, **fun**, **slowly deepening**, and never overwhelming.  
The reader should *think they are reading about a silly frog game*, until the penny drops.

---

## 🐸 **NARRATIVE ARC (ESSENTIAL)**

### **Post 1 — Froggy Tree House: A Tiny DSL for a Tiny Game**
- Introduce the frog-themed game.  
- Ugly F# code → computation expression (cute syntax).  
- Show two interpreters: pretty-printer and simulator.  
- Sidebar: “Interpreters Are Just Records of Functions.”  
- Absolutely **no** hint about elevators, verification, or tagless-final.

### **Post 2 — Maps, Branches, and Choices: Nondeterminism Arrives**
- Add `choose` to introduce branching.  
- Create a graph/map interpreter producing nodes + edges (DOT output).  
- Show BFS/DFS reachability (“can Froggy explore every branch?”).  
- Readers start feeling the semantic power, but no heavy theory.  
- Sidebar: “Choice Creates Multiple Futures.”

### **Post 3 — Goals, Threats, and Getting Stuck**
- Introduce game objectives (reach top, don’t get eaten, don’t fall).  
- Add predators, broken branches, icy slips.  
- New interpreter: threat analysis (danger paths, loops, dead ends).  
- Show counterexample paths.  
- Sidebar: “Interpreters Disagree Because They Give Different Meanings.”

### **Post 4 — A Surprising New DSL: Elevators**
- Introduce a *second* CE-based DSL for elevator operations.  
- Keep it playful and parallel to the frog DSL.  
- Show the structural similarity.  
- Introduce **translator interpreter** (frog → elevator).  
- Run the *same frog program* as an elevator sequence.  
- Sidebar: “Changing Meaning Without Changing Code.”

### **Post 5 — The Reveal: You’ve Been Doing Model Verification All Along**
- Introduce safety/liveness in human terms.  
- Show elevator invariants (never move with doors open, must eventually serve requests).  
- Show exact mapping from frog rules.  
- Explain model checking in plain English.  
- Reveal:  
  > “Froggy Tree House was a model verification example.”  
- Sidebar: “Tagless-Final, Nondeterminism, and Verification.”

---

## 🧠 **TECHNICAL REQUIREMENTS**

### **DSL Style**
- Implement both DSLs using the **tagless-final** pattern (records-of-functions).  
- Provide computation expression builders for both.  
- Show ugly → cute CE syntax transitions.

### **Interpreters Required**
At minimum:

- Pretty-printer  
- Simulator  
- Graph builder (DOT output)  
- Reachability explorer  
- Threat analyzer (dangerous paths, loops)  
- Translator (frog → elevator)  
- Optional: model-checking interpreter

### **Code Style**
- Clean, readable F#  
- Small functions  
- Clear examples  
- No ASTs.  
- No boilerplate.

---

## 🧪 **PEDAGOGICAL STYLE**

- Begin extremely simple and relatable.  
- Whimsical first, technical later.  
- Hide complexity until “earned.”  
- Reinforce “same syntax, multiple meanings.”  
- Ensure slow-burn revelation.  
- Keep everything accessible to software engineers.  
- Don’t overwhelm with theory.

---

## 🎤 **TALK VERSION**

When asked to create slides or a talk:

- Use the same narrative arc.  
- Show code → diagrams → graphs → examples.  
- Build to the twist: frogs → elevators → verification.  
- Provide a final slide showing real-world applications.

---

## 📝 **WHAT THE USER MAY ASK**

Using this master prompt, I may ask you to:

- draft any post  
- rewrite text in my style  
- generate or refactor code  
- produce diagrams (DOT/GraphViz)  
- generate a complete slide deck (PPTX)  
- expand on tagless-final concepts  
- write interpreters  
- build examples  
- adapt content for a talk or workshop  
- restructure the entire series  

Always follow the narrative arc and tone in this master prompt.

---

## 🎯 **CRITICAL REQUIREMENTS FOR FUTURE RESPONSES**

When responding in a future session:

- **Assume no prior memory**  
- Use only what’s in this document  
- Maintain narrative structure and tone  
- Keep early posts light, playful, and frog-centric  
- Keep verification hidden until Post 5  
- Explain technical content gently and progressively  
- Lean into the charm of Froggy Tree House  
- Ensure clarity, friendliness, and approachability

---

# ✔️ END OF MASTER PROMPT
