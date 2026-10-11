"""Assemble authored PNGs into per-screen deliveries. No art is drawn here.
Pillow is used only for alpha trimming, compositing and preview text.
Run from repository root: python work/ui_art_20260922/build_delivery.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import json, re, hashlib, uuid, html, math, shutil

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / 'work/ui_art_20260922'
OUT = ROOT / 'Assets/UI'
OLD = OUT / 'UI_Clean_5_Styles/01_War_Banners'
PROOF = WORK / 'proofs'
PROOF.mkdir(exist_ok=True)
W, H = 1080, 1920
INK = '#253645'
WHITE = '#fff0cc'
GOLD = '#e1bb70'
FONT = 'C:/Windows/Fonts/msyh.ttc'
FONT_BOLD = 'C:/Windows/Fonts/msyhbd.ttc'

def dump(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding='utf-8')

records = json.loads((WORK/'generation_log.json').read_text(encoding='utf-8'))
for p in sorted((WORK/'jobs').glob('*.json')):
    records.append(json.loads(p.read_text(encoding='utf-8')))
records = list({r['id']: r for r in records}.values())
SOURCE = {p.stem: p for p in OLD.glob('*.png')}
PROVENANCE = {p.stem: 'existing_war_banners' for p in OLD.glob('*.png')}
for r in records:
    m = re.search(r' as (D:\\[^\n]+?\.png) by default', r['result_hint'])
    if not m:
        raise ValueError(r['id'])
    original = Path(m.group(1))
    local = WORK/'source_art'/f'{r["id"]}.png'
    local.parent.mkdir(exist_ok=True)
    if original.exists() and (not local.exists() or hashlib.sha256(original.read_bytes()).digest()!=hashlib.sha256(local.read_bytes()).digest()):
        shutil.copyfile(original,local)
    SOURCE[r['id']] = local
    PROVENANCE[r['id']] = 'builtin_imagegen'

CAT = {}
IMAGES = {}
for key, path in SOURCE.items():
    im = Image.open(path).convert('RGBA')
    size = im.size
    alpha = im.getchannel('A')
    bbox = alpha.point(lambda p: 255 if p > 64 else 0).getbbox()
    if not bbox:
        raise ValueError('Empty asset: '+key)
    # Frame/mask pair share their union extent, so a Unity mask aligns exactly.
    if key in ('avatar_frame','avatar_mask'):
        pair = [Image.open(SOURCE[k]).convert('RGBA').getchannel('A').point(lambda p:255 if p>64 else 0).getbbox() for k in ('avatar_frame','avatar_mask')]
        bbox = (min(b[0] for b in pair), min(b[1] for b in pair), max(b[2] for b in pair), max(b[3] for b in pair))
    im = im.crop(bbox)
    IMAGES[key] = im
    CAT[key] = {'id': key, 'source': str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else path.name,
                'provenance': PROVENANCE[key], 'source_size': list(size), 'crop_xyxy': list(bbox),
                'size': list(im.size), 'alpha_extrema': list(im.getchannel('A').getextrema()),
                'text_baked': key == 'logo_title', 'preserve_aspect': True,
                'usage_note': 'UI portrait with deliberate lower-body cutoff; cover the lower edge with the summon banner or card.' if key.startswith('hero_') else None,
                'alignment_group': 'avatar_frame_mask' if key.startswith('avatar_') else None}

def meta(path, sprite=True):
    """Use the repository's importer schema; preserve existing GUID on rebuild."""
    target = Path(str(path)+'.meta')
    if target.exists():
        current=target.read_text(encoding='utf-8')
        current=re.sub(r'(^\s*spriteID:) *$', r'\g<1> '+uuid.uuid5(uuid.NAMESPACE_URL,str(path)).hex,current,flags=re.M)
        target.write_text(current,encoding='utf-8')
        return
    template = (OLD/'health_frame.png.meta').read_text(encoding='utf-8')
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'WorldIsMine/UIArt20260922/'+path.relative_to(OUT).as_posix()).hex
    template = re.sub(r'^guid: .*$', 'guid: '+guid, template, flags=re.M)
    for prop, value in {'enableMipMap':0,'nPOTScale':0,'spriteMode':1 if sprite else 0,
                        'spriteMeshType':1,'spriteGenerateFallbackPhysicsShape':0,
                        'textureType':8 if sprite else 0,'alphaIsTransparency':1,
                        'wrapU':1,'wrapV':1,'wrapW':1,'maxTextureSize':4096,'textureCompression':0}.items():
        template = re.sub(r'(^\s*'+prop+r':) [^\r\n]+', r'\g<1> '+str(value), template, flags=re.M)
    template = re.sub(r'(^\s*spriteID:) [^\r\n]*', r'\g<1> '+uuid.uuid5(uuid.NAMESPACE_URL,guid).hex, template, flags=re.M)
    target.write_text(template, encoding='utf-8')

class Screen:
    def __init__(self, folder, title, variant='default', modal=False, loading=False):
        self.folder, self.title, self.variant = folder, title, variant
        self.path = OUT/folder
        self.path.mkdir(exist_ok=True)
        (self.path/'Sprites').mkdir(exist_ok=True)
        self.layers = []
        self.used = set()
        self.canvas = Image.new('RGBA', (W,H))
        self.add('bg_loading' if loading else 'bg_battle_preview', 0,0,W,H, fit='cover', preview_only=not loading)
        if modal:
            self.veil(0.52)

    def veil(self, opacity):
        self.canvas.alpha_composite(Image.new('RGBA',(W,H),(8,20,28,round(255*opacity))))
        self.layers.append({'type':'engine_overlay','color':'#08141c','opacity':opacity,'rect':[0,0,W,H]})

    def add(self, key, x,y,w,h, fit='contain', preview_only=False, opacity=1, tag=None):
        if key not in IMAGES:
            raise KeyError('Pending generated asset: '+key)
        im = IMAGES[key]
        if fit == 'stretch':
            rw,rh = round(w),round(h)
        else:
            scale = (max if fit=='cover' else min)(w/im.width,h/im.height)
            rw,rh = max(1,round(im.width*scale)),max(1,round(im.height*scale))
        rendered = im.resize((rw,rh),Image.Resampling.LANCZOS)
        px,py = round(x+(w-rw)/2),round(y+(h-rh)/2)
        if fit == 'cover':
            left,top = round((rw-w)/2),round((rh-h)/2)
            rendered = rendered.crop((left,top,left+round(w),top+round(h)))
            px,py=round(x),round(y)
        if opacity != 1:
            rendered.putalpha(rendered.getchannel('A').point(lambda a:round(a*opacity)))
        self.canvas.alpha_composite(rendered,(px,py))
        self.layers.append({'type':'image','asset':key, 'rect':[x,y,w,h], 'rendered_rect':[px,py,rendered.width,rendered.height],
                            'fit':fit,'opacity':opacity,'preview_only':preview_only,'tag':tag})
        if not preview_only:
            self.used.add(key)

    def text(self, text,x,y,size=32,color=INK,anchor='mm',bold=False, max_width=None, tag=None):
        font = ImageFont.truetype(FONT_BOLD if bold else FONT,size)
        draw = ImageDraw.Draw(self.canvas)
        if max_width:
            while draw.textlength(text,font=font)>max_width and size>16:
                size-=1
                font=ImageFont.truetype(FONT_BOLD if bold else FONT,size)
        draw.text((x,y),text,font=font,fill=color,anchor=anchor,
                  stroke_width=1 if color==WHITE else 0,stroke_fill='#253645')
        b=list(draw.textbbox((x,y),text,font=font,anchor=anchor))
        self.layers.append({'type':'text','sample':text,'position':[x,y],'font_size':size,'color':color,'anchor':anchor,
                            'font_weight':'bold' if bold else 'regular','bbox':b,'runtime_text':True,'tag':tag})

    def button(self,label,y,x=320,w=440,key='btn_primary_gold'):
        self.add(key,x,y,w,108)
        self.text(label,x+w/2,y+54,34,INK if key=='btn_primary_gold' else WHITE,bold=True)

    def titlebar(self,title,y=365,w=600):
        self.add('notification_panel',(W-w)/2,y,w,172)
        self.text(title,540,y+83,43,WHITE,bold=True)

    def panel(self,title):
        self.add('panel_scroll_large',45,345,990,1340)
        self.titlebar(title)
        self.add('icon_close',931,386,70,70)

    def hud(self,join=True):
        # Supply clean frame/fill/track layers as well as the decorative full bars.
        self.used.update(['health_frame','health_track','health_fill_blue','health_fill_red'])
        self.add('join_blue',25,44,475,152)
        self.add('join_red',580,44,475,152)
        self.text('蓝方',245,83,27,WHITE)
        self.text('红方',835,83,27,WHITE)
        self.text('1,580',250,132,42,WHITE,bold=True)
        self.text('1,420',830,132,42,WHITE,bold=True)
        self.add('health_strip_blue_decorated',137,185,293,38)
        self.add('health_strip_red_decorated',650,185,293,38)
        self.text('城防 250,000',285,205,19,WHITE,tag='blue_castle_hp')
        self.text('城防 250,000',796,205,19,WHITE,tag='red_castle_hp')
        self.add('faction_blue',15,215,92,128)
        self.add('faction_red',971,215,92,128)
        self.add('timer_panel',435,16,210,239)
        self.text('第 3 回合',540,64,23,WHITE)
        self.text('02:48',540,141,37,WHITE,bold=True,tag='battle_timer')
        self.add('icon_settings',973,357,68,68)
        self.add('icon_rank',973,450,68,68)
        self.add('ranking_panel',746,256,218,268)
        self.text('贡献榜 TOP3',855,324,20,bold=True)
        for i,(name,score,medal) in enumerate([('阿青','12,800','gold'),('小北','9,600','silver'),('大山','8,200','bronze')]):
            y=369+i*49
            self.add('medal_'+medal,767,y-19,38,38)
            self.text(str(i+1),786,y,19,bold=True)
            self.text(name,817,y,19,anchor='lm')
            self.text(score,939,y,18,anchor='rm')
        if join:
            for x,key,label in [(35,'join_blue','发「1」加入蓝方'),(550,'join_red','发「2」加入红方')]:
                self.add(key,x,1639,495,166)
                self.text(label,x+247,1722,34,WHITE,bold=True)
            self.text('万人同屏 · 由你指挥',540,1840,28,WHITE)

    def table(self, y=700, results=False):
        columns=[(205,'名次'),(353,'玩家'),(514,'本局积分'),(652,'荣耀'),(766,'月排行'),(868,'掉落')] if results else [(205,'名次'),(387,'玩家'),(621,'胜点'),(821,'总积分')]
        for x,label in columns:self.text(label,x,y-69,23 if results else 27,bold=True)
        for i,name in enumerate(['阿青','小北','大山','长风','青衫','山河']):
            yy=y+i*112
            self.add('panel_row',172,yy-46,736,96)
            if i<3:
                self.add('medal_'+['gold','silver','bronze'][i],179,yy-27,52,54)
            self.text(str(i+1),205,yy+1,22,bold=True)
            self.add('avatar_frame',253,yy-29,58,58)
            self.used.add('avatar_mask')
            self.text(name[0],282,yy,21,bold=True)
            self.text(name,359 if results else 400,yy-13 if results else yy,25,bold=True)
            if results:
                self.text('胜点 +'+str(3 if i==0 else 1),359,yy+22,17)
                self.text(['12,800','9,600','8,200','7,200','6,400','5,800'][i],514,yy-13,23,bold=True)
                self.text('+'+str(320-i*35)+' 加成',514,yy+22,17,color='#367c56')
                self.text(str(88-i*9),652,yy,25)
                self.text(str(128+i*173)+' ↑',766,yy,23)
                self.add(['unit_sword','unit_shield','unit_bow'][i%3],843,yy-25,50,50)
            else:
                self.text(str(3200-i*350),621,yy,27)
                self.text(['128,000','96,000','82,000','72,000','64,000','58,000'][i],821,yy,25)

    def finish(self):
        for key in self.used:
            path=self.path/'Sprites'/f'{key}.png'
            if not path.exists() or Image.open(path).tobytes()!=IMAGES[key].tobytes():
                IMAGES[key].save(path,optimize=True)
            meta(path)
        # Background is only an assembly aid, not a replacement game-world map.
        preview=self.path/('Preview.png' if self.variant=='default' else f'Preview_{self.variant}.png')
        self.canvas.convert('RGB').save(preview,optimize=True)
        meta(preview,False)
        layout={'screen':self.folder,'title':self.title,'variant':self.variant,'reference_resolution':[W,H],
                'coordinate_system':'top-left pixels; ordered back to front','layers':self.layers,
                'notes':['Text and numbers are runtime data; sample strings are proof-only.',
                         'bg_battle_preview is a scenic assembly aid; use the actual Unity world camera at runtime.',
                         'Simple sprites preserve aspect. Fill strips/track alone stretch horizontally. No nine-slice.',
                         'Pointer states: normal=(1,1,1,1), pressed tint=(0.82,0.82,0.82,1), disabled alpha=0.45.']}
        dump(self.path/('layout.json' if self.variant=='default' else f'layout_{self.variant}.json'),layout)
        return {'folder':self.folder,'title':self.title,'variant':self.variant,'preview':preview.relative_to(OUT).as_posix(),'assets':sorted(self.used)}

def build():
    pages=[]
    s=Screen('01_Loading','加载界面',loading=True)
    s.add('logo_title',190,120,700,390)
    s.text('群雄逐鹿 · 一战定天下',540,505,30,WHITE)
    s.add('health_track',248,1683,584,58,fit='stretch')
    s.add('health_fill_blue',248,1683,421,58,fit='stretch',tag='progress_fill')
    s.add('health_frame',164,1632,752,162)
    s.text('正在整备军阵…  72%',540,1793,27,WHITE,tag='loading_progress')
    pages.append(s.finish())

    s=Screen('02_Controls','操作说明',modal=True);s.panel('操作说明')
    s.text('选择阵营',540,584,32,bold=True)
    for x,key,label in [(230,'1','蓝方'),(650,'2','红方')]:
        s.add('keycap',x,633,110,110);s.text(key,x+55,684,49,WHITE,bold=True)
        s.text(label,x+155,684,32,bold=True)
    s.text('移动视角',540,845,32,bold=True)
    for label,dx,dy in [('W',0,0),('A',-110,108),('S',0,108),('D',110,108)]:
        s.add('keycap',355+dx,907+dy,100,100);s.text(label,405+dx,953+dy,36,WHITE,bold=True)
    s.text('或方向键',728,1014,28)
    s.add('icon_mouse',240,1182,102,147)
    s.text('滚轮缩放',490,1248,32,bold=True)
    s.add('btn_primary_gold',643,1206,202,95);s.text('SPACE',744,1254,25,bold=True)
    s.text('回到战场中心',744,1340,26)
    s.button('我知道了',1508);pages.append(s.finish())

    s=Screen('03_ModeSelect','模式选择',modal=True);s.panel('选择对局模式')
    s.text('调兵遣将，选择你的战场',540,578,28)
    for i,(hero,key,title,desc) in enumerate([('hero_blue','join_blue','经典战场','20 分钟  ·  城防 100,000'),('hero_green','join_red','进阶战场','30 分钟  ·  城防 250,000'),('hero_red','join_blue','巅峰对决','60 分钟  ·  城防 400,000')]):
        y=637+i*270
        s.add(key,137,y+20,806,245)
        s.add(hero,164,y-26,230,282)
        c=WHITE
        s.text(title,636,y+92,39,c,bold=True)
        s.text(desc,636,y+153,25,c)
        if i==0:s.add('checkmark',865,y+100,46,46)
    s.button('确认选择',1514);pages.append(s.finish())

    s=Screen('04_Lobby','等待开战');s.hud()
    s.add('notification_panel',161,276,560,165);s.text('胜点奖池  30,000',441,352,35,WHITE,bold=True)
    s.add('hero_blue',61,470,193,279)
    s.add('join_blue',240,542,475,152);s.text('首礼召唤 · 赵云出战',478,601,30,WHITE,bold=True)
    s.text('蓝方名额 0/5  ·  红方名额 0/5',478,648,23,WHITE)
    s.titlebar('本场军功榜',795,580)
    for i,(x,y,k,n,v) in enumerate([(406,1000,'gold','阿青','12,800'),(157,1100,'silver','小北','9,600'),(655,1100,'bronze','大山','8,200')]):
        s.add('medal_'+k,x,y,270,272);s.text(str(i+1),x+135,y+111,54,bold=True)
        s.text(n,x+135,y+296,33,WHITE,bold=True);s.text(v,x+135,y+344,28,WHITE)
    s.button('开始对局',1499);pages.append(s.finish())

    s=Screen('05_Leaderboard','排行榜',modal=True);s.panel('群雄排行榜')
    s.text('半月军功 · 榜上留名',540,577,28)
    s.table(y=728)
    s.text('每半月结算一次，奖励以活动规则为准',540,1457,24)
    s.button('继续征战',1520);pages.append(s.finish())

    s=Screen('06_Settings','设置',modal=True);s.panel('战场设置')
    s.button('添加玩法贴纸',557,x=329,w=423,key='join_blue')
    s.text('背景音乐',182,766,29,anchor='lm',bold=True)
    s.add('health_track',440,753,348,22,fit='stretch')
    s.add('health_fill_blue',440,753,242,22,fit='stretch')
    s.add('slider_thumb',651,731,64,64);s.text('70%',854,766,26)
    for i,label in enumerate(['死亡表现','技能展示','保存设置']):
        yy=898+i*126
        s.add('panel_row',172,yy-46,736,96)
        s.text(label,214,yy,30,anchor='lm',bold=True)
        for x,on,txt in [(623,True,'开'),(783,False,'关')]:
            s.add('keycap',x,yy-26,53,53)
            if on:s.add('checkmark',x+4,yy-22,45,45)
            s.text(txt,x+83,yy,26)
    s.text('战场地图',182,1317,29,anchor='lm',bold=True)
    for i,label in enumerate(['随机','沙场','桃园','竹林']):
        x=171+i*190
        s.add('keycap',x,1370,48,48)
        if i==0:s.add('checkmark',x+4,1374,40,40)
        s.text(label,x+86,1394,25)
    s.button('保存设置',1520);pages.append(s.finish())

    for variant in ['default','heroes']:
        s=Screen('07_LiveGuide','直播玩法贴纸',variant);s.hud()
        s.add('panel_scroll_large',25,613,671,919)
        s.add('join_blue',77,658,270,99);s.text('兵种召唤',212,709,25,WHITE,bold=True)
        s.add('join_red',354,658,270,99);s.text('武将召唤',489,709,25,WHITE,bold=True)
        if variant=='default':
            for i,(icon,name,cmd) in enumerate([('unit_sword','步兵','点赞助阵'),('unit_shield','盾兵','送礼召唤 · 建兵营'),('unit_bow','弓兵','送礼召唤 · 建兵营'),('unit_spear','枪兵','送礼召唤 · 建兵营'),('unit_fan','策士','送礼召唤 · 建兵营'),('unit_catapult','投石车','送礼召唤 · 建兵营'),('unit_horse','骑兵','送礼召唤 · 建兵营'),('unit_elite','天兵','送礼召唤 · 建兵营'),('unit_command','全军突击','本方强化')]):
                yy=803+i*70
                s.add(icon,130,yy-29,59,59);s.text(name,218,yy,24,anchor='lm',bold=True)
                s.text(cmd,344,yy,22,anchor='lm')
        else:
            for i,(hero,name,desc) in enumerate([('hero_blue','赵云','银枪破阵'),('hero_green','关羽','青龙斩将'),('hero_red','吕布','无双战神')]):
                yy=796+i*193
                s.add(hero,128,yy-12,130,188);s.text(name,314,yy+51,32,bold=True)
                s.text(desc,445,yy+110,25)
        s.text('支持阵营 · 召唤援军',355,1416,26,bold=True)
        pages.append(s.finish())

    s=Screen('08_BattleHUD','战斗主界面');s.hud()
    s.add('notification_panel',431,555,603,179);s.add('hero_blue',442,538,125,184)
    s.text('小北召唤赵云',783,642,31,WHITE,bold=True)
    s.add('join_blue',202,945,232,85);s.text('关羽  Lv.5',318,984,22,WHITE)
    s.add('health_strip_blue_decorated',229,1037,179,26)
    s.add('join_red',645,1205,232,85);s.text('吕布  Lv.6',761,1244,22,WHITE)
    s.add('health_strip_red_decorated',672,1297,179,26)
    s.text('支援就位，向敌方城池进军',540,1524,30,WHITE)
    pages.append(s.finish())

    s=Screen('09_HeroSummon','武将召唤播报');s.hud();s.veil(0.12)
    s.add('hero_blue',53,566,605,826)
    s.add('join_blue',98,1246,884,305)
    s.text('小北召唤',721,1338,31,WHITE)
    s.text('赵云 · 龙胆银枪',660,1404,43,WHITE,bold=True)
    s.text('武将等级  Lv.6',686,1469,27,WHITE)
    pages.append(s.finish())

    s=Screen('10_TroopSummon','兵种召唤播报');s.hud()
    s.add('notification_panel',65,612,715,211)
    s.add('unit_shield',94,647,115,115)
    s.text('阿青召唤',449,681,27,WHITE)
    s.text('盾兵  × 8',466,741,39,WHITE,bold=True)
    s.add('soldier_shield',86,859,425,561)
    s.add('join_blue',96,1428,413,140);s.text('援军已抵达',302,1494,29,WHITE,bold=True)
    pages.append(s.finish())

    for variant in ['default','breach_red','victory_blue','victory_red']:
        s=Screen('11_BattleAnnouncements','战场大事件',variant);s.hud();s.veil(0.25)
        if variant in ['default','breach_red']:
            s.add('join_blue' if variant=='default' else 'join_red',58,769,963,353)
            s.text('城池已被攻破',540,928,57,WHITE,bold=True)
            s.text(('蓝方' if variant=='default' else '红方')+'攻势如虹，继续推进！',540,1124,30,WHITE)
        else:
            side=variant.split('_')[1]
            s.add('victory_'+side,75,642,930,736)
            s.text('蓝方胜利' if side=='blue' else '红方胜利',540,1148,60,WHITE,bold=True)
            s.text('旌旗所向 · 凯旋而归',540,1427,32,WHITE)
        pages.append(s.finish())

    for variant in ['default','halfmonth']:
        s=Screen('12_Results','对局结算',variant,modal=True)
        s.add('panel_scroll_large',36,317,1008,1370)
        s.add('victory_blue',334,224,412,325)
        s.text('蓝方胜利',540,459,29,WHITE,bold=True)
        s.add('icon_stats',179,584,70,70);s.text('兵团统计',214,690,20)
        s.add('join_blue',256,558,332,130);s.text('本局战报',422,617,27,WHITE,bold=True)
        s.add('join_red',612,558,332,130);s.text('半月排行',778,617,27,WHITE,bold=True)
        selected_x=391 if variant=='default' else 747
        s.add('checkmark',selected_x,685,62,49)
        s.table(y=836,results=variant=='default')
        s.text('军功已记录 · 感谢每一位将士',540,1490,25)
        s.button('继续征战',1550)
        pages.append(s.finish())
    return pages

def deliver(pages):
    folders=sorted({p['folder'] for p in pages})
    mapping={14483:'01_Loading',14484:'02_Controls',14485:'03_ModeSelect',14486:'04_Lobby',14487:'05_Leaderboard',14488:'06_Settings',14489:'04_Lobby',14490:'04_Lobby',14491:'09_HeroSummon',14492:'08_BattleHUD',14493:'08_BattleHUD',14494:'08_BattleHUD',14495:'08_BattleHUD',14496:'10_TroopSummon',14498:'11_BattleAnnouncements',14499:'11_BattleAnnouncements',14500:'11_BattleAnnouncements',14501:'12_Results',14502:'12_Results',14503:'12_Results',14504:'03_ModeSelect'}
    refs=[]
    hashes={}
    for path in sorted((ROOT/'zhibo-pic').glob('*.png')):
        num=int(path.stem.split('_')[-2]); digest=hashlib.sha256(path.read_bytes()).hexdigest()
        refs.append({'reference':path.name,'screen':mapping[num],'supplemental_screen':'07_LiveGuide' if num in [14486,14489,14490,14492,14493,14494,14495] else None,
                     'sha256':digest,'identical_to':hashes.get(digest),'note':'同一界面的镜头或贴纸位置变化归在同一文件夹；贴纸另拆为 07_LiveGuide。'})
        hashes.setdefault(digest,path.name)
    dump(OUT/'UI_Reference_Map.json',refs)
    qa=[]
    for folder in folders:
        folderpages=[p for p in pages if p['folder']==folder]
        ids=sorted({k for p in folderpages for k in p['assets']})
        manifest={'screen':folder,'title':folderpages[0]['title'],'reference_resolution':[W,H],
                  'assets':[dict(CAT[k],file='Sprites/'+k+'.png') for k in ids],
                  'previews':[p['preview'].split('/')[-1] for p in folderpages],
                  'reference_screenshots':[r['reference'] for r in refs if r['screen']==folder or r['supplemental_screen']==folder]}
        dump(OUT/folder/'manifest.json',manifest)
        (OUT/folder/'README.md').write_text(f"# {manifest['title']}\n\nSprites/ 是独立 PNG 切图；Preview*.png 是 1080×1920 组装示意，不能作为整页 Sprite 使用。layout*.json 记录绘制顺序、坐标、尺寸、文案及状态。\n\n保持图片比例，Sprite (2D and UI) / Single / Full Rect / Bilinear / Clamp / 无 mipmap；已提供对应 .meta。填充条和轨道可水平拉伸，其余使用 Simple，不默认九宫格。动态文字、数值、排行榜头像由运行时填入；预览字体使用系统微软雅黑，未分发字体文件。\n\n按 Alpha > 64 的包围盒裁边，保留原有抗锯齿 Alpha；头像框和遮罩使用共同裁切范围保证对齐。详见 manifest.json 中来源及裁切坐标。\n\n场景背景仅用于组装预览，实际使用 Unity 战场摄像机。此批为 UI 美术设计与资源，不代表功能已接入。\n",encoding='utf-8')
        for key in ids:
            path=OUT/folder/'Sprites'/f'{key}.png'
            im=Image.open(path)
            a=im.getchannel('A');b=a.point(lambda p:255 if p>64 else 0).getbbox()
            tight=b==(0,0,im.width,im.height)
            opaque=key in ['bg_loading','health_fill_blue','health_fill_red','health_track']
            passed=im.mode=='RGBA' and (a.getextrema()[0]==0 or opaque) and (tight or key=='avatar_mask')
            qa.append({'path':path.relative_to(OUT).as_posix(),'size':list(im.size),'alpha':list(a.getextrema()),'tight_bbox':tight,'pass':passed,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    dump(OUT/'UI_Asset_QA.json',{'asset_files':len(qa),'unique_assets':len({q['sha256'] for q in qa}), 'checks':qa,
                               'pass':all(q['pass'] for q in qa),'unity_runtime_verified':False})
    used=sorted({k for p in pages for k in p['assets']})
    dump(WORK/'final_generation_prompts.json',[r for r in records if r['id'] in used or r['id']=='bg_battle_preview'])
    dump(OUT/'UI_Delivery_Index.json',{'screens':pages,'page_folders':folders,'asset_files':len(qa),'unique_assets':len(used),'engine':'Unity','runtime_integration':'not part of art delivery','generation_mode':'builtin imagegen'})
    cards=''.join(f'<a class="card" href="{html.escape(p["preview"])}"><img loading="lazy" src="{html.escape(p["preview"])}"><span>{html.escape(p["folder"])} · {html.escape(p["title"])}<small>{html.escape(p["variant"])}</small></span></a>' for p in pages)
    doc='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>天下是我的 · UI 美术</title><style>body{margin:0;background:#102b35;color:#f7ebd2;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}header{padding:56px 5vw 32px;border-bottom:1px solid #62736b}h1{font-size:36px;margin:0 0 12px}p{max-width:960px;color:#c9d3c9}nav{margin-top:24px;display:flex;gap:22px;flex-wrap:wrap}a{color:#edca83;text-decoration:none}main{padding:40px 5vw;display:grid;grid-template-columns:repeat(auto-fill,minmax(250px,1fr));gap:28px}.card{background:#193a45;border:1px solid #56675d;border-radius:12px;overflow:hidden;transition:transform .2s}.card:hover{transform:translateY(-5px)}.card img{width:100%;display:block}.card span{display:block;padding:15px 18px;font-size:15px}.card small{display:block;color:#b4c8c7}footer{padding:25px 5vw;color:#adbfba}</style><header><h1>天下是我的 · 国风战旗 UI</h1><p>红蓝军阵 / 古金云纹 / 绢纸卷轴。12 类界面，含贴纸、战场播报及结算状态。点击查看原尺寸组装图；每页文件夹内 Sprites 为可独立使用的透明 PNG。</p><nav><a href="UI_Art_README.md">资源说明</a><a href="UI_Reference_Map.json">参考截图对应表</a><a href="UI_Asset_QA.json">资源检查结果</a></nav></header><main>'''+cards+'''</main><footer>1080 × 1920 设计基准 · 所有数值和玩家名称为布局样例 · 战场背景仅用于美术预览 · 尚未接入 Unity 界面逻辑</footer></html>'''
    doc=doc.replace('<nav>','<nav><a href="UI_Preview_01-06.jpg">01–06 界面总览</a><a href="UI_Preview_07-12.jpg">07–12 界面总览</a>')
    (OUT/'UI_Art_Index.html').write_text(doc,encoding='utf-8')
    (OUT/'UI_Art_README.md').write_text(f'''# 天下是我的 UI 美术资源\n\n入口：UI_Art_Index.html。共 {len(folders)} 类界面、{len(pages)} 张组装预览、{len(used)} 种独立资源、{len(qa)} 个按界面分发的 PNG。现有 UI_Clean_5_Styles 保持原状。\n\n每个界面目录包含 Sprites/、Preview*.png、layout*.json、manifest.json、README.md。同页状态共享切图；跨页复制共有切图，确保每个界面文件夹自包含。头像等运行时数据不烘焙。静态游戏标题是唯一含文字的美术 PNG。原生 Alpha 透明输出，不以棋盘格充当透明。\n\n使用 UI风格.jpg 的国风战旗方向；zhibo-pic 提供功能与布局参考。参考游戏的名称、橙方配色不沿用。重复截图保留映射，镜头及贴纸位置变化不另生成冗余页面。新增美术使用内置 imagegen，原有战旗组件经裁边复用。\n\n## 导入与布局\n\nSprite (2D and UI)、Single、Full Rect、Bilinear、Clamp、关闭 mipmap、保留 Alpha，.meta 已配置。基准分辨率 1080×1920。layout.json 坐标原点在左上，按 layers 顺序叠加；文本节点独立实现，图片 fit=contain 保持比例。Unity 顶左锚点下 anchoredPosition=(x,-y)，sizeDelta=(w,h)；非顶左 pivot 需按实际 pivot 换算。填充条单独水平伸缩，不把整条装饰边框当填充条裁切。\n\n按钮按压与禁用由运行时颜色/透明度表达；无需重复导出相同纹理。头像框与头像遮罩共同裁边，叠放时使用同一 Rect。各个弹窗的关闭、确认，模式选择、设置勾选、音量滑杆、排行和结算状态均在预览及布局数据中给出。\n\n## 检查与范围\n\nUI_Asset_QA.json 记录尺寸、RGBA、Alpha 极值、裁边和哈希。还进行了实际透明背景合成和组装预览检查。运行时尚未接入或验证，不包含游戏世界模型、角色动画及粒子特效。预览中的场景插画不是可玩的地图；实际接入应显示现有战场摄像机。数值、模式规则和礼物映射是参考布局示例，需要绑定项目实际配置。\n\n可重建脚本及生成提示词位于 work/ui_art_20260922/。''',encoding='utf-8')
    # Compact visual proof uses actual output textures on light/dark backgrounds.
    tilew,tileh=280,230
    proof=Image.new('RGB',(tilew*4,tileh*math.ceil(len(used)/4)), '#cbd2d2')
    d=ImageDraw.Draw(proof);f=ImageFont.truetype(FONT,15)
    for n,key in enumerate(used):
        x,y=n%4*tilew,n//4*tileh
        d.rectangle((x,y,x+tilew-1,y+tileh-1),fill='#17343f' if n%2 else '#ece7da')
        im=IMAGES[key].copy();im.thumbnail((250,185),Image.Resampling.LANCZOS)
        proof.paste(im,(x+(tilew-im.width)//2,y+12+(185-im.height)//2),im)
        d.text((x+8,y+202),key,font=f,fill='#ead5a7' if n%2 else '#223847')
    proof.save(PROOF/'assets_light_dark.jpg',quality=92)
    overview=Image.new('RGB',(270*4,515*math.ceil(len(pages)/4)),'#102b35')
    d=ImageDraw.Draw(overview)
    for n,p in enumerate(pages):
        im=Image.open(OUT/p['preview']);im.thumbnail((258,459))
        x,y=n%4*270,n//4*515
        overview.paste(im,(x+6,y+6));d.text((x+8,y+473),p['folder'][:2]+' '+p['title'],font=f,fill='#ead5a7')
        d.text((x+8,y+493),p['variant'],font=ImageFont.truetype(FONT,12),fill='#b5c6c5')
    overview.save(OUT/'UI_Overview.jpg',quality=93)
    # Two readable six-screen contact sheets, one assembled preview per folder.
    primary=[next(p for p in pages if p['folder']==folder and p['variant']==('victory_blue' if folder=='11_BattleAnnouncements' else 'default')) for folder in folders]
    for start in (0,6):
        board=Image.new('RGB',(1620,2036),'#102b35')
        bd=ImageDraw.Draw(board)
        for j,p in enumerate(primary[start:start+6]):
            im=Image.open(OUT/p['preview']).resize((522,928),Image.Resampling.LANCZOS)
            x,y=j%3*540,j//3*1018
            board.paste(im,(x+9,y+9))
            bd.text((x+16,y+954),p['folder'][:2]+'  '+p['title'],font=ImageFont.truetype(FONT,26),fill='#f4ddac')
            bd.text((x+16,y+990),'Sprites → Preview.png',font=ImageFont.truetype(FONT,16),fill='#adbfba')
        board.save(OUT/f'UI_Preview_{start+1:02d}-{start+6:02d}.jpg',quality=94)
    print(json.dumps({'folders':len(folders),'previews':len(pages),'unique':len(used),'asset_files':len(qa),'qa_pass':all(q['pass'] for q in qa)},ensure_ascii=True))

if __name__ == '__main__':
    deliver(build())
