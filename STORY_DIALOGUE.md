# agora vai - mission dialogue (first draft)

Written 2026-10-29 from `STORY.md`. Draft lines for Yan to cut, change and add to. Nothing here is coded.

**How to read it**
- `NAME:` a spoken line (the dialogue panel: portrait, name, typed text). `RADIO -` over the radio. `BARK -` a short line over
  a head (`Banter.cs`). `LOG -` a log line. `JOURNAL -` a player journal entry. `[CUT]` a cutscene direction.
- `[1] ... [2] ...` are the player's choices (number keys). A choice that only changes the reply is marked `->`.
- Co-op: NPCs never say "you alone"; lines work for one player or four. Whoever talks to the NPC picks the answers.
- Companions are never required: a line marked `(if Lev/Tolik is with you)` is skipped otherwise.
- Names marked * are new and up for Yan's approval.

---

# TIER 1 - EXTRACTION

## Bonfire Village: the first morning

[CUT] Morning by the fire. Smoke, a kettle, a dog asleep by the bunkhouse. The camera finds the player(s), then Yefim.

YEFIM: Awake? Good. The Zone doesn't wait for anyone to finish their dreams.
YEFIM: I'm Yefim. I sell, I buy, I listen. Mostly I listen.
[1] Where am I?
  YEFIM: Bonfire Village. Last warm place before the Zone gets serious. Guards on the fence, a medic, a bunk if you can pay.
  Nobody shoots inside the posts. That's the only rule I care about.
[2] I need work.
  YEFIM: Everyone needs work. Few survive it. Talk to me when you've got a gun that shoots straight and boots that don't leak.
[3] (Leave)
  YEFIM: Go on. The fire will still be here. Probably.

## Tutorial 1: The Courier's Pack (Yefim)

YEFIM: A courier was bringing me batteries and medicine. He was due two days ago.
YEFIM: His last call came from {COURIER_SPOT}. If he's dead, his pack isn't. Bring it to me and I'll pay - and I'll pay
  better than the crows.
[1] What killed him?
  YEFIM: Maws, if I had to bet. Blind things, all teeth. They hear you before you see them. Walk slow. Shoot fast.
[2] I'll get it.
  YEFIM: Take the bandages on the crate. Consider them a loan from a man who wants his pack back.

JOURNAL - The Courier's Pack: Yefim's courier went quiet near {COURIER_SPOT}. Find his pack.
BARK (at the body) - ...poor bastard.
LOG - The pack is still on him. Something has been chewing the straps.
YEFIM (hand-in): Medicine, batteries... and blood on all of it. Here. You earned this. Now go eat something - you look like
  the courier.

## Tutorial 2: The Barman's Problem

STAS* (barman): You. Yes, you, with the face. Three Raiders set up camp in {RAIDER_CAMP}. They've been stopping our people on
  the road, taking "tolls".
STAS: The guards won't leave the fence. I'm not asking you to start a war. I'm asking you to end a small one.
[1] Three of them. And me.
  STAS: Then don't fight three. Fight one, then another, then another. They sleep in shifts. They drink. They talk too loud.
  Stay low, stay behind them. A knife is quieter than a rifle.
[2] What's in it for me?
  STAS: Their stash, my gratitude, and a drink that isn't made from radiator fluid. Mostly the stash.

BARK (Raider, unaware) - ...told him the toll's doubled, he cried. Actually cried.
BARK (Raider, unaware) - Your watch. I'm not sitting in the rain again.
LOG - A Raider slumps without a sound.
STAS (hand-in): Quiet work. I like quiet. Here - and the drink's real. Mostly.

## The Cut Fence (Yefim)

YEFIM: Something went through the fence at Watchtower 3 last night. Guards found the wire torn open.
YEFIM: Probably a beast. Probably. Go and see what we're dealing with before it comes back hungry.

[On arrival a Pallid Prowler circles the gap.]
BARK - There! Something's at the wire!

[After the kill - if the player leaves:]
YEFIM: A Prowler. Of course. They test the fence every spring. Good work - here.

[If the player inspects the fence (E on the gap):]
LOG - The wire isn't torn. It's cut - clean, square ends. Bolt cutters.
LOG - A scrap of dark grey cloth is caught on a barb.
YEFIM (hand-in, inspected): Bolt cutters... A Prowler doesn't carry bolt cutters. And this cloth - nobody here wears grey.
YEFIM: Someone came into my village and walked out again, and the guards saw a beast. I don't like that. I don't like that
  at all. Take this. Extra. For keeping your eyes open when everyone else closed theirs.
JOURNAL - The Cut Fence: Someone cut the fence at Watchtower 3 and left a scrap of grey cloth. Not a mutant.

## First sightings of "those"

BARK (any Wanderer, idle) - Saw them again by the marsh road. Robes. Grey. Didn't say a word.
BARK - Those ones were standing in the reeds an hour, just... looking at the ground.
BARK - Don't wave at them. Last guy who waved at them came back wrong.
LOG (first time the player sees one) - Robed figures, far off. One is holding something up to the ground, like a detector.
[If the player gets close:] LOG - They turn away. They don't hurry. They just leave.
[If shot at:] LOG - A grey cloud bursts where they stood. When it clears, they're gone.

## The Burst (Yefim)

[CUT] Night. Yefim's radio on the counter crackles: a looped voice under static.
RADIO (loop) - ...anyone... this is... I have the... I can't carry it any... {DISTRESS_SPOT}... please...
YEFIM: Three hours it's been saying that. Same words, same place. A loop. Whoever set it running might be dead. Might not.
YEFIM: Go and find where it's coming from. If it's a trap, come back. If it's a man, bring him back.
JOURNAL - The Burst: a looping distress call from {DISTRESS_SPOT}.
[At the spot: a battered radio wired to a car battery, and a note.]
LOG (note) - "Bunker under the marsh. Key with the fishermen - the Headman knows. Grey ones are looking for me. If you read
  this and you're not one of them, come. - P."

## The Key (the Headman)

HEADMAN PETRO*: The bunker? Nobody goes in the bunker. The water came up and took half of it, and what lives in the other
  half doesn't like visitors.
HEADMAN: I have the key. My father had it before me. Why would I give it to you?
[1] A man's dying in there.
  HEADMAN: Men die everywhere. ...But. My son went to the marsh road last month and came back... quiet. Wrong. If you find
  out what's doing that, the key is yours. Bring me something that explains it.
  -> (a favour: clear the Prowlers nesting by the boat sheds)
[2] I'll pay. (costs Ru)
  HEADMAN: Then pay well. The key doesn't care who holds it. I do, a little.
[3] I'm taking it.
  HEADMAN: Then take it, and take my curse with it. The village will remember your face.
  -> (the fishing village turns unfriendly; prices go up, some quests close)

## The Engineer (the Flooded Bunker)

[Deep in the bunker, past the mutants, a barricaded room.]
PAVEL (through the door): Stop! I have a - I have a gun. I'll use it. Are you... are you with them?
[1] With who?
  PAVEL: The grey ones. If you don't know who I mean, you're not with them. ...God. Okay. Okay.
[2] Yefim sent us.
  PAVEL: I don't know any Yefim. But you're not wearing robes, so I'll take the chance.
[CUT] The barricade scrapes aside. A thin man, pale, a soaked bandage on his thigh, sitting on a steel crate.
PAVEL: Pavel. I was an engineer. Before. I built things for people I shouldn't have.
PAVEL: This crate - I can't explain now. If they get it, a lot of people stop being people. Can you get me out?
[1] Can you walk?
  PAVEL: Badly. I'll keep up. I'll have to.
[2] What's in it?
  PAVEL: Later. Please. When there are walls between us and the marsh, I'll tell you everything. Some of it.

## The Search (the way back)

BARK (Pavel, when "those" are in sight) - That's them. Don't let them see me. Please.
BARK (Pavel, hiding) - Down, down, down...
BARK (Pavel, hurt) - Leg's... fine. It's fine. Keep going.
LOG (if "those" spot the group) - The robed figures stop and watch. They don't come closer. They just watch.
[At the village edge:]
LOG - The robed figures stop at the edge of the guards' lamps. They stand there a long time. Then they're gone.
YEFIM: Who is this? ...Never mind. Inside. Medic! Now!
PAVEL: Thank you. I mean it. They'll know where I am now. But at least there's a fence.

## The Culvert (Pavel)

PAVEL: When I ran, I had two things. The crate, and a canister. I couldn't carry both through the marsh, so I hid the canister
  in the Rust Culvert - the drainage cave by the road.
PAVEL: Take this radio. I'll talk you through it. It's sealed, grey, about this big. Don't open it. Please don't open it.
JOURNAL - The Culvert: Pavel hid a sealed canister deep in the Rust Culvert.
RADIO (Pavel, entering) - You're in? Good. It's further than you think. I was... I was panicking.
RADIO (Pavel, deeper) - There's a pool, and past it a shelf of rock. I wedged it behind the rocks. You should see it.
[The canister is picked up. The ground shakes.]
[CUT] The camera turns from the player(s) to the dark at the far end of the cave. Something enormous stands up. Rock dust falls
  from its back. Its breath fogs in the torchlight. Controls lock for a heartbeat.
RADIO (Pavel) - What was - what's that noise? Is that - that's not - get out. GET OUT. RUN!
BARK (player's character) - Move! Move!
[The chase. At the pipe:]
LOG - The thing slams into the pipe mouth behind you. Concrete cracks. It can't fit. It screams.
RADIO (Pavel) - Are you alive? Say something. ...Oh, thank God. That thing doesn't belong here. Nothing that big lives this
  far south. Something is pushing them.

## The Husks (back in the village)

[CUT] Pavel opens the canister on the medic's table. Inside: a hard drive in foam, and a sheaf of printed logs.
PAVEL: Field test logs. They've been testing it here. In this marsh.
PAVEL: It's a transmitter. It doesn't kill you. It switches you off. What's left still walks, still holds a gun, still
  remembers how to pull a trigger - badly. Nothing else.
YEFIM: The fishermen who didn't come home. The courier.
PAVEL: I'm sorry. I'm so sorry. I helped build the first one.
[1] Who are they? The ones in grey?
  PAVEL: I never learned their name. I'm not sure they have one. People who decided the Zone should be... quieter. Obedient.
[2] How do we stop it?
  PAVEL: I don't know yet. The drive has more. I need time, and somewhere safer than a village with a fence.
JOURNAL - The Husks: "those" are testing a mind-switching transmitter in the marsh. The missing people are Husks now.
BARK (Wanderer) - Saw one of the fishermen on the road. Walking. Bleeding. Didn't stop when I called his name.
BARK - Don't trust anything that moves slow out there.

## Defend Bonfire Village (the final night)

[CUT] Dusk. A guard on the watchtower lowers his binoculars.
GUARD: Yefim... Yefim! The marsh is moving!
YEFIM: Everyone with a gun, to the fence! Pavel, bunkhouse - lock the door and don't open it for anyone you can't name!
YEFIM: (to the player) You brought him here. Now help us keep him.
BARK (guards) - Husks on the east road! / They're not stopping! / Reload, reload! / Behind the shed - they're inside!
RADIO (Pavel, from the bunkhouse) - Something's at the door. Scratching. Please hurry.
LOG (wave 3) - More of them. Some carry rifles. Some carry nothing but their hands.
[After the last wave, dawn.]
YEFIM: Still here. All of us that matter. ...Most of us.
YEFIM: They'll come again. Not tonight. But Pavel can't stay. North is where the answers are, and north is where you're going.
PAVEL: There's a scientist. Osei. At the Dried Lake, past the Scavenger's Hub. If anyone can read this drive, she can. But I
  can't walk through two more regions of that. Not yet. Find the road first. Then come for me.
JOURNAL - The Road North: find a route north for Pavel. Tier 2 lies past the Causeway and the Cutting.

---

# TIER 2 - THE ROAD NORTH

## Taken

[First steps into Tier 2, by either road.]
BARK (unseen) - Now.
[CUT] A clink at your feet. White. A ringing whine. Shapes. Boots. Thuds. Black. A short load.
[CUT] A concrete shed. A bare bulb. The player(s) on the floor, hands tied. Garrison soldiers. An officer pulls a chair round.
LIEUTENANT KROL*: Good morning. You walked onto our road, so you belong to us now. Simple rules. Simple people.
KROL: We found this on you. (holds up the passcard) A lab card. Old. Where did a scavenger get a lab card?
[1] Found it on a body.
  KROL: Everybody finds everything on a body. Lucky for you, I don't care where it came from. I care where it opens.
[2] Go to hell.
  KROL: Later. First, you're going down.
[3] What do you want?
  KROL: What everybody wants. What's down there.
KROL: Under this dock is a hatch. Under the hatch are tunnels. Somewhere in the tunnels is a door this card opens. You're going
  to take us there.
KROL: Your weapons go in the box. Your knife you can keep - you'll want it, and you can't hurt us with it. Corporal, the box.
[1] And if I don't know the way?
  KROL: Then you'll learn it fast. I am a patient man for exactly as long as my men are not bored.
JOURNAL - Taken: the Garrison want the door the passcard opens. Find it in the tunnels under the Loading Dock.

[The maze. Soldiers follow at gunpoint. Their barks harden over time - the only clock.]
BARK (soldier, early) - Which way?
BARK (soldier) - Keep walking. Hands where I can see them.
BARK (soldier, middle) - We've been past this pipe. I know this pipe.
BARK (Krol, late) - You're wasting my time. My time is expensive.
BARK (Krol, last) - Last chance. Find it, or I find out what a scavenger is worth dead.
[Too long:] [CUT] Krol sighs. A shot. Black. (game over -> the auto-save at the top of the hatch)

[The door, in the middle of a long corridor.]
KROL: That's it. That's the door. Go on. Use the card.
LOG - The reader blinks red. Then a siren, so loud the walls hum.
BARK (soldier) - What did you do?! What did you -
[CUT] The far end of the corridor: lights flicker. Movement - many shapes, fast, low. The camera pulls back to the soldiers
  raising their rifles.
KROL: Contact! Line up - line up and fire!
BARK (players' cue, any) - Now - while they're busy - RUN!
[The run back. Behind: rifle fire, shouting, then screams, then fewer rifles.]
BARK (soldier, behind) - Don't leave us - !
LOG - The shooting behind you stops. Something is still coming.
[At the top: the box.]
LOG - Your weapons are still in the box. Nobody is left to guard it.
JOURNAL - Taken: Whatever is behind that door, the Garrison wanted it badly. The card didn't work. Come back with better gear -
  and a card that does.

## The Scrap King's Call (a day later)

RADIO (the Scrap King) - You're the one who came up out of the Dock with all your fingers. Word travels.
SCRAP KING: The soldiers are gone, and some fools from a rival crew have moved into my Dock. My Dock. Clear them out and you'll
  find the Scrap King generous. Fail and you'll find him... less generous.
[1] What's the Dock to you?
  SCRAP KING: Everything that comes in or out of the Basin comes past it. Whoever sits on the Dock eats. I like to eat.
[2] Deal.
  SCRAP KING: Good. I like people who don't make me explain things twice.

## Yefim's Last Request: Bogdan (Tier 2, the Tire Fort)

RADIO (Yefim) - The keepsake you brought back. There was a name on the back I didn't want to see. Bogdan.
YEFIM: We rode together. Years ago. He sold three of us to the Garrison for a crate of rifles. One of them was my brother.
YEFIM: He holds the Tire Fort now. I'm too old to go. You're not. I'm not asking you to kill him. I'm asking you to look him in
  the eye and decide.
[At the Tire Fort, if the player reaches Bogdan without killing everyone:]
BOGDAN: Yefim sent you? Ha. The old man still counts his dead. Tell him I count mine too.
[1] (Fight)
[2] He wants you to know he remembers.
  BOGDAN: ...Tell him I remember too. Tell him it was a bad year, and I was a bad man, and I'm still a bad man, but I'm tired.
  (He tosses over a dog tag.) His brother's. I kept it. Don't know why.
YEFIM (hand-in, tag): ...Thank you. For bringing this back instead of him.

## The Runner's Patron

THE RUNNER: You know who I really work for? The Scrap King. Every crate I move, he takes half. Every road I find, he owns.
THE RUNNER: I want out. I want my own routes, my own name. But the King doesn't let people out.
[1] I'll help you get free.  -> (the King's men come for you; the King turns hostile)
[2] I work for the King.  -> (the Runner's routes become the King's; the tunnel stays open for you)

## The Liberators at the Bridge

LIBERATOR SERGEANT*: Halt. Bags open, all of you. We're free people here - free to search you, too.
LIBERATOR: Anything with a blinking light, we keep. Anything that looks like it came from a lab, we really keep.
[1] We're just passing.  -> (search: high-tech items confiscated)
[2] We'll take another crossing.  -> (the Ford or the Rail Bridge)
[3] (Pay)  LIBERATOR: Freedom has a price. Today it's this one.
[4] (After the convoy job) We kept your convoy alive.
  LIBERATOR: ...You did. Go on through. Don't make me regret it.
[If fought:] LOG - The Liberators will remember this.

## The Escort (finale)

PAVEL: You found a road. I can see it in your face. You don't look sure, but you found one.
PAVEL: I'll be slow. Don't wait for me if - no. Wait for me. Please wait for me.
BARK (Pavel, on the road) - I used to drive this road. In a car. With heating.
BARK (Pavel) - Is that... is that one of them on the ridge?
[Sniper's Lip:]
LOG - A glint on the rim. Then another.
BARK (Pavel) - Snipers! Who would pay snipers to kill me? ...Oh. Them. Of course them.
[The road cut checkpoint (Raiders) - lines depend on the Scrap King's standing.]
RAIDER: Toll. ...Wait, you're the King's friend. Go on.  /  RAIDER: Toll. And the skinny one stays.
[Arrival, the Scavenger's Hub gate.]
SENTINEL GUARD*: Weapons lowered inside the walls. Trade, drink, sleep. Start a fight and you leave in a bag.
PAVEL: Walls. Real walls. I could kiss them.
RADIO (Yefim) - Made it to the Hub? Good. Listen - that thing in the Culvert. It's still there. Scaring my hunters off the
  road. When you're stronger, come home and finish it. (-> The Brute rematch)

---

# TIER 3 - THE SAFE HAVEN

## Lev Marchuk (the Rusty Anchor)

[CUT] A corner table. A big man in a patched coat, a bottle, an untouched plate. The barman nods toward him.
BARMAN (the Rusty Anchor): That's Marchuk. Yes, that Marchuk. Don't ask him for stories. He doesn't tell them any more.
LEV: You're staring. Everyone stares. Sit or leave.
[1] They say you've been everywhere.
  LEV: They say a lot. I've been to enough places to know which ones you don't come back from. Same thing, mostly.
[2] We brought someone from the south. An engineer. Running from people in grey robes.
  [CUT] Lev puts the glass down very carefully.
  LEV: ...Say that again.
  LEV: Grey. No marks. No faction. They don't talk on the road. They stand and look at the ground like it owes them money.
  LEV: Your engineer is a dead man. So are you, if you stand next to him long enough.
[3] (Leave)
LEV (later, after the Osei delivery): You actually got him to the lake. Hm. Maybe you're luckier than I was.
JOURNAL - Lev Marchuk: a legend of the Zone, drinking himself quiet in the Rusty Anchor. He knows "those".

## The Pills (Lev's medication)

LEV: My hands. Look at them. Shaking like a rookie's. There's a pill for it - for the noise in the head. I ran out a week ago.
LEV: The Bazaar won't have it. Nobody sells it. Osei had some once. Or there was a medical crate on that ferry in the lake -
  if the things living in it haven't eaten the boxes.
[1] I'll find it.
  LEV: Don't do it for me. Do it so I'm useful. Useful is better than sorry.
[Hand-in:] LEV: ...Thank you. The noise is... quieter. Sit. I'll tell you something I haven't told anyone.

## The Confession

LEV: I was one of them.
LEV: Not born to it. Recruited. They find people who are good and angry and lonely, and they give them a reason. Order. A Zone
  that stops killing everyone. It sounds beautiful when you're tired.
LEV: Then I saw what "order" meant. The first field test. A village. They switched it off like a lamp.
LEV: I walked out. Took what I knew and made a name with it. Every legend you heard about me - I used their maps, their
  routes, their tricks. And I never once went back to stop them. Because I knew it wasn't enough. One man isn't enough.
[1] It's not one man now.
  LEV: ...No. It isn't, is it.
[2] Why tell us?
  LEV: Because your engineer built their machine, and you saved him anyway. Either you're fools or you're the first good
  news I've had in nine years.

## To the Sub-Station

DR. PRIYA OSEI (radio, when the player first reaches the Dried Lake): This is the Sub-Station. If you're coming across the
  flats after dark, you're either brave or stupid, and I need people who are both. Who's with you?
OSEI (in person): Pavel. From the institute. I read your papers. I hated your papers.
PAVEL: Everyone did. They were right to.
OSEI: Show me what you brought.

## The Lockdown

[The Sub-Station is sealed when the player arrives with Pavel - a mutant attack tripped its defences.]
RADIO (Osei, inside) - The turrets went into full lockdown when the Prowlers hit us. I can't override from in here. The
  breaker room is outside, east wall. Cut the turrets, and I can open the door - and then everything outside can come in too.
  So be quick.
BARK (Pavel) - They're coming out of the reeds. Hurry!
LOG - The turrets spin down. The door grinds open. Everyone inside. Now.

## The Reveal

[CUT] Osei's lab. Two screens, the drive, the Lab Alpha specs. Pavel, Osei, Emil Rask, Konrad Hale at the door.
OSEI: These aren't prototypes. These are production drawings. Tooling, part numbers, batch sizes.
PAVEL: They're not testing any more. They're manufacturing.
OSEI: A counter-signal is possible. Cancel their frequency - a wide band, a big antenna. But the processing... nothing out here
  can do it. Except - (she taps the map) - the old array in the north. If it still has power.
EMIL RASK: That's suicide, Priya. The north is crawling with Obelisk.
OSEI: Then we'll need someone suicidal. (looks at the player) No offence.
[1] None taken.
[2] Some taken.
OSEI: There's one more thing the counter-signal needs - a core, tuned to their frequency. We don't have one. They do.
JOURNAL - The Reveal: "those" are making the device in numbers. A counter-signal needs the old array's power - and one of
  their own tuned cores.

## Hale and Rask (the red herring)

BARK (Hale, on the radio, quiet) - ...yes, the usual crates. No, not tonight. Tomorrow.
LOG - Konrad Hale lowers a small radio into a drawer when he sees you. He doesn't lock it.
HALE: Supplies. People always think it's something else. It's always supplies.
RASK: Konrad? He's a good man. Bit nervous. Who wouldn't be, out here?
RASK: Can I get you anything? Coffee? It's terrible, but it's hot.
(Later, the truth about Rask is a Tier 5-6 beat.)

## Osei's thread: the spikes below

OSEI: The researcher who died in the Depths - his station was measuring spikes from deeper underground. Bedrock doesn't spike.
OSEI: Something under the lake is drawing power. A lot of it. I want to know what.

---

# TIER 4 - THE BARRIER

## The Liberators' Call

LIBERATOR COMMANDER MIRO* (Vanguard Barracks): The Obelisk are massing in the Crimson Woods. More than I've ever seen. And
  they're organised - since when are fanatics organised?
MIRO: They'll hit our barrier at dawn. I need every gun. Even yours. Even after the bridge. (if the player fought the
  Liberators) - Help us hold, and we'll call that forgotten.
TOLIK BRODSKY (at the barrier): Hey! New faces. I'm Tolik. This is Gertrude. (slaps his exoskeleton) Say hello, Gertrude.
  ...She's shy.
TOLIK: You hold the left, I'll stand in the middle and be very large. That's my whole plan. It usually works.

## The Siege

BARK (Liberators) - Here they come! / RPG on the ridge! / They're on the right - grenades! / Machine gun in the trees!
[CUT] Through the smoke: a tall figure in Obelisk white, a glowing violet sphere strapped to his back, arms raised.
THE PREACHER: Be still. Be quiet. Be one.
LOG - A wave of violet light rolls out from him. Your vision swims.
BARK (Tolik, if near) - Ugh - my nose is bleeding - that's normal, right? That's not normal.
RADIO (Osei) - That's it - the core on his back! That's what we need! Don't hit the sphere! Whatever you do, don't break it!
BARK (player cue) - Get down when the light comes!
[The Preacher falls.]
LOG - The violet sphere is warm, humming, intact.
MIRO: We held. We actually held. ...Whatever you just pulled off his back - take it far away from my men.
JOURNAL - The Barrier: the Preacher's core is the countermeasure's missing piece.

## The Apex (red forest side job)

HUNTER* (Vanguard Barracks): Something's taking whole patrols in the red woods. Not killing - taking. We find boots.
HUNTER: My grandfather called it {APEX_NAME}. I thought it was a story to keep kids out of the trees.
LOG (in the woods) - Claw marks on a tree, higher than a man can reach.
[After the kill:] HUNTER: That's... that's it. That's really it. Take the trophy to the scientists - they pay for anything
  with that many teeth.

## The Summit (Sentinels and Liberators)

[The Rusty Anchor's back room, or Vanguard Barracks. Miro for the Liberators, Commander Dovbush* for the Sentinels.]
DOVBUSH: I didn't come here to drink with anarchists.
MIRO: And I didn't come to be lectured by a man who salutes his own boots.
[1] The Obelisk are being driven. By the same people who make Husks. You both bleed for this or neither of you does.
[2] You did it once before, I hear. A truce, the last time someone tried to put a leash on the Zone.
DOVBUSH: ...We hold the north road. Your people hold the woods.
MIRO: Until it's done. Not a day longer.
DOVBUSH: Not a day longer.

---

# TIER 5 - THE ARRAY

## Before the push

OSEI: The countermeasure is ready. And these - (helmets on the table) - they won't stop the array, but they'll stop it from
  stopping you. Mostly.
PAVEL: I'm sorry I can't come. I'd only slow you down. ...I've been saying that for a long time, haven't I.
LEV (radio, or in person if he came): I know that road. Every checkpoint they had nine years ago. They'll have more now. Take
  the drainage ditch on the second bend - they never watched it.

## The serpentine road

BARK (Obelisk, unaware) - The song is loud today.  /  The song is always loud.
LOG - A figure in grey stands beside the Obelisk officer. Not praying. Giving orders.
BARK (player) - That's one of them. Commanding the Obelisk.
[The grey commander retreats into the bunker.]

## The breach

LOG - A machine gun holds the corridor. Nothing gets past the corner.
BARK (player hints) - We need another way in. / That hangar door - blow it.
[A "those" heavy throws a silver cylinder.]
LOG - The air folds. Everything loose slides toward a point in the middle of the room.
[A crippled Obelisk on the floor.]
OBELISK (dying): ...the song... where did the song go...
[1] (End it)  LOG - He was a stalker once. Someone's son.
[2] (Leave him)

## The Array Commander

[CUT] A control hall. The array hums so loud it shakes dust from the ceiling. A man in grey, his skin veined violet, a
  shimmering shield in front of him, a black sphere growing between his hands.
ARRAY COMMANDER: You carried our engineer across three regions. Do you know what you've cost us? Nothing. We have time. We
  are very patient.
ARRAY COMMANDER: Three minutes, and the song reaches every mind from here to the Reactor. Kneel, and you'll still be
  yourself. A quieter self.
[1] (Fight)
[2] Lev Marchuk says hello.
  ARRAY COMMANDER: ...Marchuk. Still alive. Still drinking, I'd guess. Still doing nothing.
[The fight: shield up while his guards live.]
LOG - Your rounds spark off the shield.
[All guards down - Tolik bursts in, even if he was sent away.]
TOLIK: Gertrude! Full power! (a crash) Hello! Sorry I'm late! I followed the screaming!
LOG - Tolik slams into the commander. The shield flickers out.
[Adds return.] TOLIK: More of them! I'll hold him - kill the little ones!
[Final 20 seconds, the console:]
BARK (player) - The console - burn it!
LOG - The thermite goes through the gap in the casing. The array screams, then goes silent.
[CUT] Silence. Dust settling. Somewhere above, the Obelisk stop chanting.
TOLIK: ...Is it supposed to be that quiet?
JOURNAL - The Array: the array is dead. The converted Obelisk stay converted - but "those" have lost their leash.

## The gravity weapon

LOG - The commander's gravity device is intact.
MIRO (radio): Bring it to us. Something like that could hold the barrier forever.
OSEI (radio): Bring it to me. I can learn how they built it.
[1] Give it to the Liberators.
[2] Give it to Osei.
[3] Destroy it.  ->  BARK (player) - Nobody gets one of these. Nobody.

---

# TIER 6 - THE SIEGE WARD

## The fallback

OSEI: Rask is gone. His room's empty. His terminal was wiped this morning.
HALE: I told you. I told everyone. Nobody listens to the supply man.
OSEI: Konrad - your radio. The one in the drawer.
HALE: My sister. She's in the Hub. I call her every night so she knows I'm alive. That's the whole secret.
OSEI: ...Rask took the decrypted array logs. Everything we learned about the counter-signal. Whatever they do next, they'll do
  it knowing what we know.
LEV: Then they'll go where they feel safe. The hospital. The Garrison sit on it like dogs on a porch - they think they're
  guarding a fortress for their own command. They're guarding it for "those". They've never once asked who pays them.

## The bridge ambush

[A Garrison patrol on the bridge to the Siege Ward. One survives and surrenders.]
GARRISON SOLDIER: Don't - don't shoot! I'll talk! There's twenty in there, maybe more. And the eggheads. The grey eggheads.
  They don't eat with us. They don't talk to us. They just... build things in the basement.
[1] What are they building?
  SOLDIER: A door. That's what the sergeant called it. A door to somewhere that isn't here.
[2] Who's your commander?
  SOLDIER: Major says the orders come from up the line. Nobody's ever seen up the line.

## The portal

[Sub-level 1: too quiet. Behind glass, a science team in grey around a ring of humming machines. A shimmering rip in the air.]
EMIL RASK (in the room, among them): Stabilising. Thirty seconds. The first team goes through on my mark.
BARK (player) - Rask...
[1] (Wait for the first one to step in, then shoot the console)
  [CUT] The first robed figure steps into the rip. The console bursts. The rip twists, swallows half the room, and slams
  shut on the rest. The survivors run for the only door - yours.
  BARK (player) - Barricade it! Everything you can push!
[2] (Shoot Rask now)
  [CUT] Rask drops. The team panics; one keys the portal early; the rip flares and dies. Fewer trapped, more escaped.
JOURNAL - The Siege Ward: the portal led to the deepest lab. Some of "those" went through. The rest are sealed in.

## Rask (if alive)

RASK (behind the barricade, through the door): You don't understand. You never did. The Zone kills everyone. Everyone.
  We were going to make it stop. Quiet. Peaceful. Is that so evil?
[1] Ask the Husks.
[2] Ask Pavel.
RASK: ...Tell Priya I'm sorry about the coffee.

---

# TIER 7 - THE DESCENT

## The guide

GUIDE* (a Liberator scout): There's a way under the city. Old service tunnels, flooded, full of gas. Nobody uses them because
  nobody comes back. That's why they're not watched.
GUIDE: I'll take you to the elevator. Not one step further.

## The elevator

[CUT] A freight cage dropping into green fog. The lights flicker. Water drips through the roof of the cage.
LEV (radio, or in person): This is where I walked out. Nine years ago. Up this shaft. I swore I'd never come back down.
LEV: ...Funny what you'll swear when you're scared.
LOG - A wire across the airlock, at ankle height. Somebody wanted visitors.

## The bottom (draft - the ending isn't decided)

[CUT] The deepest lab. Rows of transmitters in crates, ready to ship. And in the middle, the thing all of it was built to
  serve, still unclear - Yan to decide.
VOICE (one of "those", calm): You've come a long way to break the only thing that could save this place.
[Possible choices, from Campaign 2's ending:]
[1] Save the survivors on the surface (the counter-signal goes out; the lab is lost).
[2] Take the data (the cure for the Husks; the surface fends for itself).
[3] (Others to be decided once the ending is.)

---

## Placeholders to fill (per world, picked when it's created)

`{COURIER_SPOT}`, `{DISTRESS_SPOT}`, `{RAIDER_CAMP}` and the greenhorn's and the Runner's contact's spots - each a different
POI no other mission uses. `{APEX_NAME}` - the red forest apex mutant's name (Yan).

## New names in this draft (marked *, for approval)

Stas (Bonfire barman), Headman Petro (fishing village), Lieutenant Krol (Garrison interrogator), the Liberator sergeant,
Commander Miro (Liberators), Commander Dovbush (Sentinels), the hunter at Vanguard Barracks, the Siege Ward guide, the
Sentinel guard at the Hub gate. Tolik's exoskeleton is called Gertrude.
