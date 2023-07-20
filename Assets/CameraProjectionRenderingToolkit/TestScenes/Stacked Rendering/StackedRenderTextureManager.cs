using CameraProjectionRenderingToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class StackedRenderTextureManager : MonoBehaviour {

	/// <summary>
	/// The rendering of these cameras will be rendered into the RenderTexture
	/// </summary>
	public Camera[] inputCameras;
	/// <summary>
	/// This image will have the rendered texture.
	/// </summary>
	public RawImage outputImage;


	private RenderTexture tempRT;
	private CPRT cprt;

	public bool IsReadyToRender {
		get { return isActiveAndEnabled&&outputImage!=null&&inputCameras.Length>0&&inputCameras[0]!=null; }
	}


	private void Start() {
		cprt=GetComponent<CPRT>();
	}
	private void OnValidate() {
		cprt=GetComponent<CPRT>();
	}

	private void OnPreCull() {
		if (IsReadyToRender) {
			int aasetting;
			RenderTextureFormat format;
			RenderTextureReadWrite readwrite;
			//Rect rect=outputImage.GetPixelAdjustedRect();
			int w=(int)cprt.RenderTargetWidth,h=(int)cprt.RenderTargetHeight;
			Camera firstcam=inputCameras[0];

			//change the texture format here, if you need another behaviour
			if (firstcam.allowHDR) {
				format=RenderTextureFormat.DefaultHDR;
			} else {
				format=RenderTextureFormat.Default;
			}

			//MSAA is impossible with deferred rendering
			if (firstcam.renderingPath==RenderingPath.DeferredLighting||firstcam.renderingPath==RenderingPath.DeferredShading) {
				aasetting=1;
			} else {
				aasetting=Mathf.Max(QualitySettings.antiAliasing,1);
			}

			if (QualitySettings.activeColorSpace==ColorSpace.Gamma) {
				readwrite=RenderTextureReadWrite.sRGB;
			} else {
				readwrite=RenderTextureReadWrite.Linear;
			}

			//release previous render texture if needed
			ReleaseTempTexture();

			//actual Render Texture creation
			tempRT=RenderTexture.GetTemporary(w,h,24,format,readwrite,aasetting);
			tempRT.filterMode=FilterMode.Point;

			//setting the UI Image texture
			outputImage.texture=tempRT;

			//render the scene into the target texture
			foreach (Camera cam in inputCameras) {
				cam.targetTexture=tempRT;
				cam.fieldOfView=cprt.FieldOfViewY;
			}
		} else {
			ReleaseTempTexture();
		}
	}
	private void OnDestroy() {
		ReleaseTempTexture();
	}
	private void OnDisable() {
		ReleaseTempTexture();
	}

	private void ReleaseTempTexture() {
		if (tempRT!=null&&tempRT.IsCreated()) {
			if (outputImage!=null) outputImage.texture=null;
			foreach (Camera cam in inputCameras) {
				cam.targetTexture=null;
			}

			RenderTexture.ReleaseTemporary(tempRT);
			tempRT=null;
		}
	}
}
