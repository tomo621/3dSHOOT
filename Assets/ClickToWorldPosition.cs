using UnityEngine;

public class ClickToWorldPosition : MonoBehaviour
{
    public float cellSize = 1.0f;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);

    // CubeをInspectorからセットする
    public UnitController unit;

    // 攻撃エフェクトのPrefabをInspectorからセットする
    public GameObject hitEffectPrefab;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 敵をクリックした場合 → 攻撃処理
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(20);
                    }

                    // 攻撃エフェクトを敵の位置に出す
                    if (hitEffectPrefab != null)
                    {
                        Instantiate(hitEffectPrefab, hit.collider.transform.position, Quaternion.identity);
                    }

                    return; // 移動処理はしない
                }

                // それ以外(地面)をクリックした場合 → 移動処理
                Vector3 clickPosition = hit.point;

                int gridX = Mathf.FloorToInt((clickPosition.x - gridOrigin.x) / cellSize);
                int gridZ = Mathf.FloorToInt((clickPosition.z - gridOrigin.z) / cellSize);

                Vector3 destination = new Vector3(
                    gridOrigin.x + gridX * cellSize + cellSize / 2f,
                    0.5f,
                    gridOrigin.z + gridZ * cellSize + cellSize / 2f
                );

                Debug.Log("移動先マス: (" + gridX + ", " + gridZ + ")  ワールド座標: " + destination);

                if (unit != null)
                {
                    unit.MoveTo(destination);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x * cellSize, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0, z * cellSize);
            Vector3 end = gridOrigin + new Vector3(gridWidth * cellSize, 0, z * cellSize);
            Gizmos.DrawLine(start, end);
        }
    }
}