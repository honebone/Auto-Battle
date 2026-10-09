using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "Database", menuName = "Scriptable Objects/Database")]
public class Database : ScriptableObject
{
    public int DefenseScale = 100;
    /// <summary>基礎クリティカル率 (0.1 = 10%)</summary>
    public float BaseCriticalChance = 0.1f;
    /// <summary>基礎クリティカルダメージ倍率 (0.75 = +75%)</summary>
    public float BaseCriticalDamageRate = 0.75f;

    public float ShieldLossOvertime = 0.05f;

    public int BattleTimeLimit = 45;

    /// <summary>1キャラあたりのアイテム装備上限</summary>
    public int MaxItemSlots = 4;

    public ColorRef ColorRef;

    private const string ResourcePath = "Database";
    private static Database _instance;

    public static Database Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Resources.Load<Database>(ResourcePath);

            if (_instance == null)
            {
                Debug.LogError($"[Costants] Resources/{ResourcePath}.asset が存在しません");
            }

            return _instance;
        }
    }
}

[System.Serializable]
public class ColorRef
{
    public Color HP;
    public Color AttackPower;
    public Color MagicPownr;
    public Color AttackSpeed;
    public Color CastSpeed;
    public Color CriticalChance;
    public Color CriticalDamage;
    public Color Drain;

    public Color Damage;
    public Color Heal;
    public Color Critical;
    public Color Shield;
    public Color AP;
}