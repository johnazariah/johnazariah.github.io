---
    layout: post
    title: "The Quantum Bottleneck - Part 6: The Scheduling Nightmare"
    tags: [quantum-computing, quantum-algorithms, quantum-bottleneck]
    author: johnazariah
    summary: "The NHS spends billions on agency nurses because scheduling 10,000 staff across 50 hospitals is NP-hard. The constraints multiply faster than the solutions."
---


> **Series: The Quantum Bottleneck**
>
> Eight industry problems — and the quantum algorithms that could solve them.
> [Companion notebooks](https://johnazariah.github.io/quantum-workbooks/bottleneck/)
>
> 1. [The $50M Delivery Route](/2026/05/27/the-quantum-bottleneck-01-logistics.html)
> 2. [The Trapdoor](/2026/06/10/the-quantum-bottleneck-02-cryptography.html)
> 3. [The $2B Molecule](/2026/06/24/the-quantum-bottleneck-03-drug-discovery.html)
> 4. [The Feature Explosion](/2026/07/08/the-quantum-bottleneck-04-machine-learning.html)
> 5. [The Convergence Wall](/2026/07/22/the-quantum-bottleneck-05-finance.html)
> 6. [The Scheduling Nightmare](/2026/08/05/the-quantum-bottleneck-06-supply-chains.html) ← you are here
> 7. [The Unsimulable Material](/2026/08/19/the-quantum-bottleneck-07-materials-science.html)
> 8. [The Better Catalyst](/2026/09/02/the-quantum-bottleneck-08-climate-energy.html)

---

# The Scheduling Nightmare

**The NHS spends billions on agency nurses because scheduling 10,000 staff across 50 hospitals is NP-hard. The constraints multiply faster than the solutions.**


Every winter, Britain's National Health Service faces a scheduling crisis. Across 50 hospitals, 10,000 nurses need shift assignments — day, evening, night — across wards and specialties. Each nurse carries qualifications (ICU-certified? paediatrics-trained?), contractual limits (maximum hours, minimum rest between shifts), personal preferences (no nights, school pickup at 3 PM), and legal requirements under the Working Time Directive.

The NHS spends an estimated £3 billion per year on agency nurses — temporary staff hired at premium rates because permanent rosters can't be assembled efficiently. Not always because there aren't enough nurses, but because the scheduling problem is so tangled that planners routinely produce suboptimal rosters, leaving gaps that agencies fill at two to three times the cost.

This isn't unique to healthcare. Airlines schedule crews across thousands of flights. Manufacturers assign workers to production lines. Call centres staff agents across time zones. The mathematical structure is always the same: assign $N$ resources to $M$ slots subject to constraints, minimising total cost.

These are **constraint satisfaction problems**, and nurse scheduling was proven NP-hard by Osogami and Imai in 2000. No polynomial-time classical algorithm exists (unless P = NP). Classical solvers — integer linear programming, constraint programming, simulated annealing — handle instances with hundreds of nurses. At thousands, they struggle. At tens of thousands, they break.

## The bottleneck: interacting constraints

What makes scheduling different from pure optimisation (like MaxCut) is the mix of **hard constraints** (must satisfy) and **soft constraints** (prefer to satisfy).

Hard constraints: every shift staffed by a qualified nurse, no more than 48 hours per week, minimum 11 hours between consecutive shifts, no more than 5 consecutive working days. Soft constraints: honour nurse preferences, distribute unpopular shifts fairly, minimise inter-site travel, balance workload.

With $N$ nurses and $S$ shifts, there are $N^S$ possible assignments. For 100 nurses and 500 shifts, that's $10^{1000}$. But the raw size isn't the real problem — sorting has a large search space too, and sorting is easy.

The difficulty is the **interaction structure**. Assigning Nurse A to Monday night affects whether Nurse B can take Tuesday morning, which affects Nurse C's Wednesday availability. Constraints propagate non-locally through the roster. Classical local-search methods — improve one part of the schedule — frequently break another. Every step interacts with every other step through a web of dependencies.

## QUBO and quantum annealing: the quantum angle

The key idea is **QUBO** — Quadratic Unconstrained Binary Optimisation. We convert the constrained scheduling problem into the minimisation of a quadratic function over binary variables, then map that to the ground state of a quantum Hamiltonian.

Each decision becomes a binary variable: $x_{n,s} = 1$ if nurse $n$ is assigned to shift $s$, otherwise 0. Hard constraints become large penalty terms — violate them and the cost spikes. Soft constraints become smaller penalties proportional to their severity. The result is a cost function:

$$C(x) = \sum_{i,j} Q_{ij} x_i x_j + \sum_i c_i x_i$$

The optimal schedule is the binary string $x^*$ that minimises $C(x)$. The substitution $x_i = (1 - Z_i)/2$ converts this to an **Ising Hamiltonian** — the same mathematical object as the cost Hamiltonian in Unit 1 and the molecular Hamiltonian in Unit 3. Different origin, same structure.

**Quantum annealing** offers a different paradigm from gate-based QAOA. Start in the ground state of a simple Hamiltonian (all qubits in $|+\rangle$). Slowly morph it into the problem Hamiltonian. The adiabatic theorem guarantees: if you change slowly enough, the system stays in the ground state throughout. At the end, you're in the optimal schedule.

The physical intuition: classical simulated annealing escapes local minima by making random uphill moves with decreasing probability — it hops *over* energy barriers. Quantum annealing can **tunnel through** barriers via quantum tunnelling, the same effect behind radioactive decay. Barriers that are tall but narrow are easy to tunnel through. This gives quantum annealing a structural advantage for landscapes with many tall, narrow barriers — though whether real scheduling problems have this geometry is an open empirical question.

## The companion notebook

The companion notebook builds a micro-scale nurse scheduling problem as a QUBO:

- 3 nurses, 4 shifts, with qualification and availability constraints
- Constructs the QUBO matrix with constraint penalties
- Solves by brute-force enumeration (small enough to check all $2^{12}$ assignments)
- Compares with a simulated annealing solver

```python
# QUBO formulation for nurse scheduling:
# 1. Binary variables: x[nurse, shift] = 0 or 1
# 2. Hard constraint penalty: each shift must be covered
for s in shifts:
    Q += penalty * (1 - sum(x[n, s] for n in qualified[s]))**2
# 3. Soft constraint: nurse preferences
for n, s in preferences:
    Q += weight * x[n, s]
# 4. Find the assignment that minimises Q
```

The problem is tiny — the point is to see how real-world constraints become quadratic penalties, and how the QUBO framework unifies scheduling, routing, and any problem expressible as binary optimisation.

## Reality check

Quantum annealing hardware exists at meaningful scale, but the advantage question remains unsettled.

**D-Wave.** D-Wave's Advantage system has over 5,000 qubits — far more than any gate-based machine. It can natively solve QUBO problems with thousands of variables. But the qubits are noisy, the connectivity is limited (each qubit connects to only 15 others on the Pegasus graph), and embedding real problems onto the hardware graph introduces significant overhead.

**The speedup debate.** Despite two decades of effort, no definitive quantum speedup for annealing has been demonstrated on a practical problem. Rønnow et al. (2014) found no speedup over classical simulated annealing on random instances. King et al. (2023) showed advantage on specific crafted instances. The picture is nuanced, not settled.

**Hybrid solvers are the practical frontier.** D-Wave's hybrid classical-quantum solver (Leap) uses classical heuristics for most of the work and the annealer for specific subproblems. These hybrids outperform pure classical solvers on some benchmarks, but isolating the quantum contribution is difficult.

**What's real today:** The QUBO framework is a genuinely useful abstraction — it works regardless of whether the solver is classical, quantum, or hybrid. Quantum annealing is a real technology with real hardware, but clear speedup on practical scheduling problems has not been demonstrated. The honest framing: quantum annealing is a bet on hardware improvement, not a solved problem.

## Want more?

This post covers QUBO formulations and the annealing paradigm. The [companion notebook](../../notebooks/06-supply-chains.ipynb) lets you build a nurse scheduling QUBO from scratch. For the full QUBO engineering toolkit, the physics of quantum tunnelling, and the gate-based vs. annealing tradeoff, see *The Quantum Bottleneck*, currently being prepared for publication.

---

*This is Unit 6 of The Quantum Bottleneck series. Next up: [The Unsimulable Material](bottleneck-07-materials-science.md) — what happens when the electrons in a material are so strongly correlated that no classical method can resolve the physics.*
