using UnityEngine;

public class HexUnit : MonoBehaviour
{
    HexCell location;

    float orientation;

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
            //make the cell aware that there is a unit standing on it.
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    //sets the hexunits orientation or allows it to be changed in how it is facing
    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    //validate the unit's location after a change has been made by elevation
    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }
}
