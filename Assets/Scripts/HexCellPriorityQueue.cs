using System.Collections.Generic;

public class HexCellPriorityQueue
{

    List<HexCell> list = new List<HexCell>();

    int count = 0;

    int minimum = int.MaxValue;

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
        if (priority < minimum)
        {
            minimum = priority;
        }
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
        for (; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];
            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null;
    }

    public void Change(HexCell cell, int oldPriority)
    {
        //declaring the head of the old priority list to be the current cell
        HexCell current = list[oldPriority];
        //keep track of the next cell. We can directly grab the next cell
        HexCell next = current.NextWithSamePriority;

        //If the current cell is the changed cell, then it is the head cell 
        // and we can cut it away, as if we dequeued it.
        if (current == cell)
        {
            list[oldPriority] = next;
        }

        else //we have to follow the chain until we end up at the cell in front of the changed cell. 
        // That one holds the reference to the cell that has been changed.
        {
            while (next != cell)
            {
                current = next;
                next = current.NextWithSamePriority;
            }
            //can remove the changed cell from the linked list, by skipping it.
            current.NextWithSamePriority = cell.NextWithSamePriority;

            //cell removed will be added gain so it ends 
            // up in the list for its new priority.
            Enqueue(cell);

            //decrement count
            count -= 1;
        }
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}