using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ゲーム全体の進行を管理する中心クラス(シングルトン)。
// ターン管理(プレイヤー/敵の切り替え)・敵の行動(AI)・勝敗判定・リザルト画面の表示・リスタートを担当する。
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // どこからでもGameManager.Instanceで参照できるようにするシングルトン

    public GameObject winText;
    public GameObject loseText;
    public GameObject winBanner;
    public GameObject loseBanner;
    public GameObject resultPanel; // 勝敗が決まったときだけ表示する背景パネル(ゲーム画面を覆って結果を見せる)
    public GameObject playerUnit;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;
    public TextMeshProUGUI turnText;

    private bool isPlayerTurn = true;
    private bool isGameOver = false;
    private bool hasWon = false; // 勝利で終わったかどうか(Spaceキーでの次ステージ進行の判定に使う)
    private bool isChoosingLevelUp = false; // レベルアップの強化選択中かどうか(選択中は入力・敵ターンを止める)
    private int pendingLevelUpChoices = 0;  // まだ表示してないレベルアップ選択の残り回数
    private PlayerProgressUI progressUI; // 左上のLv./経験値バー表示
    private EnemyAI enemyAI; // 敵AI(移動・攻撃)の実行を任せる専用クラス
    private Coroutine enemyTurnCoroutine; // 今実行中の敵ターンコルーチン(勝敗が決まったときに止められるよう持っておく)
    private ClickToWorldPosition clickHandler; // プレイヤーのターン開始時に移動・攻撃範囲を自動表示するために持っておく参照
    private Image turnTextBackground; // Your Turn/Enemy Turnの文字の後ろに敷く背景パネル(見やすさアップ用)
    private Coroutine turnPulseCoroutine; // ターン切り替え時の拡大アニメーション(連続で呼ばれても重ならないように持っておく)

    void Awake()
    {
        Instance = this;

        // 念のため、timeScaleが0に固着したまま次のステージに持ち越されることがないように、
        // 毎回のAwakeで必ず1に戻しておく(ヒットストップ演出の途中でシーン遷移が起きると、
        // 時間を元に戻すはずのコルーチンが完了できないまま消えてしまって、0が残り続けることがあるための保険)。
        Time.timeScale = 1f;

        // 開始時点ではリザルト関連の表示は全部隠しておく
        // (resultPanelを隠し忘れると、ゲーム中もずっと黒い画面に覆われたままになるので要注意)
        if (winText != null) winText.SetActive(false);
        if (loseText != null) loseText.SetActive(false);
        if (winBanner != null) winBanner.SetActive(false);
        if (loseBanner != null) loseBanner.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // レベル/経験値/ステージ表示と、画面の見た目(背景色・フォグ・簡易床)を自動でアタッチする
        progressUI = gameObject.AddComponent<PlayerProgressUI>();

        ScenePolish polish = gameObject.AddComponent<ScenePolish>();
        polish.gridWidth = gridWidth;
        polish.gridHeight = gridHeight;
        polish.cellSize = cellSize;
        polish.gridOrigin = gridOrigin;

        // 敵AIの行動(移動・攻撃)はEnemyAIに任せる。グリッド情報とプレイヤー参照をここで渡しておく。
        enemyAI = gameObject.AddComponent<EnemyAI>();
        enemyAI.cellSize = cellSize;
        enemyAI.gridOrigin = gridOrigin;
        enemyAI.gridWidth = gridWidth;
        enemyAI.gridHeight = gridHeight;
        enemyAI.playerUnit = playerUnit;

        // グリッド内部の障害物(入れないマス)の見た目を配置する。判定自体はGridObstacles側の計算でやる。
        GridObstacleView obstacleView = gameObject.AddComponent<GridObstacleView>();
        obstacleView.gridWidth = gridWidth;
        obstacleView.gridHeight = gridHeight;
        obstacleView.cellSize = cellSize;
        obstacleView.gridOrigin = gridOrigin;

        // レベルアップ選択画面はUI.Buttonを使うので、クリックを受け取るためのEventSystemが必須。
        // このシーンに無い場合に備えて、無ければここで用意しておく。
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 日本語フォント対策は見た目だけのおまけ機能なので、ここで想定外の例外が起きても
        // 上でやったゲームロジックの初期化(progressUI/enemyAI/obstacleViewとか)が巻き込まれて
        // 止まらないように、一番最後に・try/catchで守った上でやる。
        // (前はFixJapaneseFontの中で例外が起きるとAwake()全体がそこで止まってしまって、
        //  HUDも敵AIも一切生成されない重大なバグになってた)
        try
        {
            // winTextだけは「Stage X Clear!」(固定)と「touch to space」(点滅)を別々の
            // テキストに分けて表示したいので、下側をそのsubText用に空けておく指定で呼ぶ。
            FixJapaneseFont(winText, true);
            FixJapaneseFont(loseText);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        SetupTurnTextPolish();
        UpdateTurnText();
    }

    // 指定したGameObjectのTextMeshProUGUIを、日本語が文字化けしない表示方式に差し替える。
    // 前はLiberationSans SDFの代わりに動的生成したTMP_FontAssetを割り当てる方式を試したけど、
    // 実機で確認したら日本語グリフが生成されず文字化けしたままだった。
    // LevelUpChoiceUIで実際に日本語表示がうまくいってるlegacy UI.Text + LegacyRuntime.ttf方式に
    // 統一することで確実に解決する(TMPのコンポーネント自体は無効化するだけで残して、
    // 元のtextだけ新しいTextに引き継ぐ)。
    //
    // 注意: 同じGameObjectにはGraphic系コンポーネント(Text/TextMeshProUGUIとか)を1つしか
    // 付けられないので、TextMeshProUGUIが付いたままgo.AddComponent<Text>()すると
    // Unity側で追加が拒否されてnullが返ってきて、その後の参照でNullReferenceExceptionになってた。
    // (これが「Awake()が毎回途中で止まって、HUD/敵AI/床が全部生成されない」バグの原因だった)
    // 対策として、legacyのTextは同じGameObjectにじゃなく、新しく作った子オブジェクトに付ける。
    //
    // reserveBottomForSubText: trueのときは、下側(約40%)を「touch to space」点滅用の別テキスト
    // (EnsureTouchToSpaceText)のスペースとして空けておいて、このLegacyTextは上側だけを使う。
    // winTextだけtrueで呼ぶ(loseTextは1行だけなので従来どおり全体を使う)。
    private void FixJapaneseFont(GameObject go, bool reserveBottomForSubText = false)
    {
        if (go == null) return;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;

        tmp.enabled = false;

        Text legacyText = null;
        Transform existingChild = go.transform.Find("LegacyText");
        if (existingChild != null)
        {
            legacyText = existingChild.GetComponent<Text>();
        }

        if (legacyText == null)
        {
            GameObject legacyGO = new GameObject("LegacyText", typeof(RectTransform));
            legacyGO.transform.SetParent(go.transform, false);

            RectTransform rt = legacyGO.GetComponent<RectTransform>();
            if (reserveBottomForSubText)
            {
                // 下側は「touch to space」点滅用テキストのスペースとして空けておいて、上側だけ使う
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                // 親(元のTextMeshProUGUIがあったRectTransform)いっぱいに広げる
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            legacyText = legacyGO.AddComponent<Text>();
        }

        legacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        legacyText.fontSize = Mathf.Max(12, Mathf.RoundToInt(tmp.fontSize));
        legacyText.color = tmp.color;
        legacyText.alignment = TextAnchor.MiddleCenter;
        // loseText(「GAME OVER\n(Press R to Restart)」)はInspector上で改行じゃなく
        // バックスラッシュ+n の2文字がそのまま文字列として入力されてたので、
        // 画面に"\n"っていう文字そのものが表示されちゃってた。
        // ここで文字列中のリテラルな"\n"を本物の改行に変換してから表示する。
        legacyText.text = tmp.text.Replace("\\n", "\n");

        // 枠(RectTransform)が親に合わせた狭い幅のままだと、文字列側で意図してないところで
        // 勝手に折り返されてしまう(「Stage 1 Clear!」みたいな短い1行のつもりの文章まで
        // 2行に割れてしまってた)。改行は文字列側の\nだけに従わせたいので、自動折り返しを止めて
        // はみ出してもそのまま1行で表示されるようにする。
        legacyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        legacyText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // winTextの下部に「touch to space」専用のTextを用意する(無ければ新規作成、あれば既存のを返す)。
    // 「Stage X Clear!」側のLegacyTextとは別のGameObjectにすることで、これだけを点滅させられるようにする。
    private Text EnsureTouchToSpaceText(GameObject go)
    {
        if (go == null) return null;

        Transform existing = go.transform.Find("TouchToSpaceText");
        if (existing != null)
        {
            return existing.GetComponent<Text>();
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();

        GameObject subGO = new GameObject("TouchToSpaceText", typeof(RectTransform));
        subGO.transform.SetParent(go.transform, false);

        RectTransform rt = subGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.42f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text subText = subGO.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = (tmp != null) ? Mathf.Max(10, Mathf.RoundToInt(tmp.fontSize * 0.7f)) : 24;
        subText.color = new Color(1f, 0.92f, 0.3f); // 目立つ黄色(点滅して気づきやすくする)
        subText.alignment = TextAnchor.MiddleCenter;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.verticalOverflow = VerticalWrapMode.Overflow;

        return subText;
    }

    // winText/loseTextの表示文字列をセットする。FixJapaneseFontで子オブジェクトのlegacy Textに
    // 差し替わってればそっちに、差し替わってなければ(万一の保険として)TextMeshProUGUIにセットする。
    private void SetResultText(GameObject go, string text)
    {
        if (go == null) return;

        Transform legacyChild = go.transform.Find("LegacyText");
        if (legacyChild != null)
        {
            Text legacyText = legacyChild.GetComponent<Text>();
            if (legacyText != null)
            {
                legacyText.text = text;
                return;
            }
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }

    // Your Turn/Enemy Turnの表示を見やすくするための装飾。
    // 左上のHP/Lv HUD(PlayerProgressUI)と対になるように、右上に同じような半透明パネルで
    // 配置し直す(サイズ・位置はシーン側の元の値を使わず、ここで明示的に指定する)。
    // シーンファイルは直接いじらず、turnText自身のRectTransformと、新しい背景用の子オブジェクトを
    // コード側で設定するだけにしてある。
    private void SetupTurnTextPolish()
    {
        if (turnText == null) return;

        // PlayerProgressUIの左上パネル(280x118、余白32px)と対になる右上の配置。
        // 画面端ギリギリだと環境によっては見切れちゃうので、余白を広めに取って安全マージンを確保する。
        Vector2 panelSize = new Vector2(260f, 56f);
        Vector2 cornerOffset = new Vector2(-32f, -32f);

        RectTransform turnRT = turnText.GetComponent<RectTransform>();
        turnRT.anchorMin = new Vector2(1f, 1f);
        turnRT.anchorMax = new Vector2(1f, 1f);
        turnRT.pivot = new Vector2(1f, 1f);
        turnRT.anchoredPosition = cornerOffset;
        turnRT.sizeDelta = panelSize;

        turnText.fontSize = 30;
        turnText.alignment = TextAlignmentOptions.Center;

        Transform parent = turnText.transform.parent;
        if (parent == null) return;

        Transform existingBg = parent.Find("TurnTextBackground");
        if (existingBg != null)
        {
            turnTextBackground = existingBg.GetComponent<Image>();
            return;
        }

        GameObject bgGO = new GameObject("TurnTextBackground", typeof(RectTransform));
        bgGO.transform.SetParent(parent, false);

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(1f, 1f);
        bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.pivot = new Vector2(1f, 1f);
        bgRT.anchoredPosition = cornerOffset;
        bgRT.sizeDelta = panelSize;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f); // HP/Lvパネルと同じ濃さの半透明黒(ターンに応じた色はUpdateTurnTextで上書きする)

        // turnTextより描画順で後ろ(=見た目では背後)になるように、turnTextの直前に並べる
        bgGO.transform.SetSiblingIndex(turnText.transform.GetSiblingIndex());

        turnTextBackground = bgImage;
    }

    void Start()
    {
        // 前のステージで選んだ強化(射程・移動距離)は、シーンを読み直すと各コンポーネントが
        // 初期値に戻ってしまうので、PlayerProgressに保存されてる値を読み直して反映する。
        ApplyPlayerStatsToSceneObjects();

        // ゲーム開始直後の最初のYour Turnでも、クリックする前から移動・攻撃範囲が見えるようにしておく
        ShowPlayerRangesAtTurnStart();

        // プレイ中のBGMを再生開始する(勝敗が決まったらEndGameで止める)。
        // ボス戦(Stage5)だけは専用の緊迫感あるBGMにする。
        AudioFX.PlayBGM(PlayerProgress.currentStage == PlayerProgress.BossStage);
    }

    void Update()
    {
        // ゲームオーバー後にRキーでリスタート(同じステージをやり直す。勝敗どっちでも使える)
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        // 勝利時だけ、Spaceキーで次に進む。
        // ただしボスを倒してのGame Clearのときだけは、次のステージには進まずタイトル画面に戻す
        // (ボスの次のステージは存在しないので、通常のStage Clearと同じ扱いにはできない)。
        if (isGameOver && hasWon && Input.GetKeyDown(KeyCode.Space))
        {
            if (PlayerProgress.currentStage == PlayerProgress.BossStage)
            {
                ReturnToTitle();
            }
            else
            {
                NextStage();
            }
        }

        // turnTextの表示/非表示はTitleScreen(タイトルをクリックで閉じる処理)側でも切り替えられるので、
        // 背景パネルの表示状態もそれに合わせておく。これをしないと、タイトル画面がまだ閉じてない
        // 間(turnTextはまだ非表示)も背景パネルだけ先に表示されちゃって、文字のない帯だけ浮いて見えてしまう。
        if (turnTextBackground != null && turnText != null
            && turnTextBackground.gameObject.activeSelf != turnText.gameObject.activeSelf)
        {
            turnTextBackground.gameObject.SetActive(turnText.gameObject.activeSelf);
        }
    }

    // 経験値バー・レベル表示を最新の値に合わせて更新する(EnemyHealth.Dieから呼ばれる)
    public void RefreshProgressUI()
    {
        if (progressUI != null)
        {
            progressUI.Refresh();
        }
    }

    // 画面左上のHP表示を更新する(PlayerHealthがHPを受けるたびに呼ぶ)
    public void RefreshPlayerHP(int current, int max)
    {
        if (progressUI != null)
        {
            progressUI.SetHP(current, max);
        }
    }

    // レベルアップ1回分の強化選択画面を表示するようキューに入れる(EnemyHealth.Dieから呼ばれる)。
    // 何回もレベルアップしたときは、1つ選ぶごとに次の選択画面を続けて表示する。
    public void ShowLevelUpChoice()
    {
        pendingLevelUpChoices++;
        isChoosingLevelUp = true;

        if (pendingLevelUpChoices == 1)
        {
            DisplayNextLevelUpChoice(); // まだ表示中じゃなければ、ここで最初の1枚を出す
        }
    }

    // 強化選択画面を1枚、新しい専用GameObjectとして作って表示する
    void DisplayNextLevelUpChoice()
    {
        GameObject upgradeGO = new GameObject("LevelUpChoiceUI");
        LevelUpChoiceUI ui = upgradeGO.AddComponent<LevelUpChoiceUI>();
        ui.Show(OnUpgradeChosen);
    }

    // 強化選択画面でボタンが押されたときに呼ばれる
    void OnUpgradeChosen(UpgradeType type)
    {
        PlayerProgress.ApplyUpgrade(type);
        ApplyPlayerStatsToSceneObjects();
        RefreshProgressUI();

        pendingLevelUpChoices--;
        if (pendingLevelUpChoices > 0)
        {
            DisplayNextLevelUpChoice(); // まだ選択が残ってれば続けて表示する
        }
        else
        {
            isChoosingLevelUp = false;
        }
    }

    // PlayerProgressに積んである強化(射程・移動距離・球の数)を、実際のシーン上のコンポーネントに反映する。
    // ステージ開始時(Start)と、強化を選んだ直後の両方から呼ばれる。
    // (球の数の強化はClickToWorldPosition/AttackFX側でPlayerProgressを直接見てるので、ここでは
    //  射程・移動距離の表示/判定用コンポーネントだけ更新すればいい)
    void ApplyPlayerStatsToSceneObjects()
    {
        ClickToWorldPosition click = FindObjectOfType<ClickToWorldPosition>();
        if (click == null) return;

        clickHandler = click; // ターン開始時に移動・攻撃範囲を自動表示するために持っておく

        click.attackRange = 1 + PlayerProgress.bonusAttackRange;

        if (click.rangeShower != null)
        {
            click.rangeShower.range = 1 + PlayerProgress.bonusMoveRange;
        }
        if (click.attackRangeShower != null)
        {
            click.attackRangeShower.attackRange = 1 + PlayerProgress.bonusAttackRange;
        }
    }

    // プレイヤーのターンが始まった瞬間(ゲーム開始直後・敵ターンが終わった直後)に、
    // 現在地を基準とした移動範囲(青)・攻撃範囲(赤)のハイライトを自動表示する。
    // 前はクリックして実際に動くまで何も表示されなくて、「今どこまで動けて、どの敵が攻撃範囲に
    // 入ってるか」がターン開始直後は分からなかった(「しゃせんが出ない」って指摘の原因)。
    void ShowPlayerRangesAtTurnStart()
    {
        if (clickHandler != null)
        {
            clickHandler.ShowRangesAtCurrentPosition();
        }
    }

    // 今プレイヤーが操作できるターンかどうか(ClickToWorldPositionとかから参照される)
    // レベルアップの強化選択中は、選び終わるまで操作を受け付けない。
    public bool IsPlayerTurn()
    {
        return isPlayerTurn && !isGameOver && !isChoosingLevelUp;
    }

    // 画面上部のターン表示を更新する。文字だけじゃなくて、背景色・拡大アニメーションも
    // 合わせて更新して、ターンが切り替わったことに気づきやすくする。
    void UpdateTurnText()
    {
        if (turnText != null && !isGameOver)
        {
            turnText.text = isPlayerTurn ? "Your Turn" : "Enemy Turn";

            // ひと目で今どっちのターンか分かるように、色でも区別する(プレイヤー=水色、敵=赤)
            Color accent = isPlayerTurn ? new Color(0.3f, 0.78f, 1f) : new Color(1f, 0.32f, 0.26f);
            turnText.color = accent;

            if (turnTextBackground != null)
            {
                turnTextBackground.color = new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.65f);
            }

            // ターンが切り替わった瞬間に一瞬拡大してから戻るアニメーションで、変化に気づきやすくする
            if (turnPulseCoroutine != null) StopCoroutine(turnPulseCoroutine);
            turnPulseCoroutine = StartCoroutine(PulseTurnText());
        }
    }

    // turnTextを一瞬だけ拡大してから元のサイズに戻す、ターン切り替え時の強調アニメーション。
    IEnumerator PulseTurnText()
    {
        if (turnText == null) yield break;

        Transform t = turnText.transform;
        Vector3 baseScale = Vector3.one;
        Vector3 bigScale = Vector3.one * 1.35f;

        float duration = 0.18f;
        float elapsed = 0f;

        t.localScale = bigScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(bigScale, baseScale, elapsed / duration);
            yield return null;
        }

        t.localScale = baseScale;
    }

    // プレイヤーのターンを終わらせて、1.5秒後に敵のターン(EnemyTurnRoutine)を呼ぶ
    public void EndPlayerTurn()
    {
        if (isGameOver) return;

        isPlayerTurn = false;
        UpdateTurnText();
        Debug.Log("enemy turn");
        enemyTurnCoroutine = StartCoroutine(EnemyTurnRoutine());
    }

    // 敵AIの行動を実行させて、終わったらプレイヤーのターンに戻す。
    // 実際の移動・攻撃ロジックはEnemyAI(責務を分けるために切り出した専用クラス)が持ってて、
    // ここでは「選択中なら少し待つ」「呼び出す」「ターンを戻す」っていうターン管理だけをやる。
    // コルーチンにしてあるのは、EnemyAI側の移動を瞬間移動じゃなくて時間をかけたアニメーションにしたので、
    // 移動が終わるまで待ってからプレイヤーのターンに戻す必要があるから。
    // try/finallyで囲ってあるのは、万一EnemyAI側で想定外の例外が起きても、
    // 「敵ターンの表示のまま操作不能になる」事態を防ぐための保険(isPlayerTurnを戻す処理だけは必ず実行する)。
    IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (isGameOver) yield break;

        // 強化選択画面が出てる間は敵を動かさないで、選び終わるまで待つ
        while (isChoosingLevelUp)
        {
            yield return new WaitForSeconds(0.3f);
            if (isGameOver) yield break;
        }

        try
        {
            if (enemyAI != null)
            {
                yield return enemyAI.TakeTurnRoutine();
            }
        }
        finally
        {
            // 敵の行動が終わったらプレイヤーのターンに戻す
            if (!isGameOver)
            {
                isPlayerTurn = true;
                UpdateTurnText();
                Debug.Log("player turn");
                ShowPlayerRangesAtTurnStart(); // 自分のターンに戻った瞬間、現在地基準の範囲を出しておく
            }
        }
    }

    // 敵の数を確認して、0体なら勝利とする。EnemyHealth.Die()から呼ばれる。
    public void CheckEnemies()
    {
        if (isGameOver) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            EndGame(true);
        }
    }

    // 勝敗が決まったときの処理。resultPanelを表示してゲーム画面を隠して、
    // 勝ち/負けに応じたテキスト・バナーを表示する。これ以降の入力(移動・攻撃・敵の行動)は止まる。
    public void EndGame(bool playerWon)
    {
        if (isGameOver) return;
        isGameOver = true;
        hasWon = playerWon;

        AudioFX.StopBGM(); // 勝敗が決まったら通常BGMを止めて、クリア/敗北の音を聞きやすくする

        CancelInvoke(); // 予約されてた呼び出しをキャンセル
        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine); // 実行中の敵ターンがあれば止める(プレイヤーの攻撃が同時にとどめを刺した場合とか)
            enemyTurnCoroutine = null;
        }

        // 攻撃演出(ヒットストップ・弾の飛ぶ時間とか)の分だけ実際のダメージ反映が遅れるので、
        // 最後の1体を倒す攻撃の直後には、もうEndPlayerTurn()が呼ばれて「Enemy Turn」表示に
        // 切り替わってしまってることがある。そのまま放置すると、Stage Clear!の勝利表示とEnemy Turnの文字が
        // 重なって表示されたままになる(ターン表示はUpdateTurnTextがisGameOverで更新を止めるだけで、
        // 既に書き換わってた文字列自体は消えないので)。勝敗が決まった時点でターン表示はもう要らないので、
        // ここで非表示にしておく(背景パネルも一緒に隠す)。
        if (turnText != null) turnText.gameObject.SetActive(false);
        if (turnTextBackground != null) turnTextBackground.gameObject.SetActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);

        if (playerWon)
        {
            // 溶岩ステージ(Stage4)の次に出てくるボスを倒したときだけ、普通の「Stage X Clear!」じゃなくて
            // 特別な「GAME CLEAR!!」表示にする
            bool isBossWin = (PlayerProgress.currentStage == PlayerProgress.BossStage);

            if (isBossWin)
            {
                AudioFX.PlayGameClear();
            }
            else
            {
                AudioFX.PlayStageClear();
            }

            if (winText != null)
            {
                winText.SetActive(true);

                string mainLine = isBossWin ? "GAME CLEAR!!" : ("Stage " + PlayerProgress.currentStage + " Clear!");
                SetResultText(winText, mainLine);

                // 「touch to space」だけ別テキストにして、これだけを点滅させる
                Text touchText = EnsureTouchToSpaceText(winText);
                if (touchText != null) touchText.text = "touch to space";

                StartCoroutine(BlinkTouchToSpaceText());
            }
            if (winBanner != null) winBanner.SetActive(true);
            Debug.Log(isBossWin ? "GAME CLEAR" : "WIN");
        }
        else
        {
            AudioFX.PlayLose();

            if (loseText != null) loseText.SetActive(true);
            if (loseBanner != null) loseBanner.SetActive(true);
            Debug.Log("LOSE");
        }
    }

    // winText内の「touch to space」専用テキスト(TouchToSpaceText)だけを0.5秒おきに
    // 表示/非表示を切り替えて点滅させる。「Stage X Clear!」側(LegacyText)は固定表示のまま触らない。
    // 次のステージへ進む/リスタートするとシーンごと読み直されてこのコルーチンも自動的に消えるので、
    // 自分で止める処理は要らない。
    IEnumerator BlinkTouchToSpaceText()
    {
        if (winText == null) yield break;

        Transform subChild = winText.transform.Find("TouchToSpaceText");
        Text subText = subChild != null ? subChild.GetComponent<Text>() : null;
        if (subText == null) yield break;

        while (winText != null && winText.activeInHierarchy)
        {
            subText.enabled = !subText.enabled;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 現在のシーンを読み直してゲームを最初からやり直す(Rキーで呼ばれる。ステージ数は変えない)
    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 次のステージへ進む(勝利後、Spaceキーで呼ばれる)。ステージ数を進めてから同じシーンを読み直す。
    // 敵の数はEnemySpawnerがPlayerProgress.currentStageを見て自動的に増やす。
    void NextStage()
    {
        PlayerProgress.currentStage++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ボス撃破(Game Clear)後、Spaceキーで呼ばれる。次のステージには進まないで、
    // レベル・経験値・強化・ステージ数とかの進行状況を全部リセットしてタイトル画面に戻す
    // (PlayerProgress.currentStageが1に戻るので、シーンを読み直した後はTitleScreen側が
    //  いつも通りタイトル表示→クリック待ちの状態になる)。
    void ReturnToTitle()
    {
        PlayerProgress.ResetAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
