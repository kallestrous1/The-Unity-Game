using System.Collections;
using UnityEngine;

public class AfterImageInstantiator : MonoBehaviour
{
    [Header("Target to capture")]
    [SerializeField] private Transform targetRoot;

    [Header("Layers")]
    [SerializeField] private LayerMask captureLayer; // set to PlayerCapture only

    [Header("Afterimage prefab")]
    [SerializeField] private AfterImage afterImagePrefab;

    [Header("Burst")]
    [SerializeField] private float spawnInterval = 0.04f;
    [SerializeField] private float defaultDuration = 0.35f;
    [SerializeField] private float defaultLifetime = 0.25f;

    [Header("Capture Quality")]
    [SerializeField] private int baseResolution = 512; // 256/512/1024
    [SerializeField] private float paddingWorld = 0.1f;

    [Header("Visual")]
    [SerializeField] private float startAlpha = 0.6f;

    private Camera captureCam;
    private Coroutine routine;

    private void Awake()
    {
        if (targetRoot == null) targetRoot = transform;

        // Create hidden capture camera
        var camGO = new GameObject("AfterImageCaptureCamera");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        captureCam = camGO.AddComponent<Camera>();
        captureCam.enabled = false;
        captureCam.orthographic = true;
        captureCam.clearFlags = CameraClearFlags.SolidColor;
        captureCam.backgroundColor = new Color(0, 0, 0, 0); // transparent
        captureCam.cullingMask = captureLayer;
        captureCam.nearClipPlane = -10f;
        captureCam.farClipPlane = 10f;
    }

    private void OnDestroy()
    {
        if (captureCam != null) Destroy(captureCam.gameObject);
    }

    public void PlayAfterImages(float duration = -1f, float lifetime = -1f)
    {
        if (afterImagePrefab == null)
        {
            Debug.LogWarning("[AfterImageCaptureInstantiator] afterImagePrefab not set.");
            return;
        }

        if (duration <= 0f) duration = defaultDuration;
        if (lifetime <= 0f) lifetime = defaultLifetime;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Burst(duration, lifetime));
    }

    public void PlayAfterImagesForAnimation()
    {
        if (afterImagePrefab == null)
        {
            Debug.LogWarning("[AfterImageCaptureInstantiator] afterImagePrefab not set.");
            return;
        }

        float duration = defaultDuration;
        float lifetime = defaultLifetime;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Burst(duration, lifetime));
    }


    private IEnumerator Burst(float duration, float lifetime)
    {
        float t = 0f;
        while (t < duration)
        {
            SpawnOne(lifetime);
            yield return new WaitForSeconds(spawnInterval);
            t += spawnInterval;
        }
        routine = null;
    }

    private void SpawnOne(float lifetime)
    {
        if (!TryGetWorldBounds(targetRoot, out Bounds b)) return;

        b.Expand(new Vector3(paddingWorld, paddingWorld, 0f));

        // Square capture (simplifies)
        float size = Mathf.Max(b.size.x, b.size.y);
        Vector3 center = b.center;

        // Position camera in front of 2D scene
        captureCam.transform.position = new Vector3(center.x, center.y, -5f);
        captureCam.transform.rotation = Quaternion.identity;

        // Orthographic size is half of world height
        captureCam.orthographicSize = size * 0.5f;

        // Render to RT -> Texture2D
        int res = baseResolution;
        RenderTexture rt = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;

        captureCam.targetTexture = rt;
        captureCam.Render();
        captureCam.targetTexture = null;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // Create sprite with pixelsPerUnit matching world size
        float ppu = res / size;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), ppu);

        // Spawn afterimage sprite renderer
        var img = Instantiate(afterImagePrefab);
        img.transform.position = center;
        img.transform.rotation = Quaternion.identity;
        img.transform.localScale = Vector3.one;

        Color c = Color.white;
        c.a = startAlpha;

        img.Init(s, c, lifetime);
    }

    private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasAny = false;
        bounds = new Bounds(root.position, Vector3.zero);

        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;

            // bounds includes current deformed mesh visual
            if (!hasAny)
            {
                bounds = r.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasAny;
    }
}
