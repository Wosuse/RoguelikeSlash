using UnityEngine;

public sealed partial class ArenaPrototype : MonoBehaviour
{

    const float JoystickRadius = 56f;

    [SerializeField] Material spriteMaterial;
    Rigidbody2D body;
    Camera view;
    Vector2 move, touchOrigin, stick;
    int finger = -1;
    int hp = 100;
    float elapsed, nextHit;

    void Awake()
    {
        view = Camera.main;
        var player = new GameObject("Player");
        var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.transform.SetParent(player.transform, false);

        Destroy(visual.GetComponent<Collider>());
        Paint(visual, playerLook, new Color(0.27f, 0.92f, 0.73f));
        body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;
        player.AddComponent<BoxCollider2D>();

    }

    void Paint(GameObject tile, Look look, Color color)
    {
        var material = new Material(spriteMaterial);
        ApplyLook(material, look, color);
        tile.GetComponent<Renderer>().sharedMaterial = material;
    }

    void Update()
    {
        if (hp == 0) return;
        elapsed += Time.deltaTime;
        ReadTouch();
        float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        float y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        move = Vector2.ClampMagnitude(new Vector2(x, y) + stick, 1);
        TickSkills();
        TickCombat(Time.deltaTime);
    }

    void ReadTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && finger < 0 &&
                touch.position.x < Screen.width * 0.5f && touch.position.y < Screen.height * 0.55f)
            {
                finger = touch.fingerId;
                touchOrigin = touch.position;
            }
            if (finger != touch.fingerId) continue;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) ClearInput();
            else stick = Vector2.ClampMagnitude((touch.position - touchOrigin) / (JoystickRadius * UiScale), 1);
        }
        if (Input.touchCount == 0 && finger >= 0) ClearInput();
    }

    void ClearInput()
    {
        finger = -1;
        move = stick = Vector2.zero;
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            ClearInput();
            InterruptSkill();
        }
    }

    void FixedUpdate() => body.velocity = Dashing ? aim * (dashDistance / dashDuration) : move * (moveSpeed * SkillMoveScale);

    void LateUpdate()
    {
        var p = body.transform.position;
        view.transform.position = new Vector3(p.x, p.y, -10);
    }

    void Retry()
    {
        ClearInput();
        body.position = Vector2.zero;
        body.velocity = Vector2.zero;
        hp = 100;
        elapsed = nextHit = 0;
        ResetCombat();
        InterruptSkill();
        System.Array.Clear(readyAt, 0, readyAt.Length);
    }

}






