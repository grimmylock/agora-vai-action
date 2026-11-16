# Elder Gaunt claw: key-pose brief (for SpriteCook edit mode, 12 credits per pose)

Source still: `redesign_v1_arms.png` (the approved Elder). Side view, facing right, antler skull on the right.
Every edit uses the same still as the reference and the same canvas size, so the body stays put.

## Rules in every prompt
- Pixel art, same palette, same outline, same pixel size. Do NOT redraw, resize, move or restyle anything except the NEAR (front) arm and the torso lean named below.
- Keep exactly: antlers (both), skull and teeth, spine knobs, rib cage and exposed bones, legs, the far arm, the background (transparent/flat).
- The near arm keeps THREE visible elbow bends (the unnatural segmented arm), full length, bone-thin but with its bones visible; the hand keeps 4 long fingers with claws. No mitts, no merged fingers, no new limbs.
- One arm only moves. No motion lines, no debris, no dust, no text.

## Pose 1: WIND-UP (frame 3 of 8)
Torso leans BACK about 12 degrees (head draws away from the target, front legs unchanged). Near arm is pulled up and BACK behind the shoulder: upper arm rises to about 60 degrees above horizontal pointing back-left, the outer elbows fold so the hand sits above and behind the spine knobs, claws open and curled, palm facing forward. Arm is clearly separate from the back (a gap of sky between hand and spine).

## Pose 2: STRIKE (frame 5 of 8, the contact frame)
Torso lunges FORWARD about 15 degrees, head dips toward the target. Near arm is thrown FORWARD and slightly down, fully extended in a line about 20 degrees below horizontal, reaching past the skull's jaw by about one head length. All three elbow bends straightened but still readable as segments. Hand open, fingers spread wide, claws forward, palm down.

## Pose 3: RECOVER (frame 7 of 8)
Torso returns to about 5 degrees forward. Near arm drops back toward the neutral hang but is still swinging: upper arm about 30 degrees forward of vertical, lower segments folding back, hand loosely open at knee height.

## Build from the three poses
Frames 1-2 and 8 come from the neutral still plus the shear lean (`gaunt_codeframes.py`). Frames 4 and 6 are in-betweens: cross-fade the arm region only between the two nearest poses and shear the body between their leans, no generative step. Timing [110,150,140,70,70,60,110,110] ms, contact on frame 5.
Show Yan the three poses first. Redo any pose that loses antlers/bones or turns the hand into a mitt before anything is built.
