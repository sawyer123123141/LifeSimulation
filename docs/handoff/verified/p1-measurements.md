### P1 measurements

- Genetic distance: **240 bytes/pair** before → 187 KB / 4.8 MB / **120 MB and 126 ms** at 40 / 200 /
  1,000 creatures. After: 4.3 KB / 21 KB / **104 KB and 50 ms**. **1,151x** less, **2.5x** faster.
- Resource allocation: cost is **O(requests × distinct resources)**, not O(requests²). 1,000 requests
  on 1 resource = **16.9 µs**; on 24 resources = **165 µs**. Full 12,000-tick runs: **0.012 /
  0.090 / 0.227 ms per tick** at peak populations 38 / 48 / 523.

---
