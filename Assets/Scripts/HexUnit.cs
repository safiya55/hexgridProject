using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class HexUnit : MonoBehaviour
{
    HexCell location;

    float orientation;

    public static HexUnit unitPrefab;

    //HexUnit remember the path it's supposed to travel, so it can visualize it using gizmos.
    List<HexCell> pathToTravel;

    const float travelSpeed = 4f;

    void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
        }
    }

    //set the unit's position. Use the time delta instead of fixed 0.1 increments. 
    // //And yield each iteration. That will move the unit from one cell to the 
    // next in one second.
    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                yield return null;
            }
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }
    }

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
            if (location)
            {
                location.Unit = null;
            }

            location = value;
            //make the cell aware that there is a unit standing on it.
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    //teleport Unit to the destination.
    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        pathToTravel = path;
        //stop all existing coroutines. 
        StopAllCoroutines();
        StartCoroutine(TravelPath());
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

    // clearing the cell's unit reference
    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        //write the unit's coordinates, and its orientation. 
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        // reading the unit data. 
        float orientation = reader.ReadSingle();

        grid.AddUnit(
            Instantiate(unitPrefab), grid.GetCell(coordinates), orientation
        );
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    //to show the last path that should be traveled,
    void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0)
        {
            return;
        }

        Vector3 a, b, c = pathToTravel[0].Position;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
			b = pathToTravel[i - 1].Position;
			c = (b + pathToTravel[i].Position) * 0.5f;
			for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed) {
				Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
			}
        }

        //To reach the center of the destination cell,
        a = c;
        //use cell's position as the final point, instead of an edge.
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0f; t < 1f; t += 0.1f)
        {
            Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
        }
    }
}
