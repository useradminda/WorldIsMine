from pathlib import Path
from html.parser import HTMLParser
import hashlib,json,shutil
from PIL import Image
from finish_icons import ROOT,WORK,OUT

manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
assert manifest['completed']==68 and manifest['all_delivered'], 'Generation is incomplete'
expected={p.name for p in (ROOT/'poc/hero-icon').glob('*.png')}
actual={p.name for p in OUT.glob('*.png')}
assert actual==expected, 'PNG inventory mismatch'
assert len({r['output_sha256'] for r in manifest['assets']})==68, 'Duplicate output'
for r in manifest['assets']:
    p=OUT/r['file']
    im=Image.open(p)
    assert im.size==(1024,1024) and im.mode=='RGBA'
    assert im.getchannel('A').getextrema()==(0,255)
    assert hashlib.sha256(p.read_bytes()).hexdigest()==r['output_sha256']
before=json.loads((WORK/'original_hashes.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/n).read_bytes()).hexdigest()==h for n,h in before.items())
class Links(HTMLParser):
    def __init__(self):super().__init__();self.links=[];self.images=0
    def handle_starttag(self,tag,attrs):
        if tag=='img':self.images+=1
        for k,v in attrs:
            if k in ('href','src'):self.links.append(v)
parser=Links();parser.feed((OUT/'index.html').read_text(encoding='utf-8'))
assert parser.images==68
assert all((OUT/n).resolve().is_file() for n in parser.links)
final_sheets={f'Overview_{a:02d}-{min(a+11,68):02d}.jpg' for a in range(1,69,12)}
archive=WORK/'intermediate_review';archive.mkdir(exist_ok=True)
for p in (OUT/'Review').glob('Overview_*.jpg'):
    assert p.resolve().parent==(OUT/'Review').resolve()
    if p.name not in final_sheets:shutil.move(str(p),str(archive/p.name))
assert {p.name for p in (OUT/'Review').glob('*.jpg')}==final_sheets
report=json.loads((OUT/'verification.json').read_text(encoding='utf-8'))
report.update({'exact_inventory_pass':True,'manifest_hashes_pass':True,'preview_links_pass':True,'preview_images':parser.images,'review_sheets':6,'protected_old_files':len(before)})
(OUT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
