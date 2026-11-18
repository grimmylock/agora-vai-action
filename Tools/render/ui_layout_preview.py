# Draws the new HUD / case / merchant at 1920x1080 from the SAME numbers as the Unity code (HudView.RE4.cs, HudView.Pack.cs, HudView.Case.cs, ShopUI.cs),
# with the game's own fonts and sprites over a render of the Tier 1 map. It is NOT a Unity screenshot.
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, glob, os
R='/home/user/agora-vai-action/'
W,H=1920,1080
TITLE=R+'Assets/Resources/Fonts/Silkscreen-Regular.ttf'; BODY=R+'Assets/Resources/Fonts/PixelifySans-Regular.ttf'
def F(path,px): return ImageFont.truetype(path,int(px))
FINE=(140,209,115); CAUTION=(242,199,64); DANGER=(230,64,51); BONE=(235,230,214); DIM=(125,122,112); YEL=(232,197,71); CYAN=(79,182,201)
BRASS=(140,110,61); BRASSD=(82,66,41); LEATHER=(19,16,13); INK=(10,9,8); GOLD=(237,199,71)
def bg(seed=0):
    im=Image.open(R+'maps/renders/tier1_detail.jpg').convert('RGB')
    w,h=im.size; cw,ch=480,270; x0,y0=int(w*0.36),int(h*0.40)
    c=im.crop((x0,y0,x0+cw,y0+ch)).resize((W,H),Image.NEAREST)
    return c
def soft(im,x,y,w,h,alpha=140):
    m=Image.new('L',(w,h),0); d=ImageDraw.Draw(m)
    for i in range(0,60):
        t=i/60; d.ellipse((int(w*t/2),int(h*t/2),w-int(w*t/2),h-int(h*t/2)),fill=int(alpha*(t**1.6)/1.0*0.02+ (alpha/60)*0 )) if False else None
    # radial falloff
    px=m.load()
    for yy in range(h):
        for xx in range(w):
            u=(xx+.5)/w*2-1; v=(yy+.5)/h*2-1; d=math.hypot(u,v); a=max(0,1-d); px[xx,yy]=int(alpha*a*a)
    im.paste((0,0,0),(x,y),m)
def text(d,xy,s,font,fill,anchor='la',outline=True,spacing=0):
    x,y=xy
    if outline:
        for ox,oy in ((-1,0),(1,0),(0,-1),(0,1)): d.text((x+ox,y+oy),s,font=font,fill=(0,0,0),anchor=anchor)
    d.text((x,y),s,font=font,fill=fill,anchor=anchor)
def ring(im,cx,cy,r,frac,color,alpha=255,thick=0.14,start=-90):
    S=4; sz=int(2*r*S)+8
    L=Image.new('RGBA',(sz,sz),(0,0,0,0)); d=ImageDraw.Draw(L)
    box=(4,4,sz-4,sz-4); tw=int(r*S*thick)
    d.arc(box,start,start+360*frac,fill=color+(alpha,),width=tw)
    L=L.resize((sz//S,sz//S),Image.LANCZOS); im.paste(L,(int(cx-L.width/2),int(cy-L.height/2)),L)
def portrait():
    a=Image.open(R+'Assets/Resources/Sprites/armor_atlas.png').convert('RGBA')
    cell=a.crop((0,0,16,16)); return cell
def wpn(key='VK74'):
    p=R+'Assets/Resources/Sprites/Weapons/weapon_%s_hd.png'%key
    if not os.path.exists(p): p=sorted(glob.glob(R+'Assets/Resources/Sprites/Weapons/weapon_*_hd.png'))[0]
    return Image.open(p).convert('RGBA')
def fit(img,w,h):
    r=min(w/img.width,h/img.height); return img.resize((max(1,int(img.width*r)),max(1,int(img.height*r))),Image.NEAREST)

def hud(state):
    im=bg().convert('RGBA'); d=ImageDraw.Draw(im)
    calm = state=='calm'
    hp,maxhp=(1000,1000) if calm else (260,1000); hpf=hp/maxhp
    col=FINE if hpf>=.67 else CAUTION if hpf>=.34 else DANGER
    if not calm:  # the red edge
        v=Image.new('RGBA',(W,H),(0,0,0,0)); px=v.load()
        for yy in range(0,H,2):
            for xx in range(0,W,2):
                u=(xx/W*2-1); w2=(yy/H*2-1); dd=math.hypot(u,w2)/1.25; a=max(0,min(1,(dd-.55)/.5)); a=a*a*(3-2*a)
                al=int(255*0.36*a)
                for ox in (0,1):
                    for oy in (0,1):
                        if xx+ox<W and yy+oy<H: px[xx+ox,yy+oy]=(204,20,13,al)
        im.alpha_composite(v)
    gA=0.30 if calm else 1.0; aA=0.30 if calm else 1.0; qA=0.40 if calm else 1.0
    layer=Image.new('RGBA',(W,H),(0,0,0,0)); ld=ImageDraw.Draw(layer)
    # --- gauge cluster, origin (56, 52 from bottom)
    gx,gy=56,H-52
    soft_layer=Image.new('RGBA',(W,H),(0,0,0,0))
    sm=Image.new('L',(880,400),0); pm=sm.load()
    for yy in range(400):
        for xx in range(880):
            u=(xx+.5)/880*2-1; v=(yy+.5)/400*2-1; dd=math.hypot(u,v); a=max(0,1-dd); pm[xx,yy]=int(255*.55*a*a)
    layer.paste((0,0,0),(gx-120,gy+100-400),sm)
    cx,cy=gx+86,gy-8-86
    ld.ellipse((cx-86,cy-86,cx+86,cy+86),fill=(10,11,13,209))
    ring(layer,cx,cy,86,1.0,(255,255,255),31)
    ring(layer,cx,cy,86,min(1,hpf+0.0 if calm else 0.52),(255,255,255),166)   # the white trail
    pulse=1 if col!=DANGER else 0.85
    ring(layer,cx,cy,86,hpf,col,int(255*pulse))
    pm2=Image.new('L',(140,140),0); ImageDraw.Draw(pm2).ellipse((0,0,139,139),fill=255)
    pb=Image.new('RGBA',(140,140),(15,17,18,255)); pt=fit(portrait(),150,150); pb.alpha_composite(pt,((140-pt.width)//2,(140-pt.height)//2))
    layer.paste(pb,(gx+16,gy-24-140),pm2)
    ld=ImageDraw.Draw(layer)
    text(ld,(gx+196,gy-128),'FINE' if hpf>=.67 else 'CAUTION' if hpf>=.34 else 'DANGER',F(TITLE,20),col,'ld')
    text(ld,(gx+196,gy-66),str(hp),F(BODY,54),BONE if col==FINE else col,'ld')
    if not calm: text(ld,(gx+196,gy-40),'BLEEDING   CONCUSSED',F(TITLE,13),DANGER,'ld')
    ld.rectangle((gx+196,gy-32,gx+196+230,gy-26),fill=(255,255,255,36)); ld.rectangle((gx+196,gy-32,gx+196+int(230*(1 if calm else .55)),gy-26),fill=BONE)
    ld.rectangle((gx+196,gy-19,gx+196+230,gy-16),fill=(255,255,255,26)); ld.rectangle((gx+196,gy-19,gx+196+int(230*(.4 if calm else .7)),gy-16),fill=CYAN)
    if not calm: text(ld,(gx+196,gy-152),'THIRSTY',F(TITLE,12),YEL,'ld')
    # fade
    if gA<1:
        a=layer.split()[3].point(lambda v:int(v*gA)); layer.putalpha(a)
    im.alpha_composite(layer)
    # --- ammo (origin bottom-right)
    ax,ay=W-56,H-52
    L2=Image.new('RGBA',(W,H),(0,0,0,0)); sm2=Image.new('L',(880,520),0); p2=sm2.load()
    for yy in range(520):
        for xx in range(880):
            u=(xx+.5)/880*2-1; v=(yy+.5)/520*2-1; dd=math.hypot(u,v); a=max(0,1-dd); p2[xx,yy]=int(255*.55*a*a)
    L2.paste((0,0,0),(ax+120-880,ay+100-520),sm2)
    d2=ImageDraw.Draw(L2)
    text(d2,(ax,ay-232),'VK-74',F(TITLE,18),BONE,'rd')
    wi=fit(wpn(),260,110); L2.alpha_composite(wi,(ax-wi.width,ay-118-wi.height+ (110-wi.height)//2 ))
    mag,res=(30,90) if calm else (6,63)
    text(d2,(ax,ay-92),'STANDARD  ·  AUTO',F(TITLE,12),DIM,'rd')
    text(d2,(ax,ay-14),'/ %d'%res,F(BODY,34),DIM,'rd')
    low = mag<=7
    text(d2,(ax-124-0,ay-6),str(mag),F(BODY,88),CAUTION if low else BONE,'rd')
    text(d2,(ax,ay+34),'Y  HALDEN .45 SERVICE',F(TITLE,11),DIM,'rd')
    if aA<1:
        a=L2.split()[3].point(lambda v:int(v*aA)); L2.putalpha(a)
    im.alpha_composite(L2)
    # --- quick slots
    L3=Image.new('RGBA',(W,H),(0,0,0,0)); d3=ImageDraw.Draw(L3)
    qr,qb=W-470,H-60; qx0=qr-190; qy0=qb-190
    offs=[(0,66),(66,0),(0,-66),(-66,0)]; names=['MEDKIT\nx2','STIM\nx1','BANDAG\nx3','']
    for i,(ox,oy) in enumerate(offs):
        cxs=qx0+95+ox; cys=qy0+95-oy
        has=bool(names[i])
        d3.rectangle((cxs-30,cys-30,cxs+30,cys+30),fill=(15,18,15,224) if has else (8,9,10,150),outline=(255,255,255,64))
        text(d3,(cxs-26,cys-27),str(i+1),F(TITLE,10),DIM,'la',outline=False)
        if has: d3.multiline_text((cxs,cys+6),names[i],font=F(BODY,12),fill=BONE,anchor='mm',align='center')
    if qA<1:
        a=L3.split()[3].point(lambda v:int(v*qA)); L3.putalpha(a)
    im.alpha_composite(L3)
    # --- place, alert, log, toasts
    d=ImageDraw.Draw(im)
    text(d,(56,46),'BONFIRE VILLAGE',F(TITLE,28),BONE,'la'); d.rectangle((56,88,206,89),fill=YEL)
    text(d,(56,98),'DAY 1   19:30   CLEAR   TIER 1   SAFE ZONE',F(TITLE,12),DIM,'la')
    if not calm: text(d,(W//2,46),'FIGHT',F(TITLE,16),(200,68,58),'ma')
    log=['You hear gunfire from the east.','[ALERT] Raider 2 has spotted the Player!'] if not calm else ['You step into the Perimeter.']
    for i,s in enumerate(log): text(d,(56,H-268-i*22-18),s,F(BODY,18),BONE,'la')
    if not calm:
        for i,s in enumerate(['Restored 200 permanent HP.','+60 XP: kill.']): text(d,(W-56,H-372-i*30),s,F(BODY,19),BONE,'rd')
        # crosshair
        cx,cy=W//2+120,H//2-60
        for dx,dy,w,h in ((14,0,3,1),(-14,0,3,1),(0,14,1,3),(0,-14,1,3)): d.rectangle((cx+dx*1-w*3//2, cy+dy-h*3//2, cx+dx+w*3//2, cy+dy+h*3//2),fill=(255,255,255))
        d.rectangle((cx-1,cy-1,cx+1,cy+1),fill=(255,80,70))
    return im.convert('RGB')

def case():
    im=bg().convert('RGBA'); dim=Image.new('RGBA',(W,H),(0,0,0,158)); im.alpha_composite(dim); d=ImageDraw.Draw(im)
    Cell=56; cols,rows=8,6; bw,bh=cols*Cell,rows*Cell; PW=bw+48+420; PH=max(bh+48+150,560)
    x0,y0=(W-PW)//2,(H-PH)//2
    d.rectangle((x0,y0,x0+PW,y0+PH),fill=BRASS); d.rectangle((x0+3,y0+3,x0+PW-3,y0+PH-3),fill=LEATHER)
    for (ax,ay) in ((0,0),(1,0),(0,1),(1,1)):
        bx=x0+3+(6 if ax==0 else PW-6-6-26); by=y0+3+(6 if ay==0 else PH-6-6-3)
        d.rectangle((bx,by,bx+26,by+3),fill=BRASS)
        vx=x0+3+(6 if ax==0 else PW-6-6-3); vy=y0+3+(6 if ay==0 else PH-6-6-26)
        d.rectangle((vx,vy,vx+3,vy+26),fill=BRASS)
    tx=x0
    for i,t in enumerate(['CASE','CHARACTER','QUESTS','MAP']):
        w=36+len(t)*13; on=i==0
        d.rectangle((tx,y0-30,tx+w,y0),fill=BRASS if on else BRASSD); d.rectangle((tx+2,y0-28,tx+w-2,y0),fill=(41,32,22) if on else (15,13,10))
        d.text((tx+w/2,y0-14),t,font=F(TITLE,13),fill=BONE if on else DIM,anchor='mm'); tx+=w+4
    text(d,(x0+21,y0+14),'TOURIST FRAME',F(TITLE,20),BONE,'la')
    cloth=(94,86,62); bx,by=x0+21,y0+51
    d.rectangle((bx,by,bx+bw+12,by+bh+12),fill=tuple(int(c*.55) for c in cloth)); d.rectangle((bx+6,by+6,bx+6+bw,by+6+bh),fill=cloth)
    for yy in range(rows):
        for xx in range(cols): d.rectangle((bx+6+xx*Cell+2,by+6+yy*Cell+2,bx+6+xx*Cell+Cell-3,by+6+yy*Cell+Cell-3),fill=tuple(int(c*.82) for c in cloth))
    items=[('VK74',0,0,5,2),('PSh18Pchela',0,2,2,2),('CorvanWasp9',2,2,3,2),('M91Trekhlineyka',0,4,6,2)]
    for k,gx,gy,w,h in items:
        wi=wpn(k) if os.path.exists(R+'Assets/Resources/Sprites/Weapons/weapon_%s_hd.png'%k) else wpn()
        f=fit(wi,w*Cell-8,h*Cell-8); im.alpha_composite(f,(bx+6+gx*Cell+4+(w*Cell-8-f.width)//2,by+6+gy*Cell+4+(h*Cell-8-f.height)//2))
    for gx,gy,lab in ((5,2,'MEDKIT'),(6,2,'STIM'),(7,2,'x3')):
        d.text((bx+6+gx*Cell+28,by+6+gy*Cell+28),lab,font=F(BODY,12),fill=BONE,anchor='mm')
    d.rectangle((bx+bw+12-96-100,y0+12,bx+bw+12-96,y0+40),fill=BRASS); d.rectangle((bx+bw+12-98-100,y0+14,bx+bw+12-98,y0+38),fill=(26,21,15))
    d.text((bx+bw+12-98-50,y0+27),'SORT',font=F(TITLE,13),fill=BONE,anchor='mm')
    rx=x0+bw+48+4
    text(d,(rx,y0+12),'EQUIPPED',F(TITLE,16),BONE,'la')
    for i,(sn,it) in enumerate((('WEAPON','VK-74'),('SECOND','Halden .45 Service'),('ARMOR','Scrap-Plated Vest'),('BACKPACK','Tourist Frame'))):
        yy=y0+46+i*72; d.rectangle((rx,yy,rx+150,yy+64),fill=(8,8,8)); text(d,(rx+160,yy+6),sn,F(TITLE,12),DIM,'la',outline=False); text(d,(rx+160,yy+28),it,F(BODY,15),BONE,'la',outline=False)
    d.multiline_text((rx,y0+346),'VK-74\n5.4 kg    5x2\nWorking\n\nassault rifle · tier 2\n5.45x39mm · magazine 30',font=F(BODY,15),fill=BONE,spacing=3)
    d.multiline_text((rx,y0+PH-84),'DRAG to move, onto a slot to equip    R turn\nRIGHT-CLICK menu    X drop    I close',font=F(TITLE,11),fill=DIM,spacing=4)
    # context menu
    mx,my=bx+6+60,by+6+60
    d.rectangle((mx,my,mx+170,my+12+3*34),fill=BRASS); d.rectangle((mx+2,my+2,mx+168,my+10+3*34),fill=LEATHER)
    for i,l in enumerate(('EQUIP','SECOND','DROP')):
        yy=my+6+i*34; d.rectangle((mx+6,yy,mx+164,yy+30),fill=(128,102,58)); d.rectangle((mx+8,yy+2,mx+162,yy+28),fill=(26,22,18))
        d.text((mx+85,yy+15),l,font=F(TITLE,14),fill=BONE,anchor='mm')
    return im.convert('RGB')

def merchant():
    im=bg().convert('RGBA'); dim=Image.new('RGBA',(W,H),(0,0,0,168)); im.alpha_composite(dim); d=ImageDraw.Draw(im)
    PW,PH=1500,840; x0,y0=(W-PW)//2,(H-PH)//2
    d.rectangle((x0,y0,x0+PW,y0+PH),fill=BRASS); d.rectangle((x0+3,y0+3,x0+PW-3,y0+PH-3),fill=LEATHER)
    bx,by=x0+3,y0+3
    text(d,(bx+32,by+22),'BONFIRE VILLAGE',F(TITLE,28),BONE,'la',outline=False); d.rectangle((bx+32,by+66,bx+182,by+68),fill=GOLD); text(d,(bx+32,by+74),'Tier 1',F(TITLE,12),DIM,'la',outline=False)
    text(d,(bx+PW-6-32,by+20),'1250',F(BODY,42),GOLD,'ra',outline=False); text(d,(bx+PW-6-32,by+70),'RU',F(TITLE,12),DIM,'ra',outline=False)
    for i,t in enumerate(['BUY','SELL','REPAIR','UPGRADE']):
        yy=by+120+i*62; on=i==0
        d.rectangle((bx+24,yy,bx+224,yy+56),fill=BRASS if on else BRASSD); d.rectangle((bx+26,yy+2,bx+222,yy+54),fill=(41,32,22) if on else (10,9,8))
        d.text((bx+124,yy+28),t,font=F(TITLE,16),fill=BONE if on else DIM,anchor='mm')
    d.multiline_text((bx+24,by+PH-6-40-36),'Q / E  TAB\nUP / DOWN  PICK\nENTER  DO    ESC  LEAVE',font=F(TITLE,10),fill=DIM,spacing=4)
    lx,ly,lw=bx+252,by+120,760
    d.rectangle((lx-6,ly-6,lx+lw+6,ly+13*46+6),fill=INK)
    rows=[('Basic Medkit','150 RU',True),('Bandage','40 RU',True),('Basic Stim','180 RU',True),('5.45x39mm Standard x30','90 RU',True),('5.45x39mm Armor-Piercing x30','240 RU',True),('9x18mm Standard x24','60 RU',True),('Splint','60 RU',True),('MRE','70 RU',True),('Small Canvas Sack (+10kg)','300 RU',True),('Frag Grenade','250 RU',True),('Smoke Grenade','150 RU',True),('Flashbang','200 RU',False),('Field Repair Kit','350 RU',False)]
    sel=3
    for i,(n,p,aff) in enumerate(rows):
        yy=ly+i*46; d.rectangle((lx,yy,lx+lw,yy+42),fill=(140,110,61,140) if i==sel else (30,28,26))
        if i==sel: d.rectangle((lx,yy,lx+lw,yy+42),fill=(88,70,40))
        d.text((lx+14,yy+10),n,font=F(BODY,19),fill=BONE if aff else DIM)
        d.text((lx+lw-14,yy+10),p,font=F(BODY,19),fill=GOLD if aff else (230,76,64),anchor='ra')
    d.text((lx+lw,ly+13*46+12),'< 1 / 2 >   (wheel)',font=F(TITLE,11),fill=DIM,anchor='ra')
    px,pw=bx+1042,PW-6-32-1042
    d.rectangle((px,by+126-6+0,px+pw,by+126+PH-6-120-36),fill=INK)
    d.rectangle((px+20,by+140,px+pw-20,by+310),fill=(31,26,20))
    ic=Image.open(R+'Assets/Resources/Sprites/Ammo/'+sorted(os.listdir(R+'Assets/Resources/Sprites/Ammo'))[0]).convert('RGBA') if os.listdir(R+'Assets/Resources/Sprites/Ammo') else None
    if ic is not None:
        f=fit(ic,pw-80,150); im.alpha_composite(f,(px+20+(pw-40-f.width)//2,by+140+(170-f.height)//2))
    d.multiline_text((px+20,by+330),'5.45X39MM STANDARD X30',font=F(TITLE,20),fill=BONE)
    d.multiline_text((px+20,by+396),'Standard rounds, 30 to a box',font=F(BODY,17),fill=DIM)
    d.text((px+20,by+PH-6-170),'0.5 kg   1x1',font=F(TITLE,11),fill=DIM)
    d.rectangle((px+20,by+PH-6-120-0,px+pw-20,by+PH-6-120+62),fill=BRASS); d.rectangle((px+22,by+PH-6-118,px+pw-22,by+PH-6-120+60),fill=(51,40,23))
    d.text((px+pw//2,by+PH-6-120+31),'PURCHASE   90 RU',font=F(TITLE,17),fill=BONE,anchor='mm')
    d.rectangle((px+20,by+PH-6-60,px+pw-20,by+PH-6-16),fill=BRASSD); d.rectangle((px+22,by+PH-6-58,px+pw-22,by+PH-6-18),fill=(15,13,10))
    d.text((px+pw//2,by+PH-6-38),'LEAVE',font=F(TITLE,14),fill=DIM,anchor='mm')
    return im.convert('RGB')

OUT=R+'maps/renders/ui/'; os.makedirs(OUT,exist_ok=True)
for n,fn in (('hud_calm',lambda:hud('calm')),('hud_combat_danger',lambda:hud('combat')),('case',case),('merchant',merchant)):
    fn().save(OUT+'%s.png'%n); print(n)
