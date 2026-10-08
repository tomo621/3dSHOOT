using UnityEngine;

// 敵のHPを管理するクラス。
// ダメージを受けたら頭上のHPバーを更新して、0以下になったら撃破の処理をする。
// 倒したらGameManager.CheckEnemies()を呼んで、敵が全滅してたら勝利判定につながる。
public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int expReward = 20; // この敵を倒したときにプレイヤーに入る経験値の基本値
    private int currentHP;
    private HealthBarController healthBar; // 頭上に出すHPバー(無かったら自動でつける)

    void Start()
    {
        currentHP = maxHP;

        healthBar = GetComponent<HealthBarController>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<HealthBarController>();
        }
        healthBar.SetHP(1f);

        // 見た目をCubeから3Dモデルに変える(モデルが無ければCubeのままでも大丈夫)
        UnitVisual visual = GetComponent<UnitVisual>();
        if (visual == null)
        {
            visual = gameObject.AddComponent<UnitVisual>();
            visual.modelResourcePath = "Models/EnemyUnit";
        }
    }

    // ダメージを受けたときの処理。ClickToWorldPosition.ApplyAttackDamageから呼ばれる
    public void TakeDamage(int damage)
    {
        AudioFX.PlayHit();

        currentHP -= damage;
        Debug.Log(gameObject.name + " が " + damage + " ダメージ受けた。残りHP: " + currentHP);

        // HPの割合をHPバーに反映する(バーの色が変わる処理はHealthBarController側でやってる)
        if (healthBar != null)
        {
            healthBar.SetHP((float)currentHP / maxHP);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 撃破処理: 経験値を入れて、Enemyタグを外してから破棄。あとGameManagerに敵の数を数え直してもらう
    void Die()
    {
        AudioFX.PlayEnemyDeath();

        Debug.Log(gameObject.name + " は倒れた");

        // ステージが進むほど経験値がちょっと多くなるようにしてる(ゆるく右肩上がり)
        int reward = expReward + (PlayerProgress.currentStage - 1) * 5;
        int levelUps = PlayerProgress.AddExp(reward);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshProgressUI(); // 経験値バー・レベル表示を更新

            // レベルアップした回数分、強化選択画面(HP/射程/移動距離/球の数から1つ選ぶやつ)を出す
            for (int i = 0; i < levelUps; i++)
            {
                GameManager.Instance.ShowLevelUpChoice();
            }
        }

        gameObject.tag = "Untagged"; // 破棄する前にタグを外しておく。じゃないと破棄直後の1フレームで他の処理がまだEnemyとして拾っちゃうことがある
        Destroy(gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckEnemies(); // 敵が0体になってたら勝利処理が走る
        }
    }
}
