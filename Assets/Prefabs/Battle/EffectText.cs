using UnityEngine;
using DG.Tweening;
using TMPro;
public class EffectText : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Header("移動")]
    [SerializeField] private float duration = 0.8f;          // 全体の時間
    [SerializeField] private float jumpPower = 1.5f;         // 放物線の高さ
    [SerializeField] private float horizontalDistance = 1.2f; // 横方向の移動量
    [SerializeField] private float dropDistance = 0.5f;      // 着地点を開始点よりどれだけ下げるか

    [Header("スケール(生成時サイズに対する倍率)")]
    [SerializeField] private float startScale = 0.5f; // 開始時
    [SerializeField] private float peakScale = 1.5f;  // 最高到達点
    [SerializeField] private float endScale = 0.2f;   // 終了時

    private void Reset()
    {
        label = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// プレハブから生成して初期化するヘルパー。
    /// </summary>
    public static EffectText Spawn(EffectText prefab, Vector3 position, string message, float sizeMultiplier, bool toRight, Transform parent = null)
    {
        EffectText instance = Instantiate(prefab, position, Quaternion.identity, parent);
        instance.Init(message, sizeMultiplier, toRight);
        return instance;
    }

    /// <summary>
    /// テキスト内容・サイズ倍率・飛ぶ方向(true = 右, false = 左)を受け取ってアニメーション開始。
    /// </summary>
    public void Init(string message, float sizeMultiplier, bool toRight)
    {
        if (label == null) label = GetComponent<TMP_Text>();
        label.text = message;

        Vector3 baseScale = transform.localScale * sizeMultiplier;
        transform.localScale = baseScale * startScale;

        float dir = toRight ? 1f : -1f;
        Vector3 endPos = transform.position + new Vector3(horizontalDistance * dir, -dropDistance, 0f);
        float half = duration * 0.5f;

        DOTween.Sequence()
            // 放物線移動。Ease.Linear にすることで頂点がちょうど duration の半分に来る
            .Append(transform.DOJump(endPos, jumpPower, 1, duration).SetEase(Ease.Linear))
            // 前半:頂点に向かって拡大
            .Insert(0f, transform.DOScale(baseScale * peakScale, half).SetEase(Ease.OutQuad))
            // 後半:落ちながら縮小
            .Insert(half, transform.DOScale(baseScale * endScale, half).SetEase(Ease.InQuad))
            .SetLink(gameObject) // 途中で破棄されてもTweenを安全に停止
            .OnComplete(() => Destroy(gameObject));
    }
}
