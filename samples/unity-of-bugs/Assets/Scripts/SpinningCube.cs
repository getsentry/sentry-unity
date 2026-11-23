using UnityEngine;
using UnityEngine.SceneManagement;

// Spins a small cube in the top-right corner of every scene's header, so screenshots, session replays and frame
// timings always show motion. Each scene's Main Camera draws it, lit by a light of its own.
public class SpinningCube : MonoBehaviour
{
    // A layer of its own, so the cube's light shades nothing else.
    private const int CubeLayer = 31;

    // In the scenes' canvas units: their UI scales to a screen 480 units high, with a 75 units high header.
    private const float Size = 28f;
    private const float EdgeGap = 12f;

    private static readonly Vector3 DegreesPerSecond = new Vector3(30f, 60f, 15f);

    private Transform _cube;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        // Stay out of the PlayMode test scenes
        if (SceneManager.GetActiveScene().name.Contains("TestScene"))
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

        // The primitive's default material doesn't make it into a player build: use a shader the builds include.
        var shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        var cubeRenderer = cube.GetComponent<Renderer>();
        if (shader != null)
        {
            cubeRenderer.material = new Material(shader);
        }

        cubeRenderer.material.color = new Color(1f, 0.45f, 0.1f);

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

        // None between scene loads
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        // A square in the header's top-right corner, the same gap from the top and the right edge
        var unit = mainCamera.pixelHeight / 480f;
        var inset = (EdgeGap + Size * 0.5f) * unit;
        var depth = mainCamera.nearClipPlane + 5f;
        var centre = new Vector3(mainCamera.pixelWidth - inset, mainCamera.pixelHeight - inset, depth);
        _cube.position = mainCamera.ScreenToWorldPoint(centre);

        // The cube's space diagonal is its widest silhouette while it spins: keep that within the square.
        var edge = mainCamera.ScreenToWorldPoint(centre + new Vector3(Size * 0.5f * unit, 0f, 0f));
        _cube.localScale = Vector3.one * (2f * Vector3.Distance(_cube.position, edge) / Mathf.Sqrt(3f));
    }
}
