using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Spins a small 3D Sentry logo in the top-right corner of every scene's header, so screenshots, session replays and
// frame timings always show motion. A camera of its own draws the logo into a texture that each scene's header shows.
public class SpinningLogo : MonoBehaviour
{
    // A layer of its own, so only the logo's camera and light see the logo.
    private const int LogoLayer = 31;

    private Transform _logo;
    private RenderTexture _texture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        // Stay out of the PlayMode test scenes
        if (SceneManager.GetActiveScene().name.Contains("TestScene"))
        {
            return;
        }

        var root = new GameObject("SpinningLogo");
        DontDestroyOnLoad(root);
        root.AddComponent<SpinningLogo>();
    }

    private void Awake()
    {
        // Far below the scene, where no scene camera sees it. The logo turns about this pivot.
        _logo = new GameObject("Logo").transform;
        _logo.SetParent(transform);
        _logo.position = new Vector3(0, -1000, 0);

        var model = CreateModel();
        model.transform.SetParent(_logo, false);

        // The primitive's and the model's default materials don't make it into a player build: use a shader the
        // builds include.
        var shader = Shader.Find("Legacy Shaders/Diffuse");
        foreach (var modelRenderer in model.GetComponentsInChildren<Renderer>())
        {
            if (shader != null)
            {
                modelRenderer.sharedMaterial = new Material(shader);
            }
            // The menu's purple accent: Sentry's own darker purple doesn't show on the dark header
            modelRenderer.material.color = Sentry.Unity.Samples.SampleUI.Accent;
        }

        foreach (var child in model.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = LogoLayer;
        }

        foreach (var collider in model.GetComponentsInChildren<Collider>())
        {
            Destroy(collider);
        }

        // Centre the model on the pivot, so it turns in place
        var bounds = Bounds(model);
        model.transform.position += _logo.position - bounds.center;

        var logoLight = new GameObject("LogoLight").AddComponent<Light>();
        logoLight.transform.SetParent(transform);
        logoLight.transform.rotation = Quaternion.Euler(30, -30, 0);
        logoLight.type = LightType.Directional;
        logoLight.cullingMask = 1 << LogoLayer;

        _texture = new RenderTexture(128, 128, 16);

        // Just wide enough for the logo's width and height, so the turning logo stays in the frame
        var logoCamera = new GameObject("LogoCamera").AddComponent<Camera>();
        logoCamera.transform.SetParent(transform);
        logoCamera.transform.position = _logo.position + new Vector3(0, 0, -10 * bounds.extents.magnitude);
        logoCamera.cullingMask = 1 << LogoLayer;
        logoCamera.clearFlags = CameraClearFlags.SolidColor;
        logoCamera.backgroundColor = Color.clear;
        logoCamera.orthographic = true;
        logoCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.1f;
        logoCamera.nearClipPlane = 0.01f;
        logoCamera.farClipPlane = 20 * bounds.extents.magnitude;
        logoCamera.targetTexture = _texture;

        ShowInHeader(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SceneManager.sceneLoaded += ShowInHeader;
    }

    // The logo model from Resources, upright in the XY plane, or a cube when the model is missing.
    private static GameObject CreateModel()
    {
        var prefab = Resources.Load<GameObject>("SentryLogo");
        if (prefab == null)
        {
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        // The importer turns the model's root to undo Blender's Z-up, but the mesh itself already stands upright,
        // with its front facing away from the camera: turn it round
        var model = Instantiate(prefab);
        model.name = "SentryLogo";
        model.transform.rotation = Quaternion.Euler(0, 180, 0);
        return model;
    }

    private static Bounds Bounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var modelRenderer in renderers)
        {
            bounds.Encapsulate(modelRenderer.bounds);
        }
        return bounds;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= ShowInHeader;
    }

    // The image belongs to the scene's UI and goes with the scene.
    private void ShowInHeader(Scene scene, LoadSceneMode mode)
    {
        var header = GameObject.Find("Header");
        if (header == null)
        {
            return;
        }

        var image = new GameObject("SpinningLogoImage").AddComponent<RawImage>();
        image.texture = _texture;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.SetParent(header.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-12, -12);
        rect.sizeDelta = new Vector2(28, 28);
    }

    private void Update()
    {
        // Turn about the vertical axis, like a sign on a turntable
        _logo.Rotate(0, 90 * Time.deltaTime, 0, Space.World);
    }
}
