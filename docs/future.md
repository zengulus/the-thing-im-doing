# Future Features — Magic Glyph Roguelike

This document is a gated roadmap for future work after the current prototype proves that short Workings, preview, casting, trace-reading, and tactical spell revision are fun.

Do not treat this as a content backlog. A feature belongs here only if it strengthens the core loop:

> Read the board → construct or revise a short spell circle → preview the omen trace → cast → learn from the result → adapt.

The game should feel like magical pseudocode expressed through ritual geometry, not like a programming exercise wearing fantasy skin.

---

# 0. Current Design Commitments

## Core Fantasy

The player is not selecting spells from a list. The player is constructing Workings out of clauses.

A Working should feel like a short ritual sentence:

> Aim at the nearest foe.
> If they are marked, damage them.
> Otherwise, mark them.

The player-facing experience should avoid words like register, variable, boolean, instruction, script, or program unless used internally. The implementation may be graph-based; the fantasy is ritual grammar.

## Current Mechanical Shape

The current code already supports:

* Workings made from nodes.
* An entry node.
* `next`, `true`, and `false` flow.
* Clause definitions loaded from content.
* Behaviour definitions loaded from content.
* Generic behaviour atoms.
* Previewing a Working on a cloned encounter.
* Casting a Working into the real encounter.
* Omen trace output.
* Actor effects.
* Tile effects.
* Counters.
* Enemy behaviours.
* Floor rules.
* JSON content loading from base content and mods.
* A basic graph editor.

Future work should build on this substrate rather than replacing it.

## Non-Negotiable Rule

Every future feature must preserve short readable Workings.

If a Working cannot be read aloud as ritual grammar, it is too complex.

---

# 1. Immediate Prototype Gate

Before expanding the language, prove the existing loop.

## Required Proof

The prototype must show that all of the following are fun:

* Choosing a target.
* Previewing a Working.
* Reading the omen trace.
* Editing a Working after preview.
* Casting the revised Working.
* Seeing enemies or terrain punish a naive Working.
* Discovering a better Working through tactical reasoning.

## Minimum Playtest Questions

After a short test encounter, the player should be able to answer:

* What did your Working try to do?
* Why did it succeed or fail?
* What would you change next turn?
* Did the preview help?
* Did the trace explain the result?
* Did the enemy or terrain make you change your spell construction?

If players cannot answer these, do not add more clauses.

---

# 2. Spell Circle Layer

The graph editor is a good prototype UI. The final form should be a spell-circle editor.

## Principle

The spell circle is a visual projection of a semantic Working graph.

The semantic graph is the source of truth. The circle is layout, presentation, and interaction.

## Semantic Working Data

A Working should be reducible to a stable JSON representation:

* schema version;
* display name;
* max steps;
* entry node;
* nodes;
* clause ids;
* outputs;
* optional layout metadata;
* optional discovered recipe metadata.

Semantic data and visual layout should remain separate.

A malformed layout must not corrupt a valid Working.

## Circle Presentation

Represent clause families as visual grammar:

* Targeting clauses: anchors, rays, pointing marks.
* Condition clauses: gates, splits, thresholds.
* Effect clauses: impact glyphs, burns, cuts, wards, bindings.
* Memory clauses: bound signs, stars, knots, echo marks.
* Refrains: repeated ring segments or orbiting marks.
* Triggers: dormant outer-ring sigils.

Execution should animate around the circle as an omen trace.

## Hover / Inspection

Every visible part of the circle should be hoverable:

* glyph;
* path;
* branch split;
* remembered sign;
* trigger;
* counter cost;
* counter gain;
* backlash risk;
* invalid target warning.

Hover should answer:

* What does this part mean?
* What clause or rule produced it?
* What does it do during execution?
* What happens if it fails?
* What target, sign, or effect does it reference?

## Text Mode

Every circle must have a plain-language rendering.

Example:

> Aim at the nearest foe.
> If they are marked, damage them.
> Otherwise, mark them.

This is necessary for accessibility, debugging, sharing, and tooltip clarity.

## Not Yet

Do not implement spell circles until the graph editor proves the core loop. The circle layer is a late-stage interface upgrade, not a reason to delay testing.

---

# 3. Working Serialization and Sharing

The game should eventually support sharing Workings as JSON.

## Goals

* Export a named Working.
* Import a named Working.
* Validate schema version.
* Reject unknown clauses cleanly.
* Preserve semantic meaning even if layout is missing.
* Allow auto-layout if visual metadata is absent.
* Support compact share codes later.

## Validation Rules

A shared Working is valid only if:

* it has one entry node;
* all node ids are unique;
* all referenced nodes exist;
* all clause ids resolve;
* outputs match clause type;
* max steps is within allowed bounds;
* the graph cannot exceed hard execution limits;
* unsupported future schema versions fail gracefully.

## Debug Export

Exports should include optional debug fields:

* estimated counter summary;
* clause family sequence;
* generated ritual prose;
* warnings;
* last preview trace, if available.

This is for sharing and troubleshooting, not core gameplay.

---

# 4. Preview and Omen Trace Improvements

Preview is central. It prevents the game from becoming opaque.

## Near-Term Improvements

* Show whether a Working changes the world.
* Show exact actor/tile affected by each clause.
* Show failed condition paths clearly.
* Show invalid target reasons.
* Show backlash source.
* Collapse trace for HUD.
* Expand trace in editor.
* Highlight the execution path through the graph or circle.

## Mid-Term Improvements

* Step forward/backward through the omen trace.
* Preview before/after a clause edit.
* Compare two Workings on the same target.
* Preview local terrain rule consequences.
* Preview enemy response in simple cases.
* Mark uncertain outcomes separately from deterministic ones.

## Circle-Specific Preview

When spell circles exist:

* animate execution around the circle;
* pulse the active glyph;
* light the taken branch;
* dim untaken branches;
* mark failed conditions;
* show backlash distortion at the failing glyph;
* project affected tiles/actors on the board.

Preview must make debugging feel like reading omens, not reading logs.

---

# 5. Spell Language Expansion

Add clauses only when the current grammar feels meaningfully constrained.

## Rule

Do not add a clause unless it satisfies at least one:

* creates a new tactical pattern;
* makes an enemy more interesting;
* makes terrain more writable/readable;
* creates a new kind of preview decision;
* interacts cleanly with existing counters/effects;
* supports spell-circle readability.

## Clause Length Limits

Suggested progression:

* early run: 4 clauses;
* mid run: 6 clauses;
* late run: 8 clauses;
* rare exception: 10 clauses.

A longer Working should remain readable aloud.

## Candidate Targeting Clauses

* aim at selected tile;
* aim at nearest foe;
* aim at nearest marked foe;
* aim at remembered sign;
* aim along marked path;
* aim at first clear tile in a direction;
* aim at actor standing on a marked tile.

## Candidate Condition Clauses

* if marked;
* if clear;
* if occupied;
* if wounded;
* if adjacent to stone;
* if standing in water;
* if burning;
* if warded;
* unless warded;
* if alone;
* if surrounded.

## Candidate Effect Clauses

* damage them;
* mark them;
* push them;
* pull them;
* raise stone;
* break stone;
* chill them;
* kindle them;
* mend them;
* ward them;
* bind them;
* weaken them;
* silence them;
* move the mark;
* copy the mark;
* scatter marks;
* grow thorns;
* open a path.

## Memory / Binding Clauses

Only expand memory after one remembered sign feels limiting in a good way.

Candidate additions:

* remember one foe;
* remember one ally;
* remember one tile;
* remember first marked target;
* remember last damaged target;
* focus remembered sign;
* swap remembered signs;
* forget remembered sign;
* return to remembered sign.

Never expose these as registers.

## Refrains / Bounded Loops

Loops must be capped, previewable, and readable as ritual refrains.

Allowed forms:

* repeat twice;
* repeat thrice;
* for each marked foe;
* for each marked tile;
* for each adjacent foe;
* until blocked, max 4 steps;
* while burning, max 3 repeats.

Never add unbounded loops.

---

# 6. Effects, Triggers, and Delayed Workings

Triggered effects are powerful but increase state complexity.

## Existing Direction

The code already supports effects with triggers such as turn start, movement, adjacency, and before damage. Future work should formalise this into player-readable ritual grammar.

## Candidate Triggered Effects

* when a foe enters a marked tile;
* when the caster is struck;
* when a raised stone breaks;
* when a mark expires;
* when an enemy casts;
* when an ally falls below half health.

## Requirements

Triggered effects must be:

* visually telegraphed on the grid;
* visible in preview where possible;
* listed in trace output;
* associated with a clear owner;
* removable or exhaustible;
* impossible to stack into unreadable state.

## Delayed Clauses

Delayed effects should use anchors, not hidden timers.

Examples:

* anchor spark here;
* release next turn;
* trigger when crossed;
* store last emitted effect;
* repeat when mark breaks.

Delayed clauses should be introduced only after normal preview is strong.

---

# 7. Enemies That Pressure Grammar

Enemies should pressure spell construction, not merely demand higher damage.

## Design Rule

An enemy is good if it makes the player revise a Working.

Bad enemy:

> Has more HP.

Good enemy:

> Punishes direct damage unless marked first.

## Current Enemy Roles to Strengthen

### Glass Hound

Role: chaser / direct pressure.

Possible grammar pressure:

* stores repeated direct effects;
* reflects the same clause if used twice;
* becomes vulnerable after being pushed into terrain;
* forces the player to branch around adjacency.

### Ash Scribe

Role: mark caster / delayed threat.

Possible grammar pressure:

* marks tiles;
* detonates marked tiles after charging;
* forces the player to move marks, clear marks, or exploit enemy-owned marks.

### Root Saint

Role: terrain shaper / healer.

Possible grammar pressure:

* grows walls;
* heals near raised stone or roots;
* rewards stone-breaking, pushing, and path-opening clauses.

## Future Enemy Families

### Archive

* Ink Leech: travels along marked paths.
* Page Wraith: visible only when marked.
* Seal Warden: blocks effects unless its seal is broken.
* Index Moth: consumes marks and reveals hidden rules.

### Ossuary

* Bone Chorister: empowers allies near corpses.
* Grave Ox: charges through marked lanes.
* Rot Apostle: weakens actors who remain still.
* Marrow Thief: steals the player’s remembered sign.

### Foundry

* Mirror Smith: creates reflective terrain.
* Charge Eater: consumes charged tiles to heal.
* Delay Imp: postpones the next clause of a Working.
* Furnace Angel: grows stronger from heat.

### Crown Below

* Debt Collector: adds cost to the next Working.
* Null Moth: consumes marks and queued effects.
* Clause Eater: temporarily removes the first condition.
* Inversion Saint: flips one local rule while alive.
* Crown Scribe: writes hostile marks under player and enemies.

---

# 8. Terrain Grammars

Each region should have a small local grammar.

A terrain grammar is not just a tile set. It is a set of readable rules that change how Workings behave.

## Rule Budget

A region should introduce:

* one primary terrain material;
* one mark interaction;
* one movement interaction;
* one enemy ecology interaction;
* one exceptional ritual rule.

Avoid introducing too many rule changes at once.

## Archive

Terrain:

* paper;
* ink;
* burning paper;
* sealed shelves;
* written marks;
* smoke-obscured tiles.

Rules:

* marked paper burns faster;
* ink carries marks;
* smoke blocks targeting;
* sealed shelves break only when marked and heated;
* burning shelves spread heat.

## Ossuary

Terrain:

* bone piles;
* corpses;
* roots;
* rot;
* thorn growth;
* grave soil.

Rules:

* corpses empower nearby enemies;
* roots grow toward wounded actors;
* rot weakens warding;
* bone piles can become temporary walls;
* healing near roots spreads growth.

## Foundry

Terrain:

* mirrors;
* charged tiles;
* molten channels;
* astral glass;
* conveyor-like paths;
* delayed-effect anchors.

Rules:

* mirrors reflect sparks;
* charged tiles amplify push;
* molten channels carry heat;
* anchors delay effects by one turn;
* astral glass stores the last emitted effect.

## Crown Below

Terrain:

* corrupted sigils;
* black stone;
* reversed marks;
* debt tiles;
* broken ritual circles;
* hostile inscriptions.

Rules:

* conditions may invert on corrupted sigils;
* marks may belong to enemies;
* failed clauses create debt;
* backlash can rewrite terrain;
* enemy Workings may attach flaws to the player’s next spell.

---

# 9. Relics and Mutators

Relics should modify the spell system, not merely increase stats.

The code has relic definitions and hook data. Before adding many relics, wire relic hooks into encounter resolution and prove that a relic can alter a Working, trace, cost, or effect in a readable way.

## First Relic Implementation Goals

* Player can hold relics.
* Relic hooks run at defined timing points.
* Relic output appears in omen trace.
* Relic effects are previewable if deterministic.
* Relic behaviour is content-driven where possible.

## Hook Timing Candidates

* before preview;
* before cast;
* after clause resolved;
* after condition failed;
* after Working resolved;
* before backlash;
* after enemy turn;
* on mark created;
* on effect expired.

## Clause Cost Relics

* first condition in each Working is free;
* first mark clause costs no focus;
* refrains cost less focus;
* triggered Workings cost less focus;
* Workings with no damage clauses refund focus.

## Clause Mutation Relics

* spark them also marks the target;
* mark them also reveals intent;
* raise stone also wards adjacent allies;
* push them leaves a mark behind;
* chill them spreads through water;
* kindle them spreads through paper;
* mend them also removes one mark.

## Risk / Reward Relics

* longer Workings gain power but produce backlash on failure;
* forgotten signs explode into marks;
* failed conditions generate warding;
* repeated clauses become stronger but more unstable;
* casting the same Working twice adds debt;
* casting three different Workings clears debt.

## Weird Relics

* marks count as water;
* stone remembers who touched it;
* fire refuses to harm marked allies;
* wounded enemies are considered clear for targeting;
* raised stone becomes brittle after one turn;
* the first failed condition each turn becomes true.

---

# 10. Procedural Generation

Procedural generation should create magical problems, not noise.

Do not build procedural generation until hand-authored encounters prove the grammar.

## Floor Generation

Possible room types:

* combat room;
* hazard room;
* shrine room;
* glyph trial room;
* proving circle;
* elite room;
* ritual gate;
* optional risk path.

## Rule Generation

Each floor may generate:

* one primary terrain rule;
* one enemy ecology rule;
* one glyph availability constraint;
* one resource pressure rule;
* one optional anomaly.

## Glyph Economy

Possible sources:

* common clause pool;
* rare clause pool;
* region-specific clauses;
* corrupted clauses;
* temporary clauses;
* shrine-granted clauses;
* enemy-taught clauses.

## Anti-Softlock Rules

The generator must avoid requiring unavailable clauses.

Examples:

* do not require chill if no chill-like clause exists;
* do not require marks if no marking method exists;
* do not require stone-breaking if no push, heat, or break clause exists;
* do not require hidden enemy detection unless marking/reveal is available;
* do not require memory interaction unless memory clauses are present.

---

# 11. Ritual Encounters

Bosses should be rule systems, not large enemies.

## Ritual Encounter Goals

A ritual encounter should:

* test the floor’s grammar;
* reward understanding of local rules;
* require spell adaptation;
* avoid one-solution puzzle design;
* make preview and trace especially useful;
* turn spell-circle editing into the dramatic centre of the fight.

## Archive Ritual Ideas

* burn sealed shelves without destroying the exit;
* mark the correct pages while enemies rewrite marks;
* break seals in a specific logical order;
* use smoke, heat, and ink to reveal the true target.

## Ossuary Ritual Ideas

* contain spreading roots;
* prevent corpses from empowering the boss;
* heal or destroy bone nodes in the right rhythm;
* redirect growth into ritual channels.

## Foundry Ritual Ideas

* reflect an effect through mirrors;
* charge anchors in sequence;
* time delayed clauses to hit moving targets;
* use enemy effects as part of the solution.

## Crown Below Ritual Ideas

* fight a boss that corrupts clauses;
* survive rule inversion phases;
* spend or cleanse accumulated debt;
* rewrite hostile marks into player-owned marks;
* protect the omen trace from interference.

---

# 12. Progression

Progression should increase expressive variety more than raw power.

## Within-Run Progression

* gain new clauses;
* increase maximum Working length;
* unlock another prepared Working slot;
* improve preview detail;
* gain one extra remembered sign;
* upgrade specific clause families;
* gain relics;
* accept curses for stronger clauses.

## Across-Run Progression

* unlock starting clause pools;
* unlock mage backgrounds;
* unlock region variants;
* unlock relic pools;
* add glossary entries;
* preserve discovered Workings as named recipes;
* unlock optional difficulty modifiers.

Meta-progression should not trivialise early tactical decisions.

## Mage Backgrounds

Mage backgrounds should change starting grammar, not just stats.

### Ash Grammarian

* starts with mark and heat clauses;
* better at burning terrain;
* worse at healing and warding.

### Stone Binder

* starts with raise and push clauses;
* better at terrain control;
* Workings cost more if they target distant foes.

### Mercy Scribe

* starts with mend and ward clauses;
* can remember allies more easily;
* lower direct damage.

### Mirror-Taught Heretic

* starts with reflection or return clauses;
* stronger preview benefits;
* higher backlash from failed conditions.

### Debt-Bound Adept

* can cast beyond focus limit;
* accumulates debt clauses;
* must build around repayment.

---

# 13. UI and Quality-of-Life

## Spell Editing

* save named Workings;
* duplicate Working;
* compare two Workings;
* highlight changed clause;
* undo/redo clause edits;
* mark favourite clauses;
* filter clauses by family;
* show discovered recipes;
* warn when a Working has no possible effect;
* auto-layout graph/circle;
* switch between circle view and text view.

## Preview

* scrub through omen trace;
* step forward/backward;
* preview enemy responses;
* preview local rule consequences;
* highlight uncertain outcomes;
* compare preview before/after edit;
* show why target is invalid;
* show source of backlash.

## Accessibility

* clear icon shapes independent of colour;
* text mode for all glyphs;
* adjustable animation speed;
* reduced visual noise mode;
* large tooltip mode;
* keyboard-only spell editing;
* controller-friendly clause selection;
* exportable plain-language Working summary.

---

# 14. Narrative and Worldbuilding

Narrative should support the system’s exactness.

## Core Themes

* magic as literal agreement;
* ruins as hostile dialects;
* failure as misworded intent;
* power through understanding;
* ritual law versus improvisation;
* ancient systems that still obey their own rules.

## Lore Delivery

* clause descriptions;
* enemy ecology notes;
* region inscriptions;
* ritual fragments;
* discovered recipe names;
* mage background memories;
* proving-circle commentary;
* post-run glossary updates.

## In-Universe Terms

Prefer:

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

Avoid player-facing use of:

* program;
* script;
* function;
* variable;
* register;
* boolean;
* AST;
* graph;
* node, except in debug/editor contexts.

---

# 15. Audio / Visual Identity

## Visual Direction

* readable dark fantasy tiles;
* bright spell geometry over subdued environments;
* distinct mark shapes per owner/faction;
* animated clause execution;
* omen-trace path overlay;
* region-specific magical scripts;
* clear enemy silhouettes;
* strong telegraph shapes;
* spell circles that remain legible when small;
* glyphs that preserve meaning under reduced visual noise.

## Audio Direction

* clause family sound motifs;
* layered sounds for multi-clause Workings;
* distinct failed-condition sound;
* backlash distortion;
* enemy intent warnings;
* region ambience;
* successful rewrite/payoff sound.

Audio should reinforce grammar, not just atmosphere.

---

# 16. Long-Term Experimental Ideas

These are risky. Do not build until the core game is stable.

* enemies that partially rewrite the player’s Working;
* corrupted clauses with double meanings;
* player-created named recipes appearing as loot/lore;
* region dialects that rename clauses but preserve mechanics;
* multiple simultaneous local rules;
* spell duels where enemy and player Workings interact;
* rituals that require non-damaging Workings;
* optional hidden rules for expert players;
* asynchronous traps triggered by saved Workings;
* ally mages with their own Working grammar;
* draft mode where the player edits during enemy telegraphs;
* procedural grammar curses that alter syntax;
* late-game bosses that attack the omen trace itself.

---

# 17. Promotion Rule

A future feature may become active scope only if it satisfies at least one:

* makes spell-writing more expressive;
* makes spell debugging clearer;
* gives enemies a better way to pressure spell choices;
* makes terrain more meaningfully writable/readable;
* improves replayability without increasing confusion;
* strengthens the magical-pseudocode fantasy;
* improves spell-circle readability;
* supports clean JSON serialization or validation.

If it only adds content, stats, lore, or complexity, defer it.

---

# 18. Current Recommended Build Order

## Phase 1 — Prove Current Loop

* tighten graph editor;
* improve trace readability;
* make preview clearer;
* add invalid-target warnings;
* make current sample Workings feel good;
* improve enemy intent display;
* add a few hand-authored challenge rooms.

## Phase 2 — Strengthen Tactical Pressure

* make enemies force spell revision;
* make floor rules matter;
* add one or two terrain interactions;
* add one more enemy per grammar role;
* keep Workings short.

## Phase 3 — Serialization

* define Working JSON schema;
* export/import Workings;
* separate semantic data from layout data;
* validate shared Workings;
* auto-layout imported Workings.

## Phase 4 — Spell Circle Renderer

* render existing Workings as circles;
* make glyphs hoverable;
* animate omen trace through the circle;
* preserve text mode;
* keep graph editor as fallback/debug view.

## Phase 5 — Relic Hooks

* wire relic hooks into encounter resolution;
* make relic effects previewable;
* add trace lines for relic intervention;
* add a small set of spell-system relics.

## Phase 6 — Language Expansion

* add bounded refrains;
* add remembered-sign expansion;
* add trigger clauses;
* add terrain-region clauses;
* keep all additions previewable.

## Phase 7 — Regions, Generation, Rituals

* build one complete region grammar;
* build hand-authored ritual encounters;
* then add procedural generation;
* then add long-term experimental systems.

Build in this order unless playtests prove a different bottleneck.