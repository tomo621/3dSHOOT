using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float lifeTime = 1f;

    private TextMeshProUGUI text;
    private float timer;
    private Color startColor;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        startColor = text.color;
    }
    public void Setup(int damageAmount)
    {
        text.text = "-" + damageAmount.ToString();
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        transform.forward = Camera.main.transform.forward;

        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifeTime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}