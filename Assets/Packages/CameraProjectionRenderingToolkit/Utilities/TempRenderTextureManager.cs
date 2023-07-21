using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using CameraProjectionRenderingToolkit;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class TempRenderTextureManager : MonoBehaviour {

	/// <summary>
	/// Camera that renders the scene, it should not see the UI Layer
	/// </summary>
	public Camera sceneCamera;

	/// <summary>
	/// An UI Image where the render will be drawn
	/// </summary>
	public RawImage renderRawImage;


	/// <summary>
	/// The sceneCamera CPRT script
	/// </summary>
	private CPRT cprt=null;

	/// <summary>
	/// The temporary render texture where the scene will be rendered
	/// </summary>
	private RenderTexture tempRT=null;

	/// <summary>
	/// Camera that renders the scene, it should not see the UI Layer
	/// </summary>
	public Camera postfxCamera;


	public bool IsReadyToRender {
		get { return isActiveAndEnabled&&renderRawImage!=null&&cprt!=null; }
	}



	private void Start() {
		Init();
	}
	private void OnValidate() {
		Init();
	}
	public void Init() {
		if (isActiveAndEnabled&&sceneCamera!=null&&renderRawImage!=null) {
			cprt=sceneCamera.GetComponent<CPRT>();
			postfxCamera=GetComponent<Camera>();
		}
	}

	private void OnPreCull() {
		if (IsReadyToRender) {
			int depthbit,aasetting;
			RenderTextureFormat format;
			RenderTextureReadWrite readwrite;
			Rect rect=renderRawImage.GetPixelAdjustedRect();
			int w=(int)rect.width,h=(int)rect.height;

			//change the texture format here, if you need another behaviour
			if (sceneCamera.allowHDR) {
				format=RenderTextureFormat.DefaultHDR;
			} else {
				format=RenderTextureFormat.Default;
			}

			//MSAA is impossible with deferred rendering
			if (sceneCamera.renderingPath==RenderingPath.DeferredLighting||sceneCamera.renderingPath==RenderingPath.DeferredShading) {
				aasetting=1;
			} else {
				aasetting=Mathf.Max(QualitySettings.antiAliasing,1);
			}

			switch (cprt.renderTextureDepthBits) {
				case CPRT.RenderTextureDepthBitCount.Depth16:
					depthbit=16;
					break;
				case CPRT.RenderTextureDepthBitCount.Depth32:
					depthbit=32;
					break;
				case CPRT.RenderTextureDepthBitCount.DepthStencil24:
				default:
					depthbit=24;
					break;
			}

			if (QualitySettings.activeColorSpace==ColorSpace.Gamma) {
				readwrite=RenderTextureReadWrite.sRGB;
			} else {
				readwrite=RenderTextureReadWrite.Linear;
			}

			//release previous render texture if needed
			ReleaseTempTexture();

			//actual Render Texture creation
			tempRT=RenderTexture.GetTemporary(w,h,depthbit,format,readwrite,aasetting);
			tempRT.filterMode=FilterMode.Point;

			//setting the UI Image texture
			renderRawImage.texture=tempRT;

			//render the scene into the target texture
			sceneCamera.enabled=true;
			if (cprt.isActiveAndEnabled) {
				cprt.RenderToTexture(tempRT);
			} else {
				sceneCamera.targetTexture=tempRT;
				sceneCamera.Render();
			}
			sceneCamera.enabled=false;
		}
	}
	private void OnPostRender() {
		if (IsReadyToRender) {
			//sceneCamera.enabled=false;
			if (!cprt.isActiveAndEnabled) {
				sceneCamera.targetTexture=null;
			}
		}
	}
	private void OnDestroy() {
		ReleaseTempTexture();
	}

	private void ReleaseTempTexture() {
		if (tempRT!=null&&tempRT.IsCreated()) {
			if (renderRawImage!=null) renderRawImage.texture=null;
			RenderTexture.ReleaseTemporary(tempRT);
			tempRT=null;
		}
	}
}
