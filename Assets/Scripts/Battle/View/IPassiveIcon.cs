using UnityEngine;

public interface IPassiveIcon
{
    void Init(Sprite Icon);
    void SetVisible(bool set);
    /// <summary>クールダウン中、条件未達等の場合はfalseにしてグレーアウト</summary>
    void SetActive(bool set);
    /// <summary>クールダウン、発動までのカウントを0-1の小数で渡してゲージで表現</summary>
    void SetGauge(float value);
    /// <summary>スタック数、残り発動回数などの数字を表示</summary>
    void SetText(string text);
}
