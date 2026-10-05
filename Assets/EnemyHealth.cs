using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;
    private Renderer rend;

    void Start()
    {
        currentHP = maxHP;
        rend = GetComponent<Renderer>();
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log(gameObject.name + " が " + damage + " ダメージ受けた。残りHP: " + currentHP);

        UpdateColor();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void UpdateColor()
    {
        float hpRatio = (float)currentHP / maxHP;

        if (rend != null)
        {
            if (hpRatio > 0.5f)
            {
                rend.material.color = Color.green;
            }
            else if (hpRatio > 0.2f)
            {
                rend.material.color = Color.yellow;
            }
            else
            {
                rend.material.color = Color.red;
            }
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " は倒れた");

        gameObject.tag = "Untagged";
        Destroy(gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckEnemies();
        }
    }
}