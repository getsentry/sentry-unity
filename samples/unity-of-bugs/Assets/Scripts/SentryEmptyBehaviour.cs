using UnityEngine;

public class SentryEmptyBehaviour : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Testing Awake");

        if (enabled)
        {
            Debug.Log("HUEHUE");
            return;
        }
        else if (Time.deltaTime > 2.0f)
        {
            Debug.Log("sad noises");
            return;
        }
        else
        {
            Debug.Log("End of if");
        }

        Debug.Log("End of testing Awake");
    }

    private void Start()
    { }
}
