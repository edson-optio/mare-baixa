"""Cenários v0.2: recorte 16:9, NEAREST, 24 cores e silhuetas em 3 tons."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
P=Path(__file__).resolve().parent;OUT=P.parents[2]/'MareBaixa/Assets/Art/Cenarios';N=Image.Resampling.NEAREST
PALETTES={'cidade':['142C2C','263F38','3A5146'],'fossa':['090D1C','12182C','25213B'],'bolha':['153E46','245960','3C7775']}
def crop169(im):
 w,h=im.size
 if w*9>h*16:
  nw=h*16//9;im=im.crop(((w-nw)//2,0,(w-nw)//2+nw,h))
 else:
  nh=w*9//16;im=im.crop((0,(h-nh)//2,w,(h-nh)//2+nh))
 return im

def main():
 OUT.mkdir(parents=True,exist_ok=True);ims={};report=[]
 for a in json.loads((P/'prompts.json').read_text())['assets']:
  name=a['name'];src=P/'originais'/f'{name}.png'
  if not src.exists():continue
  raw=Image.open(src).convert('RGBA');sil=name.startswith('silhuetas')
  if sil:
   alpha=raw.getchannel('A').point(lambda x:255 if x>=160 else 0);raw.putalpha(alpha);raw=raw.crop(alpha.getbbox()).resize((640,180),N)
   colors=PALETTES[name.split('-')[1]];pal=Image.new('P',(1,1));pal.putpalette([v for c in colors for v in bytes.fromhex(c)]+list(bytes.fromhex(colors[0]))*(256-len(colors)))
   layer=raw.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA');layer.putalpha(raw.getchannel('A'))
   im=Image.new('RGBA',(640,360));im.paste(layer,(0,180))
   # Perfil nas bordas compatível, sem degrau no terreno ao repetir.
   for y in range(180,360):im.putpixel((639,y),im.getpixel((0,y)))
   im.putdata([px if px[3] else (0,0,0,0) for px in im.getdata()])
  else:im=crop169(raw).resize((640,360),N).convert('RGB').quantize(colors=24,dither=Image.Dither.NONE).convert('RGBA')
  im.save(P/f'{name}.png');im.save(OUT/f'{name}.png');ims[name]=im
  colors=set(im.getdata());assert len(colors)<=25
  if sil:
   assert im.crop((0,0,640,180)).getbbox() is None
   assert {px[3] for px in colors}<={0,255}
   assert all(im.getpixel((0,y))==im.getpixel((639,y)) for y in range(360))
  report.append({'arquivo':name+'.png','tamanho':[640,360],'cores':len(colors),'alpha':sorted({px[3] for px in colors}),'metade_superior_transparente':sil,'bordas_horizontais_iguais':sil,'sha256':hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()})
 preview=Image.new('RGB',(1280,1080),'#101B2D')
 for row,zone in enumerate(PALETTES):
  if f'fundo-{zone}' not in ims:continue
  comp=ims[f'fundo-{zone}'].copy()
  if f'silhuetas-{zone}' in ims:comp=Image.alpha_composite(comp,ims[f'silhuetas-{zone}'])
  preview.paste(comp,(0,row*360));preview.paste(comp,(640,row*360))
 preview.save(P/'previa.png')
 (P/'validacao.json').write_text(json.dumps({'completo':len(report)==6,'filtro':'NEAREST','arquivos':report},indent=2)+'\n')
 print(f'{len(report)}/6 cenários preparados')
if __name__=='__main__':main()
