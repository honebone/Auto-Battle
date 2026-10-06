using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// パッシブ・状態異常のアイコン1つ分の表示。CharacterView.CreatePassiveIconで生成される
/// </summary>
public class PassiveIconView : MonoBehaviour, IPassiveIcon
{
    [SerializeField] Image _passiveSprite;
    [SerializeField] Image _gauge;
    [SerializeField] Color _enabledColor;
    [SerializeField] Color _disabledColor;
    [SerializeField] TextMeshProUGUI _text;

    public void Init(Sprite icon, PassiveIconType iconType)
    {
        if (icon != null) _passiveSprite.sprite = icon;
        //TODO:種類(Buff/Debuff等)に応じた枠の設定
    }

    public void SetVisible(bool set)
    {
        gameObject.SetActive(set);
    }

    public void SetActive(bool set)
    {
        _gauge.color = set ? _enabledColor : _disabledColor;
    }

    public void SetGauge(float value)
    {
        _gauge.fillAmount = value;
    }

    public void SetText(string text)
    {
        _text.text = text;
    }

    public void PlayTriggered()
    {
        //TODO:発動時の演出
    }

    public void Release()
    {
        // シーン破棄後に呼ばれた場合を考慮
        if (this != null) Destroy(gameObject);
    }
}
