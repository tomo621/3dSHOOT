using UnityEngine;
using UnityEngine.UI;

// HPの残量を頭上に表示する、ワールドスペースのHPバー。
// 前はEnemyHPBarController(敵専用で、Slider/プレハブをInspectorで繋ぐ方式)だったけど、
// プレハブや画像アセットなしで動くように全部コードで作る方式に変えて、
// PlayerHealthとEnemyHealthの両方から共通で使えるようにした。
public class HealthBarController : MonoBehaviour
{
    public float heightOffset = 1.6f; // ユニットの頭上、どれくらい浮かせるか
    public float barWidth = 1.0f;     // バーの幅(ワールド単位)
    public float barHeight = 0.14f;   // バーの高さ(ワールド単位)

    private Image fillImage;
    private Transform barRoot; // 作ったCanvas本体(位置を追従させたりビルボードさせたりする対象)
    private Camera cam;

    void Awake()
    {
        BuildBar();
    }

    void LateUpdate()
    {
        if (barRoot == null) return;

        // ユニットの頭上に追従させる
        barRoot.position = transform.position + Vector3.up * heightOffset;

        // 常にカメラの方を向かせる(ビルボード)。DamagePopupと同じやり方。
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            barRoot.forward = cam.transform.forward;
        }
    }

    // 背景Image + 塗りImageを全部コードで作る。
    // プレハブや画像アセットは一切使わないので、Inspectorで何か設定する必要はない。
    void BuildBar()
    {
        GameObject canvasGO = new GameObject("HPBarCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        barRoot = canvasGO.transform;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 14f);
        // Canvas自体はピクセル単位で作るから、ワールド上では小さく見えるようにスケールを縮めとく
        canvasGO.transform.localScale = new Vector3(barWidth / 100f, barHeight / 14f, 1f);

        // 背景(土台の暗い部分)
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 塗り(HPの割合に応じて伸び縮みする部分)。背景より少し内側に収める。
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(canvasGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
    }

    // HPの割合(0〜1)を受け取ってバーの長さと色を更新する。
    // 色の基準は元々PlayerHealth/EnemyHealthにあった色変化と同じ(0.5超えで緑、0.2超えで黄、それ以下は赤)。
    public void SetHP(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        if (fillImage == null) return;

        fillImage.fillAmount = ratio;

        if (ratio > 0.5f) fillImage.color = Color.green;
        else if (ratio > 0.2f) fillImage.color = Color.yellow;
        else fillImage.color = Color.red;
    }
}
