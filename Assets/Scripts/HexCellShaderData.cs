using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{

    Texture2D cellTexture;
    Color32[] cellTextureData;

    List<HexCell> transitioningCells = new List<HexCell>();

    const float transitionSpeed = 255f;

    //toggle whether we want immediate transitions. 
    public bool ImmediateMode { get; set; }

    bool needsVisibilityReset;

    //access to resetting the visibility of all cells
    public HexGrid Grid { get; set; }


    public void Initialize(int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Reinitialize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32, false, true
            );
            cellTexture.filterMode = FilterMode.Point;
            //cellTexture.wrapMode = TextureWrapMode.Clamp;
            cellTexture.wrapModeU = TextureWrapMode.Repeat;
			cellTexture.wrapModeV = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }

        Shader.SetGlobalVector(
            "_HexCellData_TexelSize",
            new Vector4(1f / x, 1f / z, x, z)
        );

        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }
        transitioningCells.Clear();

        //To make sure that the data is updated after creating a new map
        //enable the component after initialization.
        enabled = true;
    }

    public void RefreshTerrain(HexCell cell)
    {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    void LateUpdate()
    {
        //request a reset
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        //get delta
        int delta = (int)(Time.deltaTime * transitionSpeed);
        //It is also theoretically possible to get very high frame rates. 
        // Together with a low transition speed, this could result in a delta of zero. 
        //To guarantee progress, force the delta to have a minimum of 1.
        if (delta == 0)
        {
            delta = 1;
        }

        //loop through all transitioning cells and update their data.
        for (int i = 0; i < transitioningCells.Count; i++)
        {
            if (!UpdateCellData(transitioningCells[i], delta))
            {
                //move the last cell to the current index and then remove the last one.
                transitioningCells[i--] =
                    transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }

        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = transitioningCells.Count > 0;
    }

    public void RefreshVisibility(HexCell cell)
    {
        //determine the delta to apply to the values, by multiplying the time delta with the speed. This has to be an integer, because we don't know how 
        // large it could get. A freak frame-rate dip could make the delta larger than 255.
        int index = cell.Index;
        if (ImmediateMode)
        {
            cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
            cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
        }
        else if (cellTextureData[index].b != 255)
        {
            cellTextureData[index].b = 255;

            transitioningCells.Add(cell);
        }
        enabled = transitioningCells.Count > 0;
    }

    bool UpdateCellData(HexCell cell, int delta)
    {
        //going to need the cell's index and data to do its work
        int index = cell.Index;
        Color32 data = cellTextureData[index];
        bool stillUpdating = false;

        //If the cell is explored but its G value isn't 255 yet,
        if (cell.IsExplored && data.g < 255)
        {
            //t is still in transition, so keep track of this fact.
            stillUpdating = true;


            if (!stillUpdating)
            {
                //only add cells whose B value isn't 255.Immediately Loading Visibility
                data.b = 0;
            }
            //add the delta to the cell's G value. 
            int t = data.g + delta;
            //Arithmatic operations don't work on bytes, they are always 
            // converted to integers first. So the sum is an integer, which has to be cast to a byte.
            data.g = t >= 255 ? (byte)255 : (byte)t;
        }

        //do the same thing for the visibility, which uses the R value.
        if (cell.IsVisible && data.r < 255)
        {
            stillUpdating = true;
            int t = data.r + delta;
            data.r = t >= 255 ? (byte)255 : (byte)t;
        }

        //has to determine whether this cell still requires further updating.
        //adjusted data has to be applied and the still-updating status returned.
        cellTextureData[index] = data;
        return stillUpdating;
    }

    //Figuring out how the overall visibility situation could have changed
    public void ViewElevationChanged()
    {
        //schedule a reset of all cell visibility
        //Add a boolean field to keep track of whether this is required. 
        //reset everything
        needsVisibilityReset = true;
        enabled = true;
    }

    public void SetMapData(HexCell cell, float data)
    {
        cellTextureData[cell.Index].b =
            data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
        enabled = true;
    }
}