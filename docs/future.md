# Future — North Star and Idea Bank

This document records the design north star and a bank of ideas that may serve it. It is deliberately not a backlog, milestone plan, or promise.

[roadmap.md](roadmap.md) is the sole authority for development order, active scope, exit gates, systemic work, and content-scale targets. If this document implies a sequence or conflicts with the roadmap, the roadmap wins. [design.md](design.md) describes the game and [rules.md](rules.md) defines architectural constraints.

The purpose of this file is narrower:

* preserve the fantasy and player experience the project is trying to reach;
* distinguish the playable baseline from ideas that still need evidence;
* keep promising concepts without quietly turning them into commitments;
* state the dependency and validation question for every substantial idea;
* retire assumptions that the current build or long-term direction has superseded.

---

# 1. Status Language

Status describes confidence and maturity, not priority.

* **[Baseline]** — present in the playable build and suitable as a regression fixture. This says that the capability exists, not that human playtests have proved it fun, clear, or complete.
* **[Committed principle]** — a durable design constraint. Implementation may change, but proposals should preserve it.
* **[Candidate]** — compatible with the north star and worth keeping in the idea bank. It has no schedule or commitment.
* **[Gated]** — promising, but inappropriate to expand until the named dependency is real and the stated question can be tested.
* **[Experimental]** — high-risk research. Prototype cheaply, expect failure, and do not make core content depend on it.
* **[Superseded]** — retained only to prevent an obsolete assumption from returning. It is not active scope.

Dependencies in this file are logical prerequisites, not an ordered build plan. An idea becomes active work only when the roadmap says so.

---

# 2. Playable Baseline and Open Proof

## [Baseline] What already exists

The current build is a finishable single-map tactical sandbox. It replaces the earlier five-floor main run while retaining that authored run as a regression fixture. Its baseline includes:

* short, editable Workings assembled from clause nodes with ordinary and conditional flow;
* a semantic Working model with core versioned JSON import/export, layout metadata kept separate from meaning, and structural validation;
* deterministic preview on a cloned encounter, live casting through the same resolution path, and omen traces;
* data-driven clauses, behavior primitives and graphs, effects, enemies, local floor rules, relics, environments, encounters, rewards, and runs;
* one large procedural Archive with connected rooms and corridors, seeded terrain, exploration, fog of war, awareness, and line of sight;
* layered enemy behavior, six ordinary tactical roles, and exactly one embedded Obsidian Crown objective;
* the complete clause and relic toolkit at entry, two editable Workings, victory, defeat, same-seed retry, and new-seed regeneration;
* relic, rule, and effect hooks integrated into encounter resolution, including preview parity for the one implemented relic;
* base-content and mod-content loading, validation, a broad automated suite, and generated-sandbox playability fixtures;
* a functional graph editor and placeholder glyph rendering.

At this snapshot, the authored content contains 20 clauses, 30 behavior primitives, 36 behavior definitions, seven enemies including the boss, four reusable environments, four local rules, five effects, seven legacy rewards, one relic, six encounters, and two run definitions. The main game deliberately composes a smaller subset into one complete sandbox.

This baseline does **not** imply human playtest proof, player-facing Working sharing, auto-layout or repair, save/load or content fingerprints, or procedural synthesis of alien laws, ecologies, and entities. “Procedural” currently describes tactical map generation; the larger alien generator remains future scope governed by the roadmap.

These capabilities should not be described elsewhere in this file as work that has yet to begin. Their polish, breadth, authoring model, persistence, and long-horizon form may still be candidates or roadmap work.

## [Gated] What only human play can prove

Automated correctness is not evidence that the central loop is satisfying. Broad language growth, elaborate ritual UI, and large content multiplication should continue to answer the same unresolved human questions:

* Can a new player explain what their Working tried to do?
* Can they identify why it succeeded or failed from the board, preview, and trace?
* Does preview provoke a meaningful edit, or merely confirm an obvious action?
* Do enemies, terrain, and local laws make players revise a Working rather than repeat a dominant one?
* Does the player feel like they formed and tested a magical hypothesis?
* Are surprising outcomes legible enough to inspire another attempt rather than distrust?
* Do exploration and discovery feel rewarding without requiring extermination?
* Do Workings remain readable aloud as their expressive range grows?

**Dependency:** representative human playtests using the current playable run, followed by focused tests whenever a new interaction axis is introduced.

**Validation:** players can answer “what happened, why, and what will I change?” in their own words without an author explaining the system.

---

# 3. North Star

## [Committed principle] A readable ritual instrument

The player is not selecting spells from a list and is not writing unrestricted programs. The player constructs short Workings from ritual clauses.

A Working should read like a compact ritual sentence:

> Aim at the nearest foe.
>
> If they are marked, damage them.
>
> Otherwise, mark them.

The implementation may use graphs, IDs, counters, reducers, and behavior machines. The player-facing fantasy is intent made exact through glyphs, bindings, conditions, and consequences.

Prefer the terms:

* Working;
* Clause;
* Glyph;
* Refrain;
* Remembered sign;
* Omen trace;
* Backlash;
* Proving circle;
* Local grammar;
* Hostile dialect;
* Binding mark;
* Circle;
* Seal;
* Anchor.

Avoid player-facing use of `program`, `script`, `function`, `variable`, `register`, `boolean`, `AST`, `graph`, or `node`, except in explicit debug and authoring views.

The non-negotiable test is simple: if an ordinary Working cannot be rendered in plain language and read aloud as ritual grammar, it is too complex.

## [Committed principle] Expeditionary xenology

The long-horizon game is an expedition into a newly generated alien reality. Its matter, entities, persistence, senses, causality, ecology, history, and magical dialect may violate human expectations, but they must form a coherent and learnable system.

The player-facing loop is:

> Enter an alien stratum → observe its laws → form a hypothesis → construct a short Working → preview the omen → act → revise what you believe.

The world should feel indifferent, ancient, and partially unknowable without becoming arbitrary. Cosmic unease comes from discovering coherent implications, not from random incomprehensibility or a generic sanity meter.

Workings are both survival tools and experimental instruments. Omen previews, traces, specimens, inscriptions, repeated observations, and failed hypotheses should help the player build a reliable model of an unfamiliar reality.

## [Committed principle] Exploration is the reward loop

Combat may secure access, protect the expedition, alter an ecology, or create evidence. Killing every inhabitant must not become the default progression engine.

Reward-bearing actions should include:

* entering an unmapped stratum or optional site;
* observing a new capability, material transition, sense, or world law;
* corroborating or refuting a hypothesis;
* recovering a specimen, artefact, inscription, shed component, or historical trace;
* mapping a dangerous route or discovering a repeatable safe procedure;
* interacting with an anomaly rather than merely destroying it;
* recognizing how an earlier expedition action changed a revisited place;
* taking an informed risk to extract knowledge or material;
* finding a hidden relation, alternate route, or nonviolent resolution.

Clauses, relics, samples, supplies, omen clarity, prepared-Working options, routes, relationships, and durable knowledge are all possible rewards. Information is a first-class reward when it improves prediction or enables a new hypothesis.

## [Committed principle] Complexity belongs between simple things

The world may become extremely deep. Individual Workings should remain bounded, deterministic where promised, inspectable, and short.

Depth should come from interactions among simple clauses, material properties, effects, counters, local laws, entities, senses, drives, histories, and relationships—not from ever-longer Working syntax. Every important surprise needs a causal explanation available at the player’s current level of knowledge.

## [Committed principle] No unconditional direct damage

Choosing or aiming at a target must never be sufficient to deal direct damage. Harm needs a readable situational premise: a mark, exposure, position, collision, material state, terrain relation, observed capability, earned setup, or world law that is visible in preview and trace.

This is not satisfied by attaching an automatic resource cost to a universal attack. The player should have to read the current encounter and improvise a way to make harm possible. Environmental and indirect consequences may deal damage, but their enabling state and causal chain must remain inspectable.

The former unconditional damage debt is retired. Direct Damage is now a Consumer that requires and consumes a caster-owned mark from its focused actor or tile; a foreign mark does not qualify. The UI refuses to commit a no-effect omen.

---

# 4. Idea Bank

## [Committed principle] Omen trace grows into causal inspection

The existing preview and omen trace are the seed of a general “why?” interface. The baseline already explains invalid targets and local-rule interventions, supports collapsed and expanded trace views, steps through trace entries, highlights the taken Working path, and overlays the predicted final encounter state. It should continue to feel like reading a magical consequence, not reading a debug log.

Candidate capabilities:

* identify the actor, tile, material, effect, or relation changed by each clause;
* make untaken paths and their conditions inspectable alongside the already highlighted path;
* add typed provenance for failed conditions, backlash, prevention, amplification, and hook intervention;
* distinguish deterministic prediction from uncertainty and from facts the expedition has not learned;
* step forward and backward through per-step world states rather than trace text alone;
* compare before and after an edit, or compare two Workings against the same known state;
* project relevant enemy responses and local-law consequences without pretending to reveal hidden information;
* nest the existing concise and expanded views into a complete causal chain without flooding the combat HUD;
* extend the same provenance model to “why is this here?” and “what changed this state?”

**Dependencies:** one shared resolution path for preview and live play; typed provenance from clauses, hooks, rules, materials, and generated facts; a knowledge model that distinguishes unknown from random.

**Validation questions:** Can players locate the first cause of an unexpected result? Does added detail improve decisions without turning every turn into log analysis? Can uncertainty be communicated without breaking trust?

## [Gated] Spell-circle presentation

The graph editor is a functional authoring view. A spell circle is a candidate final projection of the same semantic Working, not a replacement data model.

Candidate visual grammar:

* targeting clauses as anchors, rays, and pointing marks;
* conditions as gates, splits, and thresholds;
* effects as impact glyphs, burns, cuts, wards, and bindings;
* memory as bound signs, stars, knots, and echoes;
* refrains as repeated ring segments or orbiting marks;
* triggers as dormant outer-ring sigils;
* execution as a lit path moving through the circle with failed and untaken branches still readable.

Every visible element should support inspection. Hover, focus, or controller selection should answer what it means, which clause or law produced it, what it references, what it costs, and what happens on failure. The affected board state should remain visually connected to the active glyph.

Every circle also needs a complete plain-language rendering. A malformed or missing layout must never corrupt the Working, and auto-layout must not change semantics.

**Dependencies:** stable semantic serialization; demonstrated player comprehension in the graph/text views; an accessible interaction model; trace events with enough provenance to animate honestly.

**Validation questions:** Can an unfamiliar circle be read more quickly than the graph it projects? Can players edit it precisely with mouse, keyboard, and controller? Does it reinforce ritual fantasy without hiding branch logic? Does it remain legible at combat-HUD scale and in reduced-motion mode?

## [Candidate] Player-facing Working sharing

Versioned Working JSON, structural validation, and semantic/layout separation already exist in the baseline. The candidate is a trustworthy player workflow around them.

Candidate capabilities:

* export and import a named Working without requiring file surgery;
* report missing or incompatible clause content clearly and without damaging existing Workings;
* preserve meaning when layout data is absent and offer deterministic auto-layout;
* show schema version, content requirements, warnings, and generated ritual prose before acceptance;
* optionally attach a counter summary, clause-family sequence, and a preview trace for troubleshooting;
* retain provenance for a discovered or player-named recipe;
* support compact share codes only as an encoding of canonical validated data;
* make shared Workings useful as expedition notes without letting imports bypass discovery or progression rules.

**Dependencies:** resolved content fingerprints and provenance; stable save and migration rules; actionable import diagnostics; a policy for content the recipient has not discovered or installed.

**Validation questions:** Can players diagnose an incompatible share without technical vocabulary? Is a shared Working still understandable without its original visual layout? Does sharing promote experimentation rather than distribute one universal solution?

## [Gated] Spell-language expansion

Add a clause only when it creates a new tactical or investigative relationship. More synonyms for damage, healing, or cost are not enough.

### [Baseline] Current vocabulary

The implemented language already includes selected and nearest-foe targeting; marked, occupied, and clear conditions; mark-gated damage, push, raised terrain, poison, bleed, wards, lightning charge, and resource-spend effects; and one primary Remembered sign that can hold and restore an actor or tile focus.

### [Candidate] Targeting extensions

* aim at the nearest marked foe;
* aim at a remembered sign;
* aim along a marked path;
* aim at the first observable tile in a direction;
* aim at an entity standing on a marked or material-qualified tile;
* aim at a component, relation, stimulus, or world-defined property the expedition can perceive.

### [Candidate] Condition extensions

* if wounded, burning, warded, alone, or surrounded;
* if adjacent to a known substance or structure;
* if standing in a known material, field, or local relation;
* unless warded;
* if the target has demonstrated a known capability;
* if the omen predicts a bounded, player-readable consequence.

### [Candidate] Effect extensions

* mend, bind, weaken, silence, chill, or kindle;
* pull, break, grow, open, or redirect;
* move, copy, scatter, invert, or consume a mark;
* alter a material state or field through the shared world rules;
* reveal evidence without automatically revealing the answer.

### [Gated] Memory and binding expansion

The baseline proves the mechanics of storing and recalling one actor or tile through a primary Remembered sign. Candidate extensions include remembering the first marked target, adding another deliberately distinct sign, swapping signs, and deliberately forgetting one. Keep the ritual vocabulary; never expose registers.

**Dependency:** human play must show that the existing Remembered sign produces useful choices without becoming required bookkeeping.

### [Gated] Refrains

Only bounded, previewable loops belong in the language: repeat twice, repeat thrice, for each visible marked foe, for each adjacent foe, or advance until blocked with a strict cap. Unbounded loops do not.

**Dependency:** traces must summarize repetition while allowing inspection of each iteration, and validation must enforce work budgets.

### [Gated] Triggers and delayed clauses

The baseline already has effect, rule, and relic triggers. Player-authored triggers could include when a foe enters a marked tile, when the caster is struck, when a structure breaks, when a mark expires, or when an observed entity emits a known action.

Delayed effects should use visible anchors and explicit events rather than hidden timers: anchor an effect here, release on the next turn, trigger when crossed, or repeat when a mark breaks.

**Dependency:** ownership, lifetime, stacking, event order, and preview uncertainty must all be visible. Trigger chains need hard execution budgets.

**Dependencies for language growth:** human comprehension of the current language; at least two meaningfully different enemies, materials, laws, or exploration problems that consume the proposed interaction; validator and trace support; and generation queries that know whether the expedition can use it.

**Validation questions for language growth:** What new decision does the clause create? Which enemies, materials, laws, or exploration problems consume it? Can a player predict it from prose and trace? Does it preserve a short Working? Can generated content ever make it mandatory when unavailable?

## [Committed principle] Enemies must pressure grammar

An enemy is interesting when it changes the Working the player wants to cast. Additional health is not a grammar pressure.

The current authored enemies remain useful exemplars and regression fixtures. The Ash Scribe already writes and detonates marks; the Root Saint already raises terrain and periodically heals near it; the Spore Cantor, Iron Pilgrim, and Moss Chirurgeon already exercise attrition, defense, and support roles. Candidate extensions include:

* **Glass Hound:** punish repeated direct effects, expose vulnerability after collision, or force a branch around adjacency;
* **Ash Scribe:** let the player move, erase, steal, or exploit its existing hostile marks;
* **Root Saint:** deepen the existing terrain-and-healing loop with breaking, pushing, redirection, and route creation;
* **Spore Cantor, Iron Pilgrim, and Moss Chirurgeon:** turn their existing attrition, defense, and support roles into composable ecology-shaped pressures;
* **Obsidian Crown:** remain a boss fixture for rule interaction rather than a template for inflated statistics.

Authored concept families may also serve as seed grammars:

* **Archive:** an Ink Leech travels marked paths; a Page Wraith becomes visible when marked; a Seal Warden blocks effects until its seal breaks; an Index Moth consumes marks while exposing hidden rules.
* **Ossuary:** a Bone Chorister empowers nearby remains; a Grave Ox charges down marked lanes; a Rot Apostle punishes staying still; a Marrow Thief steals a Remembered sign.
* **Foundry:** a Mirror Smith creates reflective terrain; a Charge Eater consumes charged tiles to mend itself; a Delay Imp postpones a visible clause; a Furnace Angel grows through heat.
* **Crown:** a Debt Collector burdens the next Working; a Null Moth consumes marks and queued effects; a Clause Eater suppresses the first condition; an Inversion Saint reverses one visible local law; a Crown Scribe writes hostile marks beneath every side.

These names are a vocabulary of tactical hypotheses, not a required fixed bestiary. Under the alien-expedition direction, ordinary entities should eventually derive from world laws, morphology, senses, drives, ecology, site history, and bounded behavior atoms. Authored creatures can remain exemplars, anomalies, bosses, and regression fixtures.

**Dependencies:** inspectable intent; counterplay validation; generated capabilities that share the same behavior substrate as clauses, effects, relics, and rules; ecological and material causes for an entity’s presence.

**Validation questions:** Does the entity force a revision rather than a damage race? Can the player state a testable hypothesis about its senses or behavior? Is at least one counterplay axis available in the current expedition? Does its morphology and ecology explain what it can do?

## [Committed principle] Terrain and local grammars must be readable

A region or stratum needs a small, readable grammar, not merely a different tile palette. A useful local grammar combines a material interaction, a mark or Working interaction, a movement consequence, an ecological consequence, and at most a small number of exceptional laws visible through evidence.

The authored environments in the baseline can continue as test fixtures and sources of interaction motifs:

| Motif | Candidate materials and interactions |
|---|---|
| Archive | paper, ink, seals, smoke, written marks, heat spreading through shelves, marks carried by ink |
| Ossuary | bone, remains, roots, rot, thorn growth, grave soil, growth redirected by healing or injury |
| Foundry | mirrors, charge, molten channels, astral glass, directional paths, visible delayed-effect anchors |
| Crown | corrupted sigils, debt, reversed marks, black stone, broken circles, local condition inversion |

Future alien strata need not preserve these categories. A generated local grammar may instead concern memory-bearing stone, crystallized intervals, attention flow, directional tissue, shed identity, or another world-defined substance and field.

Procedural generation already exists at the tactical-floor level. Its future value is not “more random rooms”; it is creating coherent magical problems from world laws, ecology, history, route choice, and known counterplay.

Anti-softlock constraints remain essential:

* never require a clause family, sense, material interaction, or remembered sign unavailable to the expedition;
* provide more than one counterplay axis for required obstacles;
* distinguish optional mysteries from mandatory route blockers;
* reject generated combinations that are incoherent, unreadable, or dominated by one universal Working;
* attach anomalies to observable evidence rather than surprise exceptions.

**Dependencies:** the layered terrain, material, physics, generation, and knowledge systems named by the roadmap; deterministic preview/live parity; generation constraints that query actual expedition capabilities.

**Validation questions:** Can players infer a local rule through repeated evidence? Does terrain change spell construction and route choice? Can a physical interaction reveal knowledge or access, not just deal damage? Do identical seeds and actions reproduce the same cascade?

## [Candidate] Relics and artefacts as rule mutations

Relic ownership, content definitions, hook timing, encounter execution, progression, traces, and deterministic preview support are baseline capabilities. Future relic design should exploit that substrate rather than reduce artefacts to passive statistics.

Candidate families:

* **Cost mutations:** make a narrow clause family cheaper, refund a deliberately constrained Working, or exchange focus for another legible obligation;
* **Clause mutations:** a push leaves a mark, a ward changes adjacent material, a chill propagates through a known conductor, or a mark reveals an intent;
* **Risk/reward bargains:** failed conditions create a benefit and a debt, repeated clauses become stronger and unstable, or varied Workings cleanse an accumulated cost;
* **World-law artefacts:** marks count as a local substance, stone remembers contact, fire refuses a known relation, or a failed condition is reinterpreted once under a visible rule;
* **Historical artefacts:** a generated relic has an origin process, material, maker or causal predecessor, relationships, inscriptions, and consequences discoverable in play.

The old strange-relic ideas remain useful provocations: forgotten signs exploding into marks, raised stone becoming briefly brittle, wounds changing target classification, or a relic storing the last emitted effect. None is committed content.

**Dependencies:** hook provenance in preview and trace; bounded trigger recursion; the unified content and event models; exploration-led acquisition; generated artefact history where applicable.

**Validation questions:** Does the relic change how a Working is written or how a world is investigated? Can the player see exactly when it intervened? Does it create several viable constructions rather than one compulsory combo? Can mods reproduce its shape without bespoke engine code?

## [Candidate] Ritual encounters

A boss and a complete run already exist. A future ritual encounter should be a legible rule system, not merely a large enemy.

Candidate encounter verbs include:

* reveal a true target through heat, smoke, marks, observation, or material transitions;
* break, preserve, or rewrite seals in an inferred causal order;
* contain or redirect growth without treating every living thing as a target to kill;
* prevent remains, structures, or fields from sustaining an entity;
* reflect an effect through visible relations;
* charge or release anchors in a readable sequence;
* use an entity’s own effect as part of a solution;
* survive a temporary local-law inversion;
* cleanse debt or convert hostile marks;
* protect evidence, an omen, a specimen, or an escape route while the encounter changes.

Rituals should test the stratum’s learned grammar, permit multiple solutions, reward prior observation, and make Working revision the dramatic centre of the encounter.

**Dependencies:** local rules already introduced through ordinary play; trustworthy preview and causal trace; encounter generation that knows available counterplay; non-combat success and reward states.

**Validation questions:** Did the player solve a system rather than discover an author’s single password? Did earlier exploration materially improve the encounter? Can a failed attempt teach a useful fact without requiring a wiki?

## [Committed principle] Expedition progression follows discovery

The baseline already carries health, rewards, clauses, and relics through a run. The candidate direction is to make those rewards consequences of exploration, evidence, optional risk, and understanding.

Candidate within-expedition rewards:

* a clause or clause-family variation learned from observation;
* an additional prepared Working option;
* improved interpretation of a known omen or material;
* a specimen, artefact, supply, route, relationship, or safe procedure;
* a named recipe preserved from a successful experiment;
* a curse, debt, or dangerous tool accepted with informed consent;
* another remembered sign only if memory has proved useful and legible.

Candidate backgrounds should alter starting grammar and investigative posture rather than merely statistics:

* **Ash Grammarian:** mark and heat interactions;
* **Stone Binder:** structure and displacement;
* **Mercy Scribe:** mending, warding, and ally relations;
* **Mirror-Taught Heretic:** reflection, return, and stronger omen use with visible risk;
* **Debt-Bound Adept:** controlled overreach paired with repayment grammar.

### [Gated] Across-expedition archive

A persistent archive may retain terminology, observations, specimen records, named Workings, failed theories, and optional starting approaches. It should preserve the history of investigation without converting a newly generated alien reality into solved content.

**Dependency:** persistent world and expedition-event identities, knowledge provenance, saves, and a clear distinction between player knowledge and current-world truth.

**Dependencies:** stable discovery and event identities; explicit novelty and reward-eligibility rules; causal inspection for why a reward became available; and playtest proof that information can feel valuable before large reward pools are authored.

**Validation questions:** Does progression increase expressive and investigative variety more than raw power? Are rewards causally tied to discoveries and stable event IDs? Does a new world still require observation? Can nonviolent and optional discoveries compete with combat rewards?

## [Committed principle] Editing and inspection remain accessible

Deep simulation must not require encyclopedic memory or precise pointer use.

Candidate editing support:

* save, name, duplicate, compare, and restore Workings;
* undo and redo semantic edits;
* highlight changed clauses and affected preview steps;
* filter available clauses by family, relation, known interaction, or provenance;
* warn when no execution path can affect the known world;
* switch among circle, structured text, and debug graph projections;
* support keyboard-only and controller-first construction.

Candidate accessibility support:

* icon shapes that remain distinct without colour;
* complete text alternatives for glyphs, traces, intents, and local laws;
* adjustable animation speed and reduced-motion execution;
* reduced visual noise, large tooltip, and scalable text modes;
* persistent focus order and no hover-only information;
* concise and expanded causal summaries;
* exportable plain-language Working descriptions;
* search, filters, comparisons, and progressive disclosure for expedition knowledge.

**Dependencies:** semantic UI actions independent of a particular visual projection; centralized terminology and text; input and focus testing; provenance rich enough to summarize; and, for saved or restored Workings, versioned persistence, migrations, and content-compatibility diagnostics.

**Validation questions:** Can the complete Working workflow be performed without colour, hover, drag-and-drop, or fast animation? Do advanced inspection tools reduce memory burden without revealing unknown facts? Can players choose the amount of detail they need?

## [Candidate] Narrative, visual, and audio identity

Narrative should reinforce exact systems: magic as literal agreement, ruins as hostile dialects, failure as misworded intent, and understanding as power.

Candidate lore surfaces include clause descriptions, specimen notes, ecology observations, inscriptions, ritual fragments, recipe names, background memories, proving-circle commentary, post-expedition archives, and causal chronicles projected from world events.

Candidate visual principles:

* bright ritual geometry against restrained, readable environments;
* faction, owner, or relation marks differentiated by shape as well as colour;
* strong silhouettes and telegraphs for observed capabilities;
* region- or world-specific scripts derived from a shared readable grammar;
* execution paths and affected state visible at board scale;
* unknown, hypothesised, and confirmed knowledge visually distinct without implying that unknown means random.

Candidate audio principles:

* clause-family motifs that layer across a Working;
* distinct sounds for failed conditions, uncertainty, intervention, and backlash;
* enemy-intent warnings linked to observed senses or capabilities;
* successful revision and discovery cues, not only damage confirmation;
* ambience that expresses the current world’s material and causal rules.

**Dependencies:** stable semantic events and content provenance; accessibility alternatives for all audio/visual information; generated presentation constrained enough to remain recognizable.

**Validation questions:** Can players hear or see a causal family before reading its details? Does presentation clarify grammar rather than decorate it? Can generated alien variation remain coherent and accessible?

---

# 5. Experimental Shelf

## [Experimental] High-risk Working and world interactions

These ideas may reveal useful mechanics, but each can damage legibility, determinism, or player ownership. They should remain isolated prototypes until evidence promotes them:

* entities that partially rewrite a Working while preserving a clear before/after account;
* corrupted clauses with two learnable meanings;
* spell duels in which bounded player and entity Workings interact;
* rituals that require non-damaging Workings;
* asynchronous traps attached to a saved Working;
* ally entities with their own small ritual grammar;
* editing during a visible enemy telegraph;
* generated dialect constraints or curses that alter syntax;
* entities that interfere with omen fidelity in a way the player can detect and counter;
* local laws that rename familiar clauses while preserving inspectable semantics;
* player-created named recipes appearing later as lore, evidence, or transmitted knowledge;
* simultaneous local rules only when their combined causal chain remains readable;
* hidden laws for expert play only when evidence can reliably reveal them;
* alien agencies that learn, steal, forbid, or mutate observed Workings.

**Dependencies:** strong causal inspection, strict graph and hook budgets, replayable simulation, knowledge provenance, and a baseline that remains understandable when the experiment is absent.

**Validation questions:** Can players distinguish deliberate interference from a bug? Does the experiment create a new hypothesis and counterplay axis? Can it be removed without invalidating ordinary content? Does preview communicate the limits of what it knows?

---

# 6. Superseded Assumptions

## [Superseded] This file as a build plan

Ordered phases, “immediate next” lists, and content targets do not belong here. The roadmap owns them.

## [Superseded] Baseline systems described as future prerequisites

The project no longer needs to postpone all design discussion until procedural floors, a complete run, Working JSON validation, progression, a boss, or relic hooks first exist. They exist. The open question is how well they support the north star and how they evolve under the roadmap’s systemic program.

## [Superseded] A fixed four-region campaign as the final destination

Archive, Ossuary, Foundry, and Crown concepts remain useful authored fixtures and motif banks. They are not a mandatory sequence for every future campaign. The north star is generated alien realities with their own ontologies, substances, ecologies, histories, and magical dialects.

## [Superseded] Procedural generation as room arrangement alone

Connected tactical floors are baseline. The meaningful frontier is coherent causal generation: laws shaping materials, ecologies, entities, sites, discoveries, and tactical problems.

## [Superseded] Floor completion and kills as the primary reward source

The current reward screen may remain a pacing fixture. Long-term reward value should derive from what the expedition explored, risked, recovered, changed, and learned.

## [Superseded] More clauses as the main source of scale

The clause library should grow cautiously. Most variety should come from relationships among laws, substances, senses, behaviors, entities, histories, sites, and existing clauses.

## [Superseded] Unconditional direct damage as a universal verb

An aim followed by an always-valid damage clause undermines improvisation and tends toward a dominant Working. Direct harm must instead be earned through readable encounter state, setup, and interaction.

---

# 7. Non-Goals

## [Committed principle] Things this project is not trying to become

* unrestricted visual programming with fantasy terminology;
* long Workings whose complexity substitutes for world depth;
* opaque randomness presented as alien mystery;
* a conventional fantasy bestiary with generated names and stat rolls;
* a kill-everything reward economy;
* a generic sanity meter or arbitrary “incomprehensible” effects;
* a catalogue of stat-only relics, enemies, or upgrades;
* unconditional direct damage hidden behind a target selector or automatic resource cost;
* hidden trigger stacks, unbounded loops, or timers the player cannot inspect;
* a spell-circle renderer that becomes the semantic source of truth;
* a simulation whose detail never affects decisions, evidence, exploration, or causal storytelling;
* a fixed authored content sequence masquerading as systemic replayability;
* mod support, validation, accessibility, explainability, determinism, or save compatibility deferred until after content production;
* this idea bank being treated as permission to bypass roadmap gates.

---

# 8. Idea Admission and Promotion

## [Committed principle] Admission test

An idea belongs in this file only if it materially strengthens at least one of these:

* readable Working construction;
* preview, trace, or causal understanding;
* enemy, terrain, or world-law pressure on spell choices;
* contextual setup for harm without unconditional direct damage;
* expeditionary observation and hypothesis testing;
* exploration-led reward and persistent consequence;
* coherent alien world generation;
* accessibility, sharing, modding, validation, or determinism;
* ritual fantasy without sacrificing semantic clarity.

Every admitted idea should name its status, dependency, and validation question. If it only adds volume, lore, statistics, or complexity, it needs a stronger interaction case.

## [Committed principle] Promotion test

A candidate becomes committed work only through an explicit roadmap change. Before promotion, ask:

* What player decision does it create?
* What existing atoms and systems does it combine?
* How will preview and trace explain it?
* What evidence would falsify the idea?
* What is its execution and cognitive budget?
* Can generated content and mods use it without a bespoke path?
* Does it preserve a playable, readable baseline?

Ideas that fail those questions should be narrowed, returned to experimental status, or marked superseded rather than left as ambiguous promises.
