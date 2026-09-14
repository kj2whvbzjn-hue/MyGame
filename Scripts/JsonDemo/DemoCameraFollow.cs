using UnityEngine;

public sealed class DemoCameraFollow : MonoBehaviour
{
    [SerializeField] private float smooth = 5f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 7f;

    private Transform target;

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
            target = player.transform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float targetX = Mathf.Clamp(target.position.x, minX, maxX);
        Vector3 desired = new Vector3(targetX, 0.2f, -10f);
        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
