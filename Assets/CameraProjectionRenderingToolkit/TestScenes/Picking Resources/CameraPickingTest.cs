using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CameraProjectionRenderingToolkit;
using System;

public class CameraPickingTest : MonoBehaviour {
	
	public GameObject clickableObjectGroup;
	public bool drawDebugLines=false;

	private Collider[] clickables;
	private Camera cam;
	private CPRT cprt;
	private bool init;

	private Rigidbody heldObject=null;
	private Rigidbody debugHeldObject=null;
	private Vector3 debugHeldPos;
	private float objectDistance=0.0f;


	// Use this for initialization
	void Start () {
		init=true;

		//get all clickable objets
		if (clickableObjectGroup==null) {
			Debug.LogWarning("CameraRedBallClicker script must have a clickable object group to work.");
			init=false;
		} else {
			clickables=clickableObjectGroup.GetComponentsInChildren<Collider>();
		}

		//get the camera used for raycasting
		cam=GetComponent<Camera>();
		if (cam==null) {
			Debug.LogError("CameraRedBallClicker script must have a camera to work.");
			init=false;
		} else {
			//Get the advanced perspective projection tool
			cprt=GetComponent<CPRT>();

			if (cam==null) {
				Debug.LogError("CameraRedBallClicker script must have a CustomCameraProjection to work.");
				init=false;
			}
		}
	}

	

		
	// Update is called once per frame
	void Update () {
		if (init) {
			if (Input.GetMouseButtonDown(0)) {
				heldObject=null;

				if (IsScreenPointInViewport(Input.mousePosition)) {//if the mouse is in the viewport
					Ray ray;

					//create a ray that go through the cursor while taking the projection into account
					ray=cprt.ScreenPointToRay(Input.mousePosition);

					if (drawDebugLines) DebugClickAt(Input.mousePosition);

					foreach (Collider collider in clickables) {//checking every clickable object for collision with the ray
						RaycastHit hitInfo;

						collider.Raycast(ray,out hitInfo,cam.farClipPlane);

						if (hitInfo.collider==collider) {//object has been caught
							heldObject=collider.GetComponent<Rigidbody>();
							objectDistance=(heldObject.transform.position-transform.position).magnitude*0.75f;
							Debug.Log("click on "+collider.name);
							break;
						}
					}
				}
			}

			if (heldObject) {
				if (Input.GetMouseButton(0)) {//holding the object
					Ray ray=cprt.ScreenPointToRay(Input.mousePosition);//create a ray that go trough the mouse while taking the projection into account
					Vector3 gotopos=ray.origin+ray.direction*objectDistance;//position where the "3D cursor" is
					Vector3 gofrompos=heldObject.transform.position;
					Vector3 forcevector=gotopos-gofrompos;
					Vector3 speedcompensation=-heldObject.velocity*0.1f;//speed (to compensate it)
					float mz=Input.GetAxis("Mouse ScrollWheel");

					heldObject.AddForce(forcevector+speedcompensation,ForceMode.VelocityChange);//attracting the object to the cursor

					if (mz!=0.0f) {//modifying object distance with the mousewheel
						objectDistance*=(mz<0.0f?(1.0f/1.1f):1.1f);
					}

					if (drawDebugLines&&heldObject==debugHeldObject) {
						Debug.DrawLine(heldObject.position,debugHeldPos,Color.cyan,4.0f);
					}
					debugHeldObject=heldObject;
					debugHeldPos=heldObject.position;
				} else {//dropping the object
					heldObject=null;
				}
			}


		}
	}

	private void DebugClickAt(Vector3 mousePosition) {//Drawing debug lines
		Ray ray;
		Vector3 lastpos1=new Vector3(),lastpos2=new Vector3();

		{//draw cross arround
			for (int i=0;i<=16;i++) {
				Vector3 pos1,pos2;
			
				ray=cprt.ScreenPointToRay(new Vector2(Screen.width*((float)i)/16.0f,Input.mousePosition.y));
				pos1=ray.origin+ray.direction*cam.fieldOfView;
				DrawCross(ray.origin+ray.direction*cam.farClipPlane,Color.cyan,5,8.0f,false);

				ray=cprt.ScreenPointToRay(new Vector2(Input.mousePosition.x,Screen.height*((float)i)/16.0f));
				pos2=ray.origin+ray.direction*cam.fieldOfView;
				DrawCross(ray.origin+ray.direction*cam.farClipPlane,Color.cyan,5,8.0f,false);

				if (i>0) {
					Debug.DrawLine(lastpos1,pos1,Color.blue,8.0f,false);
					Debug.DrawLine(lastpos2,pos2,Color.blue,8.0f,false);
				}

				lastpos1=pos1;
				lastpos2=pos2;
			}
		}

		ray=cprt.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(ray.origin,ray.direction*cam.farClipPlane,Color.red,8.0f,true);
		DrawCross(ray.origin+ray.direction*cam.farClipPlane,Color.red,10,8.0f,false);
	}

	public static void DrawCross(Vector3 pos,Color color,float size,float duration,bool depthtest) {
		Debug.DrawRay(pos+new Vector3(-size,0,0),new Vector3(size*2,0,0),color,duration,depthtest);
		Debug.DrawRay(pos+new Vector3(0,-size,0),new Vector3(0,size*2,0),color,duration,depthtest);
		Debug.DrawRay(pos+new Vector3(0,0,-size),new Vector3(0,0,size*2),color,duration,depthtest);
	}

	public bool IsScreenPointInViewport(Vector2 screen_position) {
		screen_position.x/=Screen.width;
		screen_position.y/=Screen.height;

		return cprt.viewportRect.Contains(screen_position);
	}

	
}








