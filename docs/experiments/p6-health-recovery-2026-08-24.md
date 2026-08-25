# Healing exists now, and the permanent-sterility ratchet was a smaller cause than I claimed

**2026-08-24. 80 seeds, 12,000 ticks, cap 100, moderate resources, terrain join on.**
`tools/CreatureSweep --focused 80 100 --health-recovery` and `--deaths 30 100 --health-recovery`,
against the identical seeds without it (`p6-dose-response-moderate-80seeds`).

## The defect

`CreatureNeeds.Health` was written once at birth and subtracted from in **five** places
(`NeedsSystem.cs:68, 73, 78, 85`, plus combat). **Nothing anywhere in the simulation added to it.**

That would be unremarkable if health were only a death clock. It is not — it is **one of the three
conditions on the mate-seeking gate**, which
`p6-the-mating-gate-is-the-selection-2026-08-24.md` shows is the dominant selective channel in the
model. So a creature that lost a fifth of its health was not injured. It was **permanently sterile for
the rest of its life**, with no path back.

## Measured before being called a crisis

| | default | **with recovery** |
|---|---|---|
| mean health fraction | 0.9861 | **0.9939** |
| under the health gate | 0.8% | 1.0% |
| health deaths | 161 (2.9%) | **97 (1.7%)** |
| age deaths | 5,443 (96.9%) | 5,604 (98.1%) |
| extinct | 1 / 80 | **0 / 80** |

**Almost nobody was actually caught by it.** The population evolves thermal tolerance until nothing
damages it, and creatures walk away from uncomfortable ground, so the ratchet is a **latent trap**
rather than an active one. Health deaths fall by 40% with healing on, which is the mechanism working.

The share under the gate goes *up* slightly, 0.8% to 1.0%, which reads backwards until you notice
health deaths fell: creatures that used to die of injury now survive it, so at any instant more of the
living are partway through recovering.

## The prediction, and it was overstated again

**Predicted:** the ratchet is why `temperature_tolerance` is the fiercest selection in the model —
when damage is permanent, the only winning move is never to be damaged — so healing should visibly
weaken it.

| | default | with recovery |
|---|---|---|
| `temperature_tolerance` | +0.2879 (t 26.03) | **+0.2323 (t 23.89)** |

**A 19% reduction.** Real, in the predicted direction, and **not the main cause.** The ratchet
contributes; it does not explain. The arithmetic account still stands as the primary driver —
tolerance is `2 + 8*gene` against a field bounded at 8 degrees, so the gene is worth having until it
covers the world and no further (`p6-why-temperature-tolerance-2026-08-24.md`).

That is twice tonight a mechanism I found in the source turned out to be a contributing factor rather
than the explanation. Worth stating plainly rather than rounding up.

## Everything else got stronger, and that is mostly the arm being cleaner

`body_size` −0.0133 → −0.0191, `movement_speed` t 3.93 → 5.54, `lifespan_tendency` t 17.44 → 28.35,
`urgency_exponent` t −14.55 → −16.99. **This arm has zero extinctions against one**, 80 surviving
runs against 79, and a control at t = −0.0039 — essentially exactly zero.

**Do not read those increases as effects of healing.** A cleaner arm with fewer dead worlds has less
composition noise, and t-statistics are not comparable across arms with different survival. The one
comparison this run was designed to make is the thermal one.

**A display note:** with the control at −0.0000, the report's "vs control" ratio column divides by
almost zero and prints numbers like 30,988x. **That column is meaningless in this arm** — read the
t values.

## The design, and why it is conditional

`NeedsSystem.RecoverHealth` restores **0.5% of health capacity per second**, and only while the
creature is **over half full on both energy and hydration**.

- **Paid for, not free.** Unconditional regeneration would make injury meaningless, which is the
  opposite failure to the one being fixed.
- **Slower than the damage.** Peak thermal damage is about 0.76 health per second against roughly
  0.36 recovered, so a creature standing in a hot band still nets a loss and only makes it back after
  leaving. A test pins this.
- **Body-size neutral**, being a fraction of capacity rather than a flat rate.
- Applied **after** the damage in the tick, deliberately.

## Three conditions, and it never hurts

Baseline-arm extinctions, 80 seeds per level, against the identical seeds without healing:

| level | extinct, default | **extinct, with healing** |
|---|---|---|
| moderate | 1 / 80 | **0 / 80** |
| lean | 25 / 80 | **14 / 80** |
| scarce | 68 / 80 | 68 / 80 |
| **total** | **94 / 240** | **82 / 240** |

**Better, better, identical — z = 1.14 on the pooled counts.** Not significant, but unlike the
terrain-temperature flag, which was consistently *worse* under scarcity, this one is never worse at
any level and is markedly better at lean, where 11 worlds that used to die now survive.

Controls stay quiet in both new arms: lean t = −0.08, scarce t = −0.12.

## Status

**Switched on for the `Y` terrain playtest. Configuration default stays false**, and every recorded
result on this project was measured without healing.

**A note for the next deliberate re-baseline.** Unlike the slope cost and the terrain temperature,
which *add* realism, this one **removes an artefact**: transient injury causing permanent sterility
is not a modelling choice anybody made, it is what falls out of a quantity that only decrements
gating a quantity that decides fitness. That makes it the strongest candidate in the flag list to
become the default the next time a re-baseline is being taken on purpose.

**What this unblocks.** `MetabolicPace` is a pure-cost gene
(`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md`) whose obvious honest benefit is *faster metabolism
heals faster* — private, undilutable, and pointed straight at the gate that decides fitness. That was
impossible to build while there was no healing to accelerate. It is now the immediate next step.
