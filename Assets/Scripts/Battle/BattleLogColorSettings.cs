using System;
using UnityEngine;

/// <summary>
/// BattleLoggerが使用するログの色設定。インスペクタから編集する
/// </summary>
[Serializable]
public class BattleLogColorSettings
{
    [Header("キャラクター")]
    public Color PlayerFront = new Color(0.4f, 0.6f, 1f);
    public Color PlayerBack = new Color(0.7f, 0.5f, 1f);
    public Color EnemyFront = new Color(1f, 0.45f, 0.7f);
    public Color EnemyBack = new Color(0.85f, 0.75f, 0.5f);

    [Header("効果")]
    public Color Damage = new Color(1f, 0.4f, 0.4f);
    public Color Critical = new Color(1f, 0.6f, 0.1f);
    public Color Heal = new Color(0.4f, 1f, 0.4f);
    public Color Shield = new Color(0.4f, 0.8f, 1f);
    public Color Kill = new Color(1f, 0.85f, 0.2f);

    [Header("状態異常")]
    public Color Buff = new Color(0.7f, 1f, 0.3f);
    public Color Debuff = new Color(0.85f, 0.35f, 0.6f);

    public Color GetCharacterColor(CharacterModel c) => c.IsPlayer
        ? (c.Position == 0 ? PlayerFront : PlayerBack)
        : (c.Position == 0 ? EnemyFront : EnemyBack);
}
