using UnityEngine;
using System.Collections.Generic;

// 「移動できるマス」を青っぽいハイライトで表示するクラス。
// ユニットをクリックして選んだ直後に、ClickToWorldPositionのrangeShowerから呼ばれる。
// 名前が似てるAttackRangeShower2は「攻撃できるマス」の表示用で役割が違うので注意。
public class AttackRangeShower : MonoBehaviour
{
    public GameObject rangeHighlightPrefab; // マス目に置くハイライト用のプレハブ
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int range = 1; // 中心から何マス分を範囲にするか
    public int gridWidth = 10;
    public int gridHeight = 10;

    private List<GameObject> activeHighlights = new List<GameObject>(); // 今出してるハイライト(消すときに使う)
    private HashSet<Vector2Int> validCells = new HashSet<Vector2Int>();  // 範囲内って判定したグリッド座標(クリックの判定用)

    // unitPositionを中心に、range四方のマスにハイライトを出す
    public void ShowRange(Vector3 unitPosition)
    {
        ClearRange(); // 前回のを消してから作り直す

        int centerX = Mathf.FloorToInt((unitPosition.x - gridOrigin.x) / cellSize);
        int centerZ = Mathf.FloorToInt((unitPosition.z - gridOrigin.z) / cellSize);

        var blockedCells = GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight);

        // 地形(ステージのテーマ)で移動範囲を補正する(岩場・溶岩は足場悪いから-1、氷雪は滑って+1)。
        // どんなに悪くても最低1マスは動けるようにしてある。
        int effectiveRange = Mathf.Max(1, range + TerrainEffects.GetMoveRangeModifier(PlayerProgress.currentStage));

        for (int x = -effectiveRange; x <= effectiveRange; x++)
        {
            for (int z = -effectiveRange; z <= effectiveRange; z++)
            {
                if (x == 0 && z == 0) continue; // 自分のマスは除く

                int targetX = centerX + x;
                int targetZ = centerZ + z;

                // グリッドの外のマスは無視
                if (targetX < 0 || targetX >= gridWidth || targetZ < 0 || targetZ >= gridHeight) continue;

                // 障害物のマスは移動できないから、移動範囲のハイライトにも入れない
                if (blockedCells.Contains(new Vector2Int(targetX, targetZ))) continue;

                Vector3 pos = new Vector3(
                    gridOrigin.x + targetX * cellSize + cellSize / 2f,
                    0.05f,
                    gridOrigin.z + targetZ * cellSize + cellSize / 2f
                );

                GameObject highlight = Instantiate(rangeHighlightPrefab, pos, Quaternion.identity);
                activeHighlights.Add(highlight);

                validCells.Add(new Vector2Int(targetX, targetZ));
            }
        }
    }

    // 指定したグリッド座標が「今表示してる範囲内」かどうか(移動先として有効かの判定に使う)
    public bool IsValidCell(int gridX, int gridZ)
    {
        return validCells.Contains(new Vector2Int(gridX, gridZ));
    }

    // 今、範囲が表示されてるかどうか(=ユニットが選ばれてるかの目印にも使ってる)
    public bool HasActiveRange()
    {
        return validCells.Count > 0;
    }

    // 表示中のハイライトを全部消す
    public void ClearRange()
    {
        foreach (GameObject h in activeHighlights)
        {
            if (h != null) Destroy(h);
        }
        activeHighlights.Clear();
        validCells.Clear();
    }
}
