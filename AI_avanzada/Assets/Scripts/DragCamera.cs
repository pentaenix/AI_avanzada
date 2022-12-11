using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DragCamera : MonoBehaviour {

	public static DragCamera instance;

	private Vector3 initialPosition;
	private float initialSize;

	private Vector3 Origin;

	public float minCamSize;
	public float maxCamSize;

	public float zoomStep;

	[SerializeField]
	private SpriteRenderer mapRenderer;
	private float mapMinX, mapMaxX, mapMinY, mapMaxY;

	public Vector2 Position {
		get { return transform.position; }
		set { transform.position = value; }
	}



	private void Awake() {
		instance = this;
		mapMinX = mapRenderer.transform.position.x - mapRenderer.bounds.size.x / 2f;
		mapMaxX = mapRenderer.transform.position.x + mapRenderer.bounds.size.x / 2f;

		mapMinY = mapRenderer.transform.position.y - mapRenderer.bounds.size.y / 2f;
		mapMaxY = mapRenderer.transform.position.y + mapRenderer.bounds.size.y / 2f;

		initialPosition = Camera.main.transform.position;
		initialSize = Camera.main.orthographicSize;
	}

	private void LateUpdate() {
		MoveCamera();
		if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
			ZoomIn();
		} else if (Input.GetAxis("Mouse ScrollWheel") < 0f) {
			ZoomOut();
		}
	}

	public IEnumerator ResetCamera() {
		Camera.main.transform.position = initialPosition ;
		Camera.main.orthographicSize = initialSize ;
		yield return null;
	}

	private void MoveCamera() {
		if (Input.GetMouseButtonDown(0)) Origin = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		if (Input.GetMouseButton(0)) {
			Vector3 difference = Origin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
			//Camera.main.transform.position += difference;
			Camera.main.transform.position = ClampCamera(Camera.main.transform.position + difference);
		}
	}

	public void ZoomIn() {
		float newSize = Camera.main.orthographicSize - zoomStep;
		Camera.main.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);

		Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
	}

	public void ZoomOut() {
		float newSize = Camera.main.orthographicSize + zoomStep;
		Camera.main.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);

		Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
	}


	public IEnumerator FocusOn(float MoveTime, Vector2 targetPosition) {
		// Calculate the speed at which the camera should move to reach the target position in the specified time
		float moveSpeed = Vector2.Distance(transform.position, targetPosition) / MoveTime;

		// Keep moving the camera until it reaches the target position
		while (Vector2.Distance(Camera.main.transform.position, targetPosition) > 0.5f) {
			// Move the camera towards the target position at the calculated speed
			Camera.main.transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
			Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -1f);
			Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - zoomStep / 10f, minCamSize, maxCamSize);
			Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
			yield return null;
		}
	}






	private Vector3 ClampCamera(Vector3 targetPosition) {
		
		

		float camHeight = Camera.main.orthographicSize;
		float camWidth = Camera.main.orthographicSize * Camera.main.aspect;

		float minX = mapMinX + camWidth;
		float maxX = mapMaxX - camWidth;
		float minY = mapMinY + camHeight;
		float maxY = mapMaxY - camHeight;

		float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
		float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

		return new Vector3(newX, newY, targetPosition.z);
	}
}