using UnityEngine;
using System.Collections.Generic;

// ゲーム開始時に敵をグリッド上にランダムで配置するクラス。
// プレイヤーはグリッドの左半分(PlayerSpawner参照)、敵は右半分に出すことで
// 開始時点である程度の距離を保つようにしてる。
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 3;           // ステージ1での基本の敵数
    public int extraEnemiesPerStage = 1; // ステージが進むごとに増える敵の数
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Transform playerTransform;
    public int minDistanceFromPlayer = 4; // プレイヤーとの最低マンハッタン距離(近すぎる位置には出さない)

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        // 溶岩ステージ(Stage4)の次はボス戦。雑魚敵の代わりにボスを1体だけ出現させる。
        if (PlayerProgress.currentStage == PlayerProgress.BossStage)
        {
            SpawnBoss();
            return;
        }

        List<Vector2Int> usedCells = new List<Vector2Int>(); // もう使ったマス(敵同士が重ならないように)
        HashSet<Vector2Int> blockedCells = GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight);

        int playerGridX = 0;
        int playerGridZ = 0;

        if (playerTransform != null)
        {
            playerGridX = Mathf.FloorToInt((playerTransform.position.x - gridOrigin.x) / cellSize);
            playerGridZ = Mathf.FloorToInt((playerTransform.position.z - gridOrigin.z) / cellSize);
        }

        // ステージが進むほど敵の数を増やす(PlayerProgress.currentStageはステージをまたいで保持される)
        int targetCount = enemyCount + (PlayerProgress.currentStage - 1) * extraEnemiesPerStage;

        int spawned = 0;
        int safety = 0; // 条件に合うマスがなかなか見つからないときの無限ループ防止用カウンタ

        while (spawned < targetCount && safety < 200)
        {
            safety++;

            // xはグリッドの右半分(gridWidth/2〜gridWidth-1)からだけ選ぶ
            int x = Random.Range(gridWidth / 2, gridWidth);
            int z = Random.Range(0, gridHeight);

            Vector2Int cell = new Vector2Int(x, z);

            if (usedCells.Contains(cell)) continue; // もう他の敵がいるマスならやり直し
            if (blockedCells.Contains(cell)) continue; // 障害物マスならやり直し

            // プレイヤーとのマンハッタン距離(|dx|+|dz|)が近すぎる場合もやり直し
            int diffX = Mathf.Abs(x - playerGridX);
            int diffZ = Mathf.Abs(z - playerGridZ);
            int dist = diffX + diffZ;

            if (dist < minDistanceFromPlayer) continue;

            usedCells.Add(cell);

            Vector3 pos = new Vector3(
                gridOrigin.x + x * cellSize + cellSize / 2f,
                0.5f,
                gridOrigin.z + z * cellSize + cellSize / 2f
            );

            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            SetupEnemyKind(enemy, spawned);

            spawned++;
        }
    }

    // ステージ2以降、3体に1体を遠距離タイプにする(混ぜることで行動に幅を持たせる)。
    // 見た目でも区別できるように、専用モデルがあるかどうかに関わらず分かる変化(大きさ・色)を加える。
    void SetupEnemyKind(GameObject enemy, int spawnIndex)
    {
        bool isRanged = (PlayerProgress.currentStage > 1) && (spawnIndex % 3 == 2);

        EnemyKind kind = enemy.AddComponent<EnemyKind>();
        kind.type = isRanged ? EnemyType.Ranged : EnemyType.Melee;

        if (!isRanged) return;

        enemy.transform.localScale = Vector3.one * 0.85f;

        Renderer rend = enemy.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = Color.cyan; // 遠距離攻撃の弾(AttackFX)と同じ色にして、役割を連想しやすくする
            rend.material = mat;
        }
    }

    // ボス戦(PlayerProgress.BossStage)用に、普通より大きくてHPも攻撃力も高い敵を1体だけ出す。
    // 見た目もひと目でボスって分かるように、大きなサイズと発光する紫色にする。
    void SpawnBoss()
    {
        int playerGridX = 0;
        int playerGridZ = 0;

        if (playerTransform != null)
        {
            playerGridX = Mathf.FloorToInt((playerTransform.position.x - gridOrigin.x) / cellSize);
            playerGridZ = Mathf.FloorToInt((playerTransform.position.z - gridOrigin.z) / cellSize);
        }

        HashSet<Vector2Int> blockedCells = GridObstacles.GetBlockedCells(PlayerProgress.currentStage, gridWidth, gridHeight);

        // グリッドの右端寄り、プレイヤーから離れたマスに固定で出す
        int x = gridWidth - 1;
        int z = gridHeight / 2;

        if (blockedCells.Contains(new Vector2Int(x, z)) || (x == playerGridX && z == playerGridZ))
        {
            z = 0; // 万一そこが障害物マスとかだった場合の保険
        }

        Vector3 pos = new Vector3(
            gridOrigin.x + x * cellSize + cellSize / 2f,
            0.5f,
            gridOrigin.z + z * cellSize + cellSize / 2f
        );

        GameObject boss = Instantiate(enemyPrefab, pos, Quaternion.identity);
        boss.name = "Boss";

        // 通常の敵の2倍のサイズにして、ひと目でボスって分かるようにする
        boss.transform.localScale = Vector3.one * 2f;

        Renderer rend = boss.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            Color bossColor = new Color(0.62f, 0.08f, 0.85f); // 光る紫
            mat.color = bossColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", bossColor * 1.5f);
            }
            rend.material = mat;
        }

        EnemyKind kind = boss.AddComponent<EnemyKind>();
        kind.type = EnemyType.Melee;
        kind.attackDamage = 25; // 通常の敵(10)より高いダメージ

        EnemyHealth health = boss.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.maxHP = 500;   // 通常の敵(100)よりずっと高いHP
            health.expReward = 300;
        }
    }
}
