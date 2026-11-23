using UnityEngine;
using UnityEngine.SceneManagement;

// Spins a small cube in the top-right corner of every scene, so screenshots, session replays and frame timings
// always show motion. Each scene's Main Camera draws it behind the button overlay, lit by a light of its own.
public class SpinningCube : MonoBehaviour
{
    // A layer of its own, so the cube's light shades nothing else.
    private const int CubeLayer = 31;

    // The scenes' header is 75 units tall on a canvas scaled to a 480 units high screen.
    private const float HeaderBottom = 1f - 75f / 480f;

    // Share of the screen width the spinning cube takes up, and its distance from the right edge and the header.
    private const float WidthShare = 0.2f;
    private const float Margin = 0.02f;

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

        // Well inside the clip planes, so the spinning corners never cross the near plane.
        var depth = mainCamera.nearClipPlane + 5f;
        var visibleWidth = mainCamera.orthographic
            ? 2f * mainCamera.orthographicSize * mainCamera.aspect
            : 2f * depth * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * mainCamera.aspect;

        // The cube's space diagonal is its widest silhouette while it spins: keep that within the share.
        var box = WidthShare * visibleWidth;
        _cube.localScale = Vector3.one * (box / Mathf.Sqrt(3f));

        // The box's half extent in viewport units: horizontally a share of the width, vertically scaled by the aspect.
        var halfWidth = WidthShare * 0.5f;
        var halfHeight = halfWidth * mainCamera.aspect;
        var viewport = new Vector3(
            1f - Margin - halfWidth,
            HeaderBottom - Margin * mainCamera.aspect - halfHeight,
            depth);
        _cube.position = mainCamera.ViewportToWorldPoint(viewport);
    }
}
