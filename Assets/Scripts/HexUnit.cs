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

    const float rotationSpeed = 180f;

    public HexGrid Grid { get; set; }

    const int visionRange = 3;

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
        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);

                //derivative vector aligns with the travel direction. 
                //method to convert that into a rotation for our unit
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }

        // make sure that the unit ends up exactly at its destination.
        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        //no longer need to remember which path we traveled
        // can release the cell list at the end
        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
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
                Grid.DecreaseVisibility(location, visionRange);
                location.Unit = null;
            }

            location = value;
            //make the cell aware that there is a unit standing on it.
            value.Unit = this;
            Grid.IncreaseVisibility(value, visionRange);
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
        if (location)
        {
            Grid.DecreaseVisibility(location, visionRange);
        }
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

    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;

        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation =
            Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0f)
        {
            float speed = rotationSpeed / angle;

            for (
                float t = Time.deltaTime * speed;
                t < 1f;
                t += Time.deltaTime * speed
            )
            {
                transform.localRotation =
                    Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }
}
