"""Campanha 64 px: prepara grupos completos em staging; publica só após revisão."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import importlib.util, json, math, hashlib, shutil, sys
P=Path(__file__).resolve().parent;D=P.parent;ROOT=D.parents[1]
OLD=ROOT/'MareBaixa/Assets/Art/Gerados';OUT=ROOT/'MareBaixa/Assets/Art/Sprites64';READY=P/'prontos'
N=Image.Resampling.NEAREST;CLEAR=(0,0,0,0);INK=(16,27,45,255)
spec=importlib.util.spec_from_file_location('amostra',D/'preparar.py');sample=importlib.util.module_from_spec(spec);spec.loader.exec_module(sample)
COMMON=['101B2D','123D49','F47724','B94724','E6BD85','FFF0CE','42B5AC','64834B','7050AD','FFAA52','8ED7CB','94A653','70543E','A995C2','30354F','52586A','88959A','304E38','C58ADB','39315E','182238','5373A0','C4F5E6','80553C']
WATER=['101B2D','123D49','1E5A70','287F83','42B5AC','8ED7CB','C4F5E6','FFF0CE','7050AD','A995C2','FFE36B']
FOSSA=['101B2D','090E1E','182238','25263F','39315E','51417D','7050AD','A995C2','C58ADB','5373A0','123D49','287F83','42B5AC','8ED7CB','C4F5E6','FFF0CE','E6BD85']
frames=lambda prefix,n:[f'{prefix}-{i}' for i in range(1,n+1)]
GROUPS={
 'casco':frames('casco-parado',2)+frames('casco-anda',6)+['casco-pulo','casco-queda']+frames('casco-ataque',3)+['concha-'+s for s in ['comum','tampinha','espiral','lata','lampada']]+['caco-'+s for s in ['areia','creme','turquesa','lata','lampada']],
 'recife':frames('agua-viva',4)+frames('agua-viva-carregada',4)+frames('baiacu',4)+['baiacu-inflado','espinho'],
 'cidade':['moreia-escondida','moreia-bote','moreia-volta']+frames('siri',4)+['siri-garra','siri-fugindo']+frames('valvula',2)+frames('correnteza',2),
 'fossa':frames('pescador',4)+['pescador-boca']+frames('tentaculo',4)+['tentaculo-aviso'],
 'chefes':frames('colosso-anda',4)+frames('colosso-rola',4)+['colosso-tonto','colosso-aviso','polvo-corpo','polvo-olho-aberto','polvo-olho-fechado','polvo-tinta'],
 'vila':['vovo-buzia','ostracia','dona-lagosta','bolota']}
used=set(sum(GROUPS.values(),[]));GROUPS['itens']=[p.stem for p in sorted(OLD.glob('*.png')) if p.stem not in used]
ALIASES={'agua-viva':'agua-viva-base','baiacu':'baiacu-base','perola':'perola-base','bolha':'bolha-base','alga-decor':'alga-decor-base','casa-garrafa':'casa-garrafa-corrigida','concha-tampinha':'concha-tampinha'}
PROVENANCE={}

def dims(name):
 f=OLD/f'{name}.png'
 if not f.exists():
  f=OLD/(name.rsplit('-',1)[0]+'-1.png')
  if name=='casco-ataque-3':f=OLD/'casco-ataque-1.png'
 w,h=Image.open(f).size;return w*2,h*2

def source(name):
 for folder in [P/'originais',ROOT/'design/sprites/v02/originais',ROOT/'design/sprites/originais']:
  f=folder/f'{name}.png'
  if f.exists():
   PROVENANCE[name]=str(f.relative_to(ROOT));im=Image.open(f).convert('RGBA');a=im.getchannel('A').point(lambda x:255 if x>=160 else 0);im.putalpha(a);return im.crop(a.getbbox())
 raise FileNotFoundError(name)

def fit(name,canvas,limit=None,bottom=True,pal=COMMON,xy=None,stretch=False):
 raw=source(name);limit=limit or (canvas[0]-4,canvas[1])
 s=min(limit[0]/raw.width,limit[1]/raw.height);size=limit if stretch else (max(1,round(raw.width*s)),max(1,round(raw.height*s)))
 im=sample.outline(sample.quant(raw.resize(size,N),pal));im=im.crop(im.getbbox());out=Image.new('RGBA',canvas)
 if xy is None:xy=((canvas[0]-im.width)//2,canvas[1]-im.height if bottom else (canvas[1]-im.height)//2)
 out.paste(im,xy);return out

def normalize(im):
 im.putdata([px if px[3] else CLEAR for px in im.getdata()]);return im

def ground(im):
 b=im.getbbox()
 if b and b[3]<im.height:
  out=Image.new('RGBA',im.size);out.paste(im,(0,im.height-b[3]));return out
 return im

def walk(phase):
 """Três pernas articuladas, alternância ampla; corpo/pinças em camadas fixas."""
 base=sample.base('casco-parado-1');upper=base.copy();d=ImageDraw.Draw(upper)
 # Segmentação nativa: preserva abdômen e cabeça, retira pernas e pinças móveis.
 d.polygon([(12,57),(17,49),(24,45),(28,49),(28,63),(8,63)],fill=CLEAR)
 near=base.crop((27,39,44,64));far=base.crop((44,39,60,64))
 d.rectangle((27,39,63,63),fill=CLEAR)
 bob=[0,1,2,0,1,2][phase]
 feet=[[(5,63),(19,58),(27,63)],[(8,63),(22,55),(25,60)],[(16,63),(24,56),(20,59)],[(28,63),(22,58),(7,63)],[(25,63),(20,55),(11,60)],[(21,63),(16,56),(18,59)]][phase]
 im=Image.new('RGBA',(64,64));im.alpha_composite(upper,(0,-bob));dd=ImageDraw.Draw(im)
 for j,((rx,ry),(fx,fy)) in enumerate(zip([(18,48-bob),(24,49-bob),(30,48-bob)],feet)):
  knee=((rx+fx)//2+(3 if j%2 else -3),54-bob)
  pts=[(rx,ry),knee,(fx,fy)]
  dd.line(pts,fill=INK,width=5)
  dd.line([(rx,ry),knee,(fx,fy-1)],fill='#B94724' if j==0 else '#F47724',width=3)
  dd.line([(rx,ry),knee],fill='#FFB35B' if j==2 else '#D95C22',width=1)
  dd.point((fx,fy),fill=INK)
 sway=[-1,0,1,1,0,-1][phase]
 for pts in [[(26,36-bob),(32+sway,41-bob)],[(36,36-bob),(48-sway,41-bob)]]:
  dd.line(pts,fill=INK,width=6);dd.line(pts,fill='#F47724',width=4)
 im.alpha_composite(near,(27+sway,37-bob));im.alpha_composite(far,(44-sway,38-bob))
 return normalize(sample.outline(im))

def shell(name):
 if name in ['concha-comum','concha-espiral']:return sample.base(name)
 size,xy={'concha-tampinha':((36,30),(0,24)),'concha-lata':((36,48),(0,6)),'concha-lampada':((36,40),(0,14))}[name]
 im=fit(name,(64,64),size,xy=xy,stretch=True,pal=sample.CASCO if name=='concha-lampada' else COMMON)
 d=ImageDraw.Draw(im);d.polygon([(35,42),(35,63),(20,63),(22,52),(26,46)],fill=CLEAR)
 body=sample.base('casco-parado-1').getchannel('A')
 for y in range(32):
  for x in range(27,38):
   if any(body.getpixel((xx,yy)) for xx in range(x-1,x+2) for yy in range(max(0,y-1),min(32,y+2))):im.putpixel((x,y),CLEAR)
 return sample.outline(im)

def shard(name):
 colors={'areia':('#B48A5A','#E6BD85','#FFF0CE'),'creme':('#E6BD85','#F6DBA7','#FFF0CE'),'turquesa':('#287F83','#42B5AC','#8ED7CB'),'lata':('#304E38','#64834B','#88959A'),'lampada':('#B48A5A','#FFD394','#FFF0CE')}[name[5:]]
 im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
 d.polygon([(3,1),(12,3),(14,7),(10,8),(12,13),(6,15),(2,10),(4,7),(1,5)],fill=colors[0],outline=INK)
 d.polygon([(4,3),(11,4),(9,7),(10,12),(6,13),(4,9),(6,6)],fill=colors[1]);d.line((4,3,9,4,8,7),fill=colors[2]);return im

def wave(im,phase,start=0,amplitude=2,vertical=False):
 out=Image.new('RGBA',im.size)
 for y in range(im.height):
  shift=round(amplitude*math.sin((y-start)/max(1,im.height-start)*math.pi*2+phase*math.pi/2)) if y>=start else 0
  out.paste(im.crop((0,y,im.width,y+1)),(shift,y))
 return out

def effects(name):
 w,h=dims(name);im=Image.new('RGBA',(w,h));d=ImageDraw.Draw(im)
 # Máscaras brancas: cor/opacidade da água são controladas pelo renderer Unity.
 if name=='agua':return Image.new('RGBA',(w,h),(255,255,255,255))
 if name.startswith('agua-superficie'):
  phase=int(name[-1])-1
  for x in range(w):
   y=3+round(2*math.sin((x+phase*12)*math.pi/16))
   d.line((x,y,x,h-1),fill=(255,255,255,255))
  return im
 if name.startswith('brilho'):
  phase=int(name[-1]);c=w//2;r=[10,14,7][phase-1]
  d.polygon([(c,c-r),(c+2,c-2),(c+r,c),(c+2,c+2),(c,c+r),(c-2,c+2),(c-r,c),(c-2,c-2)],fill='#FFF0CE')
  d.line((c,c-r+2,c,c+r-2),fill='#C4F5E6');d.point((c+8,c-8),fill='#8ED7CB')
  if phase==3:im.putalpha(im.getchannel('A').point(lambda a:140 if a else 0))
  return im
 if name=='espinho':
  d.polygon([(4,8),(29,14),(29,17),(4,23),(2,16)],fill='#E6BD85',outline=INK);d.polygon([(5,10),(24,14),(6,14)],fill='#FFF0CE');d.line((5,20,24,17),fill='#B48A5A');return im
 raise KeyError(name)

def make(name,group):
 size=dims(name)
 if name.startswith('concha'):return shell(name)
 if name.startswith('caco'):return shard(name)
 if name.startswith('casco-anda'):return walk(int(name[-1])-1)
 if name.startswith('casco-parado'):return sample.base('casco-parado-1') if name.endswith('1') else Image.open(P/'originais/aprovados/casco-parado-2.png').convert('RGBA')
 if name.startswith('casco'):
  f=P/'originais'/f'{name}.png';PROVENANCE[name]=str(f.relative_to(ROOT))
  im=ground(sample.outline(sample.quant(Image.open(f).convert('RGBA').resize(size,N),sample.CASCO)))
  # Registro idêntico dos olhos/hastes em poses aéreas e de ataque.
  im.paste(sample.base('casco-parado-1').crop((25,0,64,34)),(25,0))
  return im
 if name in ['agua','espinho'] or name.startswith(('brilho','agua-superficie')):return effects(name)
 if name.startswith('agua-viva'):
  phase=int(name[-1])-1;im=fit('agua-viva-base',size,(48,56),bottom=False,pal=WATER)
  im=wave(im,phase,start=26,amplitude=2);d=ImageDraw.Draw(im)
  if 'carregada' in name:
   for x,y in [(5+phase%2*3,16),(56-phase%2*2,24),(30,43)]:
    pts=[(x,y),(x-3,y+6),(x+2,y+6),(x-1,y+12)];d.line(pts,fill=INK,width=4);d.line(pts,fill='#FFE36B',width=2)
  return im
 if name.startswith('baiacu'):
  if name=='baiacu-inflado':return fit(name,size,(62,62),bottom=False,stretch=True)
  phase=int(name[-1])-1;im=fit('baiacu-base',size,(54,44),xy=(4,10),stretch=True)
  tail=im.crop((0,18,18,54));ImageDraw.Draw(im).rectangle((0,18,17,53),fill=CLEAR)
  shift=[0,-2,1,2][phase];im.alpha_composite(tail,(0,shift+18))
  d=ImageDraw.Draw(im);d.line((14,32+shift,23,32),fill=INK,width=5);d.line((14,32+shift,23,32),fill='#64834B',width=3)
  return im
 if name.startswith('siri'):
  if name!='siri-fugindo':return Image.open(P/'originais/aprovados'/f'{name}.png').convert('RGBA')
  im=Image.open(P/'originais/aprovados/siri-3.png').convert('RGBA');loot=sample.base('concha-espiral').crop((0,6,36,54)).resize((25,33),N)
  im.alpha_composite(loot,(20,6));return sample.quant(im,sample.SIRI)
 if name.startswith('pescador-') and name[-1].isdigit():
  phase=int(name[-1])-1;im=fit('pescador-1',size,bottom=False,pal=FOSSA)
  tail=im.crop((0,0,25,64));ImageDraw.Draw(im).rectangle((0,0,24,63),fill=CLEAR);shift=[0,-2,1,2][phase];im.alpha_composite(tail,(0,shift))
  d=ImageDraw.Draw(im);d.line((20,34+shift,29,34),fill=INK,width=5);d.line((20,34+shift,29,34),fill='#39315E',width=3)
  return im
 if name.startswith('tentaculo-') and name[-1].isdigit():
  phase=int(name[-1])-1;im=fit('tentaculo-1',size,pal=FOSSA);out=Image.new('RGBA',size)
  for y in range(size[1]):
   shift=round([0,4,1,-4][phase]*(1-y/(size[1]-1)));out.paste(im.crop((0,y,size[0],y+1)),(shift,y))
  return ground(out)
 if name=='tentaculo-aviso':return fit(name,size,(60,12),stretch=True,pal=FOSSA)
 if name.startswith('colosso-anda'):
  phase=int(name[-1])-1;im=fit('colosso-anda-1',size)
  # Corpo do chefe fixo; alterna a faixa inferior das pernas em 4 passos.
  out=im.copy();cut=130
  for x in range(size[0]):
   dy=3*math.sin(phase*math.pi/2+x/22)
   for y in range(cut,size[1]):
    sy=y-round(dy*(y-cut)/(size[1]-cut-1));out.putpixel((x,y),im.getpixel((x,sy)) if 0<=sy<size[1] else CLEAR)
  return ground(out)
 if name.startswith('colosso-rola'):
  phase=int(name[-1])-1;im=fit('colosso-rola-1',size,(150,150),bottom=True)
  box=im.getbbox();part=im.crop(box);wheel=Image.new('RGBA',(154,154));wheel.paste(part,((154-part.width)//2,(154-part.height)//2));wheel=wheel.rotate(phase*90,resample=N,expand=False)
  out=Image.new('RGBA',size);out.paste(wheel,((size[0]-wheel.width)//2,size[1]-wheel.height));return ground(out)
 if name=='valvula-2':
  im=make('valvula-1',group);box=im.getbbox();cx=(box[0]+box[2])//2;d=ImageDraw.Draw(im)
  d.ellipse((cx-9,6,cx+9,23),fill=INK);d.line((cx-7,7,cx+7,21),fill='#B94724',width=3);d.line((cx+7,7,cx-7,21),fill='#F47724',width=3);return im
 if name.startswith('perola'):
  im=fit('perola-base',size,(22,22),bottom=False);d=ImageDraw.Draw(im);x,y=(24,6) if name.endswith('1') else (26,4)
  d.line((x-4,y,x+4,y),fill='#FFF0CE');d.line((x,y-4,x,y+4),fill='#FFF0CE');return im
 if name.startswith('bolha'):
  return fit('bolha-base',size,(52,52) if name.endswith('1') else (56,50),bottom=False,pal=WATER,stretch=True)
 if name.startswith('alga-decor'):
  im=fit('alga-decor-base',size,(50,62));return im if name.endswith('1') else ground(wave(im,1,amplitude=2))
 if name=='saida-fechada':
  im=make('saida',group);d=ImageDraw.Draw(im)
  for x in (20,30,40):d.rectangle((x-2,34,x+2,120),fill=INK);d.line((x,36,x,118),fill='#88959A',width=2)
  for y in (56,98):d.rectangle((14,y,48,y+4),fill=INK);d.line((16,y+2,46,y+2),fill='#70543E',width=2)
  d.polygon([(16,120),(18,110),(26,108),(34,118),(46,114),(48,122)],fill='#64834B',outline=INK);return im
 if name=='casa-garrafa':return fit('casa-garrafa-corrigida',size,(80,126),stretch=True)
 if name in ['bau-aberto','bau-fechado']:
  im=fit(name,size,(60,60 if name.endswith('aberto') else 48),stretch=True)
  if name=='bau-aberto':im.paste(make('bau-fechado',group).crop((0,40,64,64)),(0,40))
  return im
 if name in ['vovo-buzia','ostracia','dona-lagosta','bolota']:
  lim={'vovo-buzia':(86,94),'ostracia':(88,88),'dona-lagosta':(92,92),'bolota':(60,58)}[name];return fit(name,size,lim)
 if name=='alga':return fit(name,size,(20,28),bottom=False,stretch=True)
 if name=='saida':return fit(name,size,(60,126))
 if name=='placa':return fit(name,size,(54,62))
 floating=name.startswith(('pescador','polvo-olho','polvo-tinta','correnteza'))
 im=fit(name,size,bottom=not floating,pal=FOSSA if group=='fossa' or name.startswith('polvo') or name=='coral-bio' else COMMON)
 if name.startswith('correnteza'):im.putalpha(im.getchannel('A').point(lambda v:144 if v else 0))
 return im

FLOATING=('concha','caco','agua','baiacu','espinho','pescador','polvo-olho','polvo-tinta','correnteza','perola','bolha','brilho')
def prepare(group):
 PROVENANCE.clear();READY.mkdir(parents=True,exist_ok=True);images={};report=[]
 for name in GROUPS[group]:
  im=normalize(make(name,group));assert im.size==dims(name),(name,im.size,dims(name))
  anchored=not name.startswith(FLOATING) and name!='alga'
  if anchored:im=ground(im);assert im.getbbox()[3]==im.height,name
  colors=set(im.getdata());assert len({px[:3] for px in colors if px[3]})<=24,(name,len(colors))
  assert all(px==CLEAR for px in colors if not px[3])
  im.save(READY/f'{name}.png');images[name]=im
  report.append({'arquivo':name+'.png','tamanho':list(im.size),'cores_opacas':len({p[:3] for p in colors if p[3]}),'alpha':sorted({p[3] for p in colors}),'bbox':im.getbbox(),'pes_ultima_linha':anchored,'sha256':hashlib.sha256((READY/f'{name}.png').read_bytes()).hexdigest()})
 cw,ch=400,360;cols=4;preview=Image.new('RGB',(cw*cols,ch*((len(images)+cols-1)//cols)),'#263748');d=ImageDraw.Draw(preview)
 for i,(name,im) in enumerate(images.items()):
  k=min(4,380//im.width,310//im.height);k=max(1,k);view=im.resize((im.width*k,im.height*k),N);x=i%cols*cw+(cw-view.width)//2;y=i//cols*ch+25
  d.rectangle((i%cols*cw+cw//2,i//cols*ch,i%cols*cw+cw-1,(i//cols+1)*ch-1),fill='#E5D7BD');preview.paste(view,(x,y),view);d.text((i%cols*cw+12,(i//cols+1)*ch-18),name,fill='#F47724')
 preview.save(P/f'previa-{group}.png')
 if group=='casco':
  walk_gif(images)
 (P/f'validacao-{group}.json').write_text(json.dumps({'grupo':group,'completo':len(report)==len(GROUPS[group]),'arquivos':report,'fontes':PROVENANCE,'resampling':'NEAREST'},indent=2)+'\n')
 print(group,len(report),'sprites preparados; nenhuma publicação parcial')

def walk_gif(images):
 frames_out=[];contact=Image.new('RGB',(6*256,3*256),'#263748')
 for i in range(6):
  body=images[f'casco-anda-{i+1}'];scene=Image.open(D/'referencia/fundo-recife.png').convert('RGBA').resize((1280,720),N)
  for col,shellname in enumerate([None,'concha-comum','concha-espiral']):
   comp=body.copy()
   if shellname:comp.alpha_composite(images[shellname])
   tile=comp.resize((256,256),N);contact.paste(tile,(i*256,col*256),tile);scene.alpha_composite(comp.resize((128,128),N),(300+col*280,550))
  frames_out.append(scene.convert('RGB').quantize(colors=128,dither=Image.Dither.NONE))
 durations=[80,80,90,80,80,90]
 frames_out[0].save(P/'casco-caminhada-12fps.gif',save_all=True,append_images=frames_out[1:],duration=durations,loop=0,disposal=2)
 contact.save(P/'casco-passada.png')
 gif=Image.open(P/'casco-caminhada-12fps.gif');total=0
 for i in range(gif.n_frames):gif.seek(i);total+=gif.info['duration']
 assert gif.n_frames==6 and total==500,(gif.n_frames,total)

LABELS={'casco':'Casco + cinco conchas + cacos','recife':'inimigos do Recife','cidade':'Cidade','fossa':'Fossa','chefes':'chefes','vila':'NPCs da vila','itens':'itens, decoração e efeitos'}
def publish(group):
 val=json.loads((P/f'validacao-{group}.json').read_text());assert val['completo'];assert len(val['arquivos'])==len(GROUPS[group])
 for a in val['arquivos']:assert hashlib.sha256((READY/a['arquivo']).read_bytes()).hexdigest()==a['sha256']
 OUT.mkdir(exist_ok=True)
 # Um lote já completo e validado; nada é copiado durante a geração.
 for a in val['arquivos']:
  shutil.copyfile(READY/a['arquivo'],OUT/a['arquivo']);shutil.copyfile(READY/a['arquivo'],D/a['arquivo'])
 for a in val['arquivos']:assert hashlib.sha256((OUT/a['arquivo']).read_bytes()).hexdigest()==a['sha256']
 record=f"\n- 2026-10-18 — Etapa 2 / {LABELS[group]}: {len(val['arquivos'])} PNGs publicados juntos em `MareBaixa/Assets/Art/Sprites64/`: "+', '.join('`'+a['arquivo']+'`' for a in val['arquivos'])+f". Prévia e validação: `design/sprites64/etapa2/previa-{group}.png` e `validacao-{group}.json`."
 if group=='casco':record+=' Caminhada refeita com passada ampla, bob de 0/1/2 px, pinças móveis e GIF verificado: seis quadros/500 ms (12 fps).'
 task=ROOT/'docs/tarefas/v03-D-arte-64.md'
 if f'Etapa 2 / {LABELS[group]}:' not in task.read_text():
  with task.open('a') as f:f.write(record+'\n')
 print('PUBLICADO',group,len(val['arquivos']))

if __name__=='__main__':
 action=sys.argv[1];group=sys.argv[2]
 if action=='preparar':prepare(group)
 elif action=='publicar':publish(group)
 elif action=='inventario':print(json.dumps(GROUPS,indent=2))
