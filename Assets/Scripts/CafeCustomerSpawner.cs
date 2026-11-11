using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Sits at the entrance. Spawns customers, but only at clean tables.
// Table life: Clean -> Occupied -> Dirty (only if the customer was served) -> Clean (player presses Q).
public class CafeCustomerSpawner : MonoBehaviour
{
    public enum TableState { Clean, Occupied, Dirty }

    public CafeCustomer customerPrefab;
    public CafeCustomer anomalyPrefab;   // the Anomaly prefab (leave empty = no anomalies)
    public Texture2D[] customerSheets;   // random look for each customer
    public Transform[] seats;            // one empty object per stool
    public Tilemap floor;                // leave empty to use the object named "floor"

    public float firstDelay = 1f;
    public float minGap = 2f;            // seconds between customers
    public float maxGap = 5f;
    public Vector2 feetClearance = new Vector2(0.6f, 0.3f);
    public int firstAnomalyMax = 5;      // first anomaly is customer number 1 to this

    CafeNavGrid grid;
    TableState[] tables;
    float timer;
    int customersSpawned;
    int nextAnomalyAt;

    void Start()
    {
        if (floor == null)
            floor = GameObject.Find("floor").GetComponent<Tilemap>();
        grid = new CafeNavGrid(floor, feetClearance);

        // Every table starts clean.
        tables = new TableState[seats.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = TableState.Clean;

        OrderManager.Clear();
        Debug.Log("[Table] All " + tables.Length + " tables are clean");
        timer = firstDelay;

        customersSpawned = 0;
        nextAnomalyAt = Random.Range(1, firstAnomalyMax + 1);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        int table = PickCleanTable();
        if (table == -1)
        {
            timer = 0.5f;   // no clean table, check again soon
            return;
        }

        SpawnCustomer(table);
        timer = Random.Range(minGap, maxGap);
    }

    // Returns a random clean table, or -1 if there is none.
    int PickCleanTable()
    {
        List<int> cleanTables = new List<int>();
        for (int i = 0; i < tables.Length; i++)
        {
            if (tables[i] == TableState.Clean)
                cleanTables.Add(i);
        }

        if (cleanTables.Count == 0) return -1;
        return cleanTables[Random.Range(0, cleanTables.Count)];
    }

    void SpawnCustomer(int table)
    {
        customersSpawned++;
        bool anomaly = anomalyPrefab != null && customersSpawned == nextAnomalyAt;

        Texture2D look = null;     // anomalies don't need a sheet, their Animator does the look
        CafeCustomer customer;
        if (anomaly)
        {
            customer = Instantiate(anomalyPrefab, transform.position, Quaternion.identity);
            nextAnomalyAt = customersSpawned + Random.Range(3, 7);   // next one in 3 to 6 customers
            Debug.Log("[Spawner] Anomaly arrived (customer #" + customersSpawned + ")");
        }
        else
        {
            look = customerSheets[Random.Range(0, customerSheets.Length)];
            customer = Instantiate(customerPrefab, transform.position, Quaternion.identity);
        }
        tables[table] = TableState.Occupied;
        customer.Init(this, grid, look, table, seats[table].position, transform.position);
    }

    // Called by a customer when it gets up to leave.
    public void CustomerLeft(int table, bool wasServed)
    {
        TableVisuals visuals = TableVisuals.Get(table);
        visuals?.SetPlate(false);   // the plate goes away when the customer gets up

        if (wasServed)
        {
            tables[table] = TableState.Dirty;
            visuals?.SetDirty(true);
            Debug.Log("[Table] Table " + (table + 1) + " is dirty. Press Q near it to clean.");
        }
        else
        {
            tables[table] = TableState.Clean;
            visuals?.SetDirty(false);
            Debug.Log("[Table] Table " + (table + 1) + " is free (customer was not served)");
        }
    }

    // Called by the player on Q. Cleans the first dirty table in range.
    public bool CleanTableNear(Vector2 playerPosition, float range)
    {
        for (int i = 0; i < tables.Length; i++)
        {
            if (tables[i] != TableState.Dirty) continue;

            // Use the table's visual position if it has one, otherwise the seat.
            TableVisuals visuals = TableVisuals.Get(i);
            Vector2 anchor = visuals != null ? (Vector2)visuals.transform.position : (Vector2)seats[i].position;

            if (Vector2.Distance(anchor, playerPosition) <= range)
            {
                tables[i] = TableState.Clean;
                visuals?.SetDirty(false);
                Debug.Log("[Table] Table " + (i + 1) + " cleaned");
                return true;
            }
        }
        return false;
    }

    // Green circle = entrance, cyan circles = seats (Scene view only).
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        if (seats == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform s in seats)
            if (s != null) Gizmos.DrawWireSphere(s.position, 0.2f);
    }
}
