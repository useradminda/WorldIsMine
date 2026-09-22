# 对局结算

Sprites/ 是独立 PNG 切图；Preview*.png 是 1080×1920 组装示意，不能作为整页 Sprite 使用。layout*.json 记录绘制顺序、坐标、尺寸、文案及状态。

保持图片比例，Sprite (2D and UI) / Single / Full Rect / Bilinear / Clamp / 无 mipmap；已提供对应 .meta。填充条和轨道可水平拉伸，其余使用 Simple，不默认九宫格。动态文字、数值、排行榜头像由运行时填入；预览字体使用系统微软雅黑，未分发字体文件。

按 Alpha > 64 的包围盒裁边，保留原有抗锯齿 Alpha；头像框和遮罩使用共同裁切范围保证对齐。详见 manifest.json 中来源及裁切坐标。

场景背景仅用于组装预览，实际使用 Unity 战场摄像机。此批为 UI 美术设计与资源，不代表功能已接入。
