#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace CameraProjectionRenderingToolkit {

	[ExecuteInEditMode]
	public class SceneLoadWarning : MonoBehaviour {
#if UNITY_EDITOR
		public void Start() {
			CheckAssets();
		}
		public void CheckAssets() {
			string[] fpscstrs=AssetDatabase.FindAssets("FirstPersonController");

			if (fpscstrs==null||fpscstrs.Length==0) {
				Debug.LogError("UnityStandardAsset not found (FirstPersonController) : try Assets -> Import Package -> Characters");
			}
		}
#endif
	}

}

#endif
