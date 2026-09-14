using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class DemoCollectible : MonoBehaviour
{
    private Vector3 baseScale;

    private void Awake()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.12f;
        transform.localScale = baseScale * pulse;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DemoPlayerController>() != null)
            DemoGameManager.Instance?.Collect(gameObject);
    }
}
