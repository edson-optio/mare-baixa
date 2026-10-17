from pathlib import Path
from PIL import Image
import shutil, json

OUT = Path(__file__).parent
SRC = Path('/Users/edson/.codex/generated_images/01a14720-5e44-7801-9ec7-e16fd6918730')
FILES = {
 'fundo-recife':'exec-0537eb2f-66bc-42ec-898d-d1845aa5b6b9.png',
 'silhuetas-recife':'exec-9778e646-b739-4e86-ae86-4cc7b5c87fa4.png',
 'fundo-vila':'exec-0999db59-cbb4-4c33-878a-c52965be6d17.png',
 'silhuetas-vila':'exec-488f79fd-f9f7-40b6-af31-9a03ffab5d5e.png',
 'menu-chave':'exec-508d7dc7-2bf0-4b22-b8a0-a2c003d436bf.png',
}

def prepare(im, name):
    w,h=im.size
    if name.startswith('silhuetas'):
        im=im.convert('RGBA')
        alpha=im.getchannel('A').point(lambda a:255 if a>=128 else 0)
        box=alpha.getbbox()
        if box and box[1]<h//2+2:
            shifted=Image.new('RGBA',im.size)
            shifted.paste(im,(0,h//2+2-box[1]))
            im=shifted
            alpha=im.getchannel('A').point(lambda a:255 if a>=128 else 0)
        colors=['1F5E66','17495A'] if name.endswith('recife') else ['C7835A','B77451','D49468']
        rgb=[tuple(bytes.fromhex(c)) for c in colors]
        pal=Image.new('P',(1,1))
        pal.putpalette([v for c in rgb for v in c]+list(rgb[-1])*(256-len(rgb)))
        im=im.convert('RGB').quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA')
        im.putalpha(alpha)
        px=im.load()
        # Join both sides through short, flat, matching ground profiles.
        margin=max(8,round(w*.025))
        level=round(h*.90)
        base=rgb[1 if name.endswith('recife') else 0]+(255,)
        for side in (0,1):
            inner=margin if side==0 else w-margin-1
            ys=[y for y in range(h//2,h) if px[inner,y][3]]
            edge_y=min(ys) if ys else level
            for d in range(margin):
                x=d if side==0 else w-1-d
                top=round(level+(edge_y-level)*d/margin)
                for y in range(h):
                    px[x,y]=base if y>=top else (0,0,0,0)
        # Transparent pixels have no hidden colored fringe.
        im.putdata([p if p[3] else (0,0,0,0) for p in im.getdata()])
    else:
        im=im.convert('RGB').quantize(colors=48 if name=='menu-chave' else 24,dither=Image.Dither.NONE).convert('RGB')
    return im

report={}
for name,src in FILES.items():
    raw=Image.open(SRC/src)
    shutil.copy2(SRC/src, OUT/'originais'/f'{name}.png')
    # Minimal centered crop: 1672x941 -> exact 16:9 1664x936.
    w,h=raw.size
    unit=min(w//16,h//9)
    nw,nh=unit*16,unit*9
    raw=raw.crop(((w-nw)//2,(h-nh)//2,(w-nw)//2+nw,(h-nh)//2+nh))
    high=prepare(raw,name)
    high.save(OUT/f'{name}.png')
    final=high.resize((640,360),Image.Resampling.NEAREST)
    final.save(OUT/f'{name}-640.png')
    assert final.size==(640,360)
    entry={'high':high.size,'final':final.size,'colors':len(final.getcolors(640*360))}
    if name.startswith('silhuetas'):
        for im in (high,final):
            w,h=im.size
            assert set(im.getchannel('A').getdata())=={0,255}
            assert im.getchannel('A').crop((0,0,w,h//2)).getbbox() is None
            assert list(im.crop((0,0,1,h)).getdata())==list(im.crop((w-1,0,w,h)).getdata())
        entry.update(alpha=[0,255],top_half_transparent=True,edges_equal=True)
    report[name]=entry
(OUT/'validacao.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
