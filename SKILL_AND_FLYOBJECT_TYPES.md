# 技能类型与飞行器类型说明

本文档以当前代码和 `Assets/streamingAssets/JsonData` 中的配置为准，记录技能表 `skillType`、飞行器表 `flyType` 的运行时分支、参数格式和注意事项。

## 1. 配置关系

技能配置来源于 `SkillCfg`，常用字段如下：

| 字段 | 作用 |
| --- | --- |
| `skillType` | 决定由 [SkillFactory.cs](Assets/Scripts/Factory/SkillFactory.cs) 创建哪一种技能逻辑 |
| `skillParam` | 技能类型自己的字符串参数；不同类型的格式不同 |
| `flyObjectId` | 需要飞行器时，指向 `FlyObjectCfg` 的 ID |
| `searchRange`、`skillArea` | 目标搜索范围、范围伤害半径；具体使用由技能或飞行器决定 |
| `skillDamageRate` | 根据施法者攻击力计算本次技能伤害快照 |
| `cd` | 技能冷却时间 |
| `buffId` | 命中后追加的 Buff ID 数组 |
| `needTarget` | 目标要求：`0` 不强制目标，`1` 敌方目标，`2` 施法者自己，`3` 敌方英雄 |

飞行器配置来源于 `FlyObjectCfg`，常用字段如下：

| 字段 | 作用 |
| --- | --- |
| `flyType` | 决定由 [FlyObjectFactory.cs](Assets/Scripts/Factory/FlyObjectFactory.cs) 创建哪一种飞行器逻辑 |
| `speed` | 飞行速度或运动速度 |
| `liveTime` | 生命周期；区域类飞行器也用它作为持续时间 |
| `damClipTime` | 区域搜索或伤害触发间隔 |
| `triggerDamageCount` | 最多触发伤害次数；负数表示不限制次数 |
| `prefab` | 飞行器表现预制体路径 |
| `arrivePrefab` | 到达或命中位置创建的特效路径 |
| `flyParam` | 飞行器类型自己的字符串参数 |

通常的执行链是：角色进入技能状态，技能逻辑执行 `SkillDoEffect`，技能逻辑直接结算伤害、添加 Buff，或通过 `FlyObjectFactory` 创建飞行器。飞行器在 `FlyObjectUpdate` / `FlyObjectLateUpdate` 中运动、异步取地图搜索结果并结算效果。

## 2. `skillType` 技能类型

所有已接入的类型都在 [SkillFactory.cs](Assets/Scripts/Factory/SkillFactory.cs) 中有明确分支。配置了未知字符串时会记录错误并返回空技能逻辑。

| `skillType` | 运行时逻辑 | 行为与参数 | 关联脚本 |
| --- | --- | --- | --- |
| `normalClose` | `SkillCloseSkill` | 普通近战技能的默认逻辑，继承基类的单目标伤害、范围追加伤害和 Buff 处理。`skillParam` 没有特殊格式。 | [SkillCloseSkill.cs](Assets/Scripts/Game/Logic/Skill/SkillCloseSkill.cs)、[SkillLogicBase.cs](Assets/Scripts/Game/Logic/Skill/SkillLogicBase.cs) |
| `remote` | `SkillRemoteSkill` | 需要目标时创建 `flyObjectId` 指定的飞行器；目标为自身 (`needTarget=2`) 时直接给施法者添加 Buff，不创建飞行器。 | [SkillRemoteSkill.cs](Assets/Scripts/Game/Logic/Skill/SkillRemoteSkill.cs) |
| `callFriend` | `CallFriendSkill` | 召唤援军。`skillParam` 推荐写成 `红方士兵配置ID,蓝方士兵配置ID`，例如 `101,201`；代码也兼容部分分隔符。每次固定召唤 20 个单位。 | [CallFriendSkill.cs](Assets/Scripts/Game/Logic/Skill/CallFriendSkill.cs) |
| `transformToCityDoor` | `TransformToCityDoorSkill` | 搜索结果中的敌人传送到其所属阵营城门，并清理不适合继续保留的搜索目标。通常使用 `skillArea` 作为范围。`skillParam` 没有特殊格式。 | [TransformToCityDoorSkill.cs](Assets/Scripts/Game/Logic/Skill/TransformToCityDoorSkill.cs) |
| `triggerBeforeDie` | `TriggerBeforeDieSkill` | 死亡前触发型技能。冷却满足时调用自身技能效果，当前实现主要给施法者添加 `buffId`。 | [TriggerBeforeDieSkill.cs](Assets/Scripts/Game/Logic/Skill/TriggerBeforeDieSkill.cs) |
| `killNow` | `KillNowSkill` | 对满足条件的敌方英雄执行立即击杀。`skillParam` 可配置生命值阈值，使用 `0~1` 小数；未能解析时使用默认阈值 `0.15`。 | [KillNowSkill.cs](Assets/Scripts/Game/Logic/Skill/KillNowSkill.cs) |
| `push` | `PushSkill` | 锁定敌人方向并进入 `PushState`，角色按冲撞速度移动；伤害由关联的 `aroundArea` 飞行器持续处理。技能的冲撞持续时间来自关联飞行器的 `liveTime`，伤害间隔来自该飞行器的 `damClipTime`。`skillParam` 当前不承担这两个参数。 | [PushSkill.cs](Assets/Scripts/Game/Logic/Skill/PushSkill.cs)、[AroundAreaFlyObject.cs](Assets/Scripts/Game/Logic/Skill/FlyObject/AroundAreaFlyObject.cs) |
| `createWall` | `CreateWallSkill` | 在施法者前方创建 RVO 动态矩形障碍。技能使用自身配置的 `flyObjectId` 查找飞行器，尺寸参数从关联飞行器的 `flyParam` 读取，格式为 `长,宽,正前方距离`，例如 `10,5,5`。 | [CreateWallSkill.cs](Assets/Scripts/Game/Logic/Skill/CreateWallSkill.cs)、[CreateWallFlyObject.cs](Assets/Scripts/Game/Logic/Skill/FlyObject/CreateWallFlyObject.cs) |
| `rectSkill` | `RectSkill` | 释放瞬间进行一次前方矩形判定，不创建飞行器。`skillParam` 格式为 `矩形宽度,前方长度`，例如 `30,5`。技能会直接遍历敌方阵营，再用施法方向判断矩形范围，并按技能表伤害和 Buff 结算。 | [RectSkill.cs](Assets/Scripts/Game/Logic/Skill/RectSkill.cs) |

### 技能表中的已知示例

当前 JSON 中的 `createWall` 技能通过自身 `flyObjectId` 关联 `flyType=createWall` 的飞行器，`rectSkill` 不使用飞行器，`push` 使用 `flyObjectId=13`，`callFriend` 使用双方士兵 ID 参数，`killNow` 使用阈值参数。技能 ID 会随着 Excel 导出变化，新增配置时应以 `skillType` 和关联 ID 为准。

## 3. `flyType` 飞行器类型

以下类型由 [FlyObjectFactory.cs](Assets/Scripts/Factory/FlyObjectFactory.cs) 创建。飞行器共用 [FlyObjectLogicBase.cs](Assets/Scripts/Game/Logic/Skill/FlyObject/FlyObjectLogicBase.cs) 保存施法者、目标、技能、伤害快照和表现对象。

| `flyType` | 运行时逻辑 | 行为与主要配置 |
| --- | --- | --- |
| `arrow` | `ArrowFlyObject` | 单目标直线箭。由 `ProjectileJobManager` 驱动从起点飞向指定目标，到达后对目标结算一次伤害并回收。主要使用 `speed`、`prefab`、`arrivePrefab`。 |
| `area` | `AreaFlyObject` | 固定落点的持续范围伤害。按 `damClipTime` 发起搜索，使用技能表 `skillArea` 作为范围，持续 `liveTime`，受 `triggerDamageCount` 限制。 |
| `stone` | `StoneFlyObject` | 飞到目标位置后，在落点异步搜索并对范围内敌人造成一次伤害，适合投石、落点爆炸类效果。范围来自技能表 `skillArea`。 |
| `line` | `LineFlyObject` | 从起点沿目标方向持续直线运动。飞行过程中按 `damClipTime` 搜索当前位置周围的敌人并造成范围伤害，运动速度用 `speed`，生命周期用 `liveTime`。 |
| `jumpArrow` | `JumpArrowFlyObject` | 追踪式跳跃箭。飞行时按 `searchRange` 搜索未攻击过的敌人，命中后可继续寻找下一个目标，次数由 `triggerDamageCount` 控制，目标失效时会重新搜索。 |
| `globalStone` | `GlobalStoneFlyObject` | 全局投石的一发抛物线飞行器。到达指定位置后按技能表 `skillArea` 搜索并造成一次范围伤害，支持额外发射延迟和飞行时间设置。全局投石的释放流程由战场投石控制器负责触发。 |
| `globalArrow` | `GlobalArrowFlyObject` | 全局箭或全局技能投射物。飞到指定落点后进行一次范围搜索和伤害；通过 `DamageContext` 可让同一轮全局技能避免重复伤害同一单位。当前代码的伤害规则由该类内部处理。 |
| `areaRandom` | `AreaRandomFlyObject` | 持续范围伤害，但每次触发时会从搜索结果中取随机单位位置作为新的表现位置。`damClipTime`、`liveTime`、`triggerDamageCount` 与 `skillArea` 控制触发。 |
| `aroundArea` | `AroundAreaFlyObject` | 跟随施法者当前位置的持续范围飞行器。每次按 `damClipTime` 搜索施法者周围敌人并结算伤害，`liveTime` 到期或施法者死亡后销毁。冲撞技能使用它处理伤害层。 |
| `createWall` | `CreateWallFlyObject` | 创建临时 RVO 动态障碍和对应表现对象。它读取自身 `flyParam` 的 `长,宽,正前方距离`，生命周期来自 `liveTime`，结束时移除 RVO 障碍并回收表现。 |
| `pointToPoint` | `PointToPointFlyObject` | 对同一目标创建多发追踪飞行器，每发使用随机贝塞尔控制点。`flyParam` 格式为 `发数,发射间隔`，例如 `3,0.1`；目标死亡、为空或 UID 不匹配时整组销毁。 |
| `blackHall` | `BlackHallFlyObject` | 跟随施法者的范围牵引效果。按 `damClipTime` 搜索技能范围内敌人，调用 RVO 移动接口把敌人拉向施法者。`flyParam` 是牵引倍率，例如 `1`；持续时间来自 `liveTime`。 |
| `translateArea` | `TranslateAreaFlyObject` | 持续搜索施法者周围敌人，将敌人传送到各自阵营城门。范围使用技能表 `skillArea`，生命周期使用 `liveTime`；传送后的状态由单位自身管理。 |

## 4. 特殊分支和容易混淆的地方

1. 城墙飞行器通过 `flyType=createWall` 进入 `CreateWallFlyObject`，不依赖固定的飞行器 ID。城墙的尺寸和前方距离配置在该飞行器的 `flyParam` 中。
2. `push` 技能的移动状态和伤害表现分开。`PushSkill` 负责进入冲撞状态和创建 `aroundArea`，`PushState` 负责角色移动，`AroundAreaFlyObject` 负责伤害搜索与伤害间隔。
3. `rectSkill` 是技能逻辑里的瞬发矩形判定，不需要 `FlyObjectCfg`。它不会沿飞行路径持续生成矩形。
4. `remote` 是通用远程入口，最终行为由 `flyObjectId` 指向的飞行器决定。新增远程技能时，先确认飞行器 ID 存在、`flyType` 已被工厂接入、参数格式正确。
5. 未知 `skillType` 会返回空技能逻辑并记录错误；未知 `flyType` 会回退到基础飞行器逻辑，这种回退对象不会自动提供运动或伤害行为。
6. 全局投石和全局箭可以由战场系统直接创建飞行器，不能只通过普通角色技能状态来推断它们的调用入口。配置表负责伤害技能、落点和飞行表现，战场控制器负责触发时机。

## 5. 新增类型时的检查清单

新增 `skillType` 时，需要同时完成技能逻辑类、[SkillFactory.cs](Assets/Scripts/Factory/SkillFactory.cs) 分支、技能参数格式说明和至少一条技能表配置。新增 `flyType` 时，需要完成飞行器逻辑类、[FlyObjectFactory.cs](Assets/Scripts/Factory/FlyObjectFactory.cs) 分支、`FlyObjectCfg` 参数约定和对应预制体路径配置。

修改参数格式后，应同步更新 Excel、导出的 JSON 和本文档。若一个技能同时依赖 `skillParam` 与 `flyParam`，文档中应分别写明两者的职责，避免把持续时间、伤害间隔、范围等参数配置到错误的表里。

## 6. 主要源码入口

- [SkillFactory.cs](Assets/Scripts/Factory/SkillFactory.cs)
- [FlyObjectFactory.cs](Assets/Scripts/Factory/FlyObjectFactory.cs)
- [SkillLogicBase.cs](Assets/Scripts/Game/Logic/Skill/SkillLogicBase.cs)
- [FlyObjectLogicBase.cs](Assets/Scripts/Game/Logic/Skill/FlyObject/FlyObjectLogicBase.cs)
- [SkillCfg.cs](Assets/Scripts/ExcelExportCs/SkillCfg.cs)
- [FlyObjectCfg.cs](Assets/Scripts/ExcelExportCs/FlyObjectCfg.cs)
- [技能配置 JSON](Assets/streamingAssets/JsonData/SkillCfg.json)
- [飞行器配置 JSON](Assets/streamingAssets/JsonData/FlyObjectCfg.json)
