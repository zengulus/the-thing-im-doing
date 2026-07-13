# Roadmap — The Living Archive

**Status:** 14 July 2026

This is the sole authoritative executable plan for The Thing I'm Doing.

- [design.md](design.md) defines the game and its player-facing invariants.
- [rules.md](rules.md) defines architectural and content constraints.
- [future.md](future.md) is the north star and idea bank. Its maturity labels do not confer delivery status, priority, promise, or implied build order; only promotion into this roadmap does that.

The order here is deliberate. Complete and review each milestone's exit gate before treating the next milestone as active. Small experiments may look ahead, but they do not change milestone status or establish a second backlog.

---

## 1. Destination: the Living Archive

The long-horizon ambition is Dwarf Fortress-scale systemic depth without building a Dwarf Fortress-like world. Each campaign is an expedition into a newly generated alien reality: an internally consistent place whose matter, life, causality, inhabitants, ruins, and magical grammar may not share human categories.

The game remains a readable tactical roguelike about constructing short magical Workings. Its centre is:

> Enter an alien stratum → observe its laws → form a hypothesis → construct a short Working → preview the omen → act → revise what you believe.

The world should eventually answer questions such as:

- What counts as an entity here, and how can one persist, divide, merge, or cease?
- What does this ecology exchange instead of ordinary food or energy?
- Why does memory adhere to one substance and flee another?
- Is an apparent enemy an organism, a social role, a weather process, a recurring event, or several at once?
- Which past process left the trace now mistaken for a ruin?
- How did the expedition's interference change a later site, ecology, agency, or history?

This ambition is about compounding interaction, not enormous catalogues. A simulation layer earns its place only when it changes prediction, exploration, tactical choice, or causal storytelling.

### Product invariants

These constraints become mandatory at the named stage and must be preserved thereafter. This distinguishes principles the baseline already supports from properties later milestones still have to establish.

1. **Short readable Workings — current onward.** Complexity belongs in relationships between simple clauses, not unrestricted programming.
2. **No unconditional direct damage — M1 onward.** Selecting or aiming at a target is never sufficient to harm it. Direct damage requires readable situational setup such as a mark, exposure, position, collision, material state, terrain relation, observed capability, or world law. An automatic resource cost alone does not qualify.
3. **Coherent strangeness — M1 onward.** Alien rules may be unfamiliar, but repeated evidence must make them learnable.
4. **Preview and resolution parity — current for Working resolution; M2 for replay.** Omen preview, trace, AI prediction, replay, and live resolution use the same mechanics where each exists.
5. **Exploration as reward — M1 onward.** Observation, evidence, optional risk, specimens, routes, and confirmed understanding matter more than body count.
6. **Inspectable causality — M1 onward.** The player and author can ask why a result occurred and receive a structured answer.
7. **Deterministic identity — M2 onward.** A seed, resolved ruleset, and action log reproduce the same result.
8. **One content model — M2 onward.** Base content, generated content, and mods use the same IDs, schemas, validation, provenance, and hooks.
9. **Persistent consequences — M4 onward.** Important expedition changes survive encounters and remain causally legible.

Alien never means arbitrary, and horror does not mean random incomprehensibility. Unease should come from coherent implications, indifference, scale, and incomplete knowledge.

---

## 2. Shipped baseline

The following is the observed playable baseline in the repository on 14 July 2026. It is a regression fixture, not proof that the larger design works.

### Playable build

- A finishable five-encounter Ashen Archive run with carried health, rewards, victory, and defeat.
- Deterministic procedural tactical maps with connected rooms and corridors, seeded terrain, fog of war, and camera-follow exploration.
- A fixed authored roster of six ordinary enemy archetypes plus one boss, with line of sight, awareness propagation, and bounded active AI.
- Two editable Workings, JSON semantic data, validation, deterministic cloned-world preview, omen traces, and live casting.
- Clauses, behavior primitives and graphs, effects, enemies, environments, rules, relics, rewards, encounters, and runs loaded from JSON content.
- A base/mod loading path and reusable content atoms.
- One relic hook exercised through the same preview/live path.
- Six representative run seeds that automated tests can finish.

Procedural maps are shipped. Procedural alien ontologies, ecologies, histories, and enemy synthesis are not.

### Observed content inventory

| Authored family | Baseline count |
|---|---:|
| Clauses | 20 |
| Behavior primitives | 30 |
| Composite behaviors | 36 |
| Enemies | 7, including the boss |
| Environments | 4 |
| Local rules | 4 |
| Effects | 5 |
| Relics | 1 |
| Rewards | 7 |
| Encounters | 5 |

The test suite had **155 passing tests** when observed. That number is a floor for preservation, not a coverage claim. Future status reports must state the command, build, and result rather than repeating the count as a timeless fact.

### What “shipped” does not mean

The baseline establishes that the current run can work. It does not yet establish:

- that first-time players understand or enjoy the Working loop;
- that the run is protected by a required 20-seed scenario corpus with action scripts, golden invariants, and repeatable reports;
- that preview, turns, generation, and UI meet measured performance budgets;
- that a canonical resolved-content snapshot or fingerprint exists;
- that a run can be saved, migrated, loaded, or replayed from a durable envelope;
- that source-pack provenance and dependency diagnostics are complete;
- that player-facing Working import/export exists;
- that exploration discoveries, rather than floor completion, drive rewards;
- that a handcrafted alien mystery is understandable;
- that any alien generator produces coherent or tactically distinct worlds.

---

## 3. Open evidence gaps

M0 is **in progress**, not complete. The playable build exists; its preservation and measurement gate remains open.

| Evidence needed | Current evidence | Required closure |
|---|---|---|
| Regression safety | 155 tests; six representative seeds auto-finish | A checked-in seed/action corpus, golden invariants, and repeatable reports |
| Human comprehension | No recorded human playtests | Structured first-time sessions with recorded answers and failure themes |
| Conditional harm | The baseline still has an unconditional direct-damage clause | Retire it in M1; every direct-damage path then requires readable situational setup |
| Performance | No documented benchmarks | A recorded reference environment, scenarios, sample sizes, p50/p95, and hard budgets |
| Resolved ruleset identity | No canonical snapshot or fingerprint | An interim baseline manifest in M0; durable semantic snapshot/fingerprint in M2 |
| Replay determinism | Seeded maps and deterministic local systems | Fresh-process replay equivalence for a fixed seed and action-log corpus |
| Durable state | Run state carries only during play | Versioned save envelope, migration path, atomic write, and round-trip evidence in M2 |
| Alien xenology | Design ambition only | One handcrafted First Mystery that players can investigate and explain |
| Alien generation | Fixed authored roster | Bounded synthesis with coherence, counterplay, novelty, and determinism reports |

Do not describe a gap as complete because an interface, schema, or placeholder exists. Completion requires the corresponding exit evidence.

---

## 4. Planning assumptions and evidence protocol

This roadmap intentionally assigns no dates, owners, or assumed team capacity. Scope is controlled by exit gates. Review may split a milestone into smaller slices, but may not silently waive its evidence.

### Dependency rule

The critical path is:

```text
M0 preserve and measure
  → M1 handcrafted First Mystery
  → M2 systematise the proven shapes
  → M3 bounded alien generator
  → M4 persistent expedition
  → M5 deeper materials, ecology, and physics
  → M6 agencies, knowledge, and flows
  → M7 history and world scale
```

Later milestones expand minimal earlier foundations. For example, M1 may use explicit authored discovery keys; M2 turns that proven need into stable event IDs; M4 expands the event envelope into persistent world history; M7 uses the same event system for deep time. Do not build the full later system early, and do not replace an earlier foundation with an unrelated second model.

### Standard evidence sets

Each evidence set activates only when its named milestone introduces the capability:

- **Smoke corpus — M0 onward:** 20 named seed-and-scenario cases covering small and large maps, all four environments, every local rule, every enemy, the boss, every reward, and the relic hook. Each case executes its recorded actions and compares named golden outputs; coverage is collective across the corpus.
- **Replay corpus — M2 onward:** at least 100 seed plus action-log cases, replayed in two fresh processes against the same resolved content.
- **Generator batch — M3 onward:** at least 1,000 seeds per generator acceptance run, with rejects and reasons retained in the report.
- **Performance sample — M0 onward:** 10 warm-up iterations and at least 100 measured iterations per frozen scenario on a recorded reference machine and build.
- **Playtest sample — M0 onward:** first-time participants who have not been told the intended solution; record participant-to-scenario assignment, observations, answers, abandoned hypotheses, assistance given, and the pass/fail rubric.
- **Regression rule — current onward:** the complete test suite passes. Removing a test requires an explicit reason and replacement evidence when behavior still matters.

Generated instances do not count as authored definitions. Batch success does not substitute for human comprehension, and a human playtest does not substitute for deterministic replay.

### Performance budgets

Every benchmark report records the commit and content fingerprint where available, release/debug configuration, runtime, operating system, CPU, memory, display mode, scenario ID, timing boundary, raw samples, and nearest-rank p95. Freeze representative scenarios before measuring: an ordinary and maximal current floor; a five-clause Working with active rule and relic hooks; a fully populated active enemy radius; and the smallest and largest supported floor generation.

Simulation timings exclude rendering and animation. UI latency is measured separately from input dispatch to the first rendered state that acknowledges the input. On the recorded reference environment:

- ordinary Working preview or cast resolution: p95 at or below 16 ms;
- ordinary enemy-turn resolution: p95 at or below 50 ms;
- input-to-visible UI response: p95 at or below 50 ms;
- current-run floor generation: p95 at or below 500 ms;
- no graph, event, relationship, physics, or hook traversal may be unbounded.

Later site, stratum, world, save, and generation budgets must be set from measured prototypes before their milestones can exit. A budget change requires a documented scenario and rationale; it cannot be hidden by averaging away a worst-case class.

### Review cadence

Review the roadmap at four points:

1. **At milestone entry:** confirm upstream evidence, freeze the milestone's smallest testable slice, and list the relevant seed and playtest scenarios.
2. **During implementation:** review after each playable slice; update measurements and risks, not the milestone order.
3. **At exit:** publish the gate report with tests, seeds, rejects, benchmarks, playtest findings, and known limitations.
4. **After exit:** promote only discoveries that change player decisions; move speculative extensions to `future.md`.

Content counts are reviewed after quality gates, never used to overrule them.

---

## 5. Now / Next / Later

### Now

- **M0 — Preserve and measure the shipped run.** Establish trustworthy regression, determinism, performance, and first-time-player evidence.

### Next

- **M1 — Handcraft the First Mystery.** Prove expeditionary xenology and exploration-led reward in one controlled micro-slice before generalising it.
- **M2 — Systematise proven shapes.** Build the resolved snapshot, fingerprint, provenance, named RNG, minimal saves/events, and tools demanded by M0–M1.

### Later

- **M3 — Build a bounded alien generator.** Generate a small, validated possibility space rather than an unconstrained universe.
- **M4 — Make one expedition persist.** Revisit a changed site and explain its causal history.
- **M5 — Deepen materials, ecology, and physics.** Let substances, bodies, environments, and niches interact consistently.
- **M6 — Add agencies, knowledge, and flows.** Give inhabitants reasons, dependencies, traditions, and changing relationships beyond encounter placement.
- **M7 — Add deep history and world scale.** Simulate causally linked pasts and multiple levels of detail without losing determinism or legibility.

“Later” means dependency-later, not optional and not scheduled.

Only M0 is active at this status snapshot. Items within Next and Later remain strictly ordered; proximity in this view does not permit M2 to start before M1 exits.

---

## 6. Milestone plan and exit gates

### M0 — Preserve and measure the shipped run

**Status:** in progress.

**Purpose:** turn the Ashen Archive from a working build into a measured regression fixture without expanding systemic scope.

**Deliver:**

- A canonical baseline inventory of content IDs, schemas, and relevant files.
- An interim source manifest/hash that detects a changed baseline. This is not the durable resolved-content fingerprint promised by M2.
- A checked-in 20-seed smoke corpus with deterministic scenario scripts and named golden outputs.
- Golden assertions for map topology, encounter composition, reward choices, preview/live outcomes, boss completion, and victory/defeat transitions.
- A benchmark harness and recorded baseline for preview, cast, enemy turns, UI response, and floor generation.
- At least five first-time human sessions on the current Working loop.
- A concise evidence report naming the command, build, environment, failures, and accepted limitations.

**Exit gate:**

- All tests pass with no unexplained regression below the observed 155-test baseline.
- All 20 smoke cases execute their recorded scenario scripts and match their named golden outputs; together they exercise every environment, local rule, enemy, boss, reward, and the relic hook. The six known auto-finish seeds still finish.
- Benchmark samples meet the budgets in Section 4 or the gate remains open with a profiled cause.
- At least four of five first-time participants can explain what their Working attempted, identify why its preview succeeded or failed, and name a useful edit without being given the answer.
- The baseline manifest changes when a relevant source definition changes and remains stable when no relevant input changes.
- The report explicitly says that production fingerprinting, saves, and full replay equivalence are not yet shipped.

M0 does not pass on the existing automated tests alone.

### M1 — Handcrafted First Mystery xenology micro-slice

**Status:** queued; blocked by M0.

**Purpose:** prove the Living Archive's distinctive investigation loop by authoring one excellent mystery before building a generator.

**Deliver:**

- One compact alien site centred on one defining law and, at most, one interacting secondary law.
- At least three independent, consistent forms of evidence: for example terrain behavior, entity behavior, a specimen, an inscription, or an omen consequence.
- A minimal observation → hypothesis → confirmed/refuted knowledge state using explicit authored discovery keys.
- A tactical or traversal problem whose safer or richer resolution depends on inferring the law.
- At least one optional discovery or route and one knowledge-bearing reward not granted for merely killing every enemy.
- Plain-language inspection and omen explanations that reveal observed facts without leaking undiscovered truth.
- At least two viable solution strategies using short Workings and the existing game centre.
- Replacement or redesign of the baseline unconditional direct-damage clause so every direct-damage path in both the Ashen Archive and First Mystery requires readable situational setup. A target selector or automatic resource cost alone is not setup.
- A “shape inventory” recording which concepts proved reusable and therefore belong in M2 schemas and tools.

**Exit gate:**

- At least eight first-time participants play without being told the law; at least six correctly state it in their own words and cite two pieces of evidence.
- At least six use preview or trace evidence to revise a Working or route choice productively.
- Across the sample, players demonstrate at least two distinct valid approaches.
- Critical Workings render as no more than five plain-language clauses.
- No legal Working can deal direct damage from target selection alone. Every accepted direct-damage trace names the enabling encounter state, and the smoke corpus still completes without a universal attack.
- Discovery and reward triggers cannot be farmed by repeating the same observation.
- Working-caused outcomes use the same preview/live path. Discoveries, routes, and rewards expose causal provenance back to their authored evidence and rules without pretending that every consequence is a spell preview.
- The micro-slice and existing Ashen Archive both pass M0 regression and performance gates.

Do not start an ontology, ecology, or enemy generator to satisfy M1. Handcraft the causality and learn what must be systematised.

### M2 — Systematise the proven shapes

**Status:** blocked by M1.

**Purpose:** turn the stable needs exposed by the shipped run and First Mystery into one deterministic content and simulation foundation.

**Deliver:**

- One immutable resolved-content snapshot per session.
- A canonical semantic content fingerprint used by saves and replays.
- Source-pack provenance, manifests, dependency order, compatibility ranges, schema versions, and explicit add/replace/disable/remove semantics.
- Typed queries for stable IDs, tags, relationships, and generation constraints.
- Unified diagnostics with severity, source, dependency context, and actionable messages.
- Named deterministic RNG streams with recorded seeds and isolation between unrelated systems.
- A versioned minimal save envelope for the current run and First Mystery, with atomic writes and migration fixtures.
- Stable discovery/event IDs and a typed minimal event envelope: time, participants, location, causes, and structured consequences.
- A validation command, resolved-registry browser, override/provenance inspector, seed runner, replay harness, and content coverage report.
- Player-facing Working export/import validation, with semantic data independent of optional layout.

This milestone creates minimal save and event foundations. M4 expands them into persistent sites and world state; it does not introduce a second save or event model.

**Exit gate:**

- Fingerprints are identical for semantically identical resolved content across fresh loads and supported build targets.
- Changing any resolved semantic value changes the fingerprint; whitespace and irrelevant source ordering do not.
- Every resolved definition reports its source and override chain.
- Adding an RNG draw to one named stream leaves unrelated streams unchanged.
- The current run and First Mystery save/load round-trip with the same fingerprint and outcome; one older fixture migrates; an incompatible fingerprint fails with an actionable diagnostic.
- The 100-case replay corpus produces equivalent canonical events and final state across two fresh processes.
- A test pack can add or override each supported content family without unrelated engine edits; invalid entries fail independently and name their source.
- The authoring tools complete against base content and the test pack with machine-readable exit status.

### M3 — Bounded alien generator

**Status:** blocked by M2.

**Purpose:** generate a small space of coherent alien realities and enemies using the proven contracts, with strict rejection and work budgets.

**Deliver:**

- A bounded campaign kernel selecting a compatible defining law, substances/states, evidence projections, local rule hooks, and magical couplings.
- Authored grammar families for morphology, persistence, senses, drives, encounter roles, and atomic behavior capabilities.
- Generated species and bounded behavior graphs with stable run-local IDs.
- Coherence checks tying morphology to locomotion, senses to targeting, drives to actions, substances to vulnerabilities, and local ecology to placement.
- Counterplay validation against actually available clauses, terrain, and knowledge.
- Generated telegraphs, intent prose, inspectable components, and codex hypotheses derived from the same atoms.
- Encounter selection constrained by campaign kernel and local niche rather than a fixed roster alone.
- Batch reports for validity, rejects, duplicate shapes, complexity, counterplay, and distribution.

**Exit gate:**

- A 1,000-seed batch terminates within explicit graph and generation work limits, with zero accepted invalid references, impossible required counters, or untelegraphed combat actions.
- Every rejection records a stable reason; rejection rate is below 5% for the exact manifest and fingerprint designated as the production pool in the gate report.
- Re-running any accepted seed with the same fingerprint yields the same laws, species, encounters, names, and behavior graphs.
- A review sample of 50 accepted species contains no semantic duplicates, defined as the same canonical morphology, persistence, senses, drives, capabilities, and behavior graph after names and numeric tuning are removed. Every sampled species can be explained from inspectable atoms.
- At least eight blinded participants each play two kernels drawn from a pool of at least eight. At least six infer the defining law in both assigned kernels, cite two observations for each, and explain one tactical distinction between them.
- Across a frozen 100-encounter sample, no unchanged semantic Working completes the encounter objective in more than 80 cases. Changing only the selected target does not count as revision.
- Preview, intent, and live execution traverse the same generated behavior representation.

Keep the possibility space deliberately small. Add grammar only after failures are understood; random stat variation does not count as novelty.

### M4 — Persistent alien expedition

**Status:** blocked by M3.

**Purpose:** make one generated reality remember the expedition and make its consequences inspectable.

**Deliver:**

- Stable world, stratum, site, species, entity, substance, artefact, relation, and event IDs built on M2.
- An append-only typed event log, world clock, reducers, schema migration, and causal links.
- One revisitable site with persistent condition, discoveries, occupants, and expedition-caused changes.
- One named entity or anomaly and one named artefact with reproducible origin, substance, relations, and history.
- Field notes, specimen records, inscriptions, and hypotheses projected from events rather than used as source of truth.
- A minimal expedition map and revisit flow.
- Save/load and replay of the persistent slice.

**Exit gate:**

- Revisit state reflects at least three earlier player actions by changing a route, risk, evidence source, occupant behavior, local condition, available interaction, or reward—not prose alone.
- The UI can answer “why is this condition here?” with a causal chain back to events and rules.
- Saving before each change, loading, and replaying produces the same site state and projected records for the 100-case replay corpus.
- Promotion from stored site state to tactical state and demotion back conserve named entities, important conditions, discoveries, and causal IDs.
- Event growth and causal inspection are bounded and meet budgets established from the prototype.

### M5 — Deeper materials, ecology, and physics

**Status:** blocked by M4.

**Purpose:** make alien places physically and ecologically coherent using shared, deterministic reducers.

**Deliver:**

- Layered terrain cells: topology/substrate, structure, surface, contained phases, fields, marks, temporary effects, ownership/coherence, and occupancy.
- Composable material properties and bounded state transitions shared by terrain, bodies, artefacts, effects, and rules.
- Deterministic reducers for collision, force, support, fracture, heat, phase change, flow, pressure, conductivity, and at least one alien field.
- Explicit ordering, dirty-region updates, work limits, conservation checks, typed events, and deterministic tie-breaking.
- Morphology topologies, components, substance layers, damage states, capabilities, persistence models, senses, and drives.
- Coarse ecology networks of sources, sinks, transformations, niches, exchanges, migration, and adaptation.
- Tactical occupants and resources projected from site conditions and ecological viability.

These systems expand the minimal substance, morphology, niche, and rule hooks from M3. They do not replace generated species with a separate handcrafted entity architecture.

**Exit gate:**

- Changing one material property changes terrain, entity, artefact, and Working interactions consistently in fixtures.
- Identical seeds and actions produce identical multi-step physical cascades in preview, replay, and live play.
- A 1,000-seed batch produces no unbounded cascade, invalid ecology, or entity unsupported by its local niche.
- At least three physical interactions open a route, expose evidence, recover a specimen, or test a hypothesis rather than only deal damage.
- Promotion/demotion between tactical and site ecology conserves named entities, important population facts, substances, and conditions.
- Physics and ecology meet budgets established at milestone entry, including worst-case dirty-region and cascade scenarios.
- Inspection remains readable: players can identify relevant layers, predicted transitions, and uncertainty without seeing undiscovered facts.

### M6 — Agencies, knowledge, and flows

**Status:** blocked by M5.

**Purpose:** make inhabitants and sites act for sustained reasons beyond combat placement.

**Deliver:**

- Alien agencies modelled through imperatives, coherence or membership, roles, continuation, contradiction, and schism without assuming human government.
- Typed relations among entities, agencies, species, substances, laws, sites, artefacts, and events.
- Coarse sources, sinks, pathways, transformations, accumulation, extraction, exchange, parasitism, tribute, and theft.
- Knowledge state for clauses, Workings, laws, observations, and countermeasures.
- Teaching, witnessing, research, theft, suppression, forgetting, mutation, and independent discovery.
- Agency/ecology-specific magical dialects built from the shared primitive vocabulary.
- Conflicts, accommodations, requests, and rewards derived from state rather than isolated quest scripts.

**Exit gate:**

- For at least three generated agencies, manifestations, artefacts, magic, relations, and opposition follow from their imperative, history, and sustaining flows.
- Disrupting a pathway produces a deterministic later scarcity, adaptation, collapse, accommodation, or conflict visible on revisit.
- A piece of knowledge moves through at least three distinct mechanisms and changes later behavior or encounters.
- A 1,000-seed batch contains no agency with unsatisfied mandatory inputs and no source, no unbounded relation traversal, and no state-derived objective without an achievable or explicitly impossible explanation.
- At least eight blinded participants each inspect two agency-caused situations; at least six explain one action by citing an imperative, relation, flow, or prior event shown in game, without reducing the explanation to a generic human faction stereotype.

### M7 — Deep history and world scale

**Status:** blocked by M6.

**Purpose:** generate deep time according to each reality's causal grammar and preserve playable evidence at multiple simulation levels.

**Deliver:**

- Historical event grammars for emergence, migration, construction or growth, exchange, invention, observation, conflict, disaster, metamorphosis, succession/continuation, cessation, and ruin.
- Tactical, site, stratum, and world simulation levels with tested promotion and demotion.
- Generated residues, archives, scars, ruins, artefacts, obligations, absences, and legends tied to event chains.
- Named actors, sites, artefacts, agencies, and anomalies consistent across state, history, prose, and play.
- Bounded world generation with progress, cancellation, diagnostics, reproducible seeds, and empty-world rejection.
- Chronicle and causal-inspection interfaces using the event log as source of truth.

**Exit gate:**

- A 1,000-world batch terminates within recorded budgets with zero accepted empty or structurally invalid worlds.
- A reviewed sample of 50 sites each contains evidence of at least three causally linked past events, including one link that changes a current decision.
- Named facts agree across simulation state, chronicle, inspection, and tactical projection.
- Promotion/demotion conserves required identity, allegiance/coherence, purpose, injuries/state, artefacts, and causal history.
- Replay equivalence holds across all four simulation levels for the accepted corpus.
- World-scale performance meets the entry budgets for generation, background advancement, save/load, and inspection.
- In at least eight two-hour sessions spanning three or more sites each, at least six participants identify three causally distinct situations, explain the evidence behind two of them, and complete the route without requiring a handcrafted campaign storyline or author intervention.

---

## 7. Parallel workstreams

These lanes run through every milestone. They do not bypass the critical path and each must contribute evidence to milestone exit.

### Explainability and xenology UX

- Extend omen trace into general causal inspection: why is this here, why did this happen, and what changed it?
- Separate observed fact, hypothesis, confidence, and hidden truth.
- Use progressive disclosure, comparisons, filters, causal links, and plain-language ritual prose.
- Treat uncertainty as structured knowledge, not arbitrary concealment.

### Determinism, tests, and observability

- Unit-test atoms and reducers; property-test geometry, generation, and invariants.
- Maintain behavior fixtures, golden seeds, replay equivalence, migration fixtures, and malformed-content fuzzing.
- Record generator rejects, traversal budgets, causal events, and profiling spans in machine-readable reports.
- Test conservation whenever state changes simulation level.

### Performance and scale discipline

- Profile before reducing fidelity.
- Keep graph, hook, relationship, event, physics, generation, and inspection work explicitly bounded.
- Simulate exact tactical state only where decisions require it; use deterministic aggregates at site, stratum, and world levels.
- Promote and demote through tested contracts rather than keeping the entire world tactically alive.

### Content authoring and modding

- Ship schemas, provenance, diagnostics, examples, migration notes, and validation with every supported family.
- Require each content item to name its interactions, decision, discovery path, preview/trace projection, fixtures, and mod reproduction path.
- Prefer atoms with several consumers and grammars with many valid instances.
- Keep base and mod content indistinguishable after resolution.

### Accessibility and readable presentation

- Keep a plain-language rendering for every Working and causal trace.
- Never require color, animation, or encyclopedic memory as the only information channel.
- Add keyboard navigation, scalable presentation, search, and nested inspection alongside systems rather than after them.
- Test the information hierarchy with first-time players at every playable milestone.

---

## 8. Long-horizon targets

These are directional library targets after the corresponding systems have passed their gates. They are not milestone exit criteria, commitments, or permission to manufacture shallow variants.

| Family | Current authored baseline | First proven library | Long horizon |
|---|---:|---:|---:|
| World/stratum grammars | 0 alien grammars | 2–4 | 16+ |
| World-law atoms/operators | 0 | 20–30 | 200+ |
| Clauses | 20 | 30–40 | 100+ carefully bounded |
| Behavior primitives | 30 | 40–50 | 100+ |
| Composite behaviors | 36 | 100–150 | 1,000+ |
| Enemy seed grammars/exemplars | 7 fixed enemies | 20–40 | 250+ |
| Morphology/component atoms | 0 explicit library | 30–60 | 400+ |
| Sense/drive atoms | implicit in current content | 20–40 | 250+ |
| Materials/substances | implicit baseline | 30–40 | 300+ |
| Terrain/features | small fixed tile vocabulary | 40–60 | 400+ |
| Physics/transition rules | collision, push, temporary terrain | 20–30 | 200+ |
| Ecology relationship atoms | 0 | 30–50 | 500+ |
| Relic/artefact templates | 1 | 40–80 | 500+ |
| Local rules/anomalies | 4 | 20–40 | 250+ |
| Encounter motifs | 5 encounters | 30–60 | 500+ |
| Agency seed grammars | 0 | 8–12 | 80+ |
| Historical event types | 0 | 20–30 | 200+ |
| Lore/inspection projections | small UI corpus | 100–200 | 3,000+ |

The clause library grows slowly because the ritual language must remain learnable. Most variety should come from laws, substances, senses, drives, relationships, behaviors, histories, and combinations—not near-duplicate damage clauses.

Long-horizon scale is accepted only when sampled instances remain coherent, distinguishable, tactically consequential, inspectable, deterministic, and performant.

---

## 9. Open decisions and review triggers

These decisions are deliberately unresolved. The stated default prevents accidental scope drift; the named review trigger is when the choice must become explicit.

| Decision | Working default | Resolve no later than |
|---|---|---|
| Role of the Ashen Archive | Regression fixture and possible onboarding expedition, not the template every generated reality must follow | M3 entry |
| Meaning of run, campaign, stratum, and site | `run` names the current five-floor fixture; do not add durable IDs for the other terms until their player-visible boundaries are written down | M2 schema freeze |
| First Mystery packaging | A controlled authored slice that may reuse the current engine and content pipeline without pretending to be a generated campaign | M1 entry |
| Spell-circle timing | Keep the graph and text projections until comprehension evidence shows a circle would clarify rather than conceal semantics | M1 exit review |
| Across-expedition archive | No retained power or universal truth by default; observations and named Workings are candidates only | M4 save-schema review |
| Reference and target hardware | Record the actual M0 reference environment before accepting budgets; do not infer a shipping target from the developer machine | M0 benchmark freeze |
| Owners, capacity, and dates | Unassigned in this repository until real staffing and availability are known; gates control order in the meantime | Each milestone entry |

A decision change updates this table, the affected milestone gate, and any conflicting entry in [future.md](future.md). It does not silently create a parallel backlog.

---

## 10. Decision rules

When choosing work or reviewing a proposal:

- Preserve and measure before expanding.
- Handcraft one understandable example before extracting a generator.
- Systematise when the next several examples would duplicate code, validation, provenance, or explanation.
- Add more examples when an abstraction has fewer than three meaningfully different consumers.
- Prefer one new interaction axis over ten numerical variants.
- Reject any direct-damage path that needs only a target or an automatic resource payment; require readable situational setup.
- Reject detail that cannot influence a player decision or causal story.
- Reject generation that cannot explain its output or bound its work.
- Do not count generated instances as authored depth.
- Do not start deep history before content identity, named RNG, saves, stable events, tools, bounded generation, and one persistent site have survived playtests.
- Preserve the Working as both a survival tool and an experimental instrument.

The Living Archive is reached by building a trustworthy machine for coherent alien realities that players can investigate—not by accumulating unrelated features or declaring unmeasured foundations complete.
