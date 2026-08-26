### Selected-creature history (DONE — `32900de`)

`CreatureActionHistory` records, for one creature at a time, a bounded list of action episodes plus
a lifetime budget of ticks per action. Each episode carries the needs it started and finished on,
which is the whole point: a `SeekFood` episode ending with **less** energy than it started is a
failed trip, and that is invisible from an instantaneous inspector reading.

**It lives outside `SimulationWorld` on purpose.** It samples the world; the world never reads it.
So it adds no simulation state, appears in no hash, and cannot change a tick. A per-creature history
held *inside* the world would be future-determining state by the letter of the fingerprint design
and would need re-arguing every time a fingerprint changed. Not config-flag-gated either — a
diagnostics flag has to be behavior-inert to be correct, and `FlagLivenessAnalysis` would then
report it inert and fail the known-inert-flag assertion. Same reasoning as `SimulationWorld.Liveness`.

Ten tests. The load-bearing one: an observed world and an unobserved world have **identical V2
fingerprints** after 400 ticks — the first real use of the fingerprint from `7343653`. Both that
test and the determinism test assert the observer actually recorded something, and a third asserts
the run produced more than one kind of episode, since a single unbroken `Wander` would satisfy
determinism while showing the player nothing.

Sampled once per simulated step, not per frame, so resolution is independent of frame rate and of
the speed multiplier. Drawn in its own panel at (464, 300) rather than lengthening the inspector,
which is already at full height with all optional trait rows showing.

---
