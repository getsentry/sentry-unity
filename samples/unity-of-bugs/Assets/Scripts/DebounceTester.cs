using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebounceTester : MonoBehaviour
{
    // Can be assigned to button click event
    public void TestTimeCoroutine()
        => StartCoroutine(SeparateTimerPerTypeCheck());

    private IEnumerator SeparateTimerPerTypeCheck()
    {
        Debug.LogError($"error"); // error BarrierOffset == Line.17
        yield return new WaitForSeconds(1f);
        Debug.LogAssertion($"assertion"); // assertion BarrierOffset
        Debug.LogError($"error"); // error BarrierOffset == Line.14
        Debug.LogAssertion($"assertion"); // assertion BarrierOffset
    }

    private IEnumerator SeparateTimerPerTypeCheckAdvanced()
    {
        Debug.LogError($"error");
        yield return new WaitForSeconds(1f);
        Debug.LogAssertion($"assertion");
        yield return new WaitForSeconds(0.5f);
        Debug.LogError($"error");
        Debug.LogError($"error");
        Debug.LogAssertion($"assertion");
    }
}
