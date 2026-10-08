using UnityEngine;
using TMPro;

// ダメージの数字をポンと出すやつ。
// 攻撃が当たった場所に出てきて、上にふわっと浮きながらフェードアウトして自動的に消える。
// 出してる場所: ClickToWorldPosition.ApplyAttackDamage(敵を攻撃したとき) / PlayerHealth.TakeDamage(自分が攻撃されたとき)
public class DamagePopup : MonoBehaviour
{
    public float floatSpeed = 1f; // 浮き上がる速さ
    public float lifeTime = 1f;   // 何秒で消えるか

    private TextMeshProUGUI text;
    private float timer;
    private Color startColor;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        startColor = text.color;
    }

    // ダメージの数値を受け取ってテキストにセットする(生成した直後に呼んでもらう用)
    public void Setup(int damageAmount)
    {
        text.text = "-" + damageAmount.ToString();
    }

    void Update()
    {
        // 上にゆっくり動きつつ、常にカメラの方を向くようにする
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        transform.forward = Camera.main.transform.forward;

        // 時間が経つにつれて透明にしていく(lifeTime秒かけて1→0になる感じ)
        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifeTime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= lifeTime)
        {
            Destroy(gameObject); // 時間切れになったら自分で消える
        }
    }
}
