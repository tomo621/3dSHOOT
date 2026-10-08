using UnityEngine;

// プレイヤーのHPを管理するクラス。
// ダメージを受けたらダメージポップアップを出して、頭上のHPバーも更新する。
// HPが0以下になったらGameManager.EndGame(false)を呼んで敗北の処理をする。
public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100; // 強化前の基本の最大HP
    public int hpPerBonus = 20; // レベルアップ選択画面で「HP+」を選ぶごとに増える最大HPの量
    public GameObject damagePopupPrefab;
    private int currentHP;
    private int effectiveMaxHP; // PlayerProgress.levelを反映した実際の最大HP
    private HealthBarController healthBar; // 頭上に出すHPバー(無かったら自動でつける)
    private bool isDead = false; // 敗北処理を呼んだ後に、もう一回EndGameが呼ばれるのを防ぐフラグ

    void Start()
    {
        // レベルアップ選択画面で選んだ「HP+」の回数ぶん最大HPを上げる。ステージが始まるたびに全回復させてる。
        effectiveMaxHP = maxHP + PlayerProgress.bonusHP * hpPerBonus;
        currentHP = effectiveMaxHP;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshPlayerHP(currentHP, effectiveMaxHP);
        }

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
            visual.modelResourcePath = "Models/PlayerUnit";
        }
    }

    // ダメージを受けたときの処理。GameManager.EnemyTurnの中(敵の近接攻撃が当たったとき)から呼ばれる
    public void TakeDamage(int damage)
    {
        if (isDead) return; // もう敗北処理済みなら何もしない

        AudioFX.PlayPlayerDamage();

        currentHP -= damage;
        Debug.Log("player damaged: " + damage + " hp: " + currentHP);

        // ダメージポップアップを出す(EnemyHealthと違って、こっちはPlayerHealth側でポップアップの生成までやってる)
        if (damagePopupPrefab != null)
        {
            Vector3 popupPos = transform.position + Vector3.up * 1.5f;
            GameObject popup = Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
            {
                dp.Setup(damage);
            }
        }

        // HPの割合をHPバーに反映する(バーの色が変わる処理はHealthBarController側でやってる)
        if (healthBar != null)
        {
            healthBar.SetHP((float)currentHP / effectiveMaxHP);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshPlayerHP(Mathf.Max(0, currentHP), effectiveMaxHP);
        }

        if (currentHP <= 0)
        {
            isDead = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame(false); // プレイヤーが負けたときの処理
            }
        }
    }
}
