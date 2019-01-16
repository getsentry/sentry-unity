using Sentry;
using UnityEngine;

public class SentryBehavior : MonoBehaviour
{
    [Header("DSN of your sentry instance")]
    public string Dsn = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnRuntimeMethodLoad()
    {
        var sentryOptions = GameObject.Find("SentryOptions");
        var op = sentryOptions.GetComponent<SentryOptions>();
        Debug.LogWarning(sentryOptions);
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
