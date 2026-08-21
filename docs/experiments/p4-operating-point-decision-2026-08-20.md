# P4 plant operating point: 24 sites versus 168 sites

> **AWAITING HUMAN DECISION.** This is a design recommendation, not a default change.

## Evidence available

| Scenario | Mean occupancy | SeedProductionRate result | Animal / plant survival |
|---|---:|---|---|
| 24 sites | 0.904–0.908 | null: +0.01953, t +3.22, 68/120 up; disabled drift 70/120 | 0/120 extinct; 0/120 frozen |
| 168 sites | 0.322–0.332 | selected: +0.02022, t +4.32, 79/120 up; disabled drift 66/120 | 0/120 extinct; 0/120 frozen |

At 24 sites the plant layer is an almost-full queue: seed production buys time but a mature patch has few empty destinations. At 168 sites there are genuinely free targets, and the same cooldown gene becomes selectable. The animal population finishes at the calibration cap (48) in every recorded arm, so these data establish survival, not that the 168-site geometry is a realistic regional ecology.

## Trade-off

24 sites is the established reliability calibration and creates strong scarcity, but it suppresses traits whose benefit is additional reproductive opportunity. 168 sites exposes that opportunity and makes `SeedProductionRate` selectable, but its large rectangular target field is an experimental manipulation, not an ecological world design: count and geometry both changed, and its carrying/resource implications have not been independently measured.

## Recommendation

Keep 24 sites as the default calibration until a human chooses an operating point. Retain 168 sites as an explicit experimental scenario for traits that require free establishment targets. Before promoting it to a watchable default, measure resource distribution, travel demand, plant patch turnover, and animal outcomes in a deliberately designed regional scenario rather than treating a selection-enabling knob as ecological validation.
