using System.Collections;
using System.Collections.Generic;
using UnityEngine;




namespace CameraProjectionRenderingToolkit {

	/// <summary>
	/// Some external plugins can interact with the camera during the "OnPreCull" Unity callback, and may be in conflict with CPRT.
	/// CPRTFrustumUpdater allows to reorder the CPRT camera handling, it should typically be first in the inspector so the other 
	/// scripts can work on CPRT modifications.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(CPRT))]
	[AddComponentMenu("Image Effects/CPRT Frustum Updater")]
	[DisallowMultipleComponent]
	public class CPRTFrustumUpdater : MonoBehaviour {
		private CPRT cprt;

		private bool canCallPrecull=false;
		public bool CanCallPrecull {
			get { return canCallPrecull; }
		}

		public void Init(CPRT _cprt) {
			cprt=_cprt;
			canCallPrecull=false;
		}

		public void OnPreCull() {
			if (cprt!=null&&isActiveAndEnabled) {
				canCallPrecull=true;
				cprt.OnPreCull();
				canCallPrecull=false;
			}
		}
	}
}