using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraProjectionRenderingToolkit;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraWorldToScreenTest : MonoBehaviour {
	public GameObject objectGroup;
	public bool showLinear=false;

	private Transform[] transforms;
	private Camera cam;
	private CPRT cprt;
	private bool init;


	// Use this for initialization
	void Start() {
		init=true;

		//get all clickable objets
		if (objectGroup!=null) {
			List<Transform> list=new List<Transform>();
			
			objectGroup.GetComponentsInChildren<Transform>(false,list);
			list.Remove(objectGroup.transform);

			transforms=list.ToArray();
		}

		//get the camera used for raycasting
		cam=GetComponent<Camera>();
		if (cam==null) {
			Debug.LogError("This script must have a camera to work.");
			init=false;
		} else {
			//Get the advanced perspective projection tool
			cprt=GetComponent<CPRT>();

			if (cam==null) {
				Debug.LogError("This script must have a CustomCameraProjection to work.");
				init=false;
			}
		}
	}

	void OnValidate() {
		Start();
	}

	

		
	// Update is called once per frame
	void Update () {
		if (init&&isActiveAndEnabled) {
			if (transforms!=null) {
				foreach (Transform tr in transforms) {

					if (showLinear) {//Classic linear world to screen projection, wrong if CPRT has a non-linear projection
						Vector3 linearvppoint=cam.WorldToViewportPoint(tr.position);

						DrawCross(linearvppoint,new Color(0.0f,0.0f,1.0f,0.5f),0.02f);//Draw in transparent-blue, don't forget to enable gizmo drawing in game view
					}

					{//CPRT projected world to screen projection
						Vector3 linearvppoint=cprt.WorldToViewportPoint(tr.position);

						DrawCross(linearvppoint,new Color(0.07f,1.0f,1.0f,1.5f),0.02f);//Draw in cyan, don't forget to enable gizmo drawing in game view
					}

				}
			}
			
			{
				bool morefovkey=Input.GetKeyDown(KeyCode.UpArrow);
				bool lessfovkey=Input.GetKeyDown(KeyCode.DownArrow);

				if (Input.GetKeyDown(KeyCode.KeypadPlus)) {
					cprt.enabled=true;
					Debug.Log("CPRT status : "+cprt.enabled+" / "+cprt.isActiveAndEnabled);
				}
				if (Input.GetKeyDown(KeyCode.KeypadMinus)) {
					cprt.enabled=false;
					Debug.Log("CPRT status : "+cprt.enabled+" / "+cprt.isActiveAndEnabled);
				}

				if (morefovkey||lessfovkey) {
					float fovadding=morefovkey?5.0f:-5.0f;

					cprt.fieldOfView+=fovadding;
					Debug.Log("CPRT fov : "+cprt.fieldOfView);
				}
			}
		}
	}



	/// <summary>
	/// draw a 2D cross in viewport
	/// </summary>
	/// <param name="vppos">Normalized viewport position, not draw if z<0.</param>
	public void DrawCross(Vector3 vppos,Color color,float size) {
		if (vppos.z>0.0f) {
			Vector3 pos1=cam.ViewportToWorldPoint(vppos+new Vector3(-size,0.0f));
			Vector3 pos2=cam.ViewportToWorldPoint(vppos+new Vector3(+size,0.0f));
			Vector3 pos3=cam.ViewportToWorldPoint(vppos+new Vector3(0.0f,-size));
			Vector3 pos4=cam.ViewportToWorldPoint(vppos+new Vector3(0.0f,+size));

			Debug.DrawLine(pos1,pos2,color);
			Debug.DrawLine(pos3,pos4,color);
		}
	}

}
