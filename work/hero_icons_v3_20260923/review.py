"""Build the V3 review gallery and verify each delivered PNG."""
from pathlib import Path
import hashlib, html, json
from PIL import Image
from finish_icons import gallery, ROOT, OUT, WORK

gallery()
manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
before=json.loads((WORK/'protected_hashes.json').read_text(encoding='utf-8'))
changed=[n for n,h in before.items() if not (ROOT/n).is_file() or hashlib.sha256((ROOT/n).read_bytes()).hexdigest()!=h]
edge=[]
for r in manifest['assets']:
    im=Image.open(ROOT/r['raw_source']).convert('RGBA')
    b=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    if b and min(b[0],b[1],im.width-b[2],im.height-b[3])<4:edge.append(r['file'])
report={'expected':68,'completed':manifest['completed'],'pending':manifest['pending'],'file_checks_pass':manifest['file_checks_pass'],'previous_files_changed':changed,'raw_edges_to_review':edge,'unique_images':len({r['output_sha256'] for r in manifest['assets']}),'all_delivered':manifest['all_delivered'] and not changed}
(OUT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')

cards=[]
for r in manifest['assets']:
    n=html.escape(r['file'])
    cards.append(f'<article><a class="view" href="{n}"><img src="{n}" data-name="{n}" loading="lazy" alt="{n}"></a><strong>{n}</strong><nav><a href="../{n}">V1</a><a href="../v2/{n}">V2</a><a href="../../hero-icon/{n}">原图</a></nav></article>')
doc='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>L3 V3 · 国风角色美术</title>
<style>*{box-sizing:border-box}body{background:#172534;color:#eee1c9;margin:0;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}header{padding:32px 5vw 18px;border-bottom:4px solid #a66743;background:linear-gradient(120deg,#1a3142,#273945)}h1{margin:0;color:#f2dcc0;letter-spacing:.05em}p{margin:10px 0;color:#c4c9c2}.actions{display:flex;flex-wrap:wrap;gap:10px;margin:18px 0}button{padding:9px 17px;background:#aa6746;border:1px solid #d8ac78;border-radius:4px;color:#fff6e9;font-size:15px;cursor:pointer}button.active{background:#dec495;color:#202b31;border-color:#fff0d0}main{padding:24px 4vw 48px;display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:18px}article{background:#253a49;border:1px solid #5a6670;border-radius:6px;overflow:hidden}article img{display:block;width:100%;background:#d8d2c4}strong{display:block;padding:8px 14px 0}nav{display:flex;gap:18px;padding:4px 14px 14px;font-size:14px}a{color:#eac68b}.dark article img{background:#253a49}.light{background:#ede8dc;color:#213443}.light article{background:#f4f0e8;color:#253442}.light article img{background:#ece7db}.light a{color:#764423}</style>
<header><h1>L3 V3 · 国风角色美术</h1><p>三国彩墨 · 工笔线条 · 漆甲铜饰 · 1024 × 1024 透明 PNG</p><p id="count"></p><div class="actions"><button class="active" data-version="v3" onclick="switchVersion('v3')">V3 国风</button><button data-version="v2" onclick="switchVersion('v2')">V2 霸气</button><button data-version="v1" onclick="switchVersion('v1')">V1 Q 版</button><button data-version="original" onclick="switchVersion('original')">原始截图</button><button onclick="document.body.classList.toggle('light')">切换深浅底</button></div></header>
<main>'''+''.join(cards)+'''</main><script>const prefix={v3:'',v2:'../v2/',v1:'../',original:'../../hero-icon/'};document.getElementById('count').textContent='已完成 '''+str(manifest['completed'])+''' / 68';function switchVersion(version){document.querySelectorAll('img[data-name]').forEach(img=>{const path=prefix[version]+img.dataset.name;img.src=path;img.parentElement.href=path});document.querySelectorAll('button[data-version]').forEach(b=>b.classList.toggle('active',b.dataset.version===version))}</script></html>'''
(OUT/'index.html').write_text(doc,encoding='utf-8')
(OUT/'README.md').write_text('''# L3 V3 · 国风角色美术

以原始角色截图为身份参考，逐张设计成三国幻想彩墨、工笔线条与漆甲铜饰结合的独立国风版本。角色的主色、兵种、武器与种族对应原图；骑兵、步兵、异兽与攻城器械共 68 张。

- 每张为 1024×1024 的透明 PNG，与原图、V1、V2 文件名对应。
- `index.html` 为全套预览，可切换 V1、V2、V3 和原始截图，以及深浅背景。
- `Review/` 是六张联系表；`manifest.json` 记录每张生成提示词和文件哈希；`verification.json` 记录验收结果。
- 使用内置 imagegen 逐张创作；仅统一缩放与透明留边，未用代码绘制角色。
''',encoding='utf-8')
print(json.dumps({'completed':report['completed'],'pending':len(report['pending']),'checks':report['file_checks_pass'],'changed_old':len(changed),'raw_edges':edge},ensure_ascii=False))
