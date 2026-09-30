using UnityEngine;

public  enum PassiveIconType { Other, Buff, Debuff}

public interface IPassiveIcon
{
    void Init(Sprite icon, PassiveIconType iconType);
    void SetVisible(bool set);
    /// <summary>クールダウン中、条件未達等の場合はfalseにしてグレーアウト</summary>
    void SetActive(bool set);
    /// <summary>クールダウン、発動までのカウントを0-1の小数で渡してゲージで表現</summary>
    void SetGauge(float value);
    /// <summary>スタック数、残り発動回数などの数字を表示</summary>
    void SetText(string text);
    void PlayTriggered();
    /// <summary>パッシブが無効になった(状態異常が消えた等)ときに呼ばれ、アイコンを破棄する</summary>
    void Release();
}

/// <summary>
/// Viewが存在しない場合(シミュレーション等)に使う、何もしないIPassiveIcon
/// </summary>
public class NullPassiveIcon : IPassiveIcon
{
    public static readonly NullPassiveIcon Instance = new NullPassiveIcon();

    private NullPassiveIcon() { }

    public void Init(Sprite icon, PassiveIconType iconType) { }
    public void SetVisible(bool set) { }
    public void SetActive(bool set) { }
    public void SetGauge(float value) { }
    public void SetText(string text) { }
    public void PlayTriggered() { }
    public void Release() { }
}
