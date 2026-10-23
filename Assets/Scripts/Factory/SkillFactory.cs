using UnityEngine;

/// <summary>根据 SkillCfg.skillType 创建运行时技能逻辑。</summary>
public static class SkillFactory
{
    public const string NormalCloseSkillType = "normalClose";
    public const string RemoteSkillType = "remote";
    public const string CallFriendSkillType = "callFriend";
    public const string TransformToCityDoorSkillType = "transformToCityDoor";
    public const string TriggerBeforeDieSkillType = "triggerBeforeDie";
    public const string KillNowSkillType = "killNow";
    public const string PushSkillType = "push";
    public const string CreateWallSkillType = "createWall";

    public static SkillLogicBase CreateSkillLogic(UnitLogicBase unitLogic, SkillCfg skillCfg)
    {
        if (unitLogic == null || skillCfg == null)
        {
            Debug.LogError("创建技能逻辑时单位或技能配置为空。");
            return null;
        }

        switch (skillCfg.skillType)
        {
            case CallFriendSkillType:
                return new CallFriendSkill(unitLogic, skillCfg);
            case TransformToCityDoorSkillType:
                return new TransformToCityDoorSkill(unitLogic, skillCfg);
            case TriggerBeforeDieSkillType:
                return new TriggerBeforeDieSkill(unitLogic, skillCfg);
            case KillNowSkillType:
                return new KillNowSkill(unitLogic, skillCfg);
            case PushSkillType:
                return new PushSkill(unitLogic, skillCfg);
            case CreateWallSkillType:
                return new CreateWallSkill(unitLogic, skillCfg);
            case NormalCloseSkillType:
                return new SkillCloseSkill(unitLogic, skillCfg);
            case RemoteSkillType:
                return new SkillRemoteSkill(unitLogic, skillCfg);
            default:
                Debug.LogError($"Unknown skill type '{skillCfg.skillType}' for SkillId={skillCfg.id}.");
                return null;
        }
    }
}
