using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// BattleModelのイベントを購読し、戦闘の経過をConsoleへ出力するデバッグ用ロガー
/// </summary>
public class BattleLogger : IDisposable
{
    private readonly BattleModel _battle;
    private readonly BattleLogColorSettings _colors;

    /// <param name="colors">ログの色設定。nullなら既定の色を使用</param>
    public BattleLogger(BattleModel battle, BattleLogColorSettings colors = null)
    {
        _battle = battle;
        _colors = colors ?? new BattleLogColorSettings();
        _battle.BattleStarted += OnBattleStarted;
        _battle.ActionPerformed += OnActionPerformed;
        _battle.BattleEnded += OnBattleEnded;
    }

    public void Dispose()
    {
        _battle.BattleStarted -= OnBattleStarted;
        _battle.ActionPerformed -= OnActionPerformed;
        _battle.BattleEnded -= OnBattleEnded;
    }

    private string TimeStamp => $"[{_battle.ElapsedTime:0.00}s]";

    /// <summary>陣営・位置ごとに色付けしたキャラクター名</summary>
    private string Name(CharacterModel c) => c.DisplayName.ColorStr(_colors.GetCharacterColor(c));

    private void OnBattleStarted()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"===== 戦闘開始 (制限時間 {_battle.TimeLimit}s) =====");
        AppendTeamStatus(sb, "プレイヤー", _battle.Players);
        AppendTeamStatus(sb, "敵", _battle.Enemies);
        Debug.Log(sb.ToString());
    }

    private void AppendTeamStatus(StringBuilder sb, string label, IReadOnlyList<CharacterModel> team)
    {
        sb.AppendLine($"--- {label} ---");
        foreach (var c in team)
        {
            sb.AppendLine($"{Name(c)} HP:{c.HP.CurrentValue} 攻撃力:{c.AttackPower.FloatValue} 魔力:{c.MagicPower.FloatValue} " +
                          $"攻撃速度:{c.AttackSpeed.FloatValue} 詠唱速度:{c.CastSpeed.FloatValue}");
        }
    }

    private void OnActionPerformed(ActionResult result)
    {
        List<string> parts = new List<string>();

        if (result.DealtDamage())
        {
            string dmg = $"{result.HPDMG + result.ShieldDMG}ダメージ";
            if (result.ShieldDMG > 0) dmg += $"(シールド {result.ShieldDMG})";
            dmg = dmg.ColorStr(_colors.Damage);
            if (result.IsCritical) dmg = "クリティカル! ".ColorStr(_colors.Critical) + dmg;
            parts.Add(dmg);
        }
        if (result.Healed())
        {
            string heal = $"回復 {result.Heal}";
            if (result.OverHeal > 0) heal += $"(過剰 {result.OverHeal})";
            parts.Add(heal.ColorStr(_colors.Heal));
        }
        if (result.Shield > 0) parts.Add($"シールド +{result.Shield}".ColorStr(_colors.Shield));
        if (result.AP != 0) parts.Add($"AP {result.AP.GetValueWithSign()}");
        if (result.StatusEffects != null)
        {
            foreach (var ste in result.StatusEffects) parts.Add(GetStatusEffectText(ste));
        }
        if (parts.Count == 0) parts.Add("効果なし");

        CharacterModel target = result.Target;
        string targetState = $"(HP {target.HP.CurrentValue}/{target.MaxHealth.IntValue}";
        if (target.Shield.CurrentValue > 0) targetState += $" Shield {target.Shield.CurrentValue}";
        targetState += ")";

        string log = $"{TimeStamp} {Name(result.Owner)} → {Name(target)} {GetSourceName(result.ActionSource)}: " +
                     $"{string.Join(" / ", parts)} {targetState}";
        if (result.Killed) log += " 撃破!".ColorStr(_colors.Kill);

        Debug.Log(log);
    }

    private void OnBattleEnded(BattleResult result)
    {
        StringBuilder sb = new StringBuilder();
        string resultStr = result switch
        {
            BattleResult.PlayerWin => "プレイヤーの勝利".ColorStr(_colors.Heal),
            BattleResult.PlayerLose => "プレイヤーの敗北".ColorStr(_colors.Damage),
            BattleResult.TimeUp => "時間切れ(プレイヤーの敗北)".ColorStr(_colors.Damage),
            _ => result.ToString(),
        };
        sb.AppendLine($"===== 戦闘終了 {TimeStamp} {resultStr} =====");
        foreach (var c in _battle.Players) sb.AppendLine(GetRemainStatus(c));
        foreach (var c in _battle.Enemies) sb.AppendLine(GetRemainStatus(c));
        Debug.Log(sb.ToString());
    }

    private string GetRemainStatus(CharacterModel c)
    {
        return c.IsAlive ? $"{Name(c)} HP {c.HP.CurrentValue}/{c.MaxHealth.IntValue}" : $"{Name(c)} 戦闘不能";
    }

    /// <summary>例: 強力 +1 (3/5)</summary>
    private string GetStatusEffectText(StatusEffectApplyResult ste)
    {
        StatusEffectData data = ste.Data;
        string name = string.IsNullOrEmpty(data.PassiveName) ? data.name : data.PassiveName;
        string text = $"{name} {ste.AppliedStack.GetValueWithSign()} ({ste.CurrentStack}/{data.MaxStack})";
        return text.ColorStr(data.IsBuff ? _colors.Buff : _colors.Debuff);
    }

    private static string GetSourceName(ActionSource source) => source switch
    {
        ActionSource.NormalAttack => "通常攻撃",
        ActionSource.ActiveSkill => "アクティブ",
        ActionSource.PassiveSkill => "パッシブ",
        _ => "その他",
    };
}
