"""Normalize imagegen output size, preserve alpha, and assemble review sheets.
Does not paint characters or invent/replace image content.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, re, hashlib, shutil, sys, html, math

ROOT=Path(__file__).resolve().parents[2]
WORK=Path(__file__).resolve().parent
OUT=ROOT/'poc/L3-hero-icon/v2'
SOURCE=ROOT/'poc/hero-icon'
OUT.mkdir(parents=True,exist_ok=True)
(WORK/'source_art').mkdir(exist_ok=True)
FONT='C:/Windows/Fonts/msyh.ttc'

def sortkey(name):
    return (0 if name.startswith('Hero') else 1,int(re.search(r'\d+',name).group()),name)

def finish(job):
    data=json.loads(job.read_text(encoding='utf-8'))
    ident=data['id']
    source=SOURCE/(ident+'.png')
    assert source.exists(), source
    raw=WORK/'source_art'/(ident+'.png')
    match=re.search(r' as (D:\\[^\n]+?\.png) by default',data['result_hint'])
    assert match, data['result_hint']
    generated=Path(match.group(1))
    if generated.exists():
        if not raw.exists() or raw.read_bytes()!=generated.read_bytes():shutil.copyfile(generated,raw)
    im=Image.open(raw).convert('RGBA')
    alpha=im.getchannel('A')
    if alpha.getextrema()[0]!=0:
        raise ValueError(ident+': generated image lacks transparent exterior')
    box=alpha.point(lambda a:255 if a>8 else 0).getbbox()
    if not box:raise ValueError(ident+': empty output')
    art=im.crop(box)
    art.thumbnail((928,928),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(1024,1024),(0,0,0,0))
    canvas.alpha_composite(art,((1024-art.width)//2,(1024-art.height)//2))
    path=OUT/(ident+'.png')
    canvas.save(path,optimize=True)
    bbox=canvas.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    report={'file':path.name,'input':source.relative_to(ROOT).as_posix(),
            'input_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
            'output_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
            'size':list(canvas.size),'mode':canvas.mode,'alpha':list(canvas.getchannel('A').getextrema()),
            'visible_bbox_alpha64':list(bbox),'raw_source':raw.relative_to(ROOT).as_posix(),
            'normalization':'Native alpha preserved; uniform resampling into 1024 square with safety margin. No painted alterations.',
            'generation':'builtin imagegen','prompt':data['prompt']}
    (WORK/'records').mkdir(exist_ok=True)
    (WORK/'records'/(ident+'.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    return report

def gallery():
    expected=sorted([p.name for p in SOURCE.glob('*.png')],key=sortkey)
    reports=[json.loads(p.read_text(encoding='utf-8')) for p in (WORK/'records').glob('*.json')]
    reports.sort(key=lambda a:sortkey(a['file']))
    pending=[name for name in expected if name not in {r['file'] for r in reports}]
    problems=[]
    for r in reports:
        im=Image.open(OUT/r['file'])
        alpha=im.getchannel('A')
        b=alpha.point(lambda a:255 if a>64 else 0).getbbox()
        if im.size!=(1024,1024) or im.mode!='RGBA' or alpha.getextrema()[0]!=0 or not b or min(b[0],b[1],1024-b[2],1024-b[3])<32:
            problems.append(r['file'])
    manifest={'expected':len(expected),'completed':len(reports),'pending':pending,'failed_file_checks':problems,
              'file_checks_pass':not problems,'all_delivered':not pending and not problems,'assets':reports}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    review=OUT/'Review'
    review.mkdir(exist_ok=True)
    for start in range(0,len(reports),12):
        group=reports[start:start+12]
        sheet=Image.new('RGB',(1200,936),'#283849');d=ImageDraw.Draw(sheet)
        for j,r in enumerate(group):
            x,y=j%4*300,j//4*312
            # Dark and light backgrounds expose potential alpha fringes.
            d.rectangle((x,y,x+298,y+310),fill='#e9e4d8' if j%2==0 else '#25394a')
            im=Image.open(OUT/r['file']);im.thumbnail((282,282),Image.Resampling.LANCZOS)
            sheet.paste(im,(x+9,y+3),im)
            d.text((x+12,y+288),r['file'],font=ImageFont.truetype(FONT,16),fill='#263847' if j%2==0 else '#f0dbaa')
        sheet.save(review/f'Overview_{start+1:02d}-{start+len(group):02d}.jpg',quality=94)
    cards=[]
    for r in reports:
        name=html.escape(r['file'])
        cards.append(f'<article><a href="{name}"><img src="{name}" loading="lazy"></a><strong>{name}</strong><a class="source" href="../../hero-icon/{name}">查看原图</a></article>')
    doc='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>L3 V2 · 霸气武将</title><style>body{background:#152737;color:#f6ead6;margin:0;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}header{padding:40px 5vw}h1{margin:0}p{color:#b9c9d1}main{padding:0 4vw 40px;display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:20px}article{background:#354b5c;border:1px solid #667783;border-radius:10px;overflow:hidden}article img{width:100%;display:block;background:linear-gradient(130deg,#73818b,#253e52)}strong{display:block;padding:12px 16px 0}a{color:#e6c886}.source{display:block;padding:4px 16px 16px;font-size:14px}button{padding:9px 16px;margin:8px 12px 0 0;background:#e3c285;border:0;border-radius:5px;cursor:pointer}.light article img{background:#e8e2d7}.light article{background:#e8e2d7;color:#243a4b}.light .source{color:#6b542b}</style><header><h1>L3 V2 · 霸气武将</h1><p>原图逐张重设计 · 保留角色辨识特征 · 1024 × 1024 · 透明 PNG<br>点击图片查看原尺寸；可切换明暗底检查透明边缘。</p>'''+f'<p>已完成 {len(reports)} / {len(expected)}</p>'+'''<button onclick="document.body.classList.remove('light')">深色背景</button><button onclick="document.body.classList.add('light')">浅色背景</button></header><main>'''+''.join(cards)+'</main></html>'
    (OUT/'index.html').write_text(doc,encoding='utf-8')
    print(json.dumps({'expected':len(expected),'completed':len(reports),'pending':len(pending),'file_checks_pass':not problems}))

if __name__=='__main__':
    args=sys.argv[1:]
    if args and args[0]!='--gallery':
        for ident in args:print(json.dumps({'finished':finish(WORK/'jobs'/(ident+'.json'))['file']}))
    elif not args:
        for job in sorted((WORK/'jobs').glob('*.json')):finish(job)
    if not args or '--gallery' in args:gallery()
