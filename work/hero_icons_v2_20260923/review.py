from pathlib import Path
import json, hashlib, html
from PIL import Image
from finish_icons import gallery, ROOT, OUT, WORK, sortkey

gallery()
manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
before=json.loads((WORK/'original_hashes.json').read_text(encoding='utf-8'))
changed=[n for n,h in before.items() if not (ROOT/n).exists() or hashlib.sha256((ROOT/n).read_bytes()).hexdigest()!=h]
edge=[]
for r in manifest['assets']:
    im=Image.open(ROOT/r['raw_source']).convert('RGBA')
    b=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    if min(b[0],b[1],im.width-b[2],im.height-b[3])<4:edge.append(r['file'])
report={'expected':68,'completed':manifest['completed'],'pending':manifest['pending'],'file_checks_pass':manifest['file_checks_pass'],'previous_files_changed':changed,'raw_edges_to_review':edge,'unique_images':len({r['output_sha256'] for r in manifest['assets']}),'all_delivered':manifest['all_delivered'] and not changed}
(OUT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
cards=[]
for r in manifest['assets']:
    n=html.escape(r['file'])
    cards.append(f'<article><a href="{n}"><img src="{n}" data-name="{n}" loading="lazy" alt="{n}"></a><b>{n}</b><nav><a href="../{n}">查看 V1</a><a href="../../hero-icon/{n}">原始参考</a></nav></article>')
doc='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>L3 V2 · 霸气武将</title><style>*{box-sizing:border-box}body{background:#111a24;color:#ead7ae;margin:0;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}header{padding:32px 5vw}h1{margin:0}p{color:#b5bdc8}main{padding:0 4vw 40px;display:grid;grid-template-columns:repeat(auto-fill,minmax(270px,1fr));gap:18px}article{background:#233040;border:1px solid #4c5665;border-radius:8px;overflow:hidden}article img{width:100%;display:block;background:radial-gradient(ellipse at center,#59616b,#202c3b)}b{display:block;padding:10px 16px 0}a{color:#e6c886}nav{display:flex;gap:20px;padding:4px 16px 16px;font-size:14px}button{padding:10px 16px;margin:6px 8px 0 0;background:#ddbd80;border:0;border-radius:4px;cursor:pointer}.light img{background:#e8e3d9}</style><header><h1>L3 V2 · 霸气武将</h1><p>成熟面部 · 自然头身比例 · 厚重盔甲 · 半写实战场气势<br>1024 × 1024 透明 PNG；保留 V1 和原图，文件名逐一对应。</p>'''+f'<p>已完成 {manifest["completed"]} / 68</p>'+'''<button onclick="document.body.classList.toggle('light')">切换深浅底</button><button onclick="toggleVersion(this)">切换整页到 V1 对比</button></header><main>'''+''.join(cards)+'''</main><script>let old=false;function toggleVersion(b){old=!old;document.querySelectorAll('img[data-name]').forEach(i=>{i.src=(old?'../':'')+i.dataset.name;i.parentElement.href=i.src});b.textContent=old?'切回 V2 霸气版':'切换整页到 V1 对比'}</script></html>'''
(OUT/'index.html').write_text(doc,encoding='utf-8')
(OUT/'README.md').write_text('''# L3 V2 · 霸气武将

基于原始 3D 截图重新设计的独立版本。成熟脸型、自然成人头身比例、强烈明暗和厚重材质；保留原角色的性别、种族、主色、武器与兵种。

- PNG：1024×1024，原生透明背景，角色完整留边，文件名与原图及 V1 对应。
- `index.html`：全套预览，可切换深浅底和 V1 对比。
- `Review/`：联系表；`manifest.json`：每张实际生成提示词、哈希与尺寸；`verification.json`：文件检查结果。
- 使用内置 imagegen 逐张生成。仅做统一缩放和透明留边规范化，不用代码绘制角色。
- 原图在 `../../hero-icon/`，V1 在父目录。本版制作不改动旧版任何文件。
''',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
