using DG.Tweening;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// キャラクター1体分の表示。Modelは参照せず、CharacterPresenterから呼ばれるメソッドのみを持つ
/// 演出はすべて呼び出し後すぐに返す(Modelの進行を待たせない)
/// </summary>
public class CharacterView : MonoBehaviour
{
    [SerializeField] private bool _isEnemy;
    [Header("パッシブアイコン")]
    [SerializeField] private PassiveIconView _passiveIconPrefab;
    [SerializeField] private Transform _passiveIconRoot;

    [SerializeField] private EffectText _effectTextPrefab;
    [SerializeField] private Transform _effectTextRoot;

    [SerializeField] private float _effectTextOffset;
    [SerializeField] private float _effectTextMaxSize = 4f;
    [SerializeField] private float _effectTextCriticalSizeMul = 2f;

    [SerializeField] private SpriteRenderer _charaSprite;
    [SerializeField] private Image _hpBar;
    [SerializeField] private Image _shieldBar;
    [SerializeField] private Image _naBar;
    [SerializeField] private Image _castBar;

    [SerializeField] private float _naDur;
    [SerializeField] private float _naMove;
    [SerializeField] private Ease _naEase1;
    [SerializeField] private Ease _naEase2;
    [SerializeField] private float _castDur;
    [SerializeField] private float _castMove;
    [SerializeField] private Ease _castEase1;
    [SerializeField] private Ease _castEase2;

    private Sequence _seq_na;
    private Sequence _seq_cast;

    /// <summary>キャラに応じた見た目の設定</summary>
    public void Setup(CharacterData data)
    {
        if (data.CharaSprite != null) _charaSprite.sprite = data.CharaSprite;
        _charaSprite.flipX = _isEnemy;
    }

    /// <summary>キャラが存在しない枠は非表示にする</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    #region ゲージ
    public void SetHP(int current, int max)
    {
        _hpBar.fillAmount = (float)current / max;
    }

    public void SetShield(int current, int max)
    {
        _shieldBar.fillAmount = (float)current / max;
    }

    /// <param name="ratio">0-1</param>
    public void SetAP(float ratio)
    {
        _castBar.fillAmount = ratio;
    }

    /// <param name="ratio">次の通常攻撃までの進行度 0-1</param>
    public void SetNATimer(float ratio)
    {
        _naBar.fillAmount  = ratio;
    }
    #endregion

    #region 演出
    public void PlayNormalAttack()
    {
        if (_seq_na != null) _seq_na.Kill(true);
        float move = _isEnemy ? -_naMove : _naMove;
        _seq_na = DOTween.Sequence();
        _seq_na.Append(_charaSprite.transform.DOLocalMoveX(move, _naDur / 2f).SetEase(_naEase1));
        _seq_na.Append(_charaSprite.transform.DOLocalMoveX(0, _naDur / 2f).SetEase(_naEase2));
        _seq_na.Play();
    }

    public void PlayActiveSkill()
    {
        if (_seq_cast != null) _seq_cast.Kill(true);
        _seq_cast = DOTween.Sequence();
        _seq_cast.Append(_charaSprite.transform.DOLocalMoveY(_castMove, _castDur / 2f).SetEase(_castEase1));
        _seq_cast.Append(_charaSprite.transform.DOLocalMoveY(0, _castDur / 2f).SetEase(_castEase2));
        _seq_cast.Play();
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
        ColorRef color = Database.Instance.ColorRef;
        if (result.DealtDamage())
        {
            float size = CalcEffectTextSize(result.HPDMG + result.ShieldDMG, result.Target.MaxHealth.FloatValue);
            if (result.IsCritical) SpawnEffectText($"{result.HPDMG + result.ShieldDMG}".ColorStr(color.Critical), size * _effectTextCriticalSizeMul);
            else SpawnEffectText($"{result.HPDMG + result.ShieldDMG}".ColorStr(color.Damage), size);
        }

        if (result.Healed()) SpawnEffectText(result.Heal.ColorStr(color.Critical), CalcEffectTextSize(result.Heal, result.Target.MaxHealth.FloatValue));
        if (result.Shield > 0) SpawnEffectText(result.Shield.ColorStr(color.Shield), CalcEffectTextSize(result.Shield, result.Target.MaxHealth.FloatValue));

        if (result.AP > 0) SpawnEffectText(result.AP.GetValueWithSign().ColorStr(color.AP), 0.75f);
    }

    private float CalcEffectTextSize(float value, float maxValue, float minSize = 1f) => Mathf.Max(value / maxValue * _effectTextMaxSize, minSize);

    private void SpawnEffectText(string message, float sizeMul)
    {
        Vector2 pos = _effectTextRoot.position;
        EffectText.Spawn(_effectTextPrefab, pos + Random.insideUnitCircle * _effectTextOffset, message, sizeMul, _isEnemy, _effectTextRoot);
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
