"""Tiles v0.2: NEAREST, paleta fixa e encaixes exatos entre blocos."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib
P=Path(__file__).resolve().parent
OUT=P.parents[2]/'MareBaixa/Assets/Art/Blocos'
N=Image.Resampling.NEAREST
PALETTES={
 'cidade':['101B2D','273B3C','3E5050','60716B','88958A','AAB4A0','304E38','64834B','94A653','70543E','A27952','C2C6B2'],
 'fossa':['090E1E','101B2D','182238','25263F','39315E','51417D','7050AD','A078CC','C58ADB','354F66','548B98','8ED7CB']}
def quant(im,colors):
 p=Image.new('P',(1,1));p.putpalette([v for c in colors for v in bytes.fromhex(c)]+list(bytes.fromhex(colors[0]))*(256-len(colors)))
 return im.convert('RGB').quantize(palette=p,dither=Image.Dither.NONE).convert('RGBA')
def main():
 OUT.mkdir(parents=True,exist_ok=True);images={};report=[]
 for zone in PALETTES:
  for kind in ['meio','topo','plataforma']:
   name=f'{zone}-{kind}';src=P/'originais'/f'{name}.png'
   if not src.exists():continue
   raw=Image.open(src).convert('RGBA')
   if kind=='plataforma':
    a=raw.getchannel('A').point(lambda x:255 if x>=160 else 0);raw.putalpha(a);raw=raw.crop(a.getbbox())
    raw=raw.resize((32,11),N);im=quant(raw,PALETTES[zone]);im.putalpha(raw.getchannel('A'))
    tile=Image.new('RGBA',(32,32));tile.paste(im,(0,0));im=tile
   else:im=quant(raw.resize((16,16),N).resize((32,32),N),PALETTES[zone])
   if kind=='topo':
    for x in range(32):
     im.putpixel((x,0),tuple(bytes.fromhex(PALETTES[zone][8 if zone=='cidade' else 6]))+(255,))
     im.putpixel((x,1),tuple(bytes.fromhex(PALETTES[zone][7 if zone=='cidade' else 4]))+(255,))
   # Mesmo perfil nas duas bordas; evita cortes de rachaduras no encontro.
   for y in range(32):im.putpixel((31,y),im.getpixel((0,y)))
   if kind=='meio':
    for x in range(32):im.putpixel((x,31),im.getpixel((x,0)))
   if kind=='topo' and f'{zone}-meio' in images:
    for x in range(32):im.putpixel((x,31),images[f'{zone}-meio'].getpixel((x,0)))
   im.putdata([px if px[3] else (0,0,0,0) for px in im.getdata()]);images[name]=im
   im.save(P/f'{name}.png');im.save(OUT/f'{name}.png')
   lr=all(im.getpixel((0,y))==im.getpixel((31,y)) for y in range(32));assert lr
   tb=all(im.getpixel((x,0))==im.getpixel((x,31)) for x in range(32)) if kind=='meio' else None
   if kind=='meio':assert tb
   if kind=='plataforma':assert im.crop((0,11,32,32)).getbbox() is None
   report.append({'arquivo':name+'.png','tamanho':[32,32],'cores':len(set(im.getdata())),'alpha':sorted(set(im.getchannel('A').getdata())),'borda_horizontal':lr,'borda_vertical':tb,'sha256':hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()})
 preview=Image.new('RGB',(1280,480),'#182238');d=ImageDraw.Draw(preview)
 for zi,zone in enumerate(PALETTES):
  for x in range(10):
   for y in range(3):
    key=f'{zone}-'+('topo' if y==0 else 'meio')
    if key in images:
     im=images[key].resize((64,64),N);preview.paste(im,(zi*640+x*64,140+y*64),im)
   key=f'{zone}-plataforma'
   if key in images:
    im=images[key].resize((64,64),N);preview.paste(im,(zi*640+x*64,50),im)
  d.text((zi*640+20,15),zone,fill='white')
 preview.save(P/'previa.png')
 (P/'validacao.json').write_text(json.dumps({'completo':len(report)==6,'arquivos':report,'paletas':PALETTES,'filtro':'NEAREST'},indent=2)+'\n')
 print(f'{len(report)}/6 tiles preparados')
if __name__=='__main__':main()
