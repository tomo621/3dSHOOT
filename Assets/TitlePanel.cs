using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public GameObject turnText;

    void Start()
    {
        if (turnText != null)
        {
            turnText.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            gameObject.SetActive(false);

            if (turnText != null)
            {
                turnText.SetActive(true);
            }
        }
    }
}