using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AfterImage : MonoBehaviour
{
    private SpriteRenderer sr;
    private float lifetime;
    private float t;
    private Color startColor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(Sprite sprite, Color color, float lifetime)
    {
        this.lifetime = Mathf.Max(0.01f, lifetime);
        t = 0f;

        sr.sprite = sprite;
        startColor = color;
        sr.color = startColor;
    }

    private void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);

        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, k);
        sr.color = c;

        if (t >= lifetime)
        {
            // IMPORTANT: destroy the runtime Texture2D too (Sprite owns it)
            if (sr.sprite != null && sr.sprite.texture != null)
                Destroy(sr.sprite.texture);
            Destroy(sr.sprite);
            Destroy(gameObject);
        }
    }
}
