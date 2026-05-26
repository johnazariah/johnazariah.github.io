---
    layout: post
    title: "The Quantum Bottleneck - Part 1: The $50M Delivery Route"
    tags: [quantum-computing, quantum-algorithms, quantum-bottleneck]
    author: johnazariah
    summary: "Every morning, 130,000 UPS drivers leave their depots. The order they visit their stops determines whether the company saves $50 million — or doesn't."
---


> **Series: The Quantum Bottleneck**
>
> Eight industry problems — and the quantum algorithms that could solve them.
> [Companion notebooks](https://johnazariah.github.io/quantum-workbooks/bottleneck/)
>
> 1. [The $50M Delivery Route](/2026/05/27/the-quantum-bottleneck-01-logistics.html) ← you are here
> 2. [The Trapdoor](/2026/06/10/the-quantum-bottleneck-02-cryptography.html)
> 3. [The $2B Molecule](/2026/06/24/the-quantum-bottleneck-03-drug-discovery.html)
> 4. [The Feature Explosion](/2026/07/08/the-quantum-bottleneck-04-machine-learning.html)
> 5. [The Convergence Wall](/2026/07/22/the-quantum-bottleneck-05-finance.html)
> 6. [The Scheduling Nightmare](/2026/08/05/the-quantum-bottleneck-06-supply-chains.html)
> 7. [The Unsimulable Material](/2026/08/19/the-quantum-bottleneck-07-materials-science.html)
> 8. [The Better Catalyst](/2026/09/02/the-quantum-bottleneck-08-climate-energy.html)

---

# The $50M Delivery Route

**Every morning, 130,000 UPS drivers leave their depots. The order they visit their stops determines whether the company saves $50 million — or doesn't.**


In 2012, UPS deployed ORION — a route optimisation system that shaved an average of one mile off each driver's daily route. One mile, 130,000 drivers, 250 working days. The result: **$50 million per year** in savings.

Now imagine what a *two*-mile improvement would be worth.

The problem is that finding the optimal route is one of the hardest problems in computer science. A driver with just 20 stops faces 2.4 quintillion possible orderings. For 50 stops, the number exceeds the atoms in the observable universe. This is the Travelling Salesman Problem, and it's NP-hard.

Nobody solves it exactly. ORION uses heuristics — simulated annealing, specialised graph algorithms — that walk through the space of solutions one at a time, hoping local improvements lead to global ones. Sometimes they get stuck in valleys: solutions better than their neighbours but far worse than the best.

What if you could explore the entire landscape at once?

## The bottleneck: a rugged landscape

Strip away the trucks and geography. You have $n$ binary decisions, a cost function that depends on how those decisions interact, and an exponentially large search space.

The cleanest version of this is **MaxCut**: take a graph, colour every node red or blue, and maximise the number of edges between different colours. It sounds abstract, but TSP, MaxCut, and every combinatorial optimisation problem share the same deep structure. The quantum algorithm we're about to build works on *any* problem with this structure — MaxCut is just the entry point.

The challenge is the cost landscape. It's a function over $\{0,1\}^n$ with exponentially many local optima. Classical search walks through this landscape one step at a time. Quantum mechanics offers a different approach: prepare a superposition of *all* candidate solutions and use interference to amplify the good ones.

## QAOA: the quantum angle

The **Quantum Approximate Optimization Algorithm** (QAOA) encodes the problem as a quantum operator:

1. **Encode** each binary decision as a qubit — $|0\rangle$ for red, $|1\rangle$ for blue.
2. **Build a cost Hamiltonian** whose energy is low when many edges are cut and high when few are.
3. **Start in uniform superposition** — every possible colouring explored simultaneously.
4. **Alternate** between two operations: the cost Hamiltonian (which adds phases that favour good solutions) and a mixer Hamiltonian (which spreads amplitude across solutions).
5. **Measure** to collapse to a single candidate. Repeat. The best result wins.

The alternating layers create interference — paths through the landscape that lead to good solutions reinforce; paths to bad solutions cancel. A classical optimiser tunes the angles at each layer, feeding measurement results back in a variational loop.

It's not magic. QAOA doesn't guarantee the optimal solution. But it explores the landscape in a fundamentally different way than classical heuristics, and for certain problem structures, that difference could matter.

## The companion notebook

The companion notebook builds a complete QAOA circuit for MaxCut on a small graph:

- Constructs the cost Hamiltonian from the graph's edge list
- Builds the QAOA circuit with parameterised layers
- Runs a classical optimiser to tune the variational parameters
- Compares the quantum result with brute-force enumeration

You can [run it yourself](../../notebooks/01-logistics.ipynb) or browse it on the companion notebooks page.

```python
# The core idea in three lines:
# 1. Encode the graph as a cost Hamiltonian
cost_hamiltonian = sum(0.5 * (I - Z(i) @ Z(j)) for i, j in edges)
# 2. Build QAOA layers that alternate cost and mixer
qaoa_circuit = build_qaoa(cost_hamiltonian, p=2, gammas=γ, betas=β)
# 3. Measure and optimise
result = optimise(qaoa_circuit, cost_hamiltonian)
```

The notebook is intentionally explicit — every gate, every measurement, every post-processing step is visible. That's a teaching choice.

## Reality check

QAOA on near-term hardware faces real obstacles:

- **Barren plateaus**: for many problem structures, the cost landscape that the classical optimiser navigates becomes exponentially flat, making parameter tuning nearly impossible.
- **Depth vs. noise**: more QAOA layers give better approximation ratios, but deeper circuits accumulate more errors on noisy hardware.
- **The advantage question**: for MaxCut specifically, the best classical algorithms are remarkably good. Whether QAOA can outperform them on any practical instance is an open — and actively debated — question.

The honest answer: QAOA on today's hardware doesn't beat classical solvers for logistics problems. The algorithm is a proof of concept for a computational paradigm — exploring cost landscapes via quantum interference — that could become practical when hardware improves. "Could", not "will".

## Want more?

This post is a standalone introduction to the problem and the algorithm. The [companion notebook](../../notebooks/01-logistics.ipynb) lets you build the circuit yourself. And if you want the full story — the mathematical structure of the cost landscape, the gate-level construction of every QAOA layer, and the research behind the reality check — that's coming in *The Quantum Bottleneck*, currently being prepared for publication.

---

*This is Unit 1 of The Quantum Bottleneck series. Next up: [The Trapdoor](bottleneck-02-cryptography.md) — what happens when quantum computers meet the security that protects your bank account.*
