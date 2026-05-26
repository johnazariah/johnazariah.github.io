---
    layout: post
    title: "The Quantum Bottleneck - Part 2: The Trapdoor"
    tags: [quantum-computing, quantum-algorithms, quantum-bottleneck]
    author: johnazariah
    summary: "RSA security relies on a trapdoor: multiplying two large primes is easy, factoring their product is hard. Quantum computers can kick it open."
---


> **Series: The Quantum Bottleneck**
>
> Eight industry problems — and the quantum algorithms that could solve them.
> [Companion notebooks](https://johnazariah.github.io/quantum-workbooks/bottleneck/)
>
> 1. [The $50M Delivery Route](/2026/05/27/the-quantum-bottleneck-01-logistics.html)
> 2. [The Trapdoor](/2026/06/10/the-quantum-bottleneck-02-cryptography.html) ← you are here
> 3. [The $2B Molecule](/2026/06/24/the-quantum-bottleneck-03-drug-discovery.html)
> 4. [The Feature Explosion](/2026/07/08/the-quantum-bottleneck-04-machine-learning.html)
> 5. [The Convergence Wall](/2026/07/22/the-quantum-bottleneck-05-finance.html)
> 6. [The Scheduling Nightmare](/2026/08/05/the-quantum-bottleneck-06-supply-chains.html)
> 7. [The Unsimulable Material](/2026/08/19/the-quantum-bottleneck-07-materials-science.html)
> 8. [The Better Catalyst](/2026/09/02/the-quantum-bottleneck-08-climate-energy.html)

---

# The Trapdoor

**RSA security relies on a trapdoor: multiplying two large primes is easy, factoring their product is hard. Quantum computers can kick it open.**


Every time you buy something online, your browser performs a quiet miracle. It agrees on a secret key with a server it has never met, over a network anyone can eavesdrop on. This is public-key cryptography, and it protects virtually every financial transaction, encrypted email, and software update on Earth.

The entire system rests on one mathematical asymmetry. Take two 1,000-digit prime numbers $p$ and $q$. Multiplying them takes microseconds. The product $N = p \times q$ is a 2,000-digit number. Now hand someone $N$ and ask them to recover $p$ and $q$. The best classical algorithms would take longer than the age of the universe.

This asymmetry — easy to combine, hard to separate — is a **trapdoor**. RSA encryption, the backbone of internet security since 1977, is built on it. If you know the factors, you can decrypt. If you only know the product, you cannot.

For nearly fifty years, this trapdoor has held. Classical factoring algorithms have improved — the General Number Field Sieve, circa 1993, is the reigning champion — but they remain sub-exponential. For 2,000-digit numbers, that's still far beyond reach. Nobody seriously worried that classical computers would break RSA.

In 1994, Peter Shor changed the calculus entirely. He showed that a quantum computer could factor integers in polynomial time — roughly $n^3$ operations where $n$ is the number of digits. Not "somewhat faster." Polynomial. And the same technique breaks Diffie-Hellman and elliptic curve cryptography too.

The cryptographic foundations of the internet became conditional on quantum computers staying small.

## The bottleneck: factoring reduces to period-finding

The deep reason factoring is hard isn't that the search space is large — sorting has a large search space too, but sorting is easy. The issue is that we don't know how to structure the search.

But there's a beautiful mathematical reduction. Pick a random number $a < N$ and define:

$$f(x) = a^x \bmod N$$

This function is periodic. There exists an integer $r$ — the **order** of $a$ modulo $N$ — such that $a^r \bmod N = 1$, and therefore $f(x+r) = f(x)$ for all $x$. If you can find $r$, algebra does the rest: factor $(a^{r/2} - 1)(a^{r/2} + 1) \equiv 0 \pmod{N}$, and $\gcd(a^{r/2} - 1, N)$ gives a non-trivial factor of $N$ with high probability.

So factoring reduces to period-finding. Classically, finding the period of $f(x) = a^x \bmod N$ is at least as hard as factoring. The function values look random; no classical shortcut is known. But quantum mechanics offers a tool purpose-built for extracting hidden periodicity.

## Shor's algorithm: the quantum angle

Shor's algorithm finds the period $r$ using three quantum ingredients — two of which are new in this unit:

1. **Superposition** (from Unit 1): prepare a register in a uniform superposition of all possible inputs, then evaluate $f$ on all of them simultaneously. One query, exponentially many function values — each tagged with its input in a joint quantum state.

2. **Phase kickback**: the function evaluation stamps the periodicity of $f$ into the *phases* of the quantum state. This is the critical move — the classical information about the period becomes quantum phase information that interference can act on.

3. **The Quantum Fourier Transform (QFT)**: apply the QFT to the input register. The QFT converts a state with hidden periodicity $r$ into sharp peaks at multiples of $N/r$. Measurement collapses to one of these peaks, and classical continued-fraction arithmetic recovers $r$.

The QFT is the quantum analogue of the discrete Fourier transform — it maps a quantum state from the "position" basis to the "frequency" basis. Hidden periodicities become visible as spectral peaks. It runs in $O(n^2)$ gates on $n$ qubits, exponentially faster than the classical FFT's $O(N \log N)$ on $N = 2^n$ inputs.

The whole algorithm: prepare, evaluate, transform, measure, post-process. The quantum computer finds the period. Classical number theory turns the period into factors.

## The companion notebook

The companion notebook implements compiled period-finding for $N = 15$, the smallest interesting case. It:

- Builds the modular exponentiation oracle for $a^x \bmod 15$
- Constructs the QFT circuit from controlled-phase gates
- Runs the full Shor circuit and extracts the period from measurement statistics
- Uses the period to compute $\gcd$ and recover the factors $3$ and $5$

```python
# The core pattern:
# 1. Superpose all inputs
circuit.h(input_register)
# 2. Evaluate f(x) = a^x mod N via modular exponentiation
circuit.append(mod_exp_oracle, input_register + output_register)
# 3. Apply inverse QFT to extract the period
circuit.append(inverse_qft, input_register)
# 4. Measure and classically recover r
```

$N = 15$ is tiny — the point is to see every gate, every phase, every measurement outcome. The algorithm is the same one that would break RSA-2048, just on a number small enough to verify by hand.

## Reality check

Shor's algorithm is mathematically settled. The open question is hardware.

**What's been factored quantumly.** The largest number factored by a genuine implementation of Shor's algorithm is $21 = 3 \times 7$ (Martín-López et al., 2012). Claims of factoring larger numbers typically use compiled circuits that exploit advance knowledge of the answer — which rather defeats the purpose.

**Resource estimates for RSA-2048.** Gidney and Ekerå (2021) estimated that factoring a 2,048-bit RSA key would require approximately **20 million noisy physical qubits** and about 8 hours using surface-code error correction. Newer architectures using quantum LDPC codes could reduce this to under 100,000 physical qubits, though with longer runtimes. The resource picture is improving, but we are not close.

**The post-quantum migration is underway.** NIST finalised post-quantum cryptography standards in 2024 — lattice-based schemes believed hard for both classical and quantum computers. The U.S. government has mandated migration for federal systems. Google, Apple, and Cloudflare have already deployed post-quantum key exchange. The cryptography community decided not to wait for a large quantum computer to be built. The risk they're hedging: **harvest now, decrypt later** — an adversary records encrypted traffic today and decrypts it years from now.

## Want more?

This post covers the what and why of Shor's algorithm. The [companion notebook](../../notebooks/02-cryptography.ipynb) lets you run compiled period-finding yourself. For the full gate-level construction of the QFT, the subtleties of modular exponentiation circuits, and the complete resource-estimate landscape, see *The Quantum Bottleneck*, currently being prepared for publication.

---

*This is Unit 2 of The Quantum Bottleneck series. Next up: [The $2B Molecule](bottleneck-03-drug-discovery.md) — what happens when the molecule you need to simulate is too quantum for any classical computer.*
