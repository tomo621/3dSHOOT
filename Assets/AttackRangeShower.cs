using UnityEngine;
using System.Collections.Generic;

public class AttackRangeShower : MonoBehaviour
{
    public GameObject rangeHighlightPrefab;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int range = 1;

    private List<GameObject> activeHighlights = new List<GameObject>();
    private HashSet<Vector2Int> validCells = new HashSet<Vector2Int>();

    public void ShowRange(Vector3 unitPosition)
    {
        ClearRange();

        int centerX = Mathf.FloorToInt((unitPosition.x - gridOrigin.x) / cellSize);
        int centerZ = Mathf.FloorToInt((unitPosition.z - gridOrigin.z) / cellSize);

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                if (x == 0 && z == 0) continue;

                int targetX = centerX + x;
                int targetZ = centerZ + z;

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

    public bool IsValidCell(int gridX, int gridZ)
    {
        return validCells.Contains(new Vector2Int(gridX, gridZ));
    }

    public bool HasActiveRange()
    {
        return validCells.Count > 0;
    }

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
