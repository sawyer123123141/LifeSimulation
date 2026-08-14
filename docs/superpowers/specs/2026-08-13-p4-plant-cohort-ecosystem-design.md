# P4 Plant-Cohort Ecosystem Design

## Goal

P4 replaces the prototype's indefinitely renewing food patches with a deterministic producer layer. The first P4 slice must demonstrate that food is produced by living plant cohorts with explicit biomass and local environmental limits, without breaking existing creature behavior, paired-seed experiments, or the simulation/presentation boundary. The full P4 program then permits plant and consumer traits to exert reciprocal selection.

## Representation decision

The authoritative representation is a dense, fixed-capacity `PlantPatchStore`: each record is a spatial plant cohort, not a Unity object and not a single visible plant. A cohort represents compatible plants at one ground-plane location and owns its biomass, capacity, genotype distribution for the initial single-lineage implementation, and local environmental budgets. It is the correct initial fidelity because animal perception already treats food as a spatial resource; it keeps growth O(patch-count), avoids per-plant all-to-all work, and can later be projected into clustered visual instances.

Individual plants are explicitly deferred. If a later near-camera ecology slice requires them, they will be an optional presentation/detail representation of a cohort, or a measured high-fidelity promotion path that preserves biomass and trait distributions. They will never become the sole simulation truth just because they are visible.

## Migration and ownership boundary

`PlantPatchStore` owns plant truth. It is updated before consumer perception at the existing resource cadence. During migration, `ResourceStore` remains the compatibility API used by creature perception, targeting, consumption, memory, and existing experiments. A deterministic projection publishes each plant cohort's currently edible biomass, food nutrition, and position into its paired food resource slot. Consumption is routed back from that slot into the owning cohort's biomass. Water remains a separate resource at first.

This one-to-one compatibility bridge is temporary but intentional: consumers do not need a broad rewrite to gain living food, and the bridge can be removed only after consumers query a general resource-provider interface. Resource IDs remain stable throughout an experiment. Plant mutation, seed establishment, and replacement cannot reorder existing resource slots; they use fixed-capacity inactive slots and stable IDs.

## First implementation slice

The first playable slice uses seeded, stationary cohorts co-located with the existing food resources. Each cohort has edible biomass, maximum biomass, intrinsic growth rate, water demand, nutritional value, defense, and a local moisture/fertility response. Growth is logistic and capped by both capacity and a limiting environmental factor:

```text
growth = growthRate * biomass * (1 - biomass / capacity)
       * min(moistureFactor, fertilityFactor, temperatureFactor)
```

Each tick of plant growth deducts a declared water/nutrient budget before adding biomass. Herbivore consumption removes cohort biomass through the bridge; a depleted cohort is dormant rather than deleted, and may recover if conditions permit. All quantities use explicit non-negative clamps and declared accounting fields. No plants reproduce, disperse, compete, mutate, or create new patch positions in this slice.

## Plant genetics and ecology expansion

The next P4 slice adds a compact `PlantGenome` and stable lineage/generation data. The initial genes are growth rate, mature biomass/seed investment, water demand, nutritional value, defense, dispersal range, and moisture/temperature tolerance. These genes must have countervailing costs: fast growth consumes more water; high nutrition costs producer growth or maintenance; defense reduces edible value but costs growth; long dispersal reduces local establishment efficiency; broad tolerance has a growth penalty. There are no strictly dominant genes.

Plant reproduction occurs in a separate deterministic phase after growth and consumption. Mature biomass creates a bounded seed budget, keyed random streams choose dispersal direction/distance, and establishment tests the target patch's capacity and environment. Establishment may replace a low-biomass resident only under an explicit competition rule; it cannot silently create biomass. Mutation and crossover are not required for initially asexual plants; the reproduction contract therefore supports clonal mutation first and can add two-parent pollen genetics later without changing patch ownership.

## Consumer interaction

The animal's resource scoring receives the projected nutrition, quantity, travel burden, and eventual plant defense cost. Digestion/food efficiency determines usable energy; defense imposes a heritable digestion burden or reduced yield, not a hardcoded animal role. The first slice retains current consumer efficiency semantics so P0--P3 paired experiments remain comparable when plant mode is disabled. P4 experiment configurations opt into plant-backed food explicitly.

## Environment fields and determinism

P4 adds slow, array-backed ground-plane fields for moisture, fertility, and temperature. They are sampled by patch position; they are not Unity terrain data. The first slice may use constant field values to isolate biomass accounting, then deterministic rainfall/temperature scenarios alter field values at scheduled ticks. A seed namespace splits world-layout, plant reproduction, and plant mutation streams from existing creature streams. Fixed-step ordering is: environment update, plant growth, plant reproduction/establishment, resource projection, creature perception/decisions/movement/consumption, creature reproduction/death, statistics/events.

## Statistics, diagnostics, and tests

Global statistics add total plant biomass, plant growth, plant consumption, dormant cohort count, plant births/deaths, mean plant traits, and conservation residual. A selected visual can display its paired cohort's biomass, growth limit, nutrition, defense, and lineage when available. Benchmark output reports active/dormant cohort counts and plant-update cost separately from creature loops.

The required test ladder is: deterministic plant growth/consumption/projection unit tests; conservation tests covering growth, consumption, and dormant recovery; fixed-seed state-hash tests with plant mode enabled; an herbivory control showing defended versus undefended cohorts change plant survival; a paired-seed consumer digestion experiment showing trait response to defense; spatial-disturbance dispersal experiments; rainfall/temperature controls; and producer removal/recovery. P4 only exits once a reciprocal plant/consumer trait response repeats across paired seeds and accounting stays within declared numerical tolerance.

## Presentation and scope limits

The initial presentation is simple pooled ground-plane vegetation, colored/scaled from cohort biomass and defense. It does not introduce terrain generation, individual meshes, biomes, planet geometry, species labels, history views, full Burst conversion, or world LOD. Those remain P5--P7 work. The plant systems use blittable-style structs, fixed arrays, IDs, and explicit loops so jobs/Burst can be introduced later without redesigning plant ownership.

