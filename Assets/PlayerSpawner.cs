using UnityEngine;

// ゲーム開始時にプレイヤーをグリッドの左半分のどこかにランダムで置くクラス。
// EnemySpawner(右半分に置く方)とペアになってて、最初からプレイヤーと敵がある程度離れた状態で始まるようにしてる。
public class PlayerSpawner : MonoBehaviour
{
    public Transform playerTransform;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;

    void Awake()
    {
        if (playerTransform != null)
        {
            Debug.Log("PlayerSpawner Awake OK name=" + playerTransform.name);

            var blockedCells = GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight);

            // xはグリッドの左半分(0〜gridWidth/2-1)からだけ選ぶ。障害物マスに当たったら引き直す
            int x = 0;
            int z = 0;
            int safety = 0;

            do
            {
                x = Random.Range(0, gridWidth / 2);
                z = Random.Range(0, gridHeight);
                safety++;
            }
            while (blockedCells.Contains(new Vector2Int(x, z)) && safety < 200);

            Vector3 pos = new Vector3(gridOrigin.x + x * cellSize + cellSize / 2f, 0.5f, gridOrigin.z + z * cellSize + cellSize / 2f);

            playerTransform.position = pos;

            Debug.Log("PlayerSpawner moved to " + pos);
        }
        else
        {
            Debug.Log("PlayerSpawner playerTransform NULL");
        }
    }
}
