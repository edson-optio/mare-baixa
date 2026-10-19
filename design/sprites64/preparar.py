"""Amostra v0.3 etapa 1: fontes preservadas, NEAREST, paletas e registro fixos."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
P=Path(__file__).resolve().parent; ROOT=P.parents[1]
OUT=ROOT/'MareBaixa/Assets/Art/Sprites64'; N=Image.Resampling.NEAREST
CLEAR=(0,0,0,0); INK=(16,27,45,255)
CASCO=['101B2D','7B301F','B94724','D95C22','F47724','FF9438','FFB35B','FFD394','FFF0CE','E89778','C97459','A96550','E6BD85','CCA16C','B48A5A','90693F','70543E','513E32','F6DBA7','FFF8E6']
SIRI=['101B2D','192B3D','263F54','35576F','4B7189','7195A5','A6B6B2','123D49','314B4B','526359','70543E','9A4A2C','B94724','D87935','E6BD85','FFB35B','FFF0CE','B48A5A']
NAMES=['casco-parado-1','casco-parado-2']+[f'casco-anda-{i}' for i in range(1,7)]+['concha-comum','concha-espiral']+[f'siri-{i}' for i in range(1,5)]+['siri-garra']

def quant(im,colors):
 pal=Image.new('P',(1,1));pal.putpalette([v for c in colors for v in bytes.fromhex(c)]+list(bytes.fromhex(colors[0]))*(256-len(colors)))
 a=im.getchannel('A').point(lambda x:255 if x>=160 else 0)
 im=im.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA');im.putalpha(a)
 im.putdata([p if p[3] else CLEAR for p in im.getdata()]);return im

def outline(im):
 a=im.getchannel('A');out=im.copy();px=out.load()
 for y in range(im.height):
  for x in range(im.width):
   if a.getpixel((x,y)) and any(xx<0 or yy<0 or xx>=im.width or yy>=im.height or not a.getpixel((xx,yy)) for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):px[x,y]=INK
 return out

def base(name):
 raw=Image.open(P/'originais'/f'{name}.png').convert('RGBA');a=raw.getchannel('A').point(lambda x:255 if x>=160 else 0);raw.putalpha(a);raw=raw.crop(a.getbbox())
 if name.startswith('casco'):size=(56,56);xy=(2,8);colors=CASCO
 elif name=='concha-comum':size=(36,38);xy=(0,16);colors=CASCO
 elif name=='concha-espiral':size=(36,48);xy=(0,6);colors=CASCO
 else:size=(60,36);xy=(2,28);colors=SIRI
 im=outline(quant(raw.resize(size,N),colors));out=Image.new('RGBA',(64,64));out.paste(im,xy)
 if name.startswith('concha'):
  ImageDraw.Draw(out).polygon([(35,42),(35,63),(20,63),(22,52),(26,46)],fill=CLEAR)
  # As conchas são aplicadas depois do corpo: recorte protege olhos e hastes.
  body=base('casco-parado-1').getchannel('A')
  for y in range(32):
   for x in range(27,38):
    if any(body.getpixel((xx,yy)) for xx in range(max(0,x-1),min(64,x+2)) for yy in range(max(0,y-1),min(32,y+2))):out.putpixel((x,y),CLEAR)
  out=outline(out)
 else:
  box=out.getbbox()
  if box[3]<64:
   shifted=Image.new('RGBA',(64,64));shifted.paste(out,(0,64-box[3]));out=shifted
 return out

def main():
 OUT.mkdir(parents=True,exist_ok=True);images={};report=[]
 for name in NAMES:
  src=P/'originais'/f'{name}.png'
  if not src.exists():continue
  if name in ['casco-parado-1','concha-comum','concha-espiral','siri-1']:im=base(name)
  else:
   # Variantes editadas sobre o canvas fixo; sem recorte/reescala individual.
   raw=Image.open(src).convert('RGBA').resize((64,64),N)
   im=outline(quant(raw,SIRI if name.startswith('siri') else CASCO))
   # Corrige somente o contato do último pixel do pé, sem mover o tronco.
   bottom=im.getbbox()[3]
   if bottom<64:
    row=im.crop((0,bottom-1,64,bottom))
    for yy in range(bottom,64):im.paste(row,(0,yy))
  images[name]=im;im.save(P/f'{name}.png');im.save(OUT/f'{name}.png')
  colors=set(im.getdata());assert len(colors)<=25;assert {p[3] for p in colors}<={0,255}
  assert all(p==CLEAR for p in colors if not p[3])
  if not name.startswith('concha'):assert im.getbbox()[3]==64,(name,im.getbbox())
  report.append({'arquivo':name+'.png','tamanho':[64,64],'cores_opacas':len({p[:3] for p in colors if p[3]}),'alpha':[0,255],'bbox':im.getbbox(),'sha256':hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()})
 (P/'validacao.json').write_text(json.dumps({'etapa':1,'esperados':15,'entregues':len(report),'completo':len(report)==15,'filtro':'NEAREST','arquivos':report},indent=2)+'\n')
 for name in ['casco-parado-1','siri-1']:
  if name in images:images[name].resize((512,512),N).save(P/'referencia'/f'{name}-base512.png')
 print(f'{len(report)}/15 sprites preparados')
if __name__=='__main__':
 # A amostra foi aprovada. A entrada principal agora reproduz a campanha inteira.
 from etapa2.preparar import GROUPS, prepare, publish
 for group in GROUPS:
  prepare(group)
  publish(group)
