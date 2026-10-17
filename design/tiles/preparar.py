from PIL import Image, ImageDraw
from pathlib import Path
import json,re,shutil
ROOT=Path(__file__).resolve().parent.parent
N=Image.Resampling.NEAREST
records=json.loads((ROOT/'tiles/prompts.json').read_text())['assets']
raw={}
for r in records:
 p=ROOT/r['folder'];(p/'originais').mkdir(exist_ok=True)
 src=Path(re.search(r'as (/[^\n]+\.png) by default',r['output_hint']).group(1))
 dest=p/'originais'/f"{r['name']}.png"
 shutil.copy2(src,dest);raw[r['name']]=Image.open(dest).convert('RGBA')

def quant(im,colors):
 a=im.getchannel('A').point(lambda v:255 if v>=128 else 0)
 if isinstance(colors,list):
  pal=Image.new('P',(1,1));rgb=[tuple(bytes.fromhex(c)) for c in colors]
  pal.putpalette([v for c in rgb for v in c]+list(rgb[-1])*(256-len(rgb)))
  im=im.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA')
 else:im=im.convert('RGB').quantize(colors=colors,dither=Image.Dither.NONE).convert('RGBA')
 im.putalpha(a)
 im.putdata([p if p[3] else (0,0,0,0) for p in im.getdata()])
 return im

def seamless(im,vertical=False):
 # Reconcile a narrow wrap neighborhood without changing tile interior.
 px=im.load();w,h=im.size
 for y in range(h):
  for d in range(3):
   a,b=px[d,y],px[w-1-d,y];t=(3-d)/6
   aa=tuple(round(a[c]*(1-t)+b[c]*t) for c in range(4))
   bb=tuple(round(b[c]*(1-t)+a[c]*t) for c in range(4))
   px[d,y],px[w-1-d,y]=aa,bb
 if vertical:
  for x in range(w):
   for d in range(3):
    a,b=px[x,d],px[x,h-1-d];t=(3-d)/6
    px[x,d]=tuple(round(a[c]*(1-t)+b[c]*t) for c in range(4))
    px[x,h-1-d]=tuple(round(b[c]*(1-t)+a[c]*t) for c in range(4))
 return im

palettes={
'recife':['17383D','24464B','2E5559','3D6B6E','508589'],
'vila':['A57848','C2955E','D9A86C','E6BD85'],
}
tiles={}
for zone in ('recife','vila'):
 pal=palettes[zone]
 mid=quant(seamless(raw[zone+'-meio'].resize((32,32),N),True),pal)
 tiles[zone+'-meio']=mid
 # Reuse the interior beneath generated decorative surface strip.
 top=mid.copy();src=raw[zone+'-topo'];w,h=src.size
 if zone=='recife':
  # Discard empty generated headroom; preserve sand and coral details.
  band=src.crop((0,round(h*.14),w,round(h*.33))).resize((32,7),N)
  p=pal+['B98C54','E6BD85','FFF0CE','C96843','64834B']
 else:
  band=src.crop((0,0,w,round(h*.27))).resize((32,7),N)
  p=pal+['FFF0CE','EBC99A','B98B80','E0AB96']
 band.putalpha(255);top.paste(band,(0,0))
 top=quant(seamless(top),p)
 # Copy exactly matching bottom row from original interior, including corners.
 top.paste(mid.crop((0,31,32,32)),(0,31))
 tiles[zone+'-topo']=top
 src=raw[zone+'-plataforma'];a=src.getchannel('A').point(lambda v:255 if v>=128 else 0)
 box=a.getbbox();src.putalpha(a)
 strip=src.crop((0,box[1],src.width,box[3])).resize((32,11),N)
 # Solid walkable upper line, irregular broken underside stays within 11 px.
 px=strip.load()
 for x in range(32):
  for y in range(2):
   if not px[x,y][3]:px[x,y]=((156,182,186,255) if zone=='recife' else (217,168,108,255))
 plat=Image.new('RGBA',(32,32));plat.paste(strip,(0,0))
 pp=['101B2D','324A59','516E7B','6B8993','9CB6BA'] if zone=='recife' else ['493429','67452F','875434','A56B40','C88D51','D9A86C','A4A6A0']
 plat=quant(seamless(plat),pp)
 tiles[zone+'-plataforma']=plat
for name,im in tiles.items():im.save(ROOT/'tiles'/f'{name}.png')

# Portraits: crop/fit, nearest only, limited palette, exact navy outline.
portraits={}
for r in records:
 if r['folder']!='retratos':continue
 name=r['name'];im=raw[name];im.putalpha(im.getchannel('A').point(lambda a:255 if a>=128 else 0))
 box=im.getchannel('A').getbbox();im=im.crop(box)
 if name in ('vovo-buzia','casco'):im=im.crop((0,0,im.width,round(im.height*.83)))
 scale=min(120/im.width,120/im.height)
 im=im.resize((round(im.width*scale),round(im.height*scale)),N)
 out=Image.new('RGBA',(128,128));out.paste(im,((128-im.width)//2,(128-im.height)//2))
 out=quant(out,24);px=out.load();mask=out.getchannel('A')
 for y in range(128):
  for x in range(128):
   c=px[x,y]
   if c[3] and (max(c[:3])<65 or any(not mask.getpixel((nx,ny)) for nx,ny in [(max(0,x-1),y),(min(127,x+1),y),(x,max(0,y-1)),(x,min(127,y+1))])):
    px[x,y]=(16,27,45,255)
 out.save(ROOT/'retratos'/f'{name}.png');portraits[name]=out

# 6x4 repeat checks for each tile and surface/interior assembly.
mos=ROOT/'tiles/mosaicos';mos.mkdir(exist_ok=True)
for name,tile in tiles.items():
 im=Image.new('RGBA',(192,128))
 for y in range(4):
  for x in range(6):im.paste(tile,(x*32,y*32))
 im.resize((768,512),N).save(mos/f'{name}-6x4.png')
for z in ('recife','vila'):
 im=Image.new('RGBA',(192,128))
 for y in range(4):
  for x in range(6):im.paste(tiles[z+('-topo' if y==0 else '-meio')],(x*32,y*32))
 im.resize((768,512),N).save(mos/f'{z}-encaixe-6x4.png')

preview=Image.new('RGB',(1280,360))
for i,z in enumerate(('recife','vila')):
 im=Image.open(ROOT/'cenarios'/f'fundo-{z}-640.png').convert('RGBA')
 im.alpha_composite(Image.open(ROOT/'cenarios'/f'silhuetas-{z}-640.png'))
 for x in range(20):
  top=264 if x<8 or x>13 else 296
  for y in range(top,360,32):im.alpha_composite(tiles[z+('-topo' if y==top else '-meio')],(x*32,y))
 for x,y in [(8,200),(9,200),(10,200),(13,144),(14,144),(15,144)]:im.alpha_composite(tiles[z+'-plataforma'],(x*32,y))
 preview.paste(im,(i*640,0))
preview.save(ROOT/'tiles/previa.png')
pv=Image.new('RGB',(640,256),(230,222,206))
for i,im in enumerate(portraits.values()):
 pv.paste(im,(i*128,0),im)
 bg=Image.new('RGBA',(128,128),(18,61,73,255));bg.alpha_composite(im);pv.paste(bg,(i*128,128))
pv.resize((1280,512),N).save(ROOT/'retratos/previa.png')

report={}
for name,im in {**tiles,**portraits}.items():
 al=set(im.getchannel('A').getdata());assert al<={0,255}
 assert im.size==((32,32) if name in tiles else (128,128))
 if name in tiles:
  assert list(im.crop((0,0,1,32)).getdata())==list(im.crop((31,0,32,32)).getdata())
  if name.endswith('meio'):assert list(im.crop((0,0,32,1)).getdata())==list(im.crop((0,31,32,32)).getdata())
  if name.endswith('plataforma'):assert im.getchannel('A').crop((0,11,32,32)).getbbox() is None
 report[name]={'size':im.size,'alpha':sorted(al),'colors':len(im.getcolors(65536))}
for z in ('recife','vila'):
 assert list(tiles[z+'-topo'].crop((0,31,32,32)).getdata())==list(tiles[z+'-meio'].crop((0,0,32,1)).getdata())
(ROOT/'tiles/validacao.json').write_text(json.dumps(report,indent=2)+'\n')
(ROOT/'retratos/prompts.json').write_text(json.dumps({'tool':'image_gen integrado','assets':[r for r in records if r['folder']=='retratos']},indent=2)+'\n')
print(json.dumps(report,indent=2))
