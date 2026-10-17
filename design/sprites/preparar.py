from PIL import Image, ImageDraw
from pathlib import Path
import json, math
P=Path(__file__).resolve().parent
N=Image.Resampling.NEAREST
CLEAR=(0,0,0,0)
def rgba(h):return tuple(bytes.fromhex(h))+(255,)
INK=rgba('101B2D');MAR=rgba('123D49');ORANGE=rgba('F47724');RUST=rgba('B94724');SAND=rgba('E6BD85');LIGHT=rgba('FFF0CE');TURQ=rgba('42B5AC');GREEN=rgba('64834B');VIOLET=rgba('7050AD')
COMMON=['101B2D','123D49','F47724','B94724','E6BD85','FFF0CE','42B5AC','64834B','7050AD']
PALETTES={
 'casco':COMMON[:6]+['FFAA52','E89778','A96550'],
 'concha': ['101B2D','123D49','E6BD85','FFF0CE','B48A5A','70543E','42B5AC','287F83','8ED7CB'],
 'agua': ['101B2D','123D49','42B5AC','8ED7CB','FFF0CE'],
 'baiacu':['101B2D','123D49','64834B','94A653','E6BD85','FFF0CE','F47724'],
 'npc':COMMON+['FFAA52','B48A5A','70543E','A995C2','E89778','8ED7CB'],
 'wood':['101B2D','123D49','42B5AC','64834B','B94724','E6BD85','FFF0CE','8D5738','B87947'],
 'coral1':['101B2D','7050AD','B561A0','E897B5','FFF0CE'],
 'coral2':['101B2D','B94724','F47724','FFAA52','FFF0CE'],
 'green':['101B2D','123D49','64834B','42B5AC','94A653'],
}

def source(name):
 im=Image.open(P/'originais'/f'{name}.png').convert('RGBA')
 im.putalpha(im.getchannel('A').point(lambda a:255 if a>=160 else 0))
 im=im.crop(im.getchannel('A').getbbox())
 return im

def quant(im,pal):
 a=im.getchannel('A').point(lambda v:255 if v>=128 else 0)
 rgb=[tuple(bytes.fromhex(c)) for c in pal]
 pi=Image.new('P',(1,1));pi.putpalette([v for c in rgb for v in c]+list(rgb[-1])*(256-len(rgb)))
 out=im.convert('RGB').quantize(palette=pi,dither=Image.Dither.NONE).convert('RGBA');out.putalpha(a)
 out.putdata([p if p[3] else CLEAR for p in out.getdata()]);return out

def outline(im):
 # Enforce a one-native-pixel contour inside existing silhouette.
 out=im.copy();a=im.getchannel('A');px=out.load();w,h=out.size
 for y in range(h):
  for x in range(w):
   if not a.getpixel((x,y)):continue
   if max(px[x,y][:3])<60 or any(nx<0 or nx>=w or ny<0 or ny>=h or not a.getpixel((nx,ny)) for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):px[x,y]=INK
 return out

def fit(name,canvas,limit=None,bottom=False,pal='npc',stretch=False,xy=None):
 im=source(name);limit=limit or canvas
 if stretch:sz=limit
 else:
  s=min(limit[0]/im.width,limit[1]/im.height);sz=(max(1,round(im.width*s)),max(1,round(im.height*s)))
 im=outline(quant(im.resize(sz,N),PALETTES[pal]))
 out=Image.new('RGBA',canvas)
 if xy is None:xy=((canvas[0]-sz[0])//2,canvas[1]-sz[1] if bottom else (canvas[1]-sz[1])//2)
 out.paste(im,xy);return out

if __name__=='__main__':
 im=fit('casco-base',(32,32),(28,25),pal='casco',stretch=True,xy=(1,5))
 im.save(P/'referencia/casco-grade.png')
 im.resize((512,512),N).save(P/'referencia/casco-grade-ampliado.png')

# All animation edits below happen directly on the final integer pixel grid.
SPRITES={}
def save(name,im):
 im.putdata([p if p[3] else CLEAR for p in im.getdata()]);im.save(P/f'{name}.png');SPRITES[name]=im

def line(d,points,color,width=1):d.line(points,fill=color,width=width)

def eye(d,x,y):
 # Shared 5x8 eyes preserve identity and rightward gaze at native scale.
 rows=['.###.','#oo##','#www#','#wwk#','#wwk#','#www#','#oo##','.###.']
 colors={'#':INK,'o':ORANGE,'w':LIGHT,'k':INK}
 for yy,row in enumerate(rows):
  for xx,ch in enumerate(row):
   if ch!='.':d.point((x+xx,y+yy),fill=colors[ch])

base=fit('casco-base',(32,32),(28,25),pal='casco',stretch=True,xy=(1,5))
d=ImageDraw.Draw(base)
# Clear generated eyes; reconstruct them without shrinking cream/pupil clusters.
d.rectangle((14,3,29,15),fill=CLEAR)
line(d,[(18,19),(18,12)],INK,3);line(d,[(18,18),(18,11)],ORANGE)
line(d,[(22,19),(25,12)],INK,3);line(d,[(22,18),(25,11)],ORANGE)
eye(d,16,4);eye(d,23,4)
# Restrained two-tone face and smile, plus claw separations.
d.point((23,18),INK);d.point((24,18),INK);d.point((24,17),SAND)
line(d,[(18,24),(20,26),(21,28)],INK)
line(d,[(24,24),(26,26)],INK)
line(d,[(14,18),(16,17),(19,17)],rgba('FFAA52'))
# The abdomen is soft salmon flesh, never sand shell material.
px=base.load()
for y in range(14,28):
 for x in range(1,11):
  if px[x,y]==SAND:px[x,y]=rgba('E89778')
  elif px[x,y]==LIGHT:px[x,y]=SAND
line(d,[(7,17),(4,18),(3,21),(5,23),(7,22),(6,20)],rgba('A96550'))
# Shared torso/head crop; legs use separate native masks.
d.rectangle((0,26,16,31),fill=CLEAR)
upper=base.copy()
front=upper.crop((23,19,30,30))
ImageDraw.Draw(upper).rectangle((23,19,31,31),fill=CLEAR)

frames=['casco-parado-1','casco-parado-2']+[f'casco-anda-{i}' for i in range(1,5)]+['casco-pulo','casco-queda','casco-ataque-1','casco-ataque-2']
bobs={'casco-parado-2':1,'casco-anda-2':1,'casco-anda-4':1}
for idx,name in enumerate(frames):
 bob=bobs.get(name,0);im=Image.new('RGBA',(32,32));dd=ImageDraw.Draw(im)
 gait=idx%4 if 'anda' in name else 0
 feet=[(7,10,15),(6,11,16),(8,12,15),(7,10,17)][gait]
 if name=='casco-pulo':feet=(9,12,16)
 if name=='casco-queda':feet=(5,11,17)
 for root,foot in zip((9,12,16),feet):
  pts=[(root,24-bob),(foot+1,28),(foot,31)]
  line(dd,pts,INK,3);line(dd,[(root,25-bob),(foot+1,28),(foot,30)],RUST)
  dd.point((foot+1,28),ORANGE)
 im.alpha_composite(upper,(0,-bob))
 if 'ataque' not in name:
  im.alpha_composite(front,(23,19-bob))
 else:
  dd=ImageDraw.Draw(im)
  line(dd,[(22,21),(25,22),(28,21)],INK,4);line(dd,[(22,21),(27,21)],ORANGE,2)
  if name.endswith('1'):
   poly=[(26,18),(29,17),(31,18),(31,20),(29,21),(31,23),(31,25),(28,25),(25,23),(25,20)]
   dd.polygon(poly,fill=ORANGE,outline=INK)
   line(dd,[(28,19),(30,19)],rgba('FFAA52'));line(dd,[(27,23),(29,24)],RUST)
   line(dd,[(29,21),(31,21)],INK)
  else:
   poly=[(25,19),(28,16),(31,15),(31,17),(28,19),(28,21),(31,24),(31,27),(28,26),(25,23)]
   dd.polygon(poly,fill=ORANGE,outline=INK)
   line(dd,[(28,17),(30,16)],rgba('FFAA52'));line(dd,[(27,23),(30,25)],RUST)
 save(name,im)

for name,src,sz,xy in [
 ('concha-comum','concha-comum-corrigida',(18,19),(0,8)),
 ('concha-tampinha','concha-tampinha',(18,15),(0,12)),
 ('concha-espiral','concha-espiral',(18,24),(0,3))]:
 im=fit(src,(32,32),sz,stretch=True,xy=xy,pal='concha')
 # The lower-right shell opening is a cutaway so overlaid shell cannot hide torso.
 dd=ImageDraw.Draw(im);dd.polygon([(17,21),(17,31),(10,31),(11,26),(13,23)],fill=CLEAR)
 save(name,im)

# Floating enemies share silhouette anchors; wave only tentacles/tail pixels.
jelly=fit('agua-viva-base',(32,32),(24,28),pal='agua')
for phase in (1,2):
 im=jelly.copy()
 if phase==2:
  for y in range(15,31):
   row=jelly.crop((0,y,32,y+1));ImageDraw.Draw(im).line((0,y,31,y),fill=CLEAR)
   shift=1 if y<22 else -1;im.paste(row,(shift,y))
 # Only inner bell pixels carry partial alpha; contour/tentacles remain binary.
 px=im.load()
 for y in range(2,14):
  for x in range(32):
   c=px[x,y]
   if c[3] and c[:3] not in (INK[:3],LIGHT[:3]):px[x,y]=c[:3]+(192,)
 save(f'agua-viva-{phase}',im)
 charged=im.copy();dd=ImageDraw.Draw(charged)
 yellow=rgba('FFE36B')
 for pts in ([[(3,9),(1,12),(4,12),(2,16)],[(28,4),(30,7),(28,9),(30,11)],[(13,18),(11,22),(14,22),(12,27)]] if phase==1 else [[(2,5),(4,8),(1,10)],[(28,14),(30,17),(27,19)],[(20,17),(18,21),(21,22),(19,28)]]):
  line(dd,pts,INK,3);line(dd,pts,yellow)
 save(f'agua-viva-carregada-{phase}',charged)

fish=fit('baiacu-base',(32,32),(27,22),pal='baiacu',stretch=True,xy=(2,5))
for phase in (1,2):
 im=fish.copy()
 if phase==2:
  im.paste(CLEAR,(1,10,7,23));tail=fish.crop((2,10,7,22));im.alpha_composite(tail,(2,9))
  dd=ImageDraw.Draw(im);line(dd,[(13,18),(15,17),(16,19)],GREEN)
 save(f'baiacu-{phase}',im)
# Inflated variant retains the generated identity with longer radial spikes.
save('baiacu-inflado',fit('baiacu-inflado',(32,32),(31,31),pal='baiacu',stretch=True))
sp=Image.new('RGBA',(16,16));dd=ImageDraw.Draw(sp);dd.polygon([(2,4),(14,7),(14,8),(2,11),(1,8)],fill=SAND,outline=INK);line(dd,[(3,5),(11,7)],LIGHT);save('espinho',sp)

for name,size,lim in [('vovo-buzia',48,(43,47)),('ostracia',48,(44,44)),('dona-lagosta',48,(46,46)),('bolota',32,(30,29))]:
 save(name,fit(name,(size,size),lim,bottom=True,pal='npc'))

pearl=fit('perola-base',(16,16),(11,11),pal='npc')
for phase in (1,2):
 im=pearl.copy();dd=ImageDraw.Draw(im)
 sx,sy=(11,3) if phase==1 else (12,2)
 line(dd,[(sx-2,sy),(sx+2,sy)],LIGHT);line(dd,[(sx,sy-2),(sx,sy+2)],LIGHT)
 save(f'perola-{phase}',im)
for phase in (1,2):
 lim=(26,26) if phase==1 else (28,25)
 im=fit('bolha-base',(32,32),lim,pal='agua',stretch=True)
 save(f'bolha-{phase}',im)
save('alga',fit('alga',(16,16),(10,14),pal='green',stretch=True))

closed=fit('bau-fechado',(32,32),(30,24),bottom=True,pal='wood',stretch=True)
opened=fit('bau-aberto',(32,32),(30,30),bottom=True,pal='wood',stretch=True)
# Fixed lower chest, only lid/inside differ between states.
opened.paste(closed.crop((0,20,32,32)),(0,20))
save('bau-fechado',closed);save('bau-aberto',opened)
for name,canvas,lim,pal in [('saida',(32,64),(30,63),'wood'),('placa',(32,32),(27,31),'wood'),('coral-1',(32,32),(27,29),'coral1'),('coral-2',(32,32),(28,30),'coral2')]:
 save(name,fit(name,canvas,lim,bottom=True,pal=pal))
algae=fit('alga-decor-base',(32,32),(25,31),bottom=True,pal='green')
save('alga-decor-1',algae)
im=algae.copy()
for y in range(25):
 row=algae.crop((0,y,32,y+1));ImageDraw.Draw(im).line((0,y,31,y),fill=CLEAR);im.paste(row,(-1 if y<12 else 1,y))
save('alga-decor-2',im)
save('casa-garrafa',fit('casa-garrafa-corrigida',(64,64),(40,63),bottom=True,pal='wood',stretch=True))

# Native preview: first ten columns = the ten animation frames.
# Four rows: body, common shell composite, cap composite, spiral composite.
# All remaining sprites follow, every sprite enlarged exactly 4x.
CW=72;CH=72
native=Image.new('RGBA',(CW*10,CH*8),(33,49,62,255));nd=ImageDraw.Draw(native)
for gy in range(0,native.height,8):
 for gx in range(0,native.width,8):
  if ((gx//8)+(gy//8))%2==0:nd.rectangle((gx,gy,gx+7,gy+7),fill=(40,58,70,255))
layout=[]
for row,shell in enumerate([None,'concha-comum','concha-tampinha','concha-espiral']):
 for col,name in enumerate(frames):
  im=SPRITES[name].copy()
  if shell:im.alpha_composite(SPRITES[shell])
  x=col*CW+20;y=row*CH+24;native.alpha_composite(im,(x,y));layout.append({'name':name,'shell':shell,'xy_native':[x,y]})
others=[n for n in SPRITES if n not in frames]
for i,name in enumerate(others):
 im=SPRITES[name];col=i%10;row=4+i//10
 x=col*CW+(CW-im.width)//2;y=row*CH+CH-im.height-4
 native.alpha_composite(im,(x,y));layout.append({'name':name,'xy_native':[x,y]})
# Remove unused final row only; maintain exact 4x per pixel.
native=native.crop((0,0,native.width,CH*7))
native.resize((native.width*4,native.height*4),N).convert('RGB').save(P/'previa.png')
(P/'previa-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
# Compact inspection versions for review, not game assets.
for label,ims in [('casco',[SPRITES[n] for n in frames]),('outros',[SPRITES[n] for n in others])]:
 out=Image.new('RGBA',(640,((len(ims)+9)//10)*64),(39,59,71,255))
 for i,im in enumerate(ims):out.alpha_composite(im,((i%10)*64+(64-im.width)//2,(i//10)*64+64-im.height))
 out.resize((out.width*3,out.height*3),N).save(P/'referencia'/f'{label}-conferencia.png')

manifest=json.loads((P/'referencia/dimensoes.json').read_text())
report={}
for name,im in SPRITES.items():
 assert list(im.size)==manifest[name+'.png'],name
 aa=set(im.getchannel('A').getdata())
 if not name.startswith('agua-viva'):assert aa<={0,255},(name,aa)
 else:
  assert aa<={0,192,255}
  assert all(p[3] in (0,255) for p in im.crop((0,14,32,32)).getdata())
 anchored=name.startswith('casco-') or name in ['vovo-buzia','ostracia','dona-lagosta','bolota','bau-fechado','bau-aberto','saida','placa','coral-1','coral-2','alga-decor-1','alga-decor-2','casa-garrafa']
 if anchored:assert im.getchannel('A').crop((0,im.height-1,im.width,im.height)).getbbox() is not None,name
 assert all(p[:3]==(0,0,0) for p in im.getdata() if p[3]==0)
 report[name]={'size':list(im.size),'alpha':sorted(aa),'colors':len(im.getcolors(65536)),'bbox':im.getchannel('A').getbbox(),'base_last_row':anchored}
assert len(SPRITES)==39,len(SPRITES)
# Head and torso registration check, compensating only specified 1px bob.
for name in frames:
 b=bobs.get(name,0)
 assert SPRITES[name].crop((16,4-b,28,17-b)).tobytes()==SPRITES['casco-parado-1'].crop((16,4,28,17)).tobytes(),name
for name in frames:
 b=bobs.get(name,0)
 assert SPRITES[name].crop((12,17-b,22,23-b)).tobytes()==SPRITES['casco-parado-1'].crop((12,17,22,23)).tobytes(),name
for name in ('casco-ataque-1','casco-ataque-2'):assert SPRITES[name].getchannel('A').crop((31,0,32,32)).getbbox()
for shell in ('concha-comum','concha-tampinha','concha-espiral'):
 # Shell stays wholly left of the face; all 30 composites fit the canvas.
 assert SPRITES[shell].getchannel('A').crop((18,0,32,32)).getbbox() is None
 for name in frames:
  composite=SPRITES[name].copy();composite.alpha_composite(SPRITES[shell]);assert composite.size==(32,32)
(P/'validacao.json').write_text(json.dumps({'sprites':report,'same_head_registration':True,'same_torso_registration':True,'shell_combinations':30,'attack_right_edge':True},indent=2)+'\n')
print('39 sprites; 30 shell composites; sizes, alpha, feet, head registration and attack edges verified.')
