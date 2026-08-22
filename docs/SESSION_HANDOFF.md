# Session Handoff — 2026-08-22

The complete current successor brief is:

`docs/CLAUDE_HANDOFF_2026-08-22.md`

Paste this to begin the next session:

```text
Continue LifeSimulation from main. Expected head at handoff is f750345, plus any handoff-doc
commit made afterward. Read docs/CLAUDE_HANDOFF_2026-08-22.md first, then the named sections of
docs/AGENT_FIELD_NOTES.md and docs/ROADMAP.md that it routes you to. Do not re-read the whole
repository and do not treat docs/superpowers/plans as a backlog.

Immediate work: the soft home-range implementation is tested and Unity-compiles, but no shipped
Play-mode scenario enables it—not 5 and not N. Add an ordinary-key matched scenario (recommended
R) that differs from ObservationStable/5 only by HomeRangeAffinityEnabled=true. Keep place memory
inert, keep defaults/5/N unchanged, and prove flag-off byte-identical. Then measure fixed-seed
off/on route reuse, centre distance, resource visits, survival, and births across stable/scarcity/
migration before calling the behavior ecologically successful. The failure mode to detect is
simple food-patch stickiness rather than useful home-range routes.

Bundle the small P5 UI correction: preserve ConfirmedContinuity in analysis, but hide routine
continuity rows by default and show “N routine continuities hidden” so split/merge/extinction
evidence is not drowned out.

Work autonomously, question premises and your own design, state numeric predictions before
experiments, narrate meaningful progress, use ordinary letter/number controls (no F-key binds),
commit/push scoped completed work to main, and ask only for a genuinely human design choice.
```

The working tree has intentionally untracked Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. Do not stage or delete them. Add named files only;
never use `git add -A`.
