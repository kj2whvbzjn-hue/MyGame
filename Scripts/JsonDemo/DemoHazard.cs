using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class DemoHazard : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DemoPlayerController>() != null)
            DemoGameManager.Instance?.ResetPlayer();
    }
}
