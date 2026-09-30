using UnityEngine;

/// <summary>
/// パッシブ・状態異常のアイコン1つ分の表示。CharacterView.CreatePassiveIconで生成される
/// </summary>
public class PassiveIconView : MonoBehaviour, IPassiveIcon
{
    public void Init(Sprite icon, PassiveIconType iconType)
    {
        //TODO:アイコン画像の設定、種類(Buff/Debuff等)に応じた枠の設定
    }

    public void SetVisible(bool set)
    {
        //TODO:表示/非表示
    }

    public void SetActive(bool set)
    {
        //TODO:グレーアウト
    }

    public void SetGauge(float value)
    {
        //TODO:ゲージ表示
    }

    public void SetText(string text)
    {
        //TODO:数字の表示
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
