using UnityEngine;

// 画面が「殺風景」にならないように、カメラの背景色・環境光・簡易フォグ・床・周りの地形装飾をコードだけで整えるクラス。
// 既存のシーン構成(GridDrawerとか)を書き換えるんじゃなくて追加するだけなので、他のスクリプトには影響しない。
// ステージ(PlayerProgress.currentStage)が進むごとに配色と装飾の量が変わるので、
// 「ステージごとに地形っぽい見た目が変わる」感じを、3Dモデルのアセットなしで実現してる。
// GameManagerのAwakeで自動的にアタッチされる。
public class ScenePolish : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);

    // ステージの配色テーマ(床・フォグ・背景・装飾物の色をまとめたもの)
    private struct StageTheme
    {
        public Color floorLight, floorDark, fogColor, skyColor, propColor;
        public string groundTextureName; // Resources/Textures/以下のファイル名(拡張子なし)。CC0の実写風タイル画像を使う
        public Color floorTint; // groundTextureNameの上からかける色味の補正(溶岩テーマを赤く焼けた感じにするとか)
    }

    // Awakeじゃなくて、GameManagerが各フィールド(gridWidthとか)をセットし終わった後に動くように
    // Start()で初期化してる(AddComponentした直後はAwakeがすぐ呼ばれちゃって、GameManager側の
    // フィールドのコピーが終わる前に実行されてしまうため)。
    void Start()
    {
        StageTheme theme = GetTheme(PlayerProgress.currentStage);
        SetupLightingAndSky(theme);
        SetupFloor(theme);
        SetupBorderProps(theme);
    }

    // ステージ番号に応じたテーマを返す(5種類を繰り返す: 草原→岩場→砂漠→溶岩→氷雪→草原...)
    StageTheme GetTheme(int stage)
    {
        StageTheme[] themes = new StageTheme[]
        {
            new StageTheme { // 1: 草原
                floorLight = new Color(0.75f, 0.78f, 0.70f), floorDark = new Color(0.60f, 0.63f, 0.56f),
                fogColor = new Color(0.60f, 0.70f, 0.80f), skyColor = new Color(0.35f, 0.55f, 0.75f),
                propColor = new Color(0.45f, 0.55f, 0.35f),
                groundTextureName = "GroundGrass", floorTint = Color.white
            },
            new StageTheme { // 2: 岩場
                floorLight = new Color(0.70f, 0.68f, 0.65f), floorDark = new Color(0.50f, 0.48f, 0.45f),
                fogColor = new Color(0.65f, 0.60f, 0.55f), skyColor = new Color(0.55f, 0.50f, 0.45f),
                propColor = new Color(0.40f, 0.38f, 0.36f),
                groundTextureName = "GroundRock", floorTint = Color.white
            },
            new StageTheme { // 3: 砂漠
                floorLight = new Color(0.85f, 0.75f, 0.55f), floorDark = new Color(0.70f, 0.60f, 0.40f),
                fogColor = new Color(0.80f, 0.70f, 0.50f), skyColor = new Color(0.90f, 0.75f, 0.50f),
                propColor = new Color(0.60f, 0.45f, 0.30f),
                groundTextureName = "GroundSand", floorTint = Color.white
            },
            new StageTheme { // 4: 溶岩(専用テクスチャが無いので、岩場のテクスチャを赤黒く焼けた色味に補正して使い回す)
                floorLight = new Color(0.45f, 0.22f, 0.18f), floorDark = new Color(0.28f, 0.12f, 0.10f),
                fogColor = new Color(0.45f, 0.20f, 0.15f), skyColor = new Color(0.35f, 0.12f, 0.10f),
                propColor = new Color(0.20f, 0.08f, 0.06f),
                groundTextureName = "GroundRock", floorTint = new Color(0.95f, 0.35f, 0.22f)
            },
            new StageTheme { // 5: 氷雪
                floorLight = new Color(0.85f, 0.90f, 0.95f), floorDark = new Color(0.70f, 0.78f, 0.85f),
                fogColor = new Color(0.85f, 0.90f, 0.95f), skyColor = new Color(0.75f, 0.85f, 0.95f),
                propColor = new Color(0.80f, 0.85f, 0.90f),
                groundTextureName = "GroundSnow", floorTint = Color.white
            },
        };

        int idx = (stage - 1) % themes.Length;
        if (idx < 0) idx = 0;
        return themes[idx];
    }

    // カメラの背景色・環境光・フォグを設定する。デフォルトの何もない灰色背景よりは画面が締まって見えるはず。
    void SetupLightingAndSky(StageTheme theme)
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = theme.skyColor;
        }

        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = theme.fogColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 15f;
        RenderSettings.fogEndDistance = 40f;
    }

    // グリッド全体を覆う床を、既存の構成より少し下(y=-0.02)に敷く。
    // 既に床がある場合はそっちが上に表示されて見えなくなるだけで、重なって壊れることはない。
    // 当たり判定(Collider)は外してあるので、クリック判定とか既存のロジックには影響しない。
    void SetupFloor(StageTheme theme)
    {
        // まずCC0の実写風テクスチャ(Resources/Textures/以下)を読み込んでみる。
        // 何かの理由で読み込めなかった場合(ファイルが置かれてないとか)は、従来の2x2チェッカーに
        // フォールバックするので、テクスチャ周りで何が起きても床自体は必ず表示される。
        Texture2D tex = LoadGroundTexture(theme);
        bool usingRealTexture = (tex != null);
        if (!usingRealTexture)
        {
            tex = BuildCheckerTexture(theme);
        }

        // シーンに既存の"MapPlane"(手で配置した地面)があれば、新しく床を作らずそっちへ直接テクスチャを当てる。
        // 前はこの下(y=-0.02)に別のProceduralFloorを新規生成してたけど、MapPlaneの方がy=0で不透明なせいで
        // 完全に隠れてしまって、チェッカーも実写テクスチャも一切画面に出てなかった
        // (「殺風景」「マップが見づらい」って感じてた本当の原因はこれだった)。
        // MapPlaneにはクリック判定用のColliderが付いてるので、そっちには一切手を加えず見た目だけ差し替える。
        GameObject mapPlane = GameObject.Find("MapPlane");
        Renderer rend = (mapPlane != null) ? mapPlane.GetComponent<Renderer>() : null;

        if (rend == null)
        {
            // MapPlaneが見つからない場合だけ、従来通り新しい床を作るフォールバック
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ProceduralFloor";
            Collider col = floor.GetComponent<Collider>();
            if (col != null) Destroy(col);

            float centerX = gridOrigin.x + (gridWidth * cellSize) / 2f;
            float centerZ = gridOrigin.z + (gridHeight * cellSize) / 2f;
            floor.transform.position = new Vector3(centerX, -0.02f, centerZ);
            // デフォルトのPlaneは10x10単位の大きさなので、実際のグリッドサイズに合わせてスケールする
            floor.transform.localScale = new Vector3(gridWidth * cellSize / 10f, 1f, gridHeight * cellSize / 10f);

            rend = floor.GetComponent<Renderer>();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.mainTexture = tex;
        // 実写テクスチャのときだけ色味補正(溶岩ステージを赤黒く焼けた感じにするとか)をかける。
        // チェッカーの方は色自体がもうテーマカラーそのものなので、ここで乗算すると意図とズレる。
        if (usingRealTexture)
        {
            mat.color = theme.floorTint;
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", new Vector2(gridWidth, gridHeight));
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", new Vector2(gridWidth, gridHeight));
        rend.material = mat;
    }

    // Resources/Textures/以下から、テーマに応じた実写風の地面テクスチャを読み込む。
    // 見つからない場合(未配置・パス違いとか)はnullを返して、呼び出し側でチェッカーにフォールバックさせる。
    Texture2D LoadGroundTexture(StageTheme theme)
    {
        if (string.IsNullOrEmpty(theme.groundTextureName)) return null;

        Texture2D tex = Resources.Load<Texture2D>("Textures/" + theme.groundTextureName);
        if (tex == null) return null;

        tex.wrapMode = TextureWrapMode.Repeat; // マス目に合わせてタイル状に繰り返す
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    // 2色を交互に並べたチェッカーボードのテクスチャを、画像ファイルを使わずその場で作る
    Texture2D BuildCheckerTexture(StageTheme theme)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixel(0, 0, theme.floorLight);
        tex.SetPixel(1, 1, theme.floorLight);
        tex.SetPixel(0, 1, theme.floorDark);
        tex.SetPixel(1, 0, theme.floorDark);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();
        return tex;
    }

    // グリッドの「外側」(プレイ領域の外)に岩っぽい装飾オブジェクトを並べて、地形っぽい雰囲気を出す。
    // プレイ領域の内側には絶対に置かないので、プレイヤー・敵のユニットと重なる心配がない
    // (=当たり判定・移動判定・クリック判定には一切影響しない、完全に見た目だけの要素)。
    // ステージが進むほど数が増えて、テーマの色が変わるので「進むごとに地形が変わる」感じになる。
    void SetupBorderProps(StageTheme theme)
    {
        int propCount = 12 + (PlayerProgress.currentStage - 1) * 4;

        // ステージ番号をシードにすることで、同じステージなら毎回同じ配置になる(完全ランダムで毎回変わるより安定する)
        System.Random rng = new System.Random(PlayerProgress.currentStage * 97 + 13);

        GameObject propsRoot = new GameObject("TerrainProps");

        for (int i = 0; i < propCount; i++)
        {
            bool onZEdge = rng.Next(2) == 0;
            float x, z;

            if (onZEdge)
            {
                x = gridOrigin.x + (float)(rng.NextDouble() * gridWidth * cellSize);
                z = (rng.Next(2) == 0)
                    ? gridOrigin.z - cellSize * (0.5f + (float)rng.NextDouble() * 1.5f)
                    : gridOrigin.z + gridHeight * cellSize + cellSize * (0.5f + (float)rng.NextDouble() * 1.5f);
            }
            else
            {
                z = gridOrigin.z + (float)(rng.NextDouble() * gridHeight * cellSize);
                x = (rng.Next(2) == 0)
                    ? gridOrigin.x - cellSize * (0.5f + (float)rng.NextDouble() * 1.5f)
                    : gridOrigin.x + gridWidth * cellSize + cellSize * (0.5f + (float)rng.NextDouble() * 1.5f);
            }

            CreateRockProp(propsRoot.transform, new Vector3(x, 0f, z), theme.propColor, rng);
        }
    }

    // 岩っぽい装飾を1個作る(見た目だけのオブジェクトで、Colliderは外してある)
    void CreateRockProp(Transform parent, Vector3 pos, Color baseColor, System.Random rng)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "TerrainProp";
        rock.transform.SetParent(parent, true);

        Collider col = rock.GetComponent<Collider>();
        if (col != null) Destroy(col);

        float scale = 0.3f + (float)rng.NextDouble() * 0.5f;
        rock.transform.position = pos + Vector3.up * (scale * 0.3f);
        rock.transform.localScale = new Vector3(scale, scale * (0.6f + (float)rng.NextDouble() * 0.3f), scale);
        rock.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

        Renderer rend = rock.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        float variance = 0.85f + (float)rng.NextDouble() * 0.3f;
        mat.color = new Color(
            Mathf.Clamp01(baseColor.r * variance),
            Mathf.Clamp01(baseColor.g * variance),
            Mathf.Clamp01(baseColor.b * variance)
        );
        rend.material = mat;
    }
}
