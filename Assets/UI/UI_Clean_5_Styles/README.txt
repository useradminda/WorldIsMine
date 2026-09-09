五套无字 UI 开发资源 / V2

本包替代之前带文字的截图切片包。
五个风格文件夹，命名对应：国风战旗、玉石鎏金、潮玩漫画、极简竞技、暗金史诗。
每套 20 张独立 PNG：16 个风格部件 + 两色纯血量填充 + 血槽底色 + 头像遮罩。
所有 PNG 无文字、无数字、无战场背景，外部透明。头像框和血条框内部镂空。
Preview.jpg 仅用于浏览，会显示英文文件名和棋盘格；不要当作游戏贴图使用。

与效果图关系：这些是按五套视觉风格重新制作的无字组件，不是从原图逐像素擦除文字，因此形状与装饰有差异。

程序叠加内容：阵营名、玩家名、计时器、兵力、排行榜标题、排名数字、贡献值、加入指令及事件文字。奖章为空白，排名数字需另加 Text/TMP。

血条层次：health_track（底）→ health_fill_blue/red（中，Horizontal Filled）→ health_frame（顶）。根据 health_frame 实际孔洞放置并裁剪填充。health_strip_*_decorated 为带端帽的展示备选，不建议直接作为动态填充条。
头像层次：avatar_mask 做 Mask（隐藏 Mask 图形）→ 子节点放玩家头像；avatar_frame 最上层覆盖。具体布局需在 Unity 项目中组装。

Unity 导入：Sprite (2D and UI)、Single、开启 Alpha Is Transparency、关闭 Mip Maps。预览时先使用无压缩检查边缘。装饰面板保持比例，不要未经检查设置 Sliced。大尺寸面板需参考 manifest 的原生像素尺寸，不要盲目放大。资源未在你的 Unity 工程中进行运行验证。

每套文件夹的 manifest.json 列出精确尺寸和用途。本包不含字体、动态文字、战场、预制体或完整游戏逻辑。
