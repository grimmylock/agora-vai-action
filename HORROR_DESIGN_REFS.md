# Monster design references: Silent Hill / Dead Space (2026-11-21)
Yan, 2026-11-21: "For the monsters, i want more silent hill style/dead space style stuff, looks too tame for now, search for references." The creatures so far read as animals (an anglerfish, an insect, a hound). What the two series do differently, from their artists' own accounts, and how it applies here.

## What the references say
**Silent Hill (Masahiro Ito):**
- No conventional monsters: the team "decided to steer clear of traditionally scary enemies, eschewing giant misshapen monsters or zombies" and disturbed the player with psychological imagery instead ([Game Rant](https://gamerant.com/silent-hill-creature-monster-design-modern-artist-inspiration-bosch-bellmer-bacon-abe/), [PC Gamer](https://www.pcgamer.com/why-i-love-the-monsters-of-silent-hill-2)).
- A human trace in every creature: Ito: "I wanted to give them a human aspect" ([Spilled](https://spilled.gg/how-masahiro-ito-created-silent-hill-2s-most-iconic-monsters)).
- Objects from the place become flesh: the Lying Figure's upper body "comes from a body bag as a sign of dead body" ([Ito's notes on the SH2 remake monsters](https://www.silenthillmemories.net/sh2_remake/monsters_en.htm)).
- Painting, not film: Francis Bacon's smeared, contorted bodies, Hans Bellmer's jointed dolls, Bosch ([Game Rant](https://gamerant.com/silent-hill-creature-monster-design-modern-artist-inspiration-bosch-bellmer-bacon-abe/)).
- Menace and misery at once: the creatures are "equal parts menacing and miserable" ([PC Gamer](https://www.pcgamer.com/why-i-love-the-monsters-of-silent-hill-2)).
- Tied to the story: each creature means something in its place.

**Dead Space (Ben Wanat, Ian Milham):**
- Made from corpses: the name came from "alien forms made from corpses"; the host's skeleton is reconfigured for offence, bones become blades ([Wikipedia: Necromorphs](https://en.wikipedia.org/wiki/Necromorphs), [GamesRadar](https://gamesradar.com/how-to-make-a-monster)).
- Parasitic fungus biology as the biggest influence: growths, sacs, pustules spreading through the body ([EA: inside Dead Space 2](https://www.ea.com/seed/news/inside-dead-space-2-new-necromorph-nightmare)).
- "Improvised terribleness": the body looks rebuilt in a hurry, wrong and functional ([Game Informer](https://gameinformer.com/b/features/archive/2009/12/16/dead-space-2-the-evolution-of-a-necromorph.aspx)).
- The open carcass: exposed rib cages run through the series; decrepit, mummified flesh in Dead Space 3 ([Destructoid](https://www.destructoid.com/?p=142598)).
- A clear silhouette first, then the gore ([Wikipedia](https://en.wikipedia.org/wiki/Necromorphs)).

## The rules for our monsters
1. **Start from a human body, then break it.** Every boss is visibly made from people: a torso where a head should be, arms used as legs, a ribcage opened into a mouth. No clean animal shapes (no plain fish, insect or dog).
2. **Bones become tools.** Saws, hooks and spikes grow out of arms and ribs, not held.
3. **The place becomes flesh.** The ferry's ropes, nets and lanterns, the morgue's body bags, gurneys and IV lines are fused into the body.
4. **Faceless or wrong-faced.** No eyes, eyes in the wrong place, a face on the belly, a mouth that opens sideways.
5. **Decay and growth.** Grey-green dead skin, wet open wounds, fungal sacs and tumours, mummified patches; one sick accent colour per boss.
6. **Asymmetry and broken proportion.** One limb too long, a body bent the wrong way, joints that bend backwards.
7. **Misery in it.** Something about it looks like it is suffering: a head held at an angle, hands clutching itself.
8. **Silhouette first.** At game size it must read in one look; the gore sits inside a strong outline.
9. **Motion that is wrong.** In the clips: twitches, jerks, stillness then sudden speed (the prompts ask for it).

10. **Match the game's look (Yan, 2026-11-21: "try and make the designs match the games current art-style too, some of them came out TOO detailed and it doesn't fit").** Generate at the size the monster is drawn in the game (32 px a tile, like `mutant_hd_*.png`: a 3-tile boss is about 96 px tall), never big and shrunk; Pixel Lab detail "medium detail", shading "basic shading", a single-colour dark outline; start from one of our own mutant sprites as the init image at a low weight so the palette and pixel density carry over; compare every candidate side by side with the Pallid Prowler and the Gaunt at the same scale before animating.

## Applied
- **The Ferryman (Stranded Ferry):** a drowned angler made of the drowned. It waits under the deck water with only its lure showing: a ship's lantern on a stalk of spine, with a face pressed inside the glass. Its "head" is a bloated human torso split open down the middle into the maw, with ribs for teeth. It walks out on human arms bent backwards as legs. Ropes and net grow through the skin.
- **The Orderly (Hospital Morgue):** an insect made of a patient. A human torso stretched into a thorax, the arms grown into long bone-saw blades, a body bag fused over the back as its wing cases, IV lines and a blood sac feeding it, a face on the underside of the abdomen. It moves in jerks on too many thin legs.
- **Mother Hollow** stays as approved.
