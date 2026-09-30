"""Build Korean fantasy V4 preview and inspect generated cutouts."""
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
    cards.append(f'<article><a class="view" href="{n}"><img src="{n}" data-name="{n}" loading="lazy" alt="{n}"></a><strong>{n}</strong><nav><a href="../{n}">V1</a><a href="../v2/{n}">V2</a><a href="../v3/{n}">V3</a><a href="../../hero-icon/{n}">原图</a></nav></article>')

style='''<style>*{box-sizing:border-box}body{margin:0;background:#151b30;color:#e5e9f7;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}header{padding:32px 5vw 18px;background:linear-gradient(125deg,#202444,#343157);border-bottom:3px solid #8872ba}h1{margin:0;letter-spacing:.04em}p{margin:10px 0;color:#bfc7e1}.actions{display:flex;flex-wrap:wrap;gap:10px;margin:18px 0}button{padding:9px 17px;background:#4c4e80;border:1px solid #817fc3;border-radius:5px;color:#f9f9ff;font-size:15px;cursor:pointer}button.active{background:#c4b4ee;color:#232339;border-color:#f1eaff}main{padding:24px 4vw 48px;display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:18px}article{background:#262e4b;border:1px solid #575e82;border-radius:6px;overflow:hidden}article img{display:block;width:100%;background:#d4d5df}strong{display:block;padding:8px 14px 0}nav{display:flex;gap:16px;padding:4px 14px 14px;font-size:14px}a{color:#c5c5ff}.dark article img{background:#252b42}.light{background:#ececf3;color:#242844}.light article{background:#f6f5fa;color:#292d45}.light article img{background:#e9e8f1}.light a{color:#5341a0}</style>'''
head='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>L3 V4 · 韩式奇幻立绘</title>'''+style+'''<header><h1>L3 V4 · 韩式奇幻立绘</h1><p>韩式奇幻游戏 · 精细线条 · 清晰赛璐璐光影 · 1024 × 1024 透明 PNG</p><p id="count"></p><div class="actions"><button class="active" data-version="v4" onclick="switchVersion('v4')">V4 韩风</button><button data-version="v3" onclick="switchVersion('v3')">V3 国风</button><button data-version="v2" onclick="switchVersion('v2')">V2 霸气</button><button data-version="v1" onclick="switchVersion('v1')">V1 Q 版</button><button data-version="original" onclick="switchVersion('original')">原始截图</button><button onclick="document.body.classList.toggle('light')">切换深浅底</button></div></header><main>'''
script='''</main><script>const prefix={v4:'',v3:'../v3/',v2:'../v2/',v1:'../',original:'../../hero-icon/'};document.getElementById('count').textContent='已完成 '''+str(manifest['completed'])+''' / 68';function switchVersion(version){document.querySelectorAll('img[data-name]').forEach(img=>{const path=prefix[version]+img.dataset.name;img.src=path;img.parentElement.href=path});document.querySelectorAll('button[data-version]').forEach(b=>b.classList.toggle('active',b.dataset.version===version))}</script></html>'''
(OUT/'index.html').write_text(head+''.join(cards)+script,encoding='utf-8')
(OUT/'README.md').write_text('''# L3 V4 · 韩式奇幻立绘

基于原始 3D 截图逐张重绘的独立 V4 风格。采用韩式奇幻游戏插画与 Manhwa 式精细线条、利落的赛璐璐光影、精致甲胄和布料。角色的兵种、性别、种族、主色与关键装备均以原始截图为准。

- 68 张 1024×1024 的透明 PNG；文件名与原图及 V1/V2/V3 一一对应。
- `index.html` 可切换 V1、V2、V3、V4 和原始截图，以及深浅背景。
- `Review/` 有六张联系表，`manifest.json` 有逐张生成提示词与哈希，`verification.json` 有验收结果。
- 通过内置 imagegen 逐张生成。尺寸规范化仅缩放、留透明边距，不以代码绘制角色。
''',encoding='utf-8')
print(json.dumps({'completed':report['completed'],'pending':len(report['pending']),'checks':report['file_checks_pass'],'changed_old':len(changed),'raw_edges':edge},ensure_ascii=False))
