using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class DemoMovingPlatform : MonoBehaviour
{
    [SerializeField] private float distance = 2.2f;
    [SerializeField] private float speed = 1.25f;

    private Rigidbody2D body;
    private Vector2 origin;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        origin = transform.position;
    }

    private void FixedUpdate()
    {
        float x = origin.x + Mathf.Sin(Time.time * speed) * distance;
        body.MovePosition(new Vector2(x, origin.y));
    }
}
