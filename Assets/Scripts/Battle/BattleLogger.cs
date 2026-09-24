using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// BattleModelのイベントを購読し、戦闘の経過をConsoleへ出力するデバッグ用ロガー
/// </summary>
public class BattleLogger : IDisposable
{
    private static readonly Color DamageColor = new Color(1f, 0.4f, 0.4f);
    private static readonly Color HealColor = new Color(0.4f, 1f, 0.4f);
    private static readonly Color ShieldColor = new Color(0.4f, 0.8f, 1f);
    private static readonly Color KillColor = new Color(1f, 0.85f, 0.2f);

    private readonly BattleModel _battle;

    public BattleLogger(BattleModel battle)
    {
        _battle = battle;
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
            sb.AppendLine($"{c.DisplayName} HP:{c.HP.CurrentValue} 攻撃力:{c.AttackPower.FloatValue} 魔力:{c.MagicPower.FloatValue} " +
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
            if (result.IsCritical) dmg += " クリティカル!";
            parts.Add(dmg.ColorStr(DamageColor));
        }
        if (result.Healed())
        {
            string heal = $"回復 {result.Heal}";
            if (result.OverHeal > 0) heal += $"(過剰 {result.OverHeal})";
            parts.Add(heal.ColorStr(HealColor));
        }
        if (result.Shield > 0) parts.Add($"シールド +{result.Shield}".ColorStr(ShieldColor));
        if (result.AP != 0) parts.Add($"AP {result.AP.GetValueWithSign()}");
        if (parts.Count == 0) parts.Add("効果なし");

        CharacterModel target = result.Target;
        string targetState = $"(HP {target.HP.CurrentValue}/{target.MaxHealth.IntValue}";
        if (target.Shield.CurrentValue > 0) targetState += $" Shield {target.Shield.CurrentValue}";
        targetState += ")";

        string log = $"{TimeStamp} {result.Owner.DisplayName} → {target.DisplayName} {GetSourceName(result.ActionSource)}: " +
                     $"{string.Join(" / ", parts)} {targetState}";
        if (result.Killed) log += " 撃破!".ColorStr(KillColor);

        Debug.Log(log);
    }

    private void OnBattleEnded(BattleResult result)
    {
        StringBuilder sb = new StringBuilder();
        string resultStr = result switch
        {
            BattleResult.PlayerWin => "プレイヤーの勝利".ColorStr(HealColor),
            BattleResult.PlayerLose => "プレイヤーの敗北".ColorStr(DamageColor),
            BattleResult.TimeUp => "時間切れ(プレイヤーの敗北)".ColorStr(DamageColor),
            _ => result.ToString(),
        };
        sb.AppendLine($"===== 戦闘終了 {TimeStamp} {resultStr} =====");
        foreach (var c in _battle.Players) sb.AppendLine(GetRemainStatus(c));
        foreach (var c in _battle.Enemies) sb.AppendLine(GetRemainStatus(c));
        Debug.Log(sb.ToString());
    }

    private string GetRemainStatus(CharacterModel c)
    {
        return c.IsAlive ? $"{c.DisplayName} HP {c.HP.CurrentValue}/{c.MaxHealth.IntValue}" : $"{c.DisplayName} 戦闘不能";
    }

    private static string GetSourceName(ActionSource source) => source switch
    {
        ActionSource.NormalAttack => "通常攻撃",
        ActionSource.ActiveSkill => "アクティブ",
        ActionSource.PassiveSkill => "パッシブ",
        _ => "その他",
    };
}
