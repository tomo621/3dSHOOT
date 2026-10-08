using UnityEngine;
using UnityEngine.UI;
using System;

// レベルアップのときに出す「ツリー風」の選択画面。
// HP/射程/移動距離/球の数の4つから1つ選んで、その場で強化を確定させる。
// プレハブなしで全部コードで作る(PlayerProgressUI/HealthBarControllerと同じやり方)。
//
// 使い方: 専用の空のGameObjectにこのスクリプトをAddComponentして、Show(callback)を呼ぶ。
// GameManager自身にはAddComponentしないこと(選んだ後にDestroy(gameObject)するので、
// GameManagerに付けるとGameManagerごと消えてしまう)。
public class LevelUpChoiceUI : MonoBehaviour
{
    private Action<UpgradeType> onChosen;

    public void Show(Action<UpgradeType> callback)
    {
        AudioFX.PlayLevelUp();

        onChosen = callback;
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("LevelUpCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // 他のHUDより手前に出す
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 画面全体を薄暗くする背景(選択中だって分かりやすくする)
        GameObject dimGO = new GameObject("Dim", typeof(RectTransform));
        dimGO.transform.SetParent(canvasGO.transform, false);
        Image dim = dimGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform dimRect = dimGO.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        // タイトル
        GameObject titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(canvasGO.transform, false);
        Text title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 28;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.text = "LEVEL UP! Lv." + PlayerProgress.level + "\n強化するステータスを選んでください";
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600f, 100f);
        titleRect.anchoredPosition = new Vector2(0f, 160f);

        string[] labels = { "HP +20", "射程 +1", "移動距離 +1", "球の数 +1" };
        UpgradeType[] types = { UpgradeType.HP, UpgradeType.AttackRange, UpgradeType.MoveRange, UpgradeType.BulletCount };

        for (int i = 0; i < 4; i++)
        {
            CreateButton(canvasGO.transform, labels[i], types[i], i);
        }
    }

    // 4択ボタンを1個作る(2x2で並べる)
    void CreateButton(Transform parent, string label, UpgradeType type, int index)
    {
        GameObject btnGO = new GameObject("Button_" + type, typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.4f, 0.55f, 1f);
        colors.pressedColor = new Color(0.15f, 0.18f, 0.25f, 1f);
        btn.colors = colors;

        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 60f);
        float x = (index % 2 == 0) ? -140f : 140f;
        float y = (index < 2) ? 20f : -60f;
        rect.anchoredPosition = new Vector2(x, y);

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        btn.onClick.AddListener(() => Choose(type));
    }

    // ボタンが押されたときの処理。選んだ結果をコールバックに渡してから、このUI自身を消す。
    void Choose(UpgradeType type)
    {
        Action<UpgradeType> callback = onChosen;
        Destroy(gameObject); // このUI専用のGameObjectを消すだけ(GameManagerには影響しない)
        callback?.Invoke(type);
    }
}
