"""Folha solicitada: cada 64px a 4x, antigo a 8x, recorte e caminhada real 2x."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
P=Path(__file__).resolve().parent;N=Image.Resampling.NEAREST
NAMES=['casco-parado-1','casco-parado-2']+[f'casco-anda-{i}' for i in range(1,7)]+['concha-comum','concha-espiral']+[f'siri-{i}' for i in range(1,5)]+['siri-garra']
try:
 FONT='/System/Library/Fonts/Supplemental/Arial.ttf';title=ImageFont.truetype(FONT,24);font=ImageFont.truetype(FONT,17);small=ImageFont.truetype(FONT,14)
except OSError:title=font=small=ImageFont.load_default()

def oldname(n):
 if n=='casco-anda-5':return 'casco-anda-1'
 if n=='casco-anda-6':return 'casco-anda-2'
 if n=='siri-3':return 'siri-1'
 if n=='siri-4':return 'siri-2'
 return n

def main():
 sheet=Image.new('RGB',(1600,3456),'#101B2D');d=ImageDraw.Draw(sheet)
 d.text((24,12),'MARÉ BAIXA · v0.3 · ETAPA 1 / AMOSTRA PARA APROVAÇÃO',font=title,fill='#FFF0CE')
 d.text((24,46),'15 sprites · 64×64 · NEAREST · paleta limitada · fundo claro/escuro · pés na linha 63',font=font,fill='#9EC6C6')
 layout=[]
 for i,n in enumerate(NAMES):
  x=i%2*800;y=80+i//2*320
  d.text((x+16,y+3),n,font=font,fill='#FFF0CE')
  for col,label in enumerate(['NOVO 64 px · 4×','ANTIGO 32 px · 8×','RECORTE DA PRANCHA']):d.text((x+16+col*264,y+30),label,font=small,fill='#9EC6C6')
  for col in range(2):
   xx=x+12+col*264; yy=y+54
   d.rectangle((xx,yy,xx+255,yy+255),fill='#263748')
   d.rectangle((xx+128,yy,xx+255,yy+255),fill='#E5D7BD')
  f=P/(n+'.png')
  if f.exists():
   im=Image.open(f).convert('RGBA').resize((256,256),N);sheet.paste(im,(x+12,y+54),im)
  old=oldname(n);im=Image.open(P/'referencia/antigos'/f'{old}.png').convert('RGBA').resize((256,256),N);sheet.paste(im,(x+276,y+54),im)
  ref='siri' if n.startswith('siri') else 'comum' if n=='concha-comum' else 'espiral-completa' if n=='concha-espiral' else 'casco'
  im=Image.open(P/'referencia'/f'{ref}.png').convert('RGB');r=min(256/im.width,256/im.height);im=im.resize((round(im.width*r),round(im.height*r)),N);sheet.paste(im,(x+540+(256-im.width)//2,y+54+(256-im.height)//2))
  if old!=n:d.text((x+540,y+282),f'Sem antigo: comparação com {old}',font=small,fill='#E6BD85')
  layout.append({'nome':n,'novo_box':[x+12,y+54,256,256],'antigo':old,'antigo_box':[x+276,y+54,256,256],'referencia':ref})
 y0=80+8*320
 d.text((24,y0+8),'CAMINHADA · seis fases consecutivas · 64 px a 2× (128 px na folha)',font=title,fill='#FFF0CE')
 d.text((24,y0+40),'Cada posição abaixo é um quadro do mesmo Casco; fundo do Recife também a 2×.',font=font,fill='#9EC6C6')
 bg=Image.open(P/'referencia/fundo-recife.png').convert('RGBA').crop((0,180,640,360)).resize((1280,360),N)
 walking=[]
 for i in range(1,7):
  f=P/f'casco-anda-{i}.png'
  if not f.exists():continue
  body=Image.open(f).convert('RGBA');shell=Image.open(P/'concha-comum.png').convert('RGBA');walking.append(Image.alpha_composite(body,shell))
 for i,im in enumerate(walking):
  im=im.resize((128,128),N);bg.alpha_composite(im,(56+i*192,212))
  ImageDraw.Draw(bg).text((84+i*192,344),f'passo {i+1}',font=small,fill='#FFF0CE')
 sheet.paste(bg,(160,y0+76))
 d.text((24,y0+452),'ENCAIXES · todas as poses de Casco com concha comum e espiral · 2×',font=title,fill='#FFF0CE')
 for row,shellname in enumerate(['concha-comum','concha-espiral']):
  f=P/(shellname+'.png')
  if not f.exists():continue
  shell=Image.open(f).convert('RGBA')
  for i,n in enumerate(NAMES[:8]):
   f=P/(n+'.png')
   if not f.exists():continue
   im=Image.alpha_composite(Image.open(f).convert('RGBA'),shell).resize((128,128),N)
   xx=160+i*160;yy=y0+500+row*145
   d.rectangle((xx,yy,xx+127,yy+127),fill='#E5D7BD' if i%2 else '#263748');sheet.paste(im,(xx,yy),im)
 sheet.save(P/'amostra.png')
 (P/'amostra-layout.json').write_text(json.dumps({'sprites':layout,'caminhada_box':[160,y0+76,1280,360],'novo_ampliacao':4,'antigo_ampliacao':8,'jogo_ampliacao':2},indent=2)+'\n')
 if len(walking)==6:
  frames=[]
  for i,im in enumerate(walking):
   frame=Image.open(P/'referencia/fundo-recife.png').convert('RGBA').resize((1280,720),N)
   frame.alpha_composite(im.resize((128,128),N),(576,560))
   frames.append(frame.convert('RGB').quantize(colors=128,dither=Image.Dither.NONE))
  frames[0].save(P/'casco-andando.gif',save_all=True,append_images=frames[1:],duration=120,loop=0,disposal=2)
 print('Prévia: design/sprites64/amostra.png')
if __name__=='__main__':main()
