using UnityEngine;
using UnityEngine.SceneManagement;

// Spins a small cube in the header's top-right corner of every scene, so screenshots, session replays and frame
// timings always show motion. Each scene's Main Camera draws it, lit by a light of its own.
public class SpinningCube : MonoBehaviour
{
    // A layer of its own, so the cube's light shades nothing else.
    private const int CubeLayer = 31;

    // The scenes' canvas is scaled to a 480 units high screen. Its header is the top 75 units, with the title centred
    // in it: the cube goes in the space right of the title, where no UI is drawn on any screen shape.
    private const float CanvasHeight = 480f;
    private const float HeaderHeight = 75f;
    private const float TitleHalfWidth = 64f;
    private const float Margin = 4f;

    // The gap between the cube and the screen's top and right edges, the same both ways. Wide enough to keep the cube
    // clear of a phone's rounded corners.
    private const float EdgeGap = 12f;

    // The most of the screen width the spinning cube takes up, on wide screens.
    private const float MaxWidthShare = 0.2f;

    private static readonly Vector3 DegreesPerSecond = new Vector3(30f, 60f, 15f);

    private Transform _cube;
    private Renderer _renderer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        // Stay out of the PlayMode test scenes
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != null && sceneName.Contains("TestScene"))
        {
            return;
        }

        var root = new GameObject("SpinningCube");
        DontDestroyOnLoad(root);
        root.AddComponent<SpinningCube>();
    }

    private void Awake()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.layer = CubeLayer;
        cube.transform.SetParent(transform, false);
        Destroy(cube.GetComponent<Collider>());

        _cube = cube.transform;
        _renderer = cube.GetComponent<Renderer>();

        // The primitive's default material doesn't make it into a player build: use a shader the builds include.
        var shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        if (shader != null)
        {
            _renderer.material = new Material(shader);
        }

        _renderer.material.color = new Color(1f, 0.45f, 0.1f);

        // The scenes have no lights, and the cube needs one to be shaded.
        var lightObject = new GameObject("CubeLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        var cubeLight = lightObject.AddComponent<Light>();
        cubeLight.type = LightType.Directional;
        cubeLight.cullingMask = 1 << CubeLayer;
    }

    private void LateUpdate()
    {
        _cube.Rotate(DegreesPerSecond * Time.deltaTime, Space.Self);

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            _renderer.enabled = false;
            return;
        }

        _renderer.enabled = true;

        // The free space in the header, in pixels: right of the title, the same gap from the top and the right edge,
        // and inside the safe area's right side (a notch in landscape).
        var width = (float)mainCamera.pixelWidth;
        var height = (float)mainCamera.pixelHeight;
        var unit = height / CanvasHeight;
        var left = width * 0.5f + (TitleHalfWidth + Margin) * unit;
        var right = Mathf.Min(width, Screen.safeArea.xMax) - EdgeGap * unit;
        var top = height - EdgeGap * unit;
        var bottom = height - (HeaderHeight - Margin) * unit;

        // The square the spinning cube fills: as big as the space allows, up to a share of the screen width.
        var box = Mathf.Min(Mathf.Min(right - left, top - bottom), MaxWidthShare * width);
        if (box <= 0f)
        {
            _renderer.enabled = false;
            return;
        }

        // Well inside the clip planes, so the spinning corners never cross the near plane.
        var depth = mainCamera.nearClipPlane + 5f;
        var visibleWidth = mainCamera.orthographic
            ? 2f * mainCamera.orthographicSize * mainCamera.aspect
            : 2f * depth * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * mainCamera.aspect;

        // The cube's space diagonal is its widest silhouette while it spins: keep that within the square.
        _cube.localScale = Vector3.one * (box / width * visibleWidth / Mathf.Sqrt(3f));

        // In the space's top-right corner.
        var centre = new Vector3(right - box * 0.5f, top - box * 0.5f, depth);
        _cube.position = mainCamera.ScreenToWorldPoint(centre);
    }
}
