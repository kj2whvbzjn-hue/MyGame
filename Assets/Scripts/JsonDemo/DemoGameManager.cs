using UnityEngine;

public sealed class DemoGameManager : MonoBehaviour
{
    public static DemoGameManager Instance { get; private set; }

    private int totalCollectibles;
    private int collected;
    private Transform player;
    private Vector3 startPosition;
    private string notice = "";
    private float noticeUntil;
    private bool won;

    public bool AllCollected => collected >= totalCollectibles;
    public bool Won => won;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        totalCollectibles = FindObjectsByType<DemoCollectible>(FindObjectsSortMode.None).Length;

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            startPosition = player.position;
        }

        ShowNotice("Collect every crystal, then reach the green portal!", 4f);
    }

    public void Collect(GameObject item)
    {
        if (item == null)
            return;

        collected++;
        Destroy(item);

        if (AllCollected)
            ShowNotice("All crystals collected! Reach the portal.", 4f);
        else
            ShowNotice($"Crystal {collected}/{totalCollectibles}", 1.5f);
    }

    public void ResetPlayer()
    {
        if (player == null || won)
            return;

        player.position = startPosition;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;

        ShowNotice("Ouch! Back to the checkpoint.", 1.5f);
    }

    public void TryWin()
    {
        if (won)
            return;

        if (!AllCollected)
        {
            ShowNotice($"Portal locked: {totalCollectibles - collected} crystal(s) left.", 2f);
            return;
        }

        won = true;
        ShowNotice("YOU WIN! JSON -> Unity automation demo complete.", 999f);
    }

    private void ShowNotice(string text, float seconds)
    {
        notice = text;
        noticeUntil = Time.unscaledTime + seconds;
    }

    private void OnGUI()
    {
        int baseSize = Mathf.Clamp(Screen.height / 34, 16, 34);

        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseSize + 5,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUIStyle text = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseSize,
            normal = { textColor = new Color(0.9f, 0.95f, 1f) }
        };

        GUI.Box(new Rect(18, 18, Mathf.Min(560, Screen.width - 36), 118), "");
        GUI.Label(new Rect(34, 28, 520, 42), "NEON CRYSTAL RUN", title);
        GUI.Label(new Rect(34, 72, 520, 32),
            $"Crystals: {collected}/{totalCollectibles}    " +
            (won ? "PORTAL COMPLETE" : "Reach the green portal"), text);
        GUI.Label(new Rect(34, 103, 520, 28),
            "Keyboard: A/D or arrows, Space to jump, R to reset", text);

        if (Time.unscaledTime < noticeUntil && !string.IsNullOrEmpty(notice))
        {
            GUI.Box(new Rect(Screen.width * 0.16f, 150, Screen.width * 0.68f, 58), "");
            GUI.Label(new Rect(Screen.width * 0.18f, 162, Screen.width * 0.64f, 38), notice, text);
        }

        // Visual guides for touch controls. Player movement code reads these screen zones.
        if (Application.isMobilePlatform || Input.touchSupported)
        {
            float h = Mathf.Clamp(Screen.height * 0.13f, 70f, 130f);
            float y = Screen.height - h - 18f;
            float w = Mathf.Clamp(Screen.width * 0.22f, 90f, 180f);

            GUI.Box(new Rect(18, y, w, h), "◀");
            GUI.Box(new Rect(28 + w, y, w, h), "▶");
            GUI.Box(new Rect(Screen.width - w - 18, y, w, h), "JUMP");
        }
    }
}
