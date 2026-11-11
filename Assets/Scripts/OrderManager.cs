using System.Collections.Generic;
using UnityEngine;

//a calss for ONE customer's order.
public class Order
{
    public int id;
    public int table;
    public bool served;
}

//keeps a list of all active orders and prints it whenever it changes.
public static class OrderManager
{
    public const string Dish = "Meat Plate";   // every customer orders this

    static List<Order> activeOrders = new List<Order>();
    static int nextId = 1;

    public static void Clear()
    {
        activeOrders.Clear();
        nextId = 1;
    }

    //new customer/anomaly spawned: add a new order.
    public static Order Create(int table)
    {
        Order order = new Order();
        order.id = nextId;
        order.table = table;
        nextId++;

        activeOrders.Add(order);
        Debug.Log("[Order] New order #" + order.id + " (" + Dish + ") for table " + (table + 1));
        PrintList();
        return order;
    }

    //player gave the plate to the customer.
    public static void MarkServed(Order order)
    {
        order.served = true;
        Debug.Log("[Order] Order #" + order.id + " served");
        PrintList();
    }

    //customer left: remove the order.
    public static void Remove(Order order)
    {
        activeOrders.Remove(order);
        Debug.Log("[Order] Order #" + order.id + " closed");
        PrintList();
    }

    static void PrintList()
    {
        //it only counts the orders that are still waiting for food.
        int unserved = 0;
        string text = "";
        foreach (Order o in activeOrders)
        {
            if (!o.served)
            {
                unserved++;
                text += "\n  #" + o.id + " table " + (o.table + 1);
            }
        }
        Debug.Log("Orders unserved: " + unserved + text);
    }
}
