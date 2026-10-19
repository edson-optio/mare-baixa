"""Arte v0.2: originais preservados, NEAREST, paleta fixa, alpha e contorno na grade."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib

P = Path(__file__).resolve().parent
ROOT = P.parents[2]
OUT = ROOT / 'MareBaixa/Assets/Art/Gerados'
N = Image.Resampling.NEAREST
COLORS = ['101B2D','123D49','F47724','B94724','E6BD85','FFF0CE','42B5AC','64834B','7050AD','FFAA52','8ED7CB','94A653','70543E','A995C2','30354F','52586A','88959A','304E38','C58ADB','39315E','182238','5373A0','C4F5E6','80553C']
INK = (16,27,45,255)
palette = Image.new('P',(1,1))
flat = [v for c in COLORS for v in bytes.fromhex(c)]
palette.putpalette(flat + list(bytes.fromhex(COLORS[0])) * (256-len(COLORS)))

def prepare(spec):
    name=spec['name']; w,h=spec['width'],spec['height']
    if name=='saida-fechada':
        out=Image.open(P.parent/'saida.png').convert('RGBA')
        d=ImageDraw.Draw(out)
        for x in (10,15,20):
            d.rectangle((x-1,17,x+1,60),fill=INK)
            d.line((x,18,x,59),fill='#88959A')
            d.point((x,39),fill='#B94724')
        for y in (28,49):
            d.rectangle((7,y,24,y+2),fill=INK)
            d.line((8,y+1,23,y+1),fill='#70543E')
        d.polygon([(8,60),(9,55),(13,54),(17,59),(23,57),(24,61)],fill=INK)
        d.line((10,57,15,59,21,59),fill='#64834B',width=2)
        return out
    if name=='valvula-2':
        out=prepare(dict(name='valvula-1',width=32,height=32))
        d=ImageDraw.Draw(out)
        d.ellipse((11,3,19,10),fill=INK)
        d.line((12,3,19,10),fill='#B94724',width=2)
        d.line((19,3,12,10),fill='#F47724',width=2)
        d.point((15,6),fill='#E6BD85')
        return out
    im=Image.open(P/'originais'/f'{name}.png').convert('RGBA')
    alpha=im.getchannel('A').point(lambda a:255 if a>=160 else 0)
    assert alpha.getbbox(), name
    im.putalpha(alpha); im=im.crop(alpha.getbbox())
    if name.startswith('concha-'):
        limit=(18,24); x,y=0,3
    else:
        limit=(w-2,h); x=y=None
    scale=min(limit[0]/im.width,limit[1]/im.height)
    size=((18,24) if name=='concha-lata' else (18,20)) if name.startswith('concha-') else (max(1,round(im.width*scale)),max(1,round(im.height*scale)))
    if name=='tentaculo-aviso': size=(30,6)
    im=im.resize(size,N)
    a=im.getchannel('A')
    im=im.convert('RGB').quantize(palette=palette,dither=Image.Dither.NONE).convert('RGBA'); im.putalpha(a)
    pix=im.load()
    for yy in range(im.height):
        for xx in range(im.width):
            if not a.getpixel((xx,yy)): pix[xx,yy]=(0,0,0,0); continue
            if any(nx<0 or ny<0 or nx>=im.width or ny>=im.height or not a.getpixel((nx,ny)) for nx,ny in [(xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)]): pix[xx,yy]=INK
    im=im.crop(im.getbbox())
    out=Image.new('RGBA',(w,h))
    if x is None:
        floating=name.startswith(('pescador','polvo-olho','polvo-tinta','caco','correnteza'))
        x=(w-im.width)//2; y=(h-im.height)//2 if floating else h-im.height
    else: y=27-im.height
    out.paste(im,(x,y))
    if name.startswith('correnteza'): out.putalpha(out.getchannel('A').point(lambda v:144 if v else 0))
    return out

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    specs=json.loads((P/'prompts.json').read_text())['assets']
    results=[]; images=[]
    for spec in specs:
        name=spec['name']
        if not (P/'originais'/f'{name}.png').exists(): continue
        im=prepare(spec); im.save(P/f'{name}.png'); im.save(OUT/f'{name}.png'); images.append((name,im))
        colors=set(im.getdata()); bbox=im.getbbox()
        record={'arquivo':name+'.png','tamanho':list(im.size),'bbox':bbox,'cores':len(colors),'alpha':sorted({c[3] for c in colors}),'sha256':hashlib.sha256((OUT/f'{name}.png').read_bytes()).hexdigest()}
        assert im.size==(spec['width'],spec['height'])
        assert all(c==(0,0,0,0) for c in colors if not c[3])
        if not name.startswith(('concha','pescador','polvo-olho','polvo-tinta','caco','correnteza')): assert bbox[3]==im.height, name
        results.append(record)
    cols=4; cw,ch=400,300
    preview=Image.new('RGB',(cols*cw,((len(images)+cols-1)//cols)*ch),'#263748'); d=ImageDraw.Draw(preview)
    for i,(name,im) in enumerate(images):
        x=(i%cols)*cw; y=(i//cols)*ch; k=min(4,360//im.width,264//im.height)
        view=im.resize((im.width*k,im.height*k),N); preview.paste(view,(x+(cw-view.width)//2,y+20),view);d.text((x+12,y+ch-18),name,fill='white')
    preview.save(P/'previa.png')
    bodies=[Image.open(f).convert('RGBA') for f in sorted(P.parent.glob('casco-*.png')) if Image.open(f).size==(32,32)]
    shells=[im for name,im in images if name.startswith('concha-')]
    if bodies and shells:
        comp=Image.new('RGB',(len(bodies)*128,len(shells)*128),'#263748')
        for row,shell in enumerate(shells):
            for col,body in enumerate(bodies):
                view=Image.alpha_composite(body,shell).resize((128,128),N);comp.paste(view,(col*128,row*128),view)
        comp.save(P/'encaixes.png')
    (P/'validacao.json').write_text(json.dumps({'esperados':len(specs),'entregues':len(results),'completo':len(results)==len(specs),'filtro':'NEAREST','paleta':COLORS,'arquivos':results},indent=2)+'\n')
    print(f'{len(results)}/{len(specs)} sprites preparados')

if __name__=='__main__': main()
