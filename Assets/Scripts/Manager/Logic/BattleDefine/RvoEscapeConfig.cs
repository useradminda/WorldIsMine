public static class RvoEscapeConfig
{
    public static float CheckWindow = 1f;
    public static float MaxDisplacement = 0.1f;
    public static float EscapeDuration = 3f;
    public static float EscapeCooldown = 1f;
    // 旧值 5 会让侧移方向压过目标方向，容易出现横移或看起来反向移动。
    // public static float LateralWeight = 5f;
    public static float LateralWeight = 1.5f;
    public static float CombatRadiusScale = 0.2f;
}
