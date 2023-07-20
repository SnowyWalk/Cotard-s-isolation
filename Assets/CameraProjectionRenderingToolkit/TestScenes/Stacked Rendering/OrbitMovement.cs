using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitMovement : MonoBehaviour {

	public Transform rotateAround;
	public float degPerSecond=1.0f;


	private Vector3 rotationAxis;



	void Start() {
		Vector3 nz=rotateAround.position-transform.position;
		Vector3 nx=Vector3.Cross(nz,Vector3.up);

		rotationAxis=Vector3.Cross(nx,nz).normalized;
	}

	void Update() {
		Vector3 aroundpos=rotateAround.position;

		transform.position=Quaternion.AngleAxis(degPerSecond*Time.deltaTime,rotationAxis)*(transform.position-aroundpos)+aroundpos;
	}
}
