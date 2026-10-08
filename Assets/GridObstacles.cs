using UnityEngine;
using System.Collections.Generic;

// グリッドの中に置く「入れないマス」を、ステージ番号から計算する静的クラス。
// 状態は何も持ってなくて、呼ばれた瞬間に(stage, gridWidth, gridHeight)だけで同じ結果になるから、
// ClickToWorldPosition/EnemyAI/AttackRangeShower/各Spawnerとか、どのスクリプトがどの順番で
// 呼んでも(Awake/Startの実行順がどうでも)絶対に同じ障害物マスになる
// (ScenePolishがステージ番号をシードに装飾を決めてるのと同じやり方)。
public static class GridObstacles
{
    // 指定したステージとグリッドサイズに対する、障害物マスの集合を返す
    public static HashSet<Vector2Int> GetBlockedCells(int stage, int gridWidth, int gridHeight)
    {
        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

        if (gridWidth <= 2 || gridHeight <= 2) return blocked; // 外周を除く余裕がないグリッドには何も置かない

        int count = Mathf.Min(3 + (stage - 1), 6); // ステージが進むにつれて少しずつ増える(最大6個)
        System.Random rng = new System.Random(stage * 131 + 7); // ステージ番号をシードにして、毎回同じ配置になるようにする

        int safety = 0;
        while (blocked.Count < count && safety < 200)
        {
            safety++;
            int x = rng.Next(1, gridWidth - 1);  // 外周1マスは空けとく(見た目の装飾と被らないように)
            int z = rng.Next(1, gridHeight - 1);
            blocked.Add(new Vector2Int(x, z));
        }

        return blocked;
    }
}

// GridObstacles.GetBlockedCellsで決まったマスの上に、入れないことを示す見た目(箱っぽいもの)を置くだけのクラス。
// 判定自体はGridObstacles側でやってるので、ここは完全に見た目担当(Colliderは外してあるから、
// クリック判定とかの既存ロジックには影響しない)。GameManager.Awakeで自動的にアタッチされる
// (ScenePolishやEnemyAIと同じやり方)。
public class GridObstacleView : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);

    void Start()
    {
        HashSet<Vector2Int> blocked = GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight);

        GameObject root = new GameObject("GridObstacles");

        foreach (Vector2Int cell in blocked)
        {
            CreateObstacleProp(root.transform, cell);
        }
    }

    void CreateObstacleProp(Transform parent, Vector2Int cell)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "GridObstacle";
        obstacle.transform.SetParent(parent, true);

        Collider col = obstacle.GetComponent<Collider>();
        if (col != null) Destroy(col); // 見た目だけでいい。入れるかどうかの判定はGridObstacles.GetBlockedCells側でやってる

        float height = 1.2f;
        Vector3 pos = new Vector3(
            gridOrigin.x + cell.x * cellSize + cellSize / 2f,
            height / 2f,
            gridOrigin.z + cell.y * cellSize + cellSize / 2f
        );

        obstacle.transform.position = pos;
        obstacle.transform.localScale = new Vector3(cellSize * 0.8f, height, cellSize * 0.8f);

        Renderer rend = obstacle.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = new Color(0.35f, 0.3f, 0.28f); // 障害物ってひと目で分かるように、ちょっと暗めの色にしてる
        rend.material = mat;
    }
}
