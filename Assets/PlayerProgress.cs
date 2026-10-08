using UnityEngine;

// レベルアップのときに選べる強化の種類(ツリーっぽく4択から選ぶ)
public enum UpgradeType
{
    HP,
    AttackRange,
    MoveRange,
    BulletCount
}

// プレイヤーの経験値・レベル・今のステージ数・強化の内訳をまとめて持ってる静的クラス。
// staticにしてあるから、SceneManager.LoadSceneでシーンを読み込み直しても値が消えない
// (Unityの再生を止めるかゲームを終了するまでは、値がずっと残ってる)。
// ステージが進むとき(GameManager.NextStage)とかリトライ(GameManager.RestartGame)をまたいでも
// レベル・経験値・強化内容を持ち越すために、この仕組みを使ってる。
public static class PlayerProgress
{
    public static int level = 1;
    public static int currentExp = 0;
    public static int currentStage = 1;

    // 溶岩ステージ(Stage4)の次に出てくるボスのステージ番号。
    // このステージだけはEnemySpawnerが雑魚の代わりにボスを1体だけ出してきて、
    // 倒すとGameManager側で普通の「Stage Clear」じゃなく「Game Clear」として扱われる。
    public const int BossStage = 5;

    // レベルアップのたびに選んだ強化の回数(4択からひとつずつ選んで伸ばしていく感じ)
    public static int bonusHP = 0;          // HP+を選んだ回数
    public static int bonusAttackRange = 0; // 射程+を選んだ回数
    public static int bonusMoveRange = 0;   // 移動距離+を選んだ回数
    public static int bonusBulletCount = 0; // 球の数+を選んだ回数

    // 次のレベルアップに必要な経験値(シンプルな右肩上がりの式。調整したくなったらここだけ変えればOK)
    public static int ExpToNextLevel()
    {
        return 50 + (level - 1) * 25;
    }

    // 経験値を加算する。必要経験値を超えてたら何回でもレベルアップさせて、上がった回数を返す。
    // レベルアップした「回数」を返すだけで、実際にどの強化を選ぶかはGameManager側のUIで決める。
    public static int AddExp(int amount)
    {
        currentExp += amount;
        int levelUps = 0;

        while (currentExp >= ExpToNextLevel())
        {
            currentExp -= ExpToNextLevel();
            level++;
            levelUps++;
        }

        return levelUps;
    }

    // レベルアップ選択画面(LevelUpChoiceUI)で選んだ強化をここに反映する
    public static void ApplyUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.HP:
                bonusHP++;
                break;
            case UpgradeType.AttackRange:
                bonusAttackRange++;
                break;
            case UpgradeType.MoveRange:
                bonusMoveRange++;
                break;
            case UpgradeType.BulletCount:
                bonusBulletCount++;
                break;
        }
    }

    // 全部の進行状況を初期値に戻す。ボスを倒してタイトルに戻るとき(GameManager.ReturnToTitle)に呼ばれてる。
    public static void ResetAll()
    {
        level = 1;
        currentExp = 0;
        currentStage = 1;
        bonusHP = 0;
        bonusAttackRange = 0;
        bonusMoveRange = 0;
        bonusBulletCount = 0;
    }
}
