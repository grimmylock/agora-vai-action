import re, sys
from PIL import Image, ImageDraw, ImageFont
import os
HERE=os.path.dirname(os.path.abspath(__file__)); ROOT=os.path.abspath(os.path.join(HERE,"..",".."))
R=os.path.join(HERE,"_render"); OUT=os.path.join(ROOT,"maps","renders")
src=open(os.path.join(ROOT,"Assets","Scripts","GameData","MacroMapDatabase.cs")).read()
names=dict(re.findall(r'Id = "([a-z_]+)", Name = "([^"]+)"', src))
def font(sz):
    try: return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", sz)
    except: return ImageFont.load_default()
def labelled(zone, width, out, title=None):
    im=Image.open(f"{R}/{zone}.png").convert("RGB")
    s=width/im.width; im=im.resize((width,int(im.height*s)),Image.LANCZOS)
    d=ImageDraw.Draw(im); f=font(max(11,int(width/80))); px=16*s
    for line in open(f"{R}/labels_{zone}.txt"):
        k,x,y,name=line.rstrip("\n").split("\t")
        if k=="E" or name.startswith("arrive:"): continue
        x,y=(int(x)+.5)*px,(int(y)+.5)*px
        if name.startswith("exit:"): col=(120,210,255); t="> "+names.get(name[5:],name[5:])
        elif name.startswith("stash:"): col=(255,150,205); t="$"
        else: col=(255,222,110); t=name
        r=max(3,int(width/300))
        top=int(width/52)+16 if title else 0
        y=max(y, top+f.size)
        d.ellipse((x-r,y-r,x+r,y+r),fill=col,outline=(0,0,0))
        if t!="$":
            tw=d.textlength(t,font=f)
            tx=x+r+3 if x+r+3+tw < width-4 else x-r-3-tw
            d.text((tx,y-f.size//2-1),t,font=f,fill=col,stroke_width=2,stroke_fill=(0,0,0))
    if title:
        tf=font(int(width/52)); d.rectangle((0,0,width,int(width/52)+16),fill=(16,18,16)); d.text((10,7),title,font=tf,fill=(235,230,210))
    im.save(f"{OUT}/{out}",quality=86); return im.size
tiers=[("perimeter","Tier 1 - The Perimeter, Toxic Marshes, Smuggler's Run, Bonfire Village"),
       ("scrapyard","Tier 2 - The Scrapyard, Industrial Basin, Black Valley"),
       ("scavengers_hub","Tier 3 - Scavenger's Hub, The Rail-Yards, Dried Lake"),
       ("vanguard_barracks","Tier 4 - Vanguard Barracks, Ghost Town, Crimson Woods"),
       ("quarry_basin","Tier 5 - Quarry Basin, Ship Graveyard, The Antenna Grid"),
       ("abandoned_city","Tier 6 - Abandoned City, The Siege Ward, Anomalous Epicenter")]
for i,(z,t) in enumerate(tiers,1):
    print(labelled(z,1400,f"tier{i}_overview.jpg",t))
    im=Image.open(f"{R}/{z}.png").convert("RGB"); im.resize((2400,2400),Image.LANCZOS).save(f"{OUT}/tier{i}_detail.jpg",quality=88)
dung={1:["secret_cave","chapel_crypt","pumping_basement","flooded_bunker","smugglers_tunnel"],2:["crusher_pit","valley_mine","secret_lab_alpha"],
      3:["stranded_ferry","sub_station_depths","maintenance_tunnels"],4:["camp_shelter","secret_lab_beta","deep_caverns"],
      5:["drydock_tunnels","underground_labyrinth","secret_lab_gamma"],6:["hospital_morgue","underground_bunker","siege_catacombs"]}
for t,zs in dung.items():
    ims=[]
    for z in zs:
        im=Image.open(f"{R}/{z}.png").convert("RGB"); sc=min(560/im.width,480/im.height); im=im.resize((int(im.width*sc),int(im.height*sc)),Image.LANCZOS)
        d=ImageDraw.Draw(im); f=font(13); px=16*sc
        for line in open(f"{R}/labels_{z}.txt"):
            k,x,y,name=line.rstrip("\n").split("\t")
            if k=="E": continue
            x,y=(int(x)+.5)*px,(int(y)+.5)*px
            col=(120,210,255) if name.startswith("exit:") else (255,150,205) if name.startswith("stash:") else (255,222,110)
            tx="exit up" if name.startswith("exit:") else "$" if name.startswith("stash:") else name
            d.ellipse((x-3,y-3,x+3,y+3),fill=col,outline=(0,0,0))
            if tx!="$": d.text((x+6,y-8),tx,font=f,fill=col,stroke_width=2,stroke_fill=(0,0,0))
        tf=font(16); d.text((6,4),names.get(z,z)+("" if z in ["secret_lab_alpha","sub_station_depths","maintenance_tunnels","secret_lab_beta","deep_caverns","underground_labyrinth","secret_lab_gamma","underground_bunker","siege_catacombs"] else "  (new)"),font=tf,fill=(255,255,255),stroke_width=2,stroke_fill=(0,0,0))
        ims.append(im)
    cols=3 if len(ims)>3 else len(ims); rows=(len(ims)+cols-1)//cols
    W=cols*568+8; H=rows*488+8
    sh=Image.new("RGB",(W,H),(14,14,14))
    for i,im in enumerate(ims): sh.paste(im,(8+(i%cols)*568,8+(i//cols)*488))
    sh.save(f"{OUT}/tier{t}_dungeons.jpg",quality=86)
print("ok")
