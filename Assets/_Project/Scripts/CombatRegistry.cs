// Підтримує швидкий реєстр активних союзників і ворогів, щоб бойові системи не шукали об'єкти по всій сцені.
using System.Collections.Generic;

public enum UnitBrainState
{
    Move,
    Search,
    Attack,
    Wait,
    ChangeLane,
    ReturnToBase,
    Dead
}

public static class CombatRegistry
{
    private static readonly HashSet<EnemyAI> Enemies = new HashSet<EnemyAI>();
    private static readonly HashSet<AllyController> Allies = new HashSet<AllyController>();
    private static readonly HashSet<TowerController> Towers = new HashSet<TowerController>();

    public static IEnumerable<EnemyAI> ActiveEnemies => Enemies;
    public static IEnumerable<AllyController> ActiveAllies => Allies;
    public static IEnumerable<TowerController> ActiveTowers => Towers;

    public static void Register(EnemyAI enemy)
    {
        if (enemy != null) Enemies.Add(enemy);
    }

    public static void Register(AllyController ally)
    {
        if (ally != null) Allies.Add(ally);
    }

    public static void Register(TowerController tower)
    {
        if (tower != null) Towers.Add(tower);
    }

    public static void Unregister(EnemyAI enemy)
    {
        if (enemy != null) Enemies.Remove(enemy);
    }

    public static void Unregister(AllyController ally)
    {
        if (ally != null) Allies.Remove(ally);
    }

    public static void Unregister(TowerController tower)
    {
        if (tower != null) Towers.Remove(tower);
    }

    public static void RemoveInvalidEntries()
    {
        Enemies.RemoveWhere(enemy => enemy == null);
        Allies.RemoveWhere(ally => ally == null);
        Towers.RemoveWhere(tower => tower == null);
    }
}
