using System.Collections.Generic;
using UnityEngine;

// One customer. Walks from the door to its table, orders, waits, then walks out.
// Leaves 5 seconds after being served, or after 15 seconds if nobody serves it.
// The look comes from a 3x4 sprite sheet (48x64 frames). Rows: front, left, right, back.
public class CafeCustomer : MonoBehaviour, Interactable
{
    public enum State { Entering, Seated, Leaving }

    public float walkSpeed = 2.5f;
    public float framesPerSecond = 6f;
    public float waitSeconds = 15f;   // leaves unserved after this
    public float eatSeconds = 5f;     // leaves this long after being served

    // --- Anomaly settings (only used on the Anomaly prefab) ---
    public bool isAnomaly;               // tick this on the Anomaly prefab only
    public MeatPickup meatPrefab;        // the meat that drops when the anomaly dies
    public int meatCount = 3;            // how many meat pieces drop
    public float walkAnimSpeed = 0.5f;   // Animator speed while walking (1 = 12 frames per second)

    // The state names inside the Animator Controller: Front, Left, Right, Back.
    static readonly string[] WalkStates = { "AnomalyWalk_Front", "AnomalyWalk_Left", "AnomalyWalk_Right", "AnomalyWalk_Back" };

    const int FrameWidth = 48;
    const int FrameHeight = 64;
    const int Columns = 3;

    // Row of the sprite sheet for each direction.
    const int Front = 0, Left = 1, Right = 2, Back = 3;

    public State state = State.Entering;

    CafeCustomerSpawner spawner;
    CafeNavGrid grid;
    int table;
    Vector3 door;          // where the feet start
    Vector3 seat;          // where the feet should end up
    Vector3 feetOffset;    // gap between the object's position and its feet

    List<Vector3> path;    // points to walk through
    int pathIndex;

    Order order;
    float timer;           // counts down while seated

    SpriteRenderer spriteRenderer;
    Sprite[] frames;
    int facing = Front;
    bool moving;

    Animator animator;     // only anomalies have one
    int shownFacing = -1;
    bool shownMoving;

    // Called by the spawner right after creating the customer.
    public void Init(CafeCustomerSpawner owner, CafeNavGrid navGrid, Texture2D sheet,
                     int tableIndex, Vector3 seatPosition, Vector3 doorPosition)
    {
        spawner = owner;
        grid = navGrid;
        table = tableIndex;
        seat = seatPosition;
        door = doorPosition;

        // The order is placed as soon as the customer spawns.
        order = OrderManager.Create(table);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Anomalies are animated by an Animator. Normal customers are cut from a sheet.
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            frames = CutSheet(sheet);
            ShowFrame(Front, 0);
        }

        // The object's position is the middle of the sprite. We want the FEET on the door.
        Bounds b = spriteRenderer.bounds;
        feetOffset = transform.position - new Vector3(b.center.x, b.min.y, transform.position.z);
        feetOffset.z = 0f;
        transform.position = door + feetOffset;

        WalkTo(seat);
    }

    // Cut the sprite sheet into 12 small frames.
    Sprite[] CutSheet(Texture2D sheet)
    {
        int rows = sheet.height / FrameHeight;
        Sprite[] result = new Sprite[rows * Columns];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                // Images count rows from the top, Unity counts y from the bottom.
                float y = sheet.height - (row + 1) * FrameHeight;
                Rect rect = new Rect(col * FrameWidth, y, FrameWidth, FrameHeight);
                result[row * Columns + col] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), 100f);
            }
        }
        return result;
    }

    Vector3 Feet()
    {
        return transform.position - feetOffset;
    }

    // Ask the nav grid for a route around the furniture.
    void WalkTo(Vector3 target)
    {
        path = null;
        if (grid != null) path = grid.FindPath(Feet(), target);
        if (path == null) path = new List<Vector3> { target };   // no route: walk straight
        pathIndex = 0;
    }

    void Update()
    {
        if (path == null) return;   // Init never ran
        moving = false;

        if (state == State.Entering)
        {
            if (FollowPath())
            {
                state = State.Seated;
                facing = Front;
                timer = waitSeconds;
                Debug.Log("[Customer] Seated at table " + (table + 1) + ", waiting for a " + OrderManager.Dish);
            }
        }
        else if (state == State.Seated)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Leave();
        }
        else if (state == State.Leaving)
        {
            if (FollowPath())
                Destroy(gameObject);
        }

        Animate();
    }

    void Leave()
    {
        state = State.Leaving;
        OrderManager.Remove(order);
        spawner.CustomerLeft(table, order.served);   // dirty if served, clean if not
        WalkTo(door);
    }

    // Walk one step toward the next path point. Returns true when the path is finished.
    bool FollowPath()
    {
        if (pathIndex >= path.Count) return true;

        Vector3 target = path[pathIndex];
        Vector3 toTarget = target - Feet();
        toTarget.z = 0f;
        float step = walkSpeed * Time.deltaTime;

        // Close enough: snap onto the point and aim for the next one.
        if (toTarget.magnitude <= step)
        {
            transform.position = target + feetOffset;
            pathIndex++;
            return pathIndex >= path.Count;
        }

        Vector3 direction = toTarget.normalized;
        transform.position += direction * step;
        moving = true;

        // Face whichever way we're mostly moving.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            facing = direction.x < 0f ? Left : Right;
        else
            facing = direction.y > 0f ? Back : Front;
        return false;
    }

    // Loop through the 3 frames while walking, frame 0 while standing.
    void Animate()
    {
        // Anomaly: the Animator plays the right clip. We only tell it which one.
        if (animator != null)
        {
            if (facing != shownFacing || moving != shownMoving)
            {
                shownFacing = facing;
                shownMoving = moving;
                animator.Play(WalkStates[facing], 0, 0f);        // start the clip on frame 0
                animator.speed = moving ? walkAnimSpeed : 0f;    // speed 0 = stand still on frame 0
                animator.Update(0f);
            }
            return;
        }

        int frame = 0;
        if (moving) frame = Mathf.FloorToInt(Time.time * framesPerSecond) % Columns;
        ShowFrame(facing, frame);
    }

    void ShowFrame(int row, int column)
    {
        int i = row * Columns + column;
        if (frames != null && i < frames.Length)
            spriteRenderer.sprite = frames[i];
    }

    // Player pressed F near an anomaly: it dies and turns into bouncing meat.
    public void Kill()
    {
        if (!isAnomaly) return;

        // Close its order and free the table (unless it was already leaving).
        if (state != State.Leaving)
        {
            OrderManager.Remove(order);
            spawner.CustomerLeft(table, false);
        }

        Debug.Log("[Kill] Anomaly killed at table " + (table + 1));

        if (meatPrefab != null)
        {
            List<MeatPickup> pieces = new List<MeatPickup>();
            Vector3 spot = Feet() + new Vector3(0f, 0.15f, 0f);
            for (int i = 0; i < meatCount; i++)
                pieces.Add(Instantiate(meatPrefab, spot, Quaternion.identity));
            foreach (MeatPickup piece in pieces)
                piece.Launch(pieces);
        }

        Destroy(gameObject);
    }

    // E works on a seated customer who hasn't been served yet.
    public bool CanInteract(PlayerInteractor player)
    {
        return state == State.Seated && !order.served;
    }

    public void Interact(PlayerInteractor player)
    {
        if (!player.holdingPlate)
        {
            Debug.Log("[Order] Table " + (table + 1) + " wants a " + OrderManager.Dish + ". Pick one up at the counter (E).");
            return;
        }

        player.holdingPlate = false;
        OrderManager.MarkServed(order);
        timer = eatSeconds;   // now leave in 5 seconds
    }
}
