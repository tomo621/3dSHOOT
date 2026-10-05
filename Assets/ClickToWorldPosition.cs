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
    public AttackRangeShower2 attackRangeShower;
    public int attackRange = 1;

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlayerTurn())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    int unitGridX = Mathf.FloorToInt((unit.transform.position.x - gridOrigin.x) / cellSize);
                    int unitGridZ = Mathf.FloorToInt((unit.transform.position.z - gridOrigin.z) / cellSize);

                    int enemyGridX = Mathf.FloorToInt((hit.collider.transform.position.x - gridOrigin.x) / cellSize);
                    int enemyGridZ = Mathf.FloorToInt((hit.collider.transform.position.z - gridOrigin.z) / cellSize);

                    int diffX = Mathf.Abs(unitGridX - enemyGridX);
                    int diffZ = Mathf.Abs(unitGridZ - enemyGridZ);

                    if (diffX > attackRange || diffZ > attackRange)
                    {
                        Debug.Log("too far");
                        return;
                    }

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

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EndPlayerTurn();
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

                float destX = gridOrigin.x + gridX * cellSize + cellSize / 2f;
                float destZ = gridOrigin.z + gridZ * cellSize + cellSize / 2f;
                Vector3 destination = new Vector3(destX, 0.5f, destZ);

                if (unit != null)
                {
                    unit.MoveTo(destination);
                }

                if (rangeShower != null)
                {
                    rangeShower.ShowRange(destination);
                }

                if (attackRangeShower != null)
                {
                    attackRangeShower.ShowAttackRange(destination);
                }

                bool enemyInRange = false;
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject enemy in enemies)
                {
                    int enemyGridX2 = Mathf.FloorToInt((enemy.transform.position.x - gridOrigin.x) / cellSize);
                    int enemyGridZ2 = Mathf.FloorToInt((enemy.transform.position.z - gridOrigin.z) / cellSize);

                    int diffX2 = Mathf.Abs(gridX - enemyGridX2);
                    int diffZ2 = Mathf.Abs(gridZ - enemyGridZ2);

                    if (diffX2 <= attackRange && diffZ2 <= attackRange)
                    {
                        enemyInRange = true;
                        break;
                    }
                }

                if (!enemyInRange && GameManager.Instance != null)
                {
                    GameManager.Instance.EndPlayerTurn();
                }
            }
        }
    }
}