using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBarController : MonoBehaviour
{
    public Slider hpSlider;
    private Transform target;
    private Vector3 offset = new Vector3(0, 1.5f, 0);

    void Awake()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
    }

    public void SetTarget(Transform enemyTransform)
    {
        target = enemyTransform;
    }

    public void SetHP(float currentHP, float maxHP)
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP / maxHP;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.forward = Camera.main.transform.forward;
        }
    }
}