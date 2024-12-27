using UnityEngine;

public class HexMapCamera : MonoBehaviour {

	Transform swivel, stick;

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}
}