using UnityEngine;

public class HexUnit : MonoBehaviour
{
    HexCell location;

    //so that Units identify the cell 
    // that they are occupying
    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            location = value;
            transform.localPosition = value.Position;
        }
    }

    HexCell location;

}
