using UnityEngine;
using System.Collections.Generic;

public class AttackRangeShower2 : MonoBehaviour
{
    public GameObject attackHighlightPrefab;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int attackRange = 1;
    public int gridWidth = 10;
    public int gridHeight = 10;

    private List<GameObject> activeHighlights = new List<GameObject>();

    public void ShowAttackRange(Vector3 unitPosition)
    {
        ClearRange();

        int centerX = Mathf.FloorToInt((unitPosition.x - gridOrigin.x) / cellSize);
        int centerZ = Mathf.FloorToInt((unitPosition.z - gridOrigin.z) / cellSize);

        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int z = -attackRange; z <= attackRange; z++)
            {
                if (x == 0 && z == 0) continue;

                int targetX = centerX + x;
                int targetZ = centerZ + z;

                if (targetX < 0 || targetX >= gridWidth || targetZ < 0 || targetZ >= gridHeight) continue;

                Vector3 pos = new Vector3(
                    gridOrigin.x + targetX * cellSize + cellSize / 2f,
                    0.06f,
                    gridOrigin.z + targetZ * cellSize + cellSize / 2f
                );

                GameObject highlight = Instantiate(attackHighlightPrefab, pos, Quaternion.identity);
                activeHighlights.Add(highlight);
            }
        }
    }

    public void ClearRange()
    {
        foreach (GameObject h in activeHighlights)
        {
            if (h != null) Destroy(h);
        }
        activeHighlights.Clear();
    }
}