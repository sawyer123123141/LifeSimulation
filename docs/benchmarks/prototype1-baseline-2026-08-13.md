# Prototype 1 baseline benchmark — 2026-08-13

This is an initial local, headless measurement of the pure simulation loop before scenario resource budgets were made population-aware. It is a development signal, not a final performance claim.

Configuration:

- Unity 6000.2.14f1, Windows desktop
- baseline scenario, seed `42`
- 200 warm-up ticks, then 2,000 measured ticks at 20 Hz
- presentation/GameObjects excluded

| Founder population | Mean simulation step |
| ---: | ---: |
| 100 | 0.042 ms |
| 500 | 0.136 ms |
| 1,000 | 0.245 ms |

The population declined naturally during these long runs (to 58, 28, and 21 survivors respectively), so the measurements do not prove sustained 1,000-creature throughput and do not describe the later population-scaled scenario budgets. The next benchmark gate will rerun the matrix on the corrected configuration and capture system-level timings before deciding where Burst/Jobs would help.
