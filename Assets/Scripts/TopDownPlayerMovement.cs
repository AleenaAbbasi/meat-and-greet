using UnityEngine;
using UnityEngine.Tilemaps;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
//this all is for catering teh isometric mvoements
//Moves in screen-cardinal directions while keeping the player on the 2D grid
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    [Tooltip("The player's feet must stay on a tile of this tilemap. Defaults to the object named 'floor'.")]
    [SerializeField] private Tilemap walkableArea;

    [Tooltip("Extra gap kept between the feet and the floor edge, in world units.")]
    [SerializeField, Min(0f)] private float edgePadding = 0.05f;

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private Vector2 moveInput;

    public static TopDownPlayerMovement Active { get; private set; }
    public Vector2 MoveInput => moveInput;

    //Bottom-centre of the feet collider. Props use this to decide draw order.
    public Vector2 FeetPosition
    {
        get
        {
            var b = playerCollider.bounds;
            return new Vector2(b.center.x, b.min.y);
        }
    }

    private void OnEnable() => Active = this;

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider == null)
            playerCollider = gameObject.AddComponent<BoxCollider2D>();
        FitColliderToFeet(playerCollider, GetCollisionSprites());

        if (walkableArea == null)
        {
            var floor = GameObject.Find("floor");
            if (floor != null)
                walkableArea = floor.GetComponent<Tilemap>();
        }
    }

    private void FitColliderToFeet(BoxCollider2D playerCollider, SpriteRenderer[] renderers)
    {
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // The full character art is tall, but collisions should happen at foot level.
        var height = Mathf.Min(0.9f, bounds.size.y * 0.3f);
        var width = bounds.size.x * 0.65f;
        var footCenter = new Vector3(bounds.center.x, bounds.min.y + height * 0.5f, transform.position.z);
        var localCenter = transform.InverseTransformPoint(footCenter);
        var localSize = transform.InverseTransformVector(new Vector3(width, height, 0f));
        playerCollider.offset = new Vector2(localCenter.x, localCenter.y);
        playerCollider.size = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
    }

    private SpriteRenderer[] GetCollisionSprites()
    {
        var allSprites = GetComponentsInChildren<SpriteRenderer>();
        var coreSprites = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sprite in allSprites)
        {
            if (!sprite.enabled || !sprite.gameObject.activeInHierarchy)
                continue;

            var partName = sprite.transform.name.ToLowerInvariant();
            if (partName == "body" || partName.StartsWith("leg"))
                coreSprites.Add(sprite);
        }

        if (coreSprites.Count > 0)
            return coreSprites.ToArray();

        var visibleSprites = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sprite in allSprites)
        {
            if (sprite.enabled && sprite.gameObject.activeInHierarchy)
                visibleSprites.Add(sprite);
        }

        return visibleSprites.Count > 0 ? visibleSprites.ToArray() : allSprites;
    }

    private void Update()
    {
        moveInput = new Vector2(
            (Pressed(KeyCode.D, Key.D) || Pressed(KeyCode.RightArrow, Key.RightArrow) ? 1f : 0f) -
            (Pressed(KeyCode.A, Key.A) || Pressed(KeyCode.LeftArrow, Key.LeftArrow) ? 1f : 0f),
            (Pressed(KeyCode.W, Key.W) || Pressed(KeyCode.UpArrow, Key.UpArrow) ? 1f : 0f) -
            (Pressed(KeyCode.S, Key.S) || Pressed(KeyCode.DownArrow, Key.DownArrow) ? 1f : 0f));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private static bool Pressed(KeyCode legacyKey, Key inputKey)
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null && Keyboard.current[inputKey].isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        pressed = pressed || Input.GetKey(legacyKey);
#endif
        return pressed;
    }

    private void FixedUpdate()
    {
        var view = Camera.main;
        Vector3 screenDirection = view == null
            ? new Vector3(moveInput.x, moveInput.y, 0f)
            : view.transform.right * moveInput.x + view.transform.up * moveInput.y;
        screenDirection.z = 0f;
        Vector2 gridDirection = new Vector2(screenDirection.x, screenDirection.y);
        if (gridDirection.sqrMagnitude > 0f)
            gridDirection.Normalize();

        Vector2 step = gridDirection * moveSpeed * Time.fixedDeltaTime;

        // Stay on the floor. If the full step leaves it, try sliding along one axis.
        if (!CanStandAfter(step))
        {
            if (CanStandAfter(new Vector2(step.x, 0f)))
                step = new Vector2(step.x, 0f);
            else if (CanStandAfter(new Vector2(0f, step.y)))
                step = new Vector2(0f, step.y);
            else
                step = Vector2.zero;
        }

        // Velocity (not MovePosition) lets the physics solver stop us at colliders.
        body.linearVelocity = step / Time.fixedDeltaTime;
    }

    private bool CanStandAfter(Vector2 offset)
    {
        if (walkableArea == null)
            return true;

        var b = playerCollider.bounds;
        float minX = b.min.x - edgePadding + offset.x;
        float maxX = b.max.x + edgePadding + offset.x;
        float minY = b.min.y - edgePadding + offset.y;
        float maxY = b.max.y + edgePadding + offset.y;

        bool ok = OnFloor(minX, minY) && OnFloor(maxX, minY) && OnFloor(minX, maxY) && OnFloor(maxX, maxY);

        // If we somehow start off the floor, don't trap the player there.
        if (!ok && offset != Vector2.zero && !CanStandAfter(Vector2.zero))
            return true;
        return ok;
    }

    private bool OnFloor(float x, float y)
    {
        return walkableArea.HasTile(walkableArea.WorldToCell(new Vector3(x, y, 0f)));
    }
}
