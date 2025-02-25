using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;

    int cellCount;

    HexCellPriorityQueue searchFrontier;

    int searchFrontierPhase;

    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    [Range(20, 200)]
    public int chunkSizeMin = 30;

    [Range(20, 200)]
    public int chunkSizeMax = 100;

    [Range(5, 95)]
    public int landPercentage = 50;

    public void GenerateMap(int x, int z)
    {
        //keep track of the amount of cells in HexMapGenerator 
        cellCount = x * z;

        //new map new map created
        grid.CreateMap(x, z);

        //Make sure that the priority queue exists before we will need it.
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        //calculate how many cells have to become land. That amount is our land budget.
        CreateLand();

        //set all terrain types once.
        SetTerrainType();

        //After a new map has been created

        for (int i = 0; i < cellCount; i++)
        {
            //search frontier of all cells is zero.
            grid.GetCell(i).SearchPhase = 0;
        }
    }

    //invoke RaiseTerrain as long as there's still land budget to be spent.     
    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);

        //keep raising land as long as it has budget.
        while (landBudget > 0)
        {
            landBudget = RaiseTerrain(
                Random.Range(chunkSizeMin, chunkSizeMax + 1), landBudget
            );
        }
    }

    int RaiseTerrain(int chunkSize, int budget)
    {
        // search for appropriate cells 
        //increasing the search frontier phase by 1
        searchFrontierPhase += 1;
        //Then initialize the frontier with the first cell by random
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        //set its distance and heuristic to zero besides setting its search phase
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        //use the first random cell as the center of the chunk. 
        HexCoordinates center = firstCell.coordinates;

        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        { // Each iteration, dequeue the next cell, set its terrain type, increase the size, 
          // then go through that cell's neighbors

            //All neighbors are simply added to the frontier
            HexCell current = searchFrontier.Dequeue();

            current.Elevation += 1;

            //When the current cell's new elevation is 1, it has 
            // just become land, so the budget decrements,
            //  which could end the chunk's growth.
            if (current.Elevation == 1 && --budget == 0)
            {
                break;
            }

            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    //distance of all other cells is relative to first random cell 
                    // as the center of the chunk.
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    //if the next Random.value number is less than some threshold, set that cell's heuristic to 1 instead of 0. Let's
                    // use jitterProbability as the threshold, which means most likely a certain percentage of the cells will be affected.
                    //mess the chunk up to make it random
                    neighbor.SearchHeuristic =
                        Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        //Once done, clear the frontier.
        searchFrontier.Clear();

        return budget;
    }

    //determines a random cell index and retrieves the 
    // corresponding cell from the grid.
    HexCell GetRandomCell()
    {
        return grid.GetCell(Random.Range(0, cellCount));
    }

    //to set all terrain types once.
    void SetTerrainType()
    {
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            cell.TerrainTypeIndex = cell.Elevation;
        }
    }
}