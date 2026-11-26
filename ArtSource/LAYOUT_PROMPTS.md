# Gemini prompts for building layouts (Yan pastes one, sends me the image, I rebuild it in the game)

How it works: you paste a prompt into Gemini, send me the picture and say which building and which map spot it is for. I read the walls, doors, windows, rooms and furniture, redraw it as a plan with our tiles, floors and props, run the reachability check and show you the render. The picture is only a blueprint; its colours and furniture style are not used.

## Base prompt (fill in the part in [brackets])
```
A flat top-down architectural floor plan of [BUILDING, e.g. "a small abandoned roadside pharmacy"], seen straight from above with no perspective, no 3D, no shadows.
Draw it on a visible square grid where every square is 1 metre, and write the width and height in squares under the plan (about [W] x [H] squares).
Thick dark walls, gaps for doors (draw each door as a short arc or a break in the wall), small rectangles for windows.
Label every room in plain text (for example: Waiting room, Counter, Storage, Toilet).
Show furniture as simple labelled top-down shapes (bed, table, chair, counter, shelf, sofa, stove, sink, toilet, bath, desk, locker, crate), not decorated.
Rooms of different sizes, a believable circulation: every room reachable from the entrance, one main entrance and one back door. Plain white background, no text except the labels.
```

## Ready-made fills (copy the sentence into [BUILDING], sizes in squares)
- Small house: "a small two-bedroom family house with a hallway, a kitchen with a table, a bathroom and a living room" (14 x 10)
- Farmhouse: "an old two-room farmhouse with a big kitchen, a pantry, a parlour and two bedrooms, a porch at the front" (16 x 12)
- Corner shop: "a corner shop with a sales floor, a counter near the door, a stock room and a toilet at the back" (14 x 9)
- Pharmacy: "a pharmacy with a waiting area, a prescription counter, a dispensing room and a storage room" (12 x 9)
- Doctor's clinic: "a small clinic with a waiting room, two consulting rooms, a treatment room, a store and a toilet" (16 x 10)
- School wing: "one wing of a village school with four classrooms off a long corridor, a staff room and toilets" (30 x 12)
- Office block floor: "an office floor with an open-plan area, a manager's office, a meeting room, a kitchenette and toilets" (24 x 14)
- Police station: "a small police station with a front desk, an open office, an interview room, two cells and an armoury" (20 x 12)
- Warehouse with offices: "a warehouse hall with a loading door, a two-room office and a staff toilet in one corner" (24 x 16)
- Block of flats floor: "one floor of a block of flats with a central corridor and six small flats, each with a living room, a kitchen nook, a bedroom and a bathroom" (30 x 12)
- Cinema: "a small cinema with a foyer, a ticket booth, a concession counter, one screening hall with rows of seats and a projector room" (26 x 16)
- Diner: "a roadside diner with a kitchen, a service counter with stools, booths along the windows and a toilet corridor" (22 x 8)

## Tips
- Ask Gemini for several versions and send me the one with the clearest walls; a plan with the grid visible is the easiest to copy exactly.
- Tell me if you want it ruined (collapsed wall, missing roof section) and where it stands (street, field, inside a compound).
