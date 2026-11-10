using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// For long diagonal props (counters, partition walls). The prop always sits at order -1.
/// When the player's feet are behind a prop that overlaps the player on screen,
/// the player drops to -2 so it draws behind that prop. Nothing else is affected.
/// Needs a Sorting Group on the Player root.
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public sealed class LongPropSorting : MonoBehaviour
{
    private static readonly List<LongPropSorting> All = new List<LongPropSorting>();
    private static int handledFrame = -1;

    private Vector2 lineStart, lineEnd;
    private Bounds screenBounds;

    private void Awake()
    {
        GetComponent<SortingGroup>().sortingOrder = -1;

        // The area the prop covers on screen.
        bool any = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!any) { screenBounds = sr.bounds; any = true; }
            else screenBounds.Encapsulate(sr.bounds);
        }

        // The two footprint corners furthest apart give the prop's long direction.
        var corners = new List<Vector2>();
        foreach (var poly in GetComponentsInChildren<PolygonCollider2D>())
            foreach (var p in poly.points)
                corners.Add(poly.transform.TransformPoint(p + poly.offset));

        float best = -1f;
        for (int i = 0; i < corners.Count; i++)
            for (int j = i + 1; j < corners.Count; j++)
            {
                float d = (corners[i] - corners[j]).sqrMagnitude;
                if (d > best) { best = d; lineStart = corners[i]; lineEnd = corners[j]; }
            }
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void LateUpdate()
    {
        // Every prop runs this, but only the first one each frame does the work.
        if (handledFrame == Time.frameCount) return;
        handledFrame = Time.frameCount;

        var player = TopDownPlayerMovement.Active;
        if (player == null) return;
        var playerGroup = player.GetComponent<SortingGroup>();
        var playerSprite = player.GetComponentInChildren<SpriteRenderer>();
        if (playerGroup == null || playerSprite == null) return;

        bool behind = false;
        foreach (var prop in All)
        {
            if (prop.Overlaps(playerSprite.bounds) && prop.IsBehind(player.FeetPosition))
            {
                behind = true;
                break;
            }
        }
        playerGroup.sortingOrder = behind ? -2 : 0;
    }

    private bool Overlaps(Bounds b) =>
        screenBounds.min.x < b.max.x && b.min.x < screenBounds.max.x &&
        screenBounds.min.y < b.max.y && b.min.y < screenBounds.max.y;

    private bool IsBehind(Vector2 feet)
    {
        Vector2 along = lineEnd - lineStart;
        // Same sign = the feet are on the back side of the footprint line.
        return Cross(along, feet - lineStart) * Cross(along, Vector2.up) > 0f;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}