using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Spins a small cube in the top-right corner of every scene's header, so screenshots, session replays and frame
// timings always show motion. A camera of its own draws the cube into a texture that each scene's header shows.
public class SpinningCube : MonoBehaviour
{
    // A layer of its own, so only the cube's camera and light see the cube.
    private const int CubeLayer = 31;

    private Transform _cube;
    private RenderTexture _texture;

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
        // Far below the scene, where no scene camera sees it
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.layer = CubeLayer;
        cube.transform.SetParent(transform);
        cube.transform.position = new Vector3(0, -1000, 0);
        Destroy(cube.GetComponent<Collider>());
        _cube = cube.transform;

        // The primitive's default material doesn't make it into a player build: use a shader the builds include.
        var shader = Shader.Find("Legacy Shaders/Diffuse");
        var cubeRenderer = cube.GetComponent<Renderer>();
        if (shader != null)
        {
            cubeRenderer.material = new Material(shader);
        }
        cubeRenderer.material.color = new Color(1f, 0.45f, 0.1f);

        var cubeLight = new GameObject("CubeLight").AddComponent<Light>();
        cubeLight.transform.SetParent(transform);
        cubeLight.transform.rotation = Quaternion.Euler(40, -30, 0);
        cubeLight.type = LightType.Directional;
        cubeLight.cullingMask = 1 << CubeLayer;

        _texture = new RenderTexture(128, 128, 16);

        // Just wide enough for the cube's space diagonal, so the spinning cube stays in the frame
        var cubeCamera = new GameObject("CubeCamera").AddComponent<Camera>();
        cubeCamera.transform.SetParent(transform);
        cubeCamera.transform.position = _cube.position + new Vector3(0, 0, -2);
        cubeCamera.cullingMask = 1 << CubeLayer;
        cubeCamera.clearFlags = CameraClearFlags.SolidColor;
        cubeCamera.backgroundColor = Color.clear;
        cubeCamera.orthographic = true;
        cubeCamera.orthographicSize = 0.9f;
        cubeCamera.targetTexture = _texture;

        ShowInHeader(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SceneManager.sceneLoaded += ShowInHeader;
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

        var image = new GameObject("SpinningCubeImage").AddComponent<RawImage>();
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
        _cube.Rotate(new Vector3(30, 60, 15) * Time.deltaTime);
    }
}
