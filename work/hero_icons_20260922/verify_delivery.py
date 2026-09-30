"""Read-only image inventory, provenance, and transparent-edge verification."""
from pathlib import Path
from PIL import Image
import json, hashlib

ROOT=Path(__file__).resolve().parents[2]
WORK=Path(__file__).resolve().parent
OUT=ROOT/'poc/L3-hero-icon'
before=json.loads((WORK/'input_hashes.json').read_text(encoding='utf-8'))
changed=[name for name,digest in before.items() if hashlib.sha256((ROOT/'poc/hero-icon'/name).read_bytes()).hexdigest()!=digest]
expected=set(before)
actual={p.name for p in OUT.glob('*.png')}
rows=[]
failures=[]
for name in sorted(actual):
    p=OUT/name
    im=Image.open(p)
    if im.mode!='RGBA':
        failures.append(name+': not RGBA')
        continue
    alpha=im.getchannel('A')
    visible=alpha.point(lambda a:255 if a>64 else 0).getbbox()
    record_path=WORK/'records'/(p.stem+'.json')
    record=json.loads(record_path.read_text(encoding='utf-8')) if record_path.exists() else {}
    digest=hashlib.sha256(p.read_bytes()).hexdigest()
    checks={'dimensions':im.size==(1024,1024),'transparent_exterior':alpha.getextrema()[0]==0,
            'opaque_body':alpha.getextrema()[1]==255,
            'safe_margin':bool(visible) and min(visible[0],visible[1],1024-visible[2],1024-visible[3])>=32,
            'output_hash':digest==record.get('output_sha256'),
            'source_hash':before.get(name)==record.get('input_sha256')}
    raw=Image.open(WORK/'source_art'/name).convert('RGBA')
    raw_box=raw.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    raw_margin=min(raw_box[0],raw_box[1],raw.width-raw_box[2],raw.height-raw_box[3]) if raw_box else 0
    rows.append({'file':name,'checks':checks,'raw_size':list(raw.size),'raw_visible_margin':raw_margin})
    failures.extend(name+': '+key for key,value in checks.items() if not value)
result={'expected':len(expected),'completed':len(actual),'missing':sorted(expected-actual),'unexpected':sorted(actual-expected),
        'changed_inputs':changed,'failed_checks':failures,'raw_border_review':[r['file'] for r in rows if r['raw_visible_margin']<4],
        'all_delivered':actual==expected and not changed and not failures,'assets':rows}
(OUT/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='assets'},ensure_ascii=False))
