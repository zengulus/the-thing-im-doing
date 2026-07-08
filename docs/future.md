# Future Features — Magic Glyph Roguelike

This list captures desirable future features after the core prototype is proven.

These are **not current implementation targets**.

Do not build these until the prototype demonstrates that short magical workings, preview, casting, trace-reading, and tactical spell revision are fun.

---

# 1. Spell Language Expansion

## Additional Clauses

Future clauses should expand expression without making workings feel like code.

Candidate additions:

* [ ] `for each marked foe:`
* [ ] `for each marked tile:`
* [ ] `repeat twice:`
* [ ] `repeat thrice:`
* [ ] `until blocked:`
* [ ] `when they enter:`
* [ ] `when struck:`
* [ ] `unless warded:`
* [ ] `if wounded:`
* [ ] `if adjacent to stone:`
* [ ] `if standing in water:`
* [ ] `if burning:`
* [ ] `if alone:`
* [ ] `if surrounded:`

## New Effect Clauses

* [ ] `chill them`
* [ ] `kindle them`
* [ ] `mend them`
* [ ] `ward them`
* [ ] `pull them`
* [ ] `bind them`
* [ ] `weaken them`
* [ ] `silence them`
* [ ] `scatter marks`
* [ ] `move the mark`
* [ ] `copy the mark`
* [ ] `break the stone`
* [ ] `grow thorns`
* [ ] `open a path`

## Memory / Binding Expansion

Only add once one remembered sign feels limiting in a good way.

* [ ] Remember one foe.
* [ ] Remember one ally.
* [ ] Remember one tile.
* [ ] Remember the first marked target.
* [ ] Remember the last damaged target.
* [ ] Swap remembered signs.
* [ ] Forget remembered sign.
* [ ] Return to remembered sign.

Avoid exposing these as “registers” in player-facing UI.

---

# 2. Advanced Working Structure

## Larger Workings

Possible progression:

* [ ] Early maximum: 4 clauses.
* [ ] Mid-run maximum: 6 clauses.
* [ ] Late-run maximum: 8 clauses.
* [ ] Rare relic maximum: 10 clauses.

Longer workings should remain readable aloud.

## Refrains / Bounded Loops

Loops must be capped and previewable.

* [ ] `repeat twice`
* [ ] `repeat thrice`
* [ ] `for each marked tile`
* [ ] `for each adjacent foe`
* [ ] `until blocked`, max 4 steps
* [ ] `while burning`, max 3 repeats

Never add unbounded loops.

## Triggered Workings

Triggered effects should be introduced carefully because they increase state complexity.

* [ ] When a foe enters a marked tile.
* [ ] When the caster is struck.
* [ ] When a raised stone breaks.
* [ ] When a mark expires.
* [ ] When an enemy casts.
* [ ] When an ally falls below half health.

Triggered workings should be visually telegraphed on the grid.

---

# 3. Relics and Modifiers

Relics should modify the spell system, not merely increase stats.

## Clause Cost Relics

* [ ] First condition in each working is free.
* [ ] First mark clause costs no focus.
* [ ] Refrains cost less focus.
* [ ] Triggered workings cost less focus.
* [ ] Workings with no damage clauses refund focus.

## Clause Mutation Relics

* [ ] `spark them` also marks the target.
* [ ] `mark them` also reveals intent.
* [ ] `raise stone` also wards adjacent allies.
* [ ] `push them` leaves a mark behind.
* [ ] `chill them` spreads through water.
* [ ] `kindle them` spreads through paper.
* [ ] `mend them` also removes one mark.

## Risk / Reward Relics

* [ ] Longer workings gain power but produce backlash on failure.
* [ ] Forgotten signs explode into marks.
* [ ] Failed conditions generate warding.
* [ ] Repeated clauses become stronger but more unstable.
* [ ] Casting the same working twice adds debt.
* [ ] Casting three different workings clears debt.

## Weird Relics

* [ ] Marks count as water.
* [ ] Stone remembers who touched it.
* [ ] Fire refuses to harm marked allies.
* [ ] Wounded enemies are considered “clear” for targeting.
* [ ] Raised stone becomes brittle after one turn.
* [ ] The first failed condition each turn becomes true.

---

# 4. Terrain Grammars

Each region should have a small terrain grammar that changes how workings behave.

## Archive Terrain

* [ ] Paper.
* [ ] Ink.
* [ ] Burning paper.
* [ ] Sealed shelves.
* [ ] Written marks.
* [ ] Smoke-obscured tiles.

Rule ideas:

* [ ] Marked paper burns faster.
* [ ] Burning shelves spread heat.
* [ ] Ink carries marks.
* [ ] Smoke blocks targeting.
* [ ] Seals break only when marked and heated.

## Ossuary Terrain

* [ ] Bone piles.
* [ ] Corpses.
* [ ] Roots.
* [ ] Rot.
* [ ] Thorn growth.
* [ ] Grave soil.

Rule ideas:

* [ ] Corpses empower nearby enemies.
* [ ] Roots grow toward wounded actors.
* [ ] Rot weakens warding.
* [ ] Bone piles can become temporary walls.
* [ ] Healing near roots spreads growth.

## Foundry Terrain

* [ ] Mirrors.
* [ ] Charged tiles.
* [ ] Molten channels.
* [ ] Astral glass.
* [ ] Conveyor-like paths.
* [ ] Delayed-effect anchors.

Rule ideas:

* [ ] Mirrors reflect sparks.
* [ ] Charged tiles amplify push.
* [ ] Molten channels carry heat.
* [ ] Anchors delay effects by one turn.
* [ ] Astral glass stores the last emitted effect.

## Crown Below Terrain

* [ ] Corrupted sigils.
* [ ] Black stone.
* [ ] Reversed marks.
* [ ] Debt tiles.
* [ ] Broken ritual circles.
* [ ] Hostile inscriptions.

Rule ideas:

* [ ] Conditions may invert on corrupted sigils.
* [ ] Marks may belong to enemies.
* [ ] Failed clauses create debt.
* [ ] Backlash can rewrite terrain.
* [ ] Enemy workings may attach flaws to the player’s next spell.

---

# 5. Enemy Expansion

Enemies should pressure spell grammar, not merely demand higher damage.

## Archive Enemies

* [ ] Ink Leech: travels along marked paths.
* [ ] Page Wraith: becomes visible only when marked.
* [ ] Seal Warden: blocks effects unless its seal is broken.
* [ ] Ash Scribe: marks tiles, then heats them.
* [ ] Index Moth: consumes marks and reveals hidden rules.

## Ossuary Enemies

* [ ] Bone Chorister: empowers allies near corpses.
* [ ] Root Saint: grows walls and heals near growth.
* [ ] Grave Ox: charges through marked lanes.
* [ ] Rot Apostle: weakens actors who remain still.
* [ ] Marrow Thief: steals the player’s remembered sign.

## Foundry Enemies

* [ ] Glass Hound: stores or reflects repeated direct effects.
* [ ] Mirror Smith: creates reflective terrain.
* [ ] Charge Eater: consumes charged tiles to heal.
* [ ] Delay Imp: postpones the next clause of a working.
* [ ] Furnace Angel: grows stronger from heat.

## Crown Below Enemies

* [ ] Debt Collector: adds cost to the next working.
* [ ] Null Moth: consumes marks and queued effects.
* [ ] Clause Eater: removes the first condition from a working temporarily.
* [ ] Inversion Saint: flips one local rule while alive.
* [ ] Crown Scribe: writes hostile marks under both player and enemies.

---

# 6. Procedural Generation

Procedural generation should create magical problems, not noise.

## Floor Generation

* [ ] Room graph.
* [ ] Combat rooms.
* [ ] Hazard rooms.
* [ ] Shrine rooms.
* [ ] Glyph trial rooms.
* [ ] Proving circles.
* [ ] Elite rooms.
* [ ] Ritual gate.
* [ ] Optional risk path.

## Rule Generation

Each floor may generate:

* [ ] One primary terrain rule.
* [ ] One enemy ecology rule.
* [ ] One glyph availability constraint.
* [ ] One resource pressure rule.
* [ ] One optional anomaly.

Avoid generating too many rule changes at once.

## Glyph Economy

* [ ] Common clause pool.
* [ ] Rare clause pool.
* [ ] Region-specific clauses.
* [ ] Corrupted clauses.
* [ ] Temporary clauses.
* [ ] Shrine-granted clauses.
* [ ] Enemy-taught clauses.

## Anti-Softlock Rules

The generator must avoid requiring unavailable clauses.

Examples:

* [ ] Do not require `chill` if no chill-like clause is available.
* [ ] Do not require marks if the player has no marking method.
* [ ] Do not require stone-breaking if no push, heat, or break clause exists.
* [ ] Do not require hidden enemy detection unless marking/reveal is available.

---

# 7. Ritual Encounters

Bosses should be rule systems, not just large enemies.

## Ritual Encounter Goals

* [ ] Test the floor’s grammar.
* [ ] Reward understanding of local rules.
* [ ] Require spell adaptation.
* [ ] Avoid one-solution puzzle design.
* [ ] Make preview and trace especially useful.

## Archive Ritual Ideas

* [ ] Burn sealed shelves without destroying the exit.
* [ ] Mark the correct pages while enemies rewrite marks.
* [ ] Break seals in a specific logical order.
* [ ] Use smoke, heat, and ink to reveal the true target.

## Ossuary Ritual Ideas

* [ ] Contain spreading roots.
* [ ] Prevent corpses from empowering the boss.
* [ ] Heal or destroy bone nodes in the right rhythm.
* [ ] Redirect growth into ritual channels.

## Foundry Ritual Ideas

* [ ] Reflect an effect through mirrors.
* [ ] Charge anchors in sequence.
* [ ] Time delayed clauses to hit moving targets.
* [ ] Use enemy effects as part of the solution.

## Crown Below Ritual Ideas

* [ ] Fight a boss that corrupts clauses.
* [ ] Survive rule inversion phases.
* [ ] Spend or cleanse accumulated debt.
* [ ] Rewrite hostile marks into player-owned marks.

---

# 8. Progression

Progression should increase expressive variety more than raw power.

## Within-Run Progression

* [ ] Gain new clauses.
* [ ] Increase maximum working length.
* [ ] Unlock another prepared working slot.
* [ ] Improve preview detail.
* [ ] Gain one extra remembered sign.
* [ ] Upgrade specific clause families.
* [ ] Gain relics.
* [ ] Accept curses for stronger clauses.

## Across-Run Progression

* [ ] Unlock starting clause pools.
* [ ] Unlock mage backgrounds.
* [ ] Unlock region variants.
* [ ] Unlock relic pools.
* [ ] Add glossary entries.
* [ ] Preserve discovered workings as named recipes.
* [ ] Unlock optional difficulty modifiers.

Meta-progression should not trivialise early tactical decisions.

---

# 9. Player Builds

Mage backgrounds should change starting grammar, not just stats.

## Candidate Backgrounds

### Ash Grammarian

* Starts with mark/heat clauses.
* Better at burning terrain.
* Worse at healing and warding.

### Stone Binder

* Starts with raise/push clauses.
* Better at terrain control.
* Workings cost more if they target distant foes.

### Mercy Scribe

* Starts with mend/ward clauses.
* Can remember allies more easily.
* Lower direct damage.

### Mirror-Taught Heretic

* Starts with reflection or return clauses.
* Strong preview benefits.
* Higher backlash from failed conditions.

### Debt-Bound Adept

* Can cast beyond focus limit.
* Accumulates debt clauses.
* Must build around repayment.

---

# 10. UI and Quality-of-Life

## Spell Editing

* [ ] Save named workings.
* [ ] Duplicate working.
* [ ] Compare two workings.
* [ ] Highlight changed clause.
* [ ] Undo/redo clause edits.
* [ ] Mark favorite clauses.
* [ ] Filter clauses by family.
* [ ] Show common recipes discovered by the player.
* [ ] Warn when a working has no possible effect.

## Preview Improvements

* [ ] Scrub through omen trace.
* [ ] Step forward/backward.
* [ ] Preview enemy responses.
* [ ] Preview local rule consequences.
* [ ] Highlight uncertain outcomes.
* [ ] Compare preview before/after editing a clause.
* [ ] Show why a target is invalid.
* [ ] Show source of backlash.

## Accessibility

* [ ] Clear icon shapes independent of color.
* [ ] Text mode for all clause icons.
* [ ] Adjustable animation speed.
* [ ] Reduced visual noise mode.
* [ ] Large tooltip mode.
* [ ] Keyboard-only spell editing.
* [ ] Controller-friendly clause selection.

---

# 11. Narrative and Worldbuilding

Narrative should support the system’s exactness.

## Core Themes

* [ ] Magic as literal agreement.
* [ ] Ruins as hostile dialects.
* [ ] Failure as misworded intent.
* [ ] Power through understanding.
* [ ] Ritual law versus improvisation.
* [ ] Ancient systems that still obey their own rules.

## Lore Delivery

* [ ] Clause descriptions.
* [ ] Enemy ecology notes.
* [ ] Region inscriptions.
* [ ] Ritual fragments.
* [ ] Discovered recipe names.
* [ ] Mage background memories.
* [ ] Proving-circle commentary.
* [ ] Post-run glossary updates.

## In-Universe Terms

* [ ] Working.
* [ ] Clause.
* [ ] Refrain.
* [ ] Remembered sign.
* [ ] Omen trace.
* [ ] Backlash.
* [ ] Proving circle.
* [ ] Local grammar.
* [ ] Hostile dialect.
* [ ] Binding mark.

---

# 12. Audio / Visual Expansion

## Visual Identity

* [ ] Readable dark fantasy tiles.
* [ ] Bright spell geometry over subdued environments.
* [ ] Distinct mark shapes per owner/faction.
* [ ] Animated clause execution.
* [ ] Omen-trace path overlay.
* [ ] Region-specific magical scripts.
* [ ] Clear enemy silhouettes.
* [ ] Strong telegraph shapes.

## Audio Identity

* [ ] Clause family sound motifs.
* [ ] Layered sounds for multi-clause workings.
* [ ] Distinct failed-condition sound.
* [ ] Backlash distortion.
* [ ] Enemy intent warnings.
* [ ] Region ambience.
* [ ] Successful rewrite/payoff sound.

Audio should reinforce grammar, not just atmosphere.

---

# 13. Long-Term Experimental Ideas

These are especially risky. Do not build until the core game is stable.

* [ ] Enemies that partially rewrite the player’s working.
* [ ] Corrupted clauses with double meanings.
* [ ] Player-created named recipes appearing as loot/lore.
* [ ] Region dialects that rename clauses but preserve mechanics.
* [ ] Multiple simultaneous local rules.
* [ ] Spell duels where enemy and player workings interact.
* [ ] Rituals that require non-damaging workings.
* [ ] Optional hidden rules for expert players.
* [ ] Branching node-graph editor.
* [ ] Asynchronous traps triggered by saved workings.
* [ ] Ally mages with their own working grammar.
* [ ] Draft mode where the player edits during enemy telegraphs.
* [ ] Procedural “grammar curses” that alter syntax.
* [ ] Late-game bosses that attack the omen trace itself.

---

# Future Feature Rule

A future feature should only be promoted into active scope if it satisfies at least one of these:

* [ ] It makes spell-writing more expressive.
* [ ] It makes spell debugging clearer.
* [ ] It gives enemies a better way to pressure spell choices.
* [ ] It makes terrain more meaningfully writable/readable.
* [ ] It improves replayability without increasing confusion.
* [ ] It strengthens the magical-pseudocode fantasy.

If it only adds content, stats, or complexity, defer it.