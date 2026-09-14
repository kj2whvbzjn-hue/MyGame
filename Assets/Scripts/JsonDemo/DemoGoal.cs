using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class DemoGoal : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null || DemoGameManager.Instance == null)
            return;

        if (DemoGameManager.Instance.AllCollected)
        {
            float glow = 0.75f + Mathf.Sin(Time.time * 5f) * 0.25f;
            spriteRenderer.color = new Color(0.1f, 1f, 0.35f, glow);
        }
        else
        {
            spriteRenderer.color = new Color(0.08f, 0.35f, 0.18f, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DemoPlayerController>() != null)
            DemoGameManager.Instance?.TryWin();
    }
}
