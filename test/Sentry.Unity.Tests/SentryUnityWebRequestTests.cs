using System.Net.Http;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using Sentry.Unity;

[TestFixture]
public class SentryUnityWebRequestTests
{
    [Test]
    public void Simple()
    {
       var unityWebRequest = SentryUnityWebRequest.Get("https://httpbin.org/status/200");
       UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = unityWebRequest.SendWebRequest();
    }

}
