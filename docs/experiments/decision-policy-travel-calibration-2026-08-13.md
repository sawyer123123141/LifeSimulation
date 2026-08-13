# Decision-policy travel calibration — 2026-08-13

## Question

Can the far-rich versus near-adequate food environment select for the heritable `TravelSensitivity` policy gene?

## Run

- Policy: `IntentUtilityV1`
- Seeds: 42–46 (paired)
- Founders: 50
- Duration: 20,000 ticks
- Control: `policy-near-adequate`
- Treatment: `policy-far-rich`

## Result

The run is a calibration result, not evolution-proof evidence for travel sensitivity.

| Metric | Treatment minus control | Effect | Direction consistency | 95% paired bootstrap interval |
|---|---:|---:|---:|---:|
| Travel sensitivity | +0.0306 | +0.288 | 60% | -0.0245 to +0.1284 |
| Population | +35.6 | +0.379 | 60% | -26.4 to +120.4 |
| Births | -161.0 | -0.811 | 80% | -316.6 to -5.0 |
| Deaths | -196.6 | -1.511 | 100% | -316.8 to -109.8 |

The travel-sensitivity interval crosses zero and the effect is below the project threshold. The far-rich environment clearly changes demographic dynamics, but the trait response is neither strong nor directionally consistent enough to claim selection.

## Next calibration action

Add per-run near/far target-choice telemetry. A revised scenario must show that the food-layout treatment causes a sustained, measurable difference in actual target selection before a larger 20-seed selection run is justified.

## Evidence files

- `ExperimentResults/decision-policy-travel-paired.csv`
- `ExperimentResults/decision-policy-travel-summary.csv`
