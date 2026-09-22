# WorldIsMine UI 美术制作

任务：以 `Assets/UI/UI风格.jpg` 为唯一风格基准，按 `zhibo-pic` 的界面功能制作独立 UI PNG，并按页面放入 `Assets/UI`。本任务只制作 UI 美术及组装预览，不修改游戏玩法或场景。

风格：三国战旗、海军蓝 / 朱红阵营色、克制的旧金云纹边饰、米色绢纸、深色漆木、左上光源。保持独立 PNG 无动态文字和数字。

参考分辨率：1080×1920；预览中文沿用现有项目。预览文字仅作布局示意，不烘焙进可复用 PNG。UI 整图等比缩放，不默认九宫格。

页面：01 Loading；02 Controls；03 ModeSelect；04 Lobby；05 Leaderboard；06 Settings；07 LiveGuide；08 BattleHUD；09 HeroSummon；10 TroopSummon；11 BattleAnnouncements；12 Results。

原始截图中的橙方统一改为红方；同一页面的镜头、贴纸位置及重复图片记录到 reference_map.json，不重复制造相同美术。

图片生成使用内置 imagegen。新独立切图优先原生透明 PNG，保留 Alpha；按 Alpha>64 紧裁。美术由图像工具生成，Python 只负责裁边、尺寸检查、分发、文字覆盖和组装预览，不绘制代用美术。

状态：美术与切图交付完成。12 类界面、17 张组装预览、46 种独立资源、184 个按页面分发的 PNG，均已写入 Assets/UI。

预览入口：Assets/UI/UI_Art_Index.html。六屏总览：UI_Preview_01-06.jpg 和 UI_Preview_07-12.jpg。每页 Sprites/ 为独立切图，Preview*.png 为实际切图拼合的预览。

验证：UI_Validation.json 通过，检查了 184 个资源文件、12 个页面目录、17 张预览和 21 张参考截图映射，无文字互相重叠或画布越界。UI_Asset_QA.json 的 RGBA、透明通道和裁边检查通过。已查看浅色/深色底透明合成、总览和关键页面大图，修正进度条对齐、列表边界、贴纸页脚和高清横幅。

范围：完成 UI 美术资源与组装设计；没有修改游戏脚本、场景或原有切图目录。Unity 运行效果未验收。提示词 final_generation_prompts.json，原始生成图 source_art/，布局脚本 build_delivery.py，验证脚本 validate_delivery.py。
