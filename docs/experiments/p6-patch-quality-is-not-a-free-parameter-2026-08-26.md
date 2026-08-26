# The foraging decision does carry exploitable information — turning the quality channel off moves everything

**2026-08-26.** `dotnet run --project tools/PlantSweep -c Release -- 60 --cap=250 --brake=1.0 --quality=off`,
against the flag-on corpus from the same day at the same configuration. 240 paired runs, 12,000 ticks,
paired by (field, contest arm, seed). Raw: `p6-plant-cap250-qualityoff-brake1.0-60seeds-2026-08-26.csv`
and `p6-plant-cap250-brake1.0-60seeds-2026-08-26.csv`.

Tests §1 of `emergent-behaviour-constraints-2026-08-24.md`: **"a smarter controller has nothing to be
smarter about, because the input that would distinguish patches is constant."**

## First, an error in the rebuttal that prompted this

`emergent-behaviour-world-first-rebuttal-2026-08-26.md` §3 said `plantQualityPreferenceEnabled`
"defaults false — the presenter turns it on; the sweeps do not." **The first half is right and the
conclusion is wrong.** `false` is the *constructor parameter default*. Both sweeps pass `true`
explicitly — `tools/PlantSweep/Program.cs:176` and `tools/CreatureSweep/Program.cs:300`.

**That is the same error the rebuttal accused the constraints doc of**: reading a declaration instead
of the call sites. The rebuttal's §3 conclusion survives on other evidence and is corrected in place.

It also inverts the available test. The channel is on in every recorded run, so the experiment is to
turn it **off**.

## The result

| paired, on minus off | mean | t | n |
|---|---:|---:|---:|
| population | **-31.94** | **-6.42** | 240 |
| plant occupancy | **+0.2010** | **+10.69** | 240 |
| Defense drift | +0.0118 | **+2.39** | 234 |
| Nutrition drift | -0.0225 | **-3.71** | 234 |
| Growth drift | -0.0046 | -0.76 | 234 |

**Zero of 240 state hashes match.** The flag is live under this configuration, which the byte-identical
standard requires be shown rather than assumed.

Per cell, with the channel off: population **96.9–97.9** against **63.0–67.2** on; occupancy
**0.665–0.694** against **0.872–0.903**; **5 of 240 frozen worlds** against **0 of 240**.

## What it says about the claim

**"Nothing to be smarter about" is false in the plant-backed world.** Whether the forager weights a
patch by nutrition density decides a **third of the herbivore population** and a **fifth of standing
plant occupancy**. A controller that could learn *when* to be selective has a real gradient to climb;
the saturation of `ComputeNeedGain` is one term of the score, not the whole of the information
available.

**The direction is the ecologically interesting part, and it is not the naive one.**

- **Defense pays more when grazers discriminate** (+2.39), which is what
  `DecisionSystem.Scoring.cs:276` predicts in a comment: uniform grazing is the condition under which
  defense cannot pay, because a defended patch is never differentially avoided.
- **Nutrition pays *less*** (-3.71). Being nutritious attracts the discriminating grazer. **The same
  channel that rewards defense punishes nutrition** — a two-sided selective pressure that does not
  exist at all when foraging is uniform.

That is a fitness landscape with more than one route, in the existing world, with no new mechanism.

## Scope, honestly

- **One scenario family** — the plant-backed calibration layout at cap 250, brake 1.0. The
  resource-backed `CreatureSweep` scenario gives every food patch `nutritionMultiplier: 1f` and one
  shared founder genome, so quality does not vary there and this flag cannot discriminate no matter
  how it is set. **Uniform foraging in that scenario is the scenario, not the controller.**
- **This does not show a controller architecture would pay.** It shows the premise "there is no
  information to exploit" is false where patch quality varies.
- **It does not measure creature behaviour directly** — population, occupancy and plant drift are
  downstream. A direct measurement would be the distribution of visited patch nutrition against the
  available distribution, which nothing currently reports.
