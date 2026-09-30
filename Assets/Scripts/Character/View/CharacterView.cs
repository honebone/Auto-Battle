using UnityEngine;
using DG.Tweening;

/// <summary>
/// キャラクター1体分の表示。Modelは参照せず、CharacterPresenterから呼ばれるメソッドのみを持つ
/// 演出はすべて呼び出し後すぐに返す(Modelの進行を待たせない)
/// </summary>
public class CharacterView : MonoBehaviour
{
    [Header("パッシブアイコン")]
    [SerializeField] private PassiveIconView _passiveIconPrefab;
    [SerializeField] private Transform _passiveIconRoot;

    /// <summary>キャラに応じた見た目の設定</summary>
    public void Setup(CharacterData data)
    {
        //TODO:キャラに応じたSpriteの設定
    }

    /// <summary>キャラが存在しない枠は非表示にする</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    #region ゲージ
    public void SetHP(int current, int max)
    {
        //TODO:HPゲージの表示
    }

    public void SetShield(int current, int max)
    {
        //TODO:Shieldゲージの表示
    }

    /// <param name="ratio">0-1</param>
    public void SetAP(float ratio)
    {
        //TODO:APゲージの表示
    }

    /// <param name="ratio">次の通常攻撃までの進行度 0-1</param>
    public void SetNATimer(float ratio)
    {
        //TODO:NATimerゲージの表示
    }
    #endregion

    #region 演出
    public void PlayNormalAttack()
    {
        //TODO:攻撃時のアニメーション
    }

    public void PlayActiveSkill()
    {
        //TODO:アクティブ発動時のアニメーション
    }

    public void PlayDeath()
    {
        //TODO:戦闘不能時アニメーション
    }

    /// <summary>行動の対象になったとき、行動内容に応じた演出を自身の位置で生成</summary>
    public void PlayActionEffect(ActionResult result)
    {
        //TODO:result.PresentationのVisualEffectの生成、SoundEffectの再生
        //TODO:ダメージ・回復量などの数値表示
    }
    #endregion

    /// <summary>
    /// パッシブアイコンを生成して返す(PassiveModelが有効になるときにPresenterから呼ばれ、PassiveModelに渡される)
    /// </summary>
    public IPassiveIcon CreatePassiveIcon()
    {
        if (_passiveIconPrefab == null) return NullPassiveIcon.Instance;

        return Instantiate(_passiveIconPrefab, _passiveIconRoot != null ? _passiveIconRoot : transform);
    }
}
