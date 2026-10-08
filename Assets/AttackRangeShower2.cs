using UnityEngine;
using System.Collections.Generic;

// 「攻撃できるマス」を表示するクラス(AttackRangeShowerの攻撃バージョン)。
// ユニットの移動先が決まったあとに、ClickToWorldPositionのattackRangeShowerから呼ばれて、
// そこから攻撃が届く範囲を見た目だけ出す(あくまで見た目だけで、攻撃できるかどうかの判定自体はClickToWorldPosition側のdiffX/diffZでやってる)。
public class AttackRangeShower2 : MonoBehaviour
{
    public GameObject attackHighlightPrefab; // マス目に置くハイライト用のプレハブ(AttackRangeShowerとは違う見た目にするつもり)
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int attackRange = 1; // 中心から何マス分を攻撃範囲にするか
    public int gridWidth = 10;
    public int gridHeight = 10;

    private List<GameObject> activeHighlights = new List<GameObject>(); // 今表示してるハイライト(消すときに使う)

    // unitPositionを中心に、attackRange以内に敵が実際にいるマスだけ攻撃範囲のハイライトを出す。
    // 前はattackRange四方のマス全部(最大(2*range+1)^2-1マス)を塗ってたんだけど、
    // 移動範囲(AttackRangeShowerの青)とほぼ全部重なって画面が常に青赤だらけになっちゃって、
    // 「マップ見づらい」の大きな原因になってた。
    // 実際に攻撃できる敵がいるマスだけ出す方が、見た目もすっきりするし分かりやすい。
    public void ShowAttackRange(Vector3 unitPosition)
    {
        ClearRange();

        int centerX = Mathf.FloorToInt((unitPosition.x - gridOrigin.x) / cellSize);
        int centerZ = Mathf.FloorToInt((unitPosition.z - gridOrigin.z) / cellSize);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            int targetX = Mathf.FloorToInt((enemy.transform.position.x - gridOrigin.x) / cellSize);
            int targetZ = Mathf.FloorToInt((enemy.transform.position.z - gridOrigin.z) / cellSize);

            int diffX = Mathf.Abs(targetX - centerX);
            int diffZ = Mathf.Abs(targetZ - centerZ);

            if (diffX > attackRange || diffZ > attackRange) continue; // 攻撃範囲の外にいる敵は対象外
            if (diffX == 0 && diffZ == 0) continue; // 自分のマス(普通は起きないけど念のため)

            Vector3 pos = new Vector3(
                gridOrigin.x + targetX * cellSize + cellSize / 2f,
                0.06f, // AttackRangeShower(0.05f)よりちょっと高くして、移動範囲の表示と重なっても上に出るようにしてる
                gridOrigin.z + targetZ * cellSize + cellSize / 2f
            );

            GameObject highlight = Instantiate(attackHighlightPrefab, pos, Quaternion.identity);
            activeHighlights.Add(highlight);
        }
    }

    // 表示中のハイライトを全部消す
    public void ClearRange()
    {
        foreach (GameObject h in activeHighlights)
        {
            if (h != null) Destroy(h);
        }
        activeHighlights.Clear();
    }
}
