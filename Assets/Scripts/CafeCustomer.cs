using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A customer that walks from the entrance to a free stool along a path around the
/// furniture, stands there for a few seconds, then walks back out the way it came.
/// Uses a 3x4 sprite sheet (48x64 frames): rows are front, left, right, back.
/// Set up like the Player: this script on the root, the SpriteRenderer on a child.
/// </summary>
public sealed class CafeCustomer : MonoBehaviour, Interactable
{
    public enum State { Entering, Seated, Leaving }
    private enum Facing { Front = 0, Left = 1, Right = 2, Back = 3 }

    [SerializeField, Min(0.1f)] private float walkSpeed = 2.5f;
    [SerializeField, Min(1f)] private float framesPerSecond = 6f;
    [Tooltip("Which way the customer faces while standing at the stool.")]
    [SerializeField] private Facing seatedFacing = Facing.Front;

    [Header("Sprite sheet layout")]
    [SerializeField] private int frameWidth = 48;
    [SerializeField] private int frameHeight = 64;
    [SerializeField] private int columns = 3;
    [SerializeField] private float pixelsPerUnit = 100f;
    [SerializeField] private string[] possibleOrders = { "Burger", "Steak", "Meat Pie", "Kebab" };
    public string Order { get; private set; }

    public State CurrentState { get; private set; } = State.Entering;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;                 // index = row * columns + column
    private Facing facing = Facing.Front;
    private bool moving;

    private CafeCustomerSpawner spawner;
    private CafeNavGrid grid;
    private int seatIndex;
    private Vector3 entrance;                // feet position
    private Vector3 seat;                    // feet position
    private Vector3 feetOffset;              // pivot minus feet
    private List<Vector3> path;
    private int pathIndex;
    private float stayTimer;

    // Sliced sheets are shared between customers that use the same texture.
    private static readonly Dictionary<Texture2D, Sprite[]> SheetCache = new Dictionary<Texture2D, Sprite[]>();

    public void Init(CafeCustomerSpawner owner, CafeNavGrid navGrid, Texture2D sheet,
                     int seatIdx, Vector3 seatFeet, Vector3 entranceFeet, float staySeconds)
    {
        spawner = owner;
        grid = navGrid;
        seatIndex = seatIdx;
        seat = seatFeet;
        entrance = entranceFeet;
        stayTimer = staySeconds;
        //random order when customer spawns shows on console
        Order = possibleOrders[Random.Range(0, possibleOrders.Length)];
        Debug.Log($"[Order] {sheet.name} arrived and wants: {Order}");

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        frames = Slice(sheet);
        SetFrame(Facing.Front, 0);

        //workout where the feet are relative to the pivot, then stand the feet on the entrance.
        var b = spriteRenderer.bounds;
        feetOffset = transform.position - new Vector3(b.center.x, b.min.y, transform.position.z);
        feetOffset.z = 0f;
        transform.position = entrance + feetOffset;

        // Re-measure the depth sorter now that the sprite is set.
        var sortable = GetComponent<IsoSortable>();
        if (sortable != null) { sortable.CacheComponents(); sortable.MarkMoving(); }

        StartPath(seat);
    }

    private Sprite[] Slice(Texture2D sheet)
    {
        if (SheetCache.TryGetValue(sheet, out var cached)) return cached;

        int rows = sheet.height / frameHeight;
        var result = new Sprite[rows * columns];
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
        {
            // Row 0 is the TOP of the image, texture coordinates start at the bottom.
            var rect = new Rect(col * frameWidth, sheet.height - (row + 1) * frameHeight, frameWidth, frameHeight);
            var s = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = sheet.name + "_" + (row * columns + col);
            result[row * columns + col] = s;
        }
        SheetCache[sheet] = result;
        return result;
    }

    private void StartPath(Vector3 targetFeet)
    {
        path = grid != null ? grid.FindPath(Feet, targetFeet) : null;
        if (path == null) path = new List<Vector3> { targetFeet };   // no route found: walk straight
        pathIndex = 0;
    }

    private Vector3 Feet => transform.position - feetOffset;

    private void Update()
    {
        moving = false;
        switch (CurrentState)
        {
            case State.Entering:
                if (FollowPath())
                {
                    CurrentState = State.Seated;
                    facing = seatedFacing;
                }
                break;

            case State.Seated:
                stayTimer -= Time.deltaTime;
                if (stayTimer <= 0f)
                {
                    CurrentState = State.Leaving;
                    if (spawner != null) spawner.FreeSeat(seatIndex);
                    StartPath(entrance);
                }
                break;

            case State.Leaving:
                if (FollowPath())
                    Destroy(gameObject);
                break;
        }
        Animate();
    }

    /// <summary>Moves along the path. Returns true when the last point is reached.</summary>
    private bool FollowPath()
    {
        if (pathIndex >= path.Count) return true;

        var target = path[pathIndex];
        var feet = Feet;
        var delta = target - feet;
        delta.z = 0f;
        float step = walkSpeed * Time.deltaTime;

        if (delta.magnitude <= step)
        {
            transform.position = target + feetOffset;
            pathIndex++;
            return pathIndex >= path.Count;
        }

        var dir = delta.normalized;
        transform.position += dir * step;
        moving = true;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            facing = dir.x < 0f ? Facing.Left : Facing.Right;
        else
            facing = dir.y > 0f ? Facing.Back : Facing.Front;
        return false;
    }

    private void Animate()
    {
        int frame = moving ? Mathf.FloorToInt(Time.time * framesPerSecond) % columns : 0;
        SetFrame(facing, frame);
    }

    private void SetFrame(Facing f, int column)
    {
        int i = (int)f * columns + column;
        if (frames != null && i < frames.Length && frames[i] != null)
            spriteRenderer.sprite = frames[i];
    }
    public bool CanInteract(PlayerInteractor player) => CurrentState == State.Seated;

    public void Interact(PlayerInteractor player)
    {
        Debug.Log($"[Order] Customer at seat {seatIndex + 1} wants: {Order}");
    }
}
