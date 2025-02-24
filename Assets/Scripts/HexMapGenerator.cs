using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;

    int cellCount;

    public void GenerateMap(int x, int z)
    {
        //keep track of the amount of cells in HexMapGenerator 
        cellCount = x * z;

        //new map new map created
        grid.CreateMap(x, z);

        //set the terrain of the middle 
        // cell column to grass, using offset coordinates.
        RaiseTerrain(7);
    }

    void RaiseTerrain(int chunkSize)
    {
        for (int i = 0; i < chunkSize; i++)
        {
            GetRandomCell().TerrainTypeIndex = 1;
        }
    }

    //determines a random cell index and retrieves the 
    // corresponding cell from the grid.
    HexCell GetRandomCell()
    {
        return grid.GetCell(Random.Range(0, cellCount));
    }
}