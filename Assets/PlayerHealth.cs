using UnityEngine;

public class PlayerHealth : MonoBehaviour
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
        Debug.Log("player damaged: " + damage + " hp: " + currentHP);

        if (rend != null)
        {
            float hpRatio = (float)currentHP / maxHP;
            if (hpRatio > 0.5f) rend.material.color = Color.blue;
            else if (hpRatio > 0.2f) rend.material.color = Color.yellow;
            else rend.material.color = Color.red;
        }

        if (currentHP <= 0)
        {
            Debug.Log("player dead GAME OVER");
        }
    }
}