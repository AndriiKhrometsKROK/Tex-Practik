// Описує дві бойові лінії та єдині маршрути, якими рухаються союзні й ворожі юніти.
using UnityEngine;

public enum BattleLane
{
    Upper,
    Lower
}

public static class BattleLaneUtility
{
    public const float AttackX = -3.1f;
    public const float DefenseX = 3.1f;

    private static readonly Vector2[] AttackPath =
    {
        new Vector2(-2.25f, 4.2f),
        new Vector2(-4.05f, 4.2f),
        new Vector2(-4.05f, 2.45f),
        new Vector2(-3.05f, 2.45f),
        new Vector2(-3.05f, 0.75f),
        new Vector2(-4.35f, 0.75f),
        new Vector2(-4.35f, -1.05f),
        new Vector2(-3.1f, -1.05f),
        new Vector2(-3.1f, -2.72f),
        new Vector2(-2.15f, -4.05f)
    };

    private static readonly Vector2[] DefensePath =
    {
        new Vector2(2.25f, 4.2f),
        new Vector2(4.05f, 4.2f),
        new Vector2(4.05f, 2.45f),
        new Vector2(3.05f, 2.45f),
        new Vector2(3.05f, 0.75f),
        new Vector2(4.35f, 0.75f),
        new Vector2(4.35f, -1.05f),
        new Vector2(3.1f, -1.05f),
        new Vector2(3.1f, -2.72f),
        new Vector2(2.15f, -4.05f)
    };

    public static int PathLength => AttackPath.Length;

    public static float GetX(BattleLane lane)
    {
        return lane == BattleLane.Upper ? AttackX : DefenseX;
    }

    public static Vector2[] GetPath(BattleLane lane)
    {
        return lane == BattleLane.Upper ? AttackPath : DefensePath;
    }

    public static Vector2 GetPathPoint(BattleLane lane, int index)
    {
        Vector2[] path = GetPath(lane);
        return path[Mathf.Clamp(index, 0, path.Length - 1)];
    }

    public static string GetUkrainianName(BattleLane lane)
    {
        return lane == BattleLane.Upper ? "Атака" : "Захист";
    }
}
