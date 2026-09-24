using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "Database", menuName = "Scriptable Objects/Database")]
public class Database : ScriptableObject
{
    /// <summary>基礎クリティカル率 (0.1 = 10%)</summary>
    public float BaseCriticalChance = 0.1f;
    /// <summary>基礎クリティカルダメージ倍率 (0.75 = +75%)</summary>
    public float BaseCriticalDamageRate = 0.75f;

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
