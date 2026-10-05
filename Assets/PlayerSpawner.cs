using UnityEngine;

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

            int x = Random.Range(0, gridWidth / 2);
            int z = Random.Range(0, gridHeight);

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