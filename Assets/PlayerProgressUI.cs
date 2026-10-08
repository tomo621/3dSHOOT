using UnityEngine;
using UnityEngine.UI;

// 画面左上に「Lv.」「Stage」「経験値バー」を出すHUD。
// HealthBarControllerと同じ考え方で、プレハブや画像アセットを使わず全部コードで作ってる
// (スクリーン空間のCanvasだから、HealthBarController(ワールド空間)とはまた別物)。
// GameManagerのAwakeで自動的にアタッチされて、経験値が変わるたびにRefresh()で表示を更新する。
public class PlayerProgressUI : MonoBehaviour
{
    private Text hpText;
    private Text levelText;
    private Text stageText;
    private Image expFillImage;
    private GameObject canvasRoot; // タイトル画面が出てる間はTitleScreenから隠せるように、Canvas本体を持っておく

    void Awake()
    {
        BuildUI();
        Refresh();
    }

    // タイトル画面(TitleScreen)が閉じるまでHUDを隠して、タイトルが閉じたら表示するために呼ばれる
    public void SetVisible(bool visible)
    {
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(visible);
        }
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("ProgressUICanvas", typeof(RectTransform));
        canvasRoot = canvasGO;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGO.AddComponent<CanvasScaler>();

        // 左上の半透明パネル(土台)
        GameObject panelGO = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        // 画面端ギリギリだと環境によっては見切れちゃうので、余白を広めに取って安全マージンを確保してる
        panelRect.anchoredPosition = new Vector2(32f, -32f);
        // 右上のturnText(GameManager.SetupTurnTextPolish)と「対」になるように、前より一回り大きくした
        panelRect.sizeDelta = new Vector2(280f, 118f);

        // 「HP: 現在値 / 最大値」の表示(一番目立つ上段に置く。プレイヤーのHPが一番気になる情報だから)
        hpText = CreateText(panelGO.transform, "HPText", 26, new Vector2(10f, -6f), new Vector2(260f, 30f));
        hpText.color = new Color(1f, 0.45f, 0.45f); // HPってひと目で分かるように赤系の色にしてる

        // 「Lv. X」の表示
        levelText = CreateText(panelGO.transform, "LevelText", 24, new Vector2(10f, -38f), new Vector2(260f, 26f));

        // 「Stage Y」の表示
        stageText = CreateText(panelGO.transform, "StageText", 18, new Vector2(10f, -66f), new Vector2(260f, 22f));

        // 経験値バー(背景)
        GameObject expBgGO = new GameObject("ExpBarBg", typeof(RectTransform));
        expBgGO.transform.SetParent(panelGO.transform, false);
        Image expBg = expBgGO.AddComponent<Image>();
        expBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform expBgRect = expBgGO.GetComponent<RectTransform>();
        // anchorMax.xを1にすると「親の幅いっぱいに伸びる」ストレッチ指定になって、
        // sizeDelta.xが固定幅じゃなく追加オフセット扱いになってパネルからはみ出しちゃうので、
        // anchorMin/anchorMaxを同じ点(0,0)にして、sizeDeltaをそのまま固定幅として使ってる。
        expBgRect.anchorMin = new Vector2(0f, 0f);
        expBgRect.anchorMax = new Vector2(0f, 0f);
        expBgRect.pivot = new Vector2(0f, 0f);
        expBgRect.anchoredPosition = new Vector2(10f, 10f);
        expBgRect.sizeDelta = new Vector2(260f, 15f);

        // 経験値バー(塗りの部分)
        GameObject expFillGO = new GameObject("ExpBarFill", typeof(RectTransform));
        expFillGO.transform.SetParent(expBgGO.transform, false);
        expFillImage = expFillGO.AddComponent<Image>();
        expFillImage.color = new Color(0.3f, 0.7f, 1f, 1f);
        expFillImage.type = Image.Type.Filled;
        expFillImage.fillMethod = Image.FillMethod.Horizontal;
        expFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform expFillRect = expFillGO.GetComponent<RectTransform>();
        expFillRect.anchorMin = Vector2.zero;
        expFillRect.anchorMax = Vector2.one;
        expFillRect.offsetMin = new Vector2(1f, 1f);
        expFillRect.offsetMax = new Vector2(-1f, -1f);
    }

    // Unity標準フォントでUI.Textを1個作るときの共通処理
    Text CreateText(Transform parent, string name, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return t;
    }

    // 今のPlayerProgressの値に合わせて表示を更新する。経験値が変わったときに呼ぶ。
    public void Refresh()
    {
        if (levelText != null) levelText.text = "Lv. " + PlayerProgress.level;
        // 地形テーマ名と、移動範囲への補正(TerrainEffects)を一緒に出して、
        // 「このステージは動きにくいのか動きやすいのか」が見ただけで分かるようにしてる
        if (stageText != null) stageText.text = "Stage " + PlayerProgress.currentStage + "  " + TerrainEffects.GetThemeLabel(PlayerProgress.currentStage);

        if (expFillImage != null)
        {
            float ratio = (float)PlayerProgress.currentExp / PlayerProgress.ExpToNextLevel();
            expFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    // プレイヤーのHP表示を更新する。PlayerHealthがHPが変わるたびに
    // GameManager.RefreshPlayerHP経由で呼んでくる。
    public void SetHP(int current, int max)
    {
        if (hpText != null)
        {
            hpText.text = "HP: " + current + " / " + max;
        }
    }
}
