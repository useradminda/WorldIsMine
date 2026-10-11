from pathlib import Path
import json, hashlib
ROOT=Path(__file__).resolve().parents[2]
WORK=Path(__file__).resolve().parent
OUT=ROOT/'poc/L3-hero-icon/v4'
OUT.mkdir(parents=True,exist_ok=True)
(WORK/'jobs').mkdir(exist_ok=True)
source=(ROOT/'work/hero_icons_v3_20260923/finish_icons.py').read_text(encoding='utf-8')
source=source.replace("OUT=ROOT/'poc/L3-hero-icon/v3'","OUT=ROOT/'poc/L3-hero-icon/v4'")
(WORK/'finish_icons.py').write_text(source,encoding='utf-8')
protected=list((ROOT/'poc/hero-icon').glob('*'))
protected+=list((ROOT/'poc/L3-hero-icon').glob('*'))
protected+=list((ROOT/'poc/L3-hero-icon/Review').glob('*'))
protected+=list((ROOT/'poc/L3-hero-icon/v2').rglob('*'))
protected+=list((ROOT/'poc/L3-hero-icon/v3').rglob('*'))
snapshot={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.is_file()}
(WORK/'protected_hashes.json').write_text(json.dumps(snapshot,indent=2),encoding='utf-8')
print('protected:',len(snapshot))
