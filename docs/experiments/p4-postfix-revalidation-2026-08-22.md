# Post-fix revalidation of plant trait selection

**Date:** 2026-08-22
**Raw data:** `p4-postfix-revalidation-2026-08-22.csv` (720 runs)
**Purpose:** re-measure the plant conclusions that sit on the competition path, on fixed code, with
varying founders.

## Design

120 seeds (42–161), 12,000 ticks, `maximumPopulation` 48, 12 founders, cognition and physiology on,
`IntentUtilityV1`. Six combinations:

- **Site conditions:** `24site` (the calibration's 6 active plus its 18 dispersal targets) and
  `168site` (`AbundantSiteReplicationModerate`'s lattice — see
  `p4-168-site-replication-2026-08-22.md`).
- **Arms:** `contest-off` (competition + mortality), `contest-on` (adds the establishment contest),
  and `drift` (competition **off**, mortality on).

Founders vary: each of the six active sites gets a different genome, every trait rotated across
0.30–0.70 so traits are not mutually correlated. Uniform founders give drift only.

**Survival is clean everywhere: 0/120 extinct and 0/120 frozen in all six combinations.**

## Two limitations of this sweep, stated before any conclusion

**1. The `drift` arm is not a null distribution, and its name oversells it.** Turning site
competition off does not disable any trait — dispersal, seed investment and the rest keep acting.
It is a different ecological regime, not a matched disabled control. It is why `Dispersal` reads
*larger* there (+0.1885, t +26.71) than with competition on (+0.1119). **Nothing in this document
uses the drift arm to adjudicate "selected versus drift."** A real drift control disables the
trait's channel, as the seed-production sweeps did with a charge of zero.

**2. The dispersal charge is at its default, not the value the establishment experiment used.** The
recorded result specifies `SeedlingResilience` paying a `DispersalRange` charge of 2. This sweep did
not set it, so the cost structure differs from the original.

## Occupancy: the harness is faithful, the 168-site replication is not

| condition | measured occupancy | recorded |
|---|---|---|
| 24site / contest-off | **0.914** | 0.908 |
| 24site / contest-on | 0.912 | 0.904 |
| 168site / contest-off | **0.840** | 0.332 |
| 168site / contest-on | 0.836 | 0.322 |

The 24-site arm reproduces the recorded occupancy to within 0.006, which validates the harness,
the config transcription and the founder setup.

**The 168-site replication fails to reproduce the low-occupancy condition — 0.84 against 0.32.**
Because the 24-site arm lands correctly, the failure is attributable to the target *geometry*, not
to the harness. A regular lattice at spacing 4 is evidently far easier to colonise than whatever the
original used; sites are close enough together that dispersal saturates them.

**Consequence: the three low-occupancy documents remain un-auditable.** This sweep cannot speak to
them. Their banners stay exactly as they are. What has been gained is a negative result worth
recording — *a uniform dense lattice does not recreate the low-occupancy operating point* — and a
committed scenario that a future attempt can vary from. The next attempt should push targets far
apart, not merely add more of them.

## Positive controls: confirmed

| trait | 24site contest-off | 24site contest-on | recorded |
|---|---|---|---|
| Dispersal | **+0.1119, t +15.63, 110/120** | +0.1429, t +19.78, 117/120 | t +14 to +19.6, 105–119/120 |
| SeedInvestment | **+0.0872, t +7.10, 91/120** | +0.0439, t +3.28, 69/120 | t +4.8 to +6.8 |

Both reproduce their recorded magnitude class and sign counts on fixed code. `Dispersal` remains the
strong positive control and `SeedInvestment` the weaker second. At the abundant condition Dispersal
is stronger still (t +38 to +47, 120/120).

## Establishment: the contest manipulation replicates

Absolute `SeedlingResilience` declines in every arm here (24site: −0.0903 contest-off, −0.0540
contest-on). That is **not** the comparison the establishment conclusion makes. The conclusion is
about what enabling the contest does, so the matched within-design test is contest-on minus
contest-off, paired by seed:

| condition | ΔSeedlingResilience (on − off) | t | seeds up |
|---|---|---|---|
| **24site** | **+0.0362** | **+3.22** | **72/120** |
| 168site (occupancy 0.84) | +0.0712 | +12.37 | 102/120 |

Against the recorded `t +4.03, 76/120 up`. **The 24-site figure replicates it in direction,
magnitude class and sign count** — t +3.22 versus +4.03, 72/120 versus 76/120 — despite this sweep
omitting the dispersal charge of 2 and using a different founder-variance rule.

The contest is therefore doing what it was claimed to do: it confers a specific, measurable
advantage on `SeedlingResilience`. The absolute decline in both arms is a separate matter — some
other pressure pushes the trait down in this founder design — and does not bear on the manipulation.

## SeedProductionRate at 24 sites: null, as recorded

−0.0240, t −2.80, **43/120 up** at `24site / contest-off`. A negative t with a minority sign count
is not selection. The recorded 24-site verdict (null; kept as the live negative control) stands.

Its recorded *positive* verdict at 168 sites cannot be checked, for the occupancy reason above.

## Growth-rate trait nulls

Not adjudicated. The recorded nulls for the six growth-rate traits are 168-site conclusions, and
this sweep has no valid low-occupancy condition and no matched disabled control. The 24-site numbers
here are reported in the CSV but should not be read as confirming or refuting them.

## Verdict

| conclusion | status after this sweep |
|---|---|
| Dispersal positive control | **confirmed on fixed code** |
| SeedInvestment second positive control | **confirmed on fixed code** |
| Establishment contest raises SeedlingResilience | **replicated** (t +3.22, 72/120 vs recorded t +4.03, 76/120) |
| SeedProductionRate null at 24 sites | **confirmed** |
| Plant lifetime accounting (34% / 2s / 51.9% / R² ≈ 0) | confirmed separately — `p4-postfix-lifetime-accounting-2026-08-22.md` |
| All 168-site low-occupancy conclusions | **still un-auditable**; replication geometry failed |
| Six growth-rate trait nulls | **not adjudicated** (168-site conclusions) |
| Mortality / lifespan headroom at low occupancy | **not adjudicated** (168-site conclusion) |

Banners can be lifted from the establishment documents and from the two positive-control documents.
The low-occupancy banners stay, and the reason they stay is now sharper than "no scenario exists":
a scenario exists, and it does not reproduce the condition.
