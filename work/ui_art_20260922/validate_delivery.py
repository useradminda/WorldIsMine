"""Verify the actual exported art, layouts and import settings."""
from pathlib import Path
from PIL import Image
import json, re, hashlib

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/UI'
index=json.loads((OUT/'UI_Delivery_Index.json').read_text(encoding='utf-8'))
errors=[]
overlaps=[]
guids={}
hash_by_key={}
count=0
for folder in index['page_folders']:
    base=OUT/folder
    manifest=json.loads((base/'manifest.json').read_text(encoding='utf-8'))
    expected={a['file'] for a in manifest['assets']}
    actual={p.relative_to(base).as_posix() for p in (base/'Sprites').glob('*.png')}
    if expected != actual:errors.append([folder,'manifest mismatch',sorted(expected^actual)])
    for a in manifest['assets']:
        p=base/a['file'];im=Image.open(p);count+=1
        if im.mode!='RGBA' or list(im.size)!=a['size']:errors.append([str(p),'image format or size'])
        content=hashlib.sha256(p.read_bytes()).hexdigest()
        if a['id'] in hash_by_key and hash_by_key[a['id']]!=content:errors.append([str(p),'copies differ'])
        hash_by_key[a['id']]=content
        md=Path(str(p)+'.meta').read_text(encoding='utf-8')
        for prop,value in [('textureType','8'),('spriteMode','1'),('enableMipMap','0'),('alphaIsTransparency','1'),('nPOTScale','0')]:
            if not re.search(r'^\s*'+prop+': '+value+r'$',md,re.M):errors.append([str(p),'importer '+prop])
        guid=re.search(r'^guid: (\w+)',md,re.M).group(1)
        if guid in guids:errors.append([str(p),'duplicate GUID',guids[guid]])
        guids[guid]=str(p)
        if not re.search(r'^\s*spriteID: [a-f0-9]{32}$',md,re.M):errors.append([str(p),'missing Sprite ID'])
    for layoutpath in base.glob('layout*.json'):
        layout=json.loads(layoutpath.read_text(encoding='utf-8'))
        texts=[]
        for layer in layout['layers']:
            if layer['type']=='image':
                if not layer['preview_only'] and 'Sprites/'+layer['asset']+'.png' not in expected:errors.append([str(layoutpath),'missing layer asset',layer['asset']])
                x,y,w,h=layer['rendered_rect']
                if x<0 or y<0 or x+w>1080 or y+h>1920:errors.append([str(layoutpath),'image outside canvas',layer['asset']])
            elif layer['type']=='text':
                x0,y0,x1,y1=layer['bbox']
                if x0<0 or y0<0 or x1>1080 or y1>1920:errors.append([str(layoutpath),'text outside canvas',layer['sample']])
                texts.append(layer)
        for i,a in enumerate(texts):
            for b in texts[i+1:]:
                aa,bb=a['bbox'],b['bbox']
                dx=min(aa[2],bb[2])-max(aa[0],bb[0]);dy=min(aa[3],bb[3])-max(aa[1],bb[1])
                if dx>2 and dy>2:overlaps.append([str(layoutpath.relative_to(OUT)),a['sample'],b['sample'],[dx,dy]])
    for preview in manifest['previews']:
        if Image.open(base/preview).size!=(1080,1920):errors.append([folder,'preview resolution'])
refs=json.loads((OUT/'UI_Reference_Map.json').read_text(encoding='utf-8'))
if len(refs)!=len(list((ROOT/'zhibo-pic').glob('*.png'))):errors.append('reference count mismatch')
if len(index['page_folders'])!=12:errors.append('screen count mismatch')
qa=json.loads((OUT/'UI_Asset_QA.json').read_text(encoding='utf-8'))
if not qa['pass']:errors.append(['alpha QA failed',[x['path'] for x in qa['checks'] if not x['pass']]])
result={'asset_files_checked':count,'page_folders':len(index['page_folders']),'previews':len(index['screens']),
        'reference_screenshots':len(refs),'errors':errors,'text_overlaps':overlaps,'pass':not errors and not overlaps,
        'scope':'Files, alpha, layout references, canvas bounds, text overlap and importer metadata; no Unity runtime execution.'}
(OUT/'UI_Validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=True,indent=2))
raise SystemExit(0 if result['pass'] else 1)
