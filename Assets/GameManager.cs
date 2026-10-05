using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject winText;

    void Awake()
    {
        Instance = this;

        if (winText != null)
        {
            winText.SetActive(false);
        }
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
            Debug.Log("WIN!");
        }
    }
}