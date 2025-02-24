using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;

    public void GenerateMap(int x, int z)
    {
        //new map new map created
        grid.CreateMap(x, z);

        //set the terrain of the middle 
        // cell column to grass, using offset coordinates.
        for (int i = 0; i < z; i++)
        {
            grid.GetCell(x / 2, i).TerrainTypeIndex = 1;
        }
    }
}