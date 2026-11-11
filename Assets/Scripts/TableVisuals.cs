using System.Collections.Generic;
using UnityEngine;

// One per table. tableIndex must match the table number the spawner uses (0 = Table 1).
public class TableVisuals : MonoBehaviour
{
    public int tableIndex;
    public GameObject plateVisual;
    public GameObject dirtyVisual;

    static readonly List<TableVisuals> all = new List<TableVisuals>();

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        if (plateVisual) plateVisual.SetActive(false);
        if (dirtyVisual) dirtyVisual.SetActive(false);
    }

    public static TableVisuals Get(int index)
    {
        foreach (var t in all)
            if (t.tableIndex == index) return t;
        return null;
    }

    public void SetPlate(bool on) { if (plateVisual) plateVisual.SetActive(on); }
    public void SetDirty(bool on) { if (dirtyVisual) dirtyVisual.SetActive(on); }
}