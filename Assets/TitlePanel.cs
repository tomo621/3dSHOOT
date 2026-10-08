using UnityEngine;

// タイトル画面を動かすクラス。
// 一応メモ: ファイル名はTitlePanel.csだけど、クラス名はTitleScreenのまま(Unity的には何も問題ないし、
// Add Componentで探すときは"TitleScreen"で検索すれば出てくる)。
//
// 画面のどこかをクリックするとタイトルを隠して、turnText(「Your Turn」とかの表示)をオンにしてゲームを始める。
public class TitleScreen : MonoBehaviour
{
    public GameObject turnText; // ゲーム開始と同時に出すターン表示

    void Start()
    {
        // ステージ2以降(NextStageで読み込み直したとき)は、毎回クリックさせるのは面倒なので
        // 自動でタイトルを閉じてそのままゲームを始めるようにしてる
        if (PlayerProgress.currentStage > 1)
        {
            gameObject.SetActive(false);
            if (turnText != null) turnText.SetActive(true);
            SetProgressUIVisible(true);
            return;
        }

        // ゲーム始まる前はターン表示と、左上のLv./経験値のHUDを隠しておく
        // (タイトル画面にゲーム中のHUDが写り込むのを防ぐため)
        if (turnText != null)
        {
            turnText.SetActive(false);
        }
        SetProgressUIVisible(false);
    }

    void Update()
    {
        // 画面のどこかがクリックされたらタイトルを閉じてゲーム開始
        if (Input.GetMouseButtonDown(0))
        {
            gameObject.SetActive(false);

            if (turnText != null)
            {
                turnText.SetActive(true);
            }
            SetProgressUIVisible(true);
        }
    }

    // 左上のLv./経験値HUD(PlayerProgressUI)を表示するかどうかを切り替える。
    // PlayerProgressUIは実行時にGameManager.Awakeで作られるものなので、シーンに最初から置いてある
    // 参照をInspectorで持たせられない。だから毎回FindFirstObjectByTypeで探して呼んでる
    // (呼ばれるのはタイトル出すときとクリックしたときの最大2回だけだから、重くなる心配はない)。
    void SetProgressUIVisible(bool visible)
    {
        PlayerProgressUI progressUI = FindFirstObjectByType<PlayerProgressUI>();
        if (progressUI != null)
        {
            progressUI.SetVisible(visible);
        }
    }
}
