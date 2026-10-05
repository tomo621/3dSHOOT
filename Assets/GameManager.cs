using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject winText;
    public GameObject playerUnit;
    public float cellSize = 1.0f;
    public Vector3 gridOrigin = new Vector3(-5f, 0f, -5f);
    public int gridWidth = 10;
    public int gridHeight = 10;
    public TextMeshProUGUI turnText;

    private bool isPlayerTurn = true;

    void Awake()
    {
        Instance = this;

        if (winText != null)
        {
            winText.SetActive(false);
        }

        UpdateTurnText();
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    void UpdateTurnText()
    {
        if (turnText != null)
        {
            turnText.text = isPlayerTurn ? "Your Turn" : "Enemy Turn";
        }
    }

    public void EndPlayerTurn()
    {
        isPlayerTurn = false;
        UpdateTurnText();
        Debug.Log("enemy turn");
        Invoke("EnemyTurn", 1.5f);
    }

    void EnemyTurn()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length > 0 && playerUnit != null)
        {
            GameObject mover = enemies[Random.Range(0, enemies.Length)];

            int moverGridX = Mathf.FloorToInt((mover.transform.position.x - gridOrigin.x) / cellSize);
            int moverGridZ = Mathf.FloorToInt((mover.transform.position.z - gridOrigin.z) / cellSize);

            int dx = Random.Range(-1, 2);
            int dz = Random.Range(-1, 2);

            int newGridX = Mathf.Clamp(moverGridX + dx, 0, gridWidth - 1);
            int newGridZ = Mathf.Clamp(moverGridZ + dz, 0, gridHeight - 1);

            Vector3 newPos = new Vector3(
                gridOrigin.x + newGridX * cellSize + cellSize / 2f,
                mover.transform.position.y,
                gridOrigin.z + newGridZ * cellSize + cellSize / 2f
            );

            mover.transform.position = newPos;
            Debug.Log(mover.name + " moved");

            int playerGridX = Mathf.FloorToInt((playerUnit.transform.position.x - gridOrigin.x) / cellSize);
            int playerGridZ = Mathf.FloorToInt((playerUnit.transform.position.z - gridOrigin.z) / cellSize);

            int diffX = Mathf.Abs(newGridX - playerGridX);
            int diffZ = Mathf.Abs(newGridZ - playerGridZ);

            if (diffX <= 1 && diffZ <= 1)
            {
                Debug.Log(mover.name + " attacks");

                PlayerHealth ph = playerUnit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(10);
                }
            }
            else
            {
                Debug.Log(mover.name + " did not attack (too far)");
            }
        }

        isPlayerTurn = true;
        UpdateTurnText();
        Debug.Log("player turn");
    }

    public void CheckEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            if (winText != null)
            {
                winText.SetActive(true);
            }
            Debug.Log("WIN");
        }
    }
}