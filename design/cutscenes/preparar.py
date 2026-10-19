"""16 quadros de campanha: recorte 16:9, NEAREST e 48 cores, sem texto sobreposto."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
P=Path(__file__).resolve().parent;OUT=P.parents[1]/'MareBaixa/Assets/Art/Cutscenes';N=Image.Resampling.NEAREST

def prepare(src):
 im=Image.open(src).convert('RGB');w,h=im.size
 if w*9>h*16:
  nw=h*16//9;box=((w-nw)//2,0,(w-nw)//2+nw,h)
 else:
  nh=w*9//16;box=(0,(h-nh)//2,w,(h-nh)//2+nh)
 return im.crop(box).resize((640,360),N).quantize(colors=48,dither=Image.Dither.NONE).convert('RGBA'),box

def main():
 OUT.mkdir(parents=True,exist_ok=True);images=[];report=[]
 for a in json.loads((P/'prompts.json').read_text())['assets']:
  name=a['name'];src=P/'originais'/f'{name}.png'
  if not src.exists():continue
  im,box=prepare(src);assert im.size==(640,360)
  colors=set(im.getdata());assert len(colors)<=48;assert all(p[3]==255 for p in colors)
  im.save(P/f'{name}.png');im.save(OUT/f'{name}.png');images.append((name,im))
  report.append({'arquivo':name+'.png','tamanho':[640,360],'cores':len(colors),'alpha':[255],'recorte_original':box,'sha256':hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()})
 preview=Image.new('RGB',(1280,816),'#101B2D');d=ImageDraw.Draw(preview)
 for i,(name,im) in enumerate(images):
  x=i%4*320;y=i//4*204;preview.paste(im.resize((320,180),N),(x,y));d.text((x+8,y+185),name,fill='#FFF0CE')
 preview.save(P/'previa.png')
 for scene in range(1,5):
  selected=[(n,im) for n,im in images if n.startswith(f'cena{scene}-')]
  if not selected:continue
  sheet=Image.new('RGB',(1280,((len(selected)+1)//2)*384),'#101B2D');d=ImageDraw.Draw(sheet)
  for i,(name,im) in enumerate(selected):
   x=i%2*640;y=i//2*384;sheet.paste(im,(x,y));d.text((x+8,y+365),name,fill='#FFF0CE')
  sheet.save(P/f'previa-cena{scene}.png')
 (P/'validacao.json').write_text(json.dumps({'completo':len(report)==16,'esperados':16,'entregues':len(report),'filtro':'NEAREST','arquivos':report},indent=2)+'\n')
 print(f'{len(report)}/16 quadros preparados')
if __name__=='__main__':main()
