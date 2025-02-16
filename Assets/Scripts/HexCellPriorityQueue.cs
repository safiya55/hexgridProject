using System.Collections.Generic;

public class HexCellPriorityQueue
{

    List<HexCell> list = new List<HexCell>();

    int count = 0;

    public int Count
    {
        get
        {
            return count;
        }
    }

    public void Enqueue(HexCell cell)
    {
        count += 1;
        int priority = cell.SearchPriority;
        while (priority >= list.Count)
        {
            list.Add(null);
        }
        cell.NextWithSamePriority = list[priority];
        list[priority] = cell;
    }

    public HexCell Dequeue()
    {
        count -= 1;
        return null;
    }

    public void Change(HexCell cell, int oldPriority)
    {
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
    }
}