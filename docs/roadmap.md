# Roadmap — The Living Archive

**Status:** 19 July 2026

**Current product:** complete systemic sandbox

**North star:** expeditionary xenology through learnable emergent systems

[design.md](design.md) is the shipped product contract. [rules.md](rules.md) contains mandatory design and engineering constraints. [future.md](future.md) preserves the wider idea bank; it is not a backlog.

## Direction

The long-horizon ambition is a generated alien reality whose matter, life, causality, inhabitants, and traces form an internally consistent system. The player enters, observes, forms a hypothesis, writes a short Working, previews an omen, acts, and revises what they believe.

The project grows by adding relationships between simple things, not by piling up floors, health bars, or near-duplicate content. An unfamiliar rule earns its place only when it is:

- coherent enough to learn from repeated evidence;
- important to exploration or tactical decisions;
- expressible through a short Working;
- deterministic where promised and honest about uncertainty;
- visible in preview, trace, and post-action causality;
- able to combine with existing terrain, actors, effects, or laws.

The Obsidian Crown is an eventual objective, not the centre of the game. It supplies closure while the alien environment and the systems that emerge inside it supply the play.

## Shipped sandbox

The main scene now delivers one complete, replayable expedition:

- one deterministic 40×28 connected Archive per seed;
- free exploration with fog of war, line of sight, camera follow, and dormant distant inhabitants;
- every ordinary enemy archetype placed into the same ecosystem;
- exactly one remote Crown, with ordinary combat optional and Crown death the sole victory requirement;
- a delayed, coarse echo that only points toward the objective after substantial exploration;
- two editable Workings and the complete registered clause/relic toolkit from the start;
- required Generator, Operator, or Consumer classification for every clause;
- setup-gated direct damage that consumes only the caster's own mark;
- deterministic cloned-world preview, transactional resolution, and stepwise omen traces;
- mutable terrain, pushing, collisions, marks, poison, bleed, wards, counters, memory, enemy support, and local-law hooks;
- explicit no-effect feedback, disabled no-op casting, inspectable intents/statuses, help, victory, defeat, seed retry, and confirmed restart;
- data-driven content and mod loading through stable IDs and validated registries.

The earlier five-encounter run remains in content as a regression fixture. It is not the current product structure.

## Completion gate

The sandbox is complete when all of the following remain true:

1. A generated map is connected, deterministic for its seed, and contains exactly one reachable Crown at a meaningful exploration distance.
2. Killing an ordinary enemy never ends the run; killing the Crown does, even if support enemies remain.
3. The full toolkit is available immediately and every clause has exactly one valid role.
4. Every direct-damage clause requires a visible semantic premise beyond target selection or an automatic resource toll.
5. Preview and live resolution use the same mechanics, and failed resolution rolls back partial changes.
6. The main scene imports, builds, starts headlessly, and reaches both victory and defeat states without script errors.
7. Automated tests, debug build, and release export all pass with no warnings or unexplained regressions.
8. The opening guide and fixed HUD explain exploration, the objective, clause roles, target controls, terrain, preview, and restart safety.

Changes that break this gate are regressions, even if they serve a future milestone.

## Next: prove one alien mystery

The next meaningful expansion is not another floor. It is one handcrafted, replayable mystery inside the sandbox that makes the player investigate an alien environmental law.

The smallest acceptable slice should include:

- one nonhuman material, field, or persistence rule with two or more observable consequences;
- at least three independent clues that let a player infer the rule;
- two valid ways to exploit or accommodate it;
- an interaction with at least one enemy behavior and one existing Working axis;
- a discovery outcome more interesting than mandatory extermination;
- preview and trace evidence that distinguishes fact, uncertainty, and an untested hypothesis;
- no special-case encounter script that bypasses the shared behavior/rule systems.

Exit evidence is human comprehension: first-time players can state the law in their own words, cite evidence, revise a Working or route because of it, and reach more than one valid outcome.

## Then: systematise only what the mystery proves

After the handcrafted mystery demonstrates which concepts matter, promote those shapes into reusable infrastructure:

- stable discovery and event IDs;
- typed causal provenance for “why did this happen?”;
- resolved-content snapshots and fingerprints;
- named random streams and action-log replay;
- minimal save/load and schema migration;
- authoring diagnostics for rules, evidence, and counterplay;
- player-facing Working import/export only when compatibility can be explained clearly.

Do not generalise speculative ontology before the player-facing slice proves its needs.

## Later horizons

These remain dependency-ordered research directions, not simultaneous commitments:

1. **Bounded alien generation** — generate a small validated possibility space of laws, evidence, and counterplay; reject incoherent seeds visibly.
2. **Persistent expedition** — revisit a changed site and explain what prior actions caused.
3. **Materials, bodies, ecology, and physics** — deepen interactions without losing bounded execution or readable causal chains.
4. **Agencies, knowledge, and flows** — give inhabitants needs, perceptions, dependencies, and changing relationships beyond encounter roles.
5. **History and scale** — derive ruins, traces, and large-scale change from the same event model at multiple levels of detail.

At every horizon, generated novelty is subordinate to coherence, tactical consequence, counterplay, determinism, and inspectability.

## Permanent guardrails

- Workings stay short and readable aloud as ritual grammar.
- Alien never means arbitrary; horror comes from coherent implications and incomplete knowledge.
- Exploration and understanding matter more than clearing a map.
- Enemy and environment design should pressure the grammar the player wants to write.
- Selecting a target is never sufficient setup for direct damage.
- The boss obeys the same world, status, terrain, preview, and behavior rules as other inhabitants.
- Base content, generated content, and mods share one semantic model.
- Simulation work remains bounded; no graph, trigger, physics, or relationship traversal may be unbounded.
- New content must create a new decision or interaction, not merely a renamed number.

## Review rule

Before promoting future work, answer four questions:

1. What new observation or decision does this create?
2. Which existing systems can it combine with?
3. How will the player predict and inspect it?
4. What evidence would prove that it improves the alien-sandbox loop?

If those answers are weak, keep the idea in [future.md](future.md) rather than turning it into implementation scope.
