using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class DemoPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7.5f;
    [SerializeField] private float jumpSpeed = 11.5f;

    private Rigidbody2D body;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private float moveInput;
    private bool jumpPressed;
    private bool previousTouchJump;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 3.2f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GetComponent<BoxCollider2D>().size = new Vector2(0.82f, 0.92f);
    }

    private void Update()
    {
        if (DemoGameManager.Instance != null && DemoGameManager.Instance.Won)
        {
            moveInput = 0f;
            return;
        }

        moveInput = 0f;
        bool keyboardJump = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                moveInput -= 1f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                moveInput += 1f;

            keyboardJump =
                Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame;

            if (Keyboard.current.rKey.wasPressedThisFrame)
                DemoGameManager.Instance?.ResetPlayer();
        }

        bool touchJump = false;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                Vector2 pos = touch.position.ReadValue();

                // Only bottom 40% is used for controls.
                if (pos.y > Screen.height * 0.4f)
                    continue;

                float x01 = pos.x / Screen.width;

                if (x01 < 0.30f)
                    moveInput = -1f;
                else if (x01 < 0.60f)
                    moveInput = 1f;
                else
                    touchJump = true;
            }
        }

        jumpPressed = keyboardJump || (touchJump && !previousTouchJump);
        previousTouchJump = touchJump;

        if (transform.position.y < -9f)
            DemoGameManager.Instance?.ResetPlayer();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);

        if (jumpPressed && groundContacts.Count > 0)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            jumpPressed = false;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.45f)
            {
                groundContacts.Add(collision.collider);
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
    }
}
