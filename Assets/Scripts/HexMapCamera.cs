using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    Transform swivel, stick;
    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;

    public float moveSpeedMinZoom, moveSpeedMaxZoom;float moveSpeed;

    //camera should stay inside the map by getting the boundaries
    public HexGrid grid;

    public float rotationSpeed;

    float rotationAngle;

    //value of 0 means that we are fully zoomed out,
    // while a value of 1 is fully zoomed in.
    float zoom = 1f;

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

    void Update () {
        //sooming in and out with scroll wheel
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

        float rotationDelta = Input.GetAxis("Rotation");
		if (rotationDelta != 0f) {
			AdjustRotation(rotationDelta);
		}

        //camera movement
        float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");
		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		}
	}

    void AdjustRotation (float delta) 
    {
        //Keep track of the rotation angle and adjust it in AdjustRotation. 
        //Then rotate the entire camera rig.
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
		
        //As a full circle is 360 degrees, 
        //wrap the rotation angle so it stays within 0 and 360.
        if (rotationAngle < 0f) {
			rotationAngle += 360f;
		}
		else if (rotationAngle >= 360f) {
			rotationAngle -= 360f;
		}

        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

    void AdjustPosition (float xDelta, float zDelta) 
    {
        // normalize the delta vector. for consistent speed
        Vector3 direction = 
            transform.localRotation *
            new Vector3(xDelta, 0f, zDelta).normalized;

        //get rid of delay where release result in camera movement stopping immediately
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));

        //gets the camera moving while we hold down the arrow or WASD keys
        float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;
        
        // fetch the current position of the camera rig
        Vector3 position = transform.localPosition;
        //add the X and Z deltas to it
		position += direction * distance;
        //assign the result back to the rig's position.
		transform.localPosition = ClampPosition(position);;
	}

    Vector3 ClampPosition (Vector3 position) {

        // X position has a minimum of zero, 
        //and a maximum defined by the map size.
		float xMax =
			(grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) *
			(2f * HexMetrics.innerRadius);
		position.x = Mathf.Clamp(position.x, 0f, xMax);

        //for z position
        float zMax =
			(grid.chunkCountZ * HexMetrics.chunkSizeZ - 1) *
			(1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);
        
        return position;
	}
	
	void AdjustZoom (float delta) {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

}
