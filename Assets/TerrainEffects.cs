// 地形(ScenePolishが決めてるステージのテーマ: 草原→岩場→砂漠→溶岩→氷雪)によって、
// 見た目だけじゃなくて実際のゲームプレイ(移動範囲)にも変化をつけるための静的クラス。
// GridObstaclesと同じ考え方で、ステージ番号(PlayerProgress.currentStage)だけから
// 結果が決まる(状態を持ってないから、どのスクリプトがどの順番で呼んでも結果は同じになる)。
public static class TerrainEffects
{
    // ステージ番号に応じた移動範囲の補正値を返す。AttackRangeShower.ShowRangeで
    // 基本の移動範囲に足して使う(最終的な範囲は呼び出し側で最低1マスになるようにしてある)。
    //   草原: 補正なし(基準になる歩きやすい地形)
    //   岩場: -1 (足場が悪くて、普通より動きにくい)
    //   砂漠: 補正なし(見た目は変わるけど、動きやすさ自体は平地と同じくらい)
    //   溶岩: -1 (危なくて足場が悪い地形。岩場と同じ理由で動きにくい)
    //   氷雪: +1 (氷の上を滑って、かえって遠くまで進める)
    public static int GetMoveRangeModifier(int stage)
    {
        int idx = GetThemeIndex(stage);

        switch (idx)
        {
            case 1: return -1; // 岩場
            case 3: return -1; // 溶岩
            case 4: return 1;  // 氷雪
            default: return 0; // 草原・砂漠
        }
    }

    // ステージ番号に応じた地形テーマ名(ScenePolishの配色テーマに対応する表示用の名前)を返す
    public static string GetThemeName(int stage)
    {
        int idx = GetThemeIndex(stage);

        switch (idx)
        {
            case 0: return "草原";
            case 1: return "岩場";
            case 2: return "砂漠";
            case 3: return "溶岩";
            case 4: return "氷雪";
            default: return "";
        }
    }

    // UI表示用: 地形名に移動範囲の補正値をくっつけた文字列を返す(補正が無いときは地形名だけ)。
    // 例: "岩場(-1)" "氷雪(+1)" "草原"
    public static string GetThemeLabel(int stage)
    {
        string name = GetThemeName(stage);
        int mod = GetMoveRangeModifier(stage);

        if (mod == 0) return name;
        string sign = (mod > 0) ? "+" : ""; // マイナスの値はToString自体に"-"が付くのでそのままでOK
        return name + "(" + sign + mod + ")";
    }

    // ScenePolish.GetThemeと全く同じ計算式(5種類のテーマを順番に繰り返す)。
    // ScenePolish側のテーマ定義はprivateなインスタンスメソッドだから、ここでは同じ式を別に持ってる。
    static int GetThemeIndex(int stage)
    {
        int idx = (stage - 1) % 5;
        if (idx < 0) idx = 0;
        return idx;
    }
}
