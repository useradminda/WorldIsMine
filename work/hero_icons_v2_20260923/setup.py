from pathlib import Path
import json, hashlib
ROOT=Path(__file__).resolve().parents[2]
WORK=Path(__file__).resolve().parent
OUT=ROOT/'poc/L3-hero-icon/v2'
OUT.mkdir(parents=True, exist_ok=True)
(WORK/'jobs').mkdir(exist_ok=True)
old=ROOT/'work/hero_icons_20260922'
script=(old/'finish_icons.py').read_text(encoding='utf-8')
script=script.replace("OUT=ROOT/'poc/L3-hero-icon'", "OUT=ROOT/'poc/L3-hero-icon/v2'")
script=script.replace('../hero-icon/', '../../hero-icon/')
script=script.replace('L3 角色立绘', 'L3 V2 · 霸气武将')
(WORK/'finish_icons.py').write_text(script,encoding='utf-8')
protected=list((ROOT/'poc/hero-icon').glob('*'))+list((ROOT/'poc/L3-hero-icon').glob('*'))+list((ROOT/'poc/L3-hero-icon/Review').glob('*'))
snapshot={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.is_file()}
if not (WORK/'original_hashes.json').exists():
    (WORK/'original_hashes.json').write_text(json.dumps(snapshot,indent=2),encoding='utf-8')
print('Prepared v2; protected existing files:',len(snapshot))
