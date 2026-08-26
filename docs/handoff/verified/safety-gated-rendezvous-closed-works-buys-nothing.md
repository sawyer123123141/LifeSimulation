### Safety-gated rendezvous (CLOSED — "works, buys nothing")

The 2026-08-21 null was partly unmeasurable: **all 240 of its runs ended at exactly 48**, the
population cap, zero variance. Its birth null stands; its survival null was a ceiling.

**The population cap is load-bearing ecology, not a guard rail.** Extinct 0/8 at cap 72, **5/8 at
84**, 8/8 at 96 and above, where runs boom to ~293 births and collapse on starvation. Cap 84 is the
only point where survival is free to move, so the rerun used it.

Re-measured, 120 paired seeds, cap 84 (`p4a-rendezvous-headroom-2026-08-22.md`):

| | delta | t | sign |
|---|---:|---:|---|
| flee rate per creature-tick | −0.0285 | **−5.07** | 80/120 down |
| **predation deaths** | **−2.275** | **−4.64** | 70/120 down |
| births, raw | +12.81 | +2.04 | 72/120 up |
| births per creature-tick | +0.00001 | +1.24 | not significant |
| births, both-survived seeds (n=28) | +11.71 | +1.01 | not significant |
| starvation deaths | +1.15 | +0.85 | null |

Extinction 75/120 vs 66/120 **does not survive pairing**: discordant 26 vs 17, McNemar χ² 1.49.
The raw birth gain is **exposure, not fertility**.

**Verdict: the mechanism works and the ecology declines to reward it.** Starvation, not predation,
limits this population. Flag stays default `false`. Do not build pack architecture to force an
effect; do not tune the gate. Reopen only in a predation-limited habitat — a scenario question, not
a mechanism question. **This is not the home-range case**: home range was closed for the wrong sign,
this for a right-signed effect that reaches no outcome that matters.

**Provenance:** the 2026-08-21 configuration could not be recovered — 81 candidates tried against its
recorded state hash and births, none matched. The rerun is a **new condition**, not a rerun, and its
CSV carries an `ExperimentManifest`.

---
