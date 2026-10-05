using UnityEngine;

public class ClickToWorldPosition : MonoBehaviour
{
    public float cellSize = 1.0f;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);

    public UnitController unit;
    public GameObject hitEffectPrefab;
    public GameObject damagePopupPrefab;
    public AttackRangeShower rangeShower;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        int damage = 20;
                        enemyHealth.TakeDamage(damage);

                        if (damagePopupPrefab != null)
                        {
                            Vector3 popupPos = hit.collider.transform.position + Vector3.up * 1.5f;
                            GameObject popup = Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
                            popup.GetComponent<DamagePopup>().Setup(damage);
                        }
                    }

                    if (hitEffectPrefab != null)
                    {
                        Instantiate(hitEffectPrefab, hit.collider.transform.position, Quaternion.identity);
                    }

                    return;
                }

                Vector3 clickPosition = hit.point;

                int gridX = Mathf.FloorToInt((clickPosition.x - gridOrigin.x) / cellSize);
                int gridZ = Mathf.FloorToInt((clickPosition.z - gridOrigin.z) / cellSize);

                if (rangeShower != null && rangeShower.HasActiveRange() && !rangeShower.IsValidCell(gridX, gridZ))
                {
                    Debug.Log("range NG");
                    return;
                }

                Vector3 destination = new Vector3(
                    gridOrigin.x + gridX * cellSize + cellSize / 2f,
                    0.5f,
                    gridOrigin.z + gridZ * cellSize + cellSize / 2f
                );

                if (unit != null)
                {
                    unit.MoveTo(destination);
                }

                if (rangeShower != null)
                {
                    rangeShower.ShowRange(destination);
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
            Vector3 start2 = gridOrigin + new Vector3(0, 0, z * cellSize);
            Vector3 end2 = gridOrigin + new Vector3(gridWidth * cellSize, 0, z * cellSize);
            Gizmos.DrawLine(start2, end2);
        }
    }
}