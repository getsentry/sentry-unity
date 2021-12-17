using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdditionalButtons : MonoBehaviour
{
    public void SetUser()
    {
        SentrySdk.ConfigureScope(s =>
        {
            s.User = new User
            {
                Email = "ant@farm.bug",
                Username = "ant",
                Id = "ant-id"
            };
        });
        Debug.Log("User set: ant");
    }

    class PlayerCharacter
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string AttackType { get; set; }
    }

    public void CaptureMessageWithContext()
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.Contexts["character"] = new PlayerCharacter
            {
                Name = "Mighty Fighter",
                Age = 19,
                AttackType = "melee"
            };
        });

        SentrySdk.CaptureMessage("Capturing with player character context.");
        SentrySdk.ConfigureScope(scope => scope.Contexts = null);
    }

    public void BackgroundBreadcrumb() =>
        Task.Run(() => SentrySdk.AddBreadcrumb("Breadcrumb from the background", "background task"));

    public void CaptureMessageWithScreenshot() => StartCoroutine(CaptureScreenshot());

    private IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();
        SentrySdk.ConfigureScope(s =>
        {
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            s.AddAttachment(screenshot.EncodeToJPG(), "screenshot.jpg");
        });

        SentrySdk.CaptureMessage("Captured a message with a screenshot attachment");
        SentrySdk.ConfigureScope(scope => scope.ClearAttachments());
    }

    public void LoadBugfarm() => SceneManager.LoadScene("1_Bugfarm");
    public void LoadMobileNativeSupport() => SceneManager.LoadScene("2_MobileNativeSupport");
}
