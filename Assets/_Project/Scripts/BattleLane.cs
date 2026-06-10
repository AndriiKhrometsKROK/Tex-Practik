public enum BattleLane
{
    Upper,
    Lower
}

public static class BattleLaneUtility
{
    public const float AttackX = -3.1f;
    public const float DefenseX = 3.1f;

    public static float GetX(BattleLane lane)
    {
        return lane == BattleLane.Upper ? AttackX : DefenseX;
    }

    public static string GetUkrainianName(BattleLane lane)
    {
        return lane == BattleLane.Upper ? "Атака" : "Захист";
    }
}
