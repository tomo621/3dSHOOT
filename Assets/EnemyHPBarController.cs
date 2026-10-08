using UnityEngine;
using UnityEngine.UI;

// 敵の頭上に出すHPバー(ワールド空間UI)を動かすやつ。
// 対象の敵をずっと追いかけて、カメラの方をずっと向くようにして「頭上にHPバーが浮いてる」感じを出してる。
// ※気になる点: 今のところ他のスクリプトからSetTarget/SetHPを呼んでるところが見当たらない。
// Inspectorのイベントとかで繋いでないと動いてない可能性があるので、
// HPバーが反応しないときはプレハブ(HPber.prefab)の設定を見てみてください。
public class EnemyHPBarController : MonoBehaviour
{
    public Slider hpSlider;
    private Transform target;                       // 追いかける相手(敵)のTransform
    private Vector3 offset = new Vector3(0, 1.5f, 0); // 敵の頭上に出すためのズラし量

    void Awake()
    {
        // 子にあるCanvasをワールド空間表示に変えて、3D空間に馴染むサイズまで縮める
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
    }

    // このHPバーが追いかける相手をセットする
    public void SetTarget(Transform enemyTransform)
    {
        target = enemyTransform;
    }

    // HPバーの表示割合を更新する(0〜1の範囲にする)
    public void SetHP(float currentHP, float maxHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP / maxHP;
        }
    }

    void LateUpdate()
    {
        // 相手の位置に追従しつつ、常にカメラの方を向く(いわゆるビルボード)
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.forward = Camera.main.transform.forward;
        }
    }
}
