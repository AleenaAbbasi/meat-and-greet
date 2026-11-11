using System.Collections.Generic;
using UnityEngine;

// On the Meat prefab. The meat hops out of the dead anomaly, bounces a few times
// (fake gravity), then stays in one spot. Walk over it to collect it.
public class MeatPickup : MonoBehaviour
{
    public float gravity = 25f;        // how fast it falls back down
    public float bounciness = 0.5f;    // 0.5 = every bounce is half as high as the last
    public float pickupRadius = 0.7f;  // how close the player must be to collect it

    Vector2 ground;      // where the meat is on the floor
    float height;        // how high above the floor it is right now
    float upSpeed;       // up/down speed (positive = going up)
    Vector2 slide;       // sideways speed
    float z;
    bool landed;
    bool collected;
    List<MeatPickup> group;   // the 3 pieces from the same anomaly

    // Called by the dying anomaly right after it creates the meat.
    public void Launch(List<MeatPickup> pieces)
    {
        group = pieces;
        ground = transform.position;
        z = transform.position.z;
        upSpeed = Random.Range(6f, 8f);
        slide = Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.8f);
    }

    void Update()
    {
        if (collected || group == null) return;

        if (!landed)
        {
            float dt = Time.deltaTime;
            upSpeed -= gravity * dt;
            height += upSpeed * dt;
            ground += slide * dt;

            if (height <= 0f)
            {
                height = 0f;
                upSpeed = -upSpeed * bounciness;   // bounce back up, lower each time
                slide *= 0.6f;                     // lose some sideways speed
                if (upSpeed < 1.5f)                // too small to bounce again: stop here
                {
                    landed = true;
                    slide = Vector2.zero;
                }
            }

            transform.position = new Vector3(ground.x, ground.y + height, z);
            return;
        }

        // Landed: wait for the player to walk over it.
        TopDownPlayerMovement player = TopDownPlayerMovement.Active;
        if (player == null) return;
        if (Vector2.Distance(player.FeetPosition, ground) <= pickupRadius)
            Collect();
    }

    // Picking up one piece collects all the pieces from that anomaly.
    void Collect()
    {
        int count = group.Count;
        foreach (MeatPickup piece in group)
        {
            if (piece == null) continue;
            piece.collected = true;
            Destroy(piece.gameObject);
        }
        Debug.Log("+" + count + " meat collected");
    }
}
