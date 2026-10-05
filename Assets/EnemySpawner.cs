using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 3;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Transform playerTransform;
    public int minDistanceFromPlayer = 4;

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        List<Vector2Int> usedCells = new List<Vector2Int>();

        int playerGridX = 0;
        int playerGridZ = 0;

        if (playerTransform != null)
        {
            playerGridX = Mathf.FloorToInt((playerTransform.position.x - gridOrigin.x) / cellSize);
            playerGridZ = Mathf.FloorToInt((playerTransform.position.z - gridOrigin.z) / cellSize);
        }

        int spawned = 0;
        int safety = 0;

        while (spawned < enemyCount && safety < 200)
        {
            safety++;

            int x = Random.Range(gridWidth / 2, gridWidth);
            int z = Random.Range(0, gridHeight);

            Vector2Int cell = new Vector2Int(x, z);

            if (usedCells.Contains(cell)) continue;

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

            Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawned++;
        }
    }
}