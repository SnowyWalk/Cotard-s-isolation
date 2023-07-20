//#define CPRT_LOG_BUFFERS//Logs what happens during the rendering (about buffers)
//#define CPRT_LOG_FRAMERATE//Logs the true rendering framerate (the native unity graphics framerate seem to be broken by this plugin)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;


/*
 * Autor : Melvin REY
 * For any request/question, contact me at refoldedgames@gmail.com
 */


namespace CameraProjectionRenderingToolkit {
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Image Effects/CPRT")]
	[DisallowMultipleComponent]
	public class CPRT : MonoBehaviour {

		//some constants, change them at your own risk
		public const int MaxRenderTextureSize=16384;//To never get errors on low performance system, set this to 4096.
		public const float MinRenderSizeFactor=0.1f;//v1.6
		public const float MaxRenderSizeFactor=12.0f;
		public const int MinProjectionPrecision=4;
		public const int MaxProjectionPrecision=250;



		/// <summary>
		/// A projection type
		/// </summary>
		public enum CPRTType : int {
			//perspectives :

			/// <summary>
			/// No projection modification, but like every other modes, gives access to oversampling/downsampling
			/// </summary>
			SimpleOversampling = 1,
			/// <summary>
			/// Simple postprocessing effect that mimics a lens distortion effect (inaccurate). This effect was in the
			/// unity built'in image effects, but wasn't able to oversample, so the center pixels where downsampled.
			/// </summary>
			UnityBuiltinFisheye,

			//stereo perspectives :

			/// <summary>
			/// This perspective projection isn't very realistic but gives a pretty good feeling of being small relative to 
			/// the architectural structures.
			/// Classic "Pannini" projection. It's a cylindrical stereoprojection (basic projections are called rectilinear). 
			/// It's useful for reducing the distortion of objects near of the screen's X borders when the field of view is 
			/// high. 
			/// Furthermore, this projection gives more space to the center objects, which has the effect
			/// of "zooming" the center pixels. This is why you'll need to oversample the rendering to not have a 
			/// blurry/pixelated image at center.
			/// By the inspector, you have a "Min Pixel Sample Count" slider to automatically set quality related settings.
			/// By C# scripting, please refer to the MinPixelSampleCount property.
			/// </summary>
			Pannini,
			/// <summary>
			/// This perspective projection is more suited to FPS and other "natural", frequently moving, not 
			/// horizon-aligned views. 
			/// It keeps the horizon of the projection conform to the X-Z plane. The Pannini projection create a singularity
			/// just below the camera (which is never visible unless the Y field of view is over 180°), so the 
			/// AdaptivePannini will attenuate itself when the camera will be able to see it. 
			/// It needs some adjustments,but the default values are quite good for a ~120° maximum Y field of view. 
			/// Personally I recommend not exceeding 90° field of view for a FPS camera, with intensity set to ~0,47.
			/// </summary>
			AdaptivePannini,
			/// <summary>
			/// This perspective projection is the mathematically most accurate immitation of what the human eye sees.
			/// It reduce objects distortion when they are far of the center of the screen (pannini only do it horizontally).
			/// Like Pannini, it gives more space to the center objects, which has the effect
			/// of "zooming" the center pixels. This is why you'll need to oversample the rendering to not have a 
			/// blurry/pixelated image at center. 
			/// By the inspector, you have a "Min Pixel Sample Count" slider to automatically set quality related settings.
			/// By C# scripting, please refer to the MinPixelSampleCount property.
			/// This projection require a pretty big mesh to be drawn so the default mesh precision will be quite 
			/// performance-heavy, so don't hesitate to halve it (31 is pretty good for real-time application).
			/// </summary>
			Stereospherical,

			//orthographics :

			/// <summary>
			/// This orthographic projection can imitate what you see in classic RPGs, like Gameboy Pokemons or 
			/// Final-Fantasy Tactics. It can draw Top-Down, Isometric, Military and Cavalier projections. 
			/// Please refer to obliqueBias to see typical settings.
			/// </summary>
			ObliqueOrthographic,
			/// <summary>
			/// This projection mimics an orthographic projection with a perspective in order to allow deferred rendering.
			/// It introduces the perspectiveOffset and orthographicSize parameters. 
			/// Make sure the center of interest is around halfway of the camera near & far clip plane.
			/// </summary>
			PseudoOrthographic,
		};

		/// <summary>
		/// Bit count of a depth buffer used for a rendering. Can be casted to an int that gives this bit count.
		/// </summary>
		public enum RenderTextureDepthBitCount {
			Depth16 = 16, DepthStencil24 = 24, Depth32 = 32
		};
		/// <summary>
		/// This enumerator defines the filter used for blending rendered pixels when they are smaller or bigger than the 
		/// screen pixels (happen with SSAA, Pannini and Stereospherical projection).
		/// </summary>
		public enum CPRTFilterMode : int {
			/// <summary>
			/// No filter applied, the nearest sample is selected. Useful to get a pixelated rendering when renderSizeFactor<0.
			/// </summary>
			NearestNeightboor,
			/// <summary>
			/// This filter just mixes the 4 nearest samples. Sometimes a “blurring” pattern can appear. The projection 
			/// distortion isn’t taken into account. 
			/// </summary>
			SimpleBilinear,
			/// <summary>
			/// Faster and less accurate version of DistordedBilinear.
			/// </summary>
			DistordedBilinearOptimized,
			/// <summary>
			/// Same as SimpleBilinear, but also take into account the projection distortion, useful with Pannini and 
			/// Stereospherical projection when aliasing is present on the border on the screen.
			/// The distortion tolerance can be set with filterSharpen (1.414 is a pretty good value).
			/// </summary>
			DistordedBilinear,
			/// <summary>
			/// Take into account the projection distortion and can remove the "blur pattern", but is blurrier and more GPU demanding
			/// than the other modes. It can be sharper with a higher value of filterSharpen (1.414 is a pretty good value).
			/// </summary>
			UniformBilinear,
		};

		public enum FOVSettingType {
			/// <summary>
			/// Vertical FOV is the default FOV setting in video games. Vertical FOV setting allow you to set the vertical aperture, and will stay equal with any screen ratio (while Horizontal FOV will not).
			/// </summary>
			Vertical,
			/// <summary>
			/// Horizontal FOV setting allow you to set the horizontal aperture, and will stay equal with any screen ratio (while Vertical FOV will not).
			/// </summary>
			Horizontal,
			/// <summary>
			/// Diagonal FOV setting allow you to set the FOV by the wider viewport angle. This is the common unit for real life lenses.
			/// </summary>
			Diagonal,
		};

		public enum ScreenshotPassCount : int {
			Normal = 1, x2 = 2, x4 = 4, x8 = 8, x16 = 16
		};



		/// <summary>
		/// Projection type, must be coherent relative to the Camera.orthographic mode.
		/// RefreshEffect() and RefreshViewport() must be called if it has been changed.
		/// </summary>
		public CPRTType projectionType=CPRTType.SimpleOversampling;
		/// <summary>
		/// Camera vertical or horizontal field of view (see fieldOfViewSetting), in degrees. This parameter will override Camera.fieldOfView.
		/// </summary>
		[Range(1.0f,170.0f)]
		public float fieldOfView=80.0f;
		/// <summary>
		/// Define which camera FOV setting will be set.
		/// </summary>
		public FOVSettingType fieldOfViewSetting=FOVSettingType.Vertical;
		/// <summary>
		/// Power of the projection deformation.
		/// </summary>
		[Range(0.0f,1.0f)]
		public float intensity=1.0f;

		/// <summary>
		/// Does the AdaptivePannini work on automatic settings ?
		/// </summary>
		public bool isAdaptiveAutomatic=false;
		/// <summary>
		/// When projectionType is on AdaptivePannini, define the smoothness of the perspective adaptation.
		/// </summary>
		[Range(1.0f,16.0f)]
		public float adaptivePower=3.8f;
		/// <summary>
		/// When projectionType is on AdaptivePannini, define the perspective's artifact tolerance : 
		/// set 1 if no artefact are visible, else bring it closer to 0.
		/// </summary>
		[Range(0.0f,1.0f)]
		public float adaptiveTolerance=0.76f;
		/// <summary>
		/// When projectionType is on AdaptivePannini, define what is the direction of the projection "up" 
		/// (Orthogonal to the desired horizon).
		/// </summary>
		public Vector3 adaptiveReferenceUp=new Vector3(0.0f,1.0f,0.0f);

		/// <summary>
		/// When projectionType is on AdaptivePannini, define the amout of x/y bias per z unit.
		/// Typical settings : 
		///  - Isometric : {0 ; 0.1709} with camera rotation at {45 ; 45 ; 0}
		///  - Top-Down : {0 ; 1} with camera rotation at {90 ; 0 ; 0}
		///  - Cavalier : {0.707106 ; -0.707106} with camera rotation at {0 ; 0 ; 0}
		///  - Military : {0 ; 1} with camera rotation at {90 ; 45 ; 0}
		/// </summary>
		public Vector2 obliqueBias=new Vector2(0,0);
		/// <summary>
		/// When projectionType is on AdaptivePannini, define the distance where the bias is 0.
		/// If z is inferior to zeroOblique, the opposite of the bias is used per unit.
		/// </summary>
		public float obliqueZeroDistance=0.0f;

		/// <summary>
		/// When projectionType is on PseudoOrthographic, defines the distance of the perspective from the camera.
		/// The higher it is, the closer the perspective is to an orthographic projection.
		/// </summary>
		[Range(0.1f,5000.0f)]
		public float perspectiveOffset=2000.0f;
		/// <summary>
		/// When projectionType is on PseudoOrthographic, replaces the Camera.orthographicSize parameter.
		/// This parameter exists because when the camera is on perspective mode, orthographicSize isn't visible
		/// in the editor.
		/// </summary>
		public float orthographicSize=100.0f;

		/// <summary>
		/// Rendering size factor. useful for making SSAA. Non-linear projections also need a bigger rendering precision.
		/// Mustn't be greater than MaxRenderSizeFactor.
		/// Can be autoadjusted with MinPixelSampleCount. This value may be clamped if the render size cannot be supported.
		/// </summary>
		[Range(MinRenderSizeFactor,MaxRenderSizeFactor)]
		public float renderSizeFactor=1.0f;
		[Obsolete("oversamplingFactor attribute is now deprecated, use renderSizeFactor instead.")]
		public float oversamplingFactor { get { return renderSizeFactor; } set { renderSizeFactor=value; } }

		/// <summary>
		/// Precision of the rendering mesh, can be autoadjusted with AutoProjectionPrecision().
		/// </summary>
		[Range(MinProjectionPrecision,MaxProjectionPrecision)]
		public int projectionPrecision=32;

		/// <summary>
		/// The render target you want to draw on : a RenderTexture or null if you want to draw on the screen.
		/// You MUST use this parameter instead of the Camera.targetTexture.
		/// </summary>
		public RenderTexture targetTexture=null;
		/// <summary>
		/// Viewport's drawing rectangle in normalized coordinates.
		/// When the effect is activated, use this parameter instead of the native camera viewport-rect parameter.
		/// </summary>
		public Rect viewportRect=new Rect(0,0,1,1);

		/// <summary>
		/// Filter mode used for drawing oversampled (or downsampled) rendered image.
		/// </summary>
		public CPRTFilterMode filterMode=CPRTFilterMode.SimpleBilinear;
		/// <summary>
		/// Sharpening of the filter, used on whole screen when filterMode is on UniformBilinear, or on distorded zones when
		/// filterMode is on DistordedBilinear or DistordedBilinearOptimized.
		/// </summary>
		[Range(0.66666667f,2.0f)]
		public float filterSharpen=1.414f;//1.122f;

		/// <summary>
		/// Depth rendering precision
		/// </summary>
		public RenderTextureDepthBitCount renderTextureDepthBits=RenderTextureDepthBitCount.DepthStencil24;

		/// <summary>
		/// This option draw the projection's mesh wireframe
		/// </summary>
		public bool projectionWireframe=false;

		/// <summary>
		/// Screenshot rendering size factor. Useful for making high quality screenshots with SSAA (should not be higher than 2x renderSizeFactor).
		/// </summary>
		[Range(MinRenderSizeFactor,MaxRenderSizeFactor)]
		public float screenshotRenderSizeFactor=1.0f;
		/// <summary>
		/// When screenshotPassCount>1: 
		/// If screenshotPassImproveSSAA is false, screenshotPassCount is the number of successive rendering (It can 
		/// help troubleshooting post-processing effects which accumuate samples over frames)
		/// If screenshotPassImproveSSAA is true, screenshotPassCount also affect SSAA quality by multpliying it by this factor.
		/// </summary>
		public ScreenshotPassCount screenshotPassCount=ScreenshotPassCount.Normal;
		/// <summary>
		/// refer to screenshotPassCount.
		/// </summary>
		public bool screenshotPassImproveSSAA=true;
		/// <summary>
		/// Screenshot viewport width.
		/// </summary>
		[Range(16,MaxRenderTextureSize)]
		public int screenshotWidth=1920;
		/// <summary>
		/// Screenshot viewport height.
		/// </summary>
		[Range(16,MaxRenderTextureSize)]
		public int screenshotHeight=1080;



		public Shader shaderOverSampling;
		public Shader shaderStereoShapeProj;
		public Shader shaderUnityFishEye;




		/// <summary>
		/// Returns if the needed rendering resolution is too high, RefreshViewport() must be called to update this value (it's updated on each render).
		/// </summary>
		public bool IsResolutionSupported { get { return isResolutionSupported; } }

		public bool IsInit { get { return cam!=null; } }

		/// <summary>
		/// Factor applied on intensity when projectionType is on AdaptivePannini
		/// </summary>
		public float IntensityFactor
		{
			get
			{
				return GetIntensityFactor(projectionType);
			}
		}

		/// <summary>
		/// Rendering buffer width (with oversampleFactor taken into account)
		/// </summary>
		public int RenderTargetWidth { get { return rtW; } }
		/// <summary>
		/// Rendering buffer height (with oversampleFactor taken into account)
		/// </summary>
		public int RenderTargetHeight { get { return rtH; } }
		/// <summary>
		/// Rendering buffer MSAA Setting
		/// </summary>
		public int RenderTargetMSAA { get { return rtMSAA; } }

		/// <summary>
		/// Viewport rectangle in pixel coordinates
		/// </summary>
		public Rect PixelRect {
			get {
				int w,h;

				ComputeFinalViewportSize(out w,out h);

				return new Rect(viewportRect.x*w,viewportRect.y*h,viewportRect.width*w,viewportRect.height*h);
			}
		}


		public bool IsNonLinearProjection {
			get {
				return
					projectionType==CPRTType.UnityBuiltinFisheye||
					projectionType==CPRTType.Pannini||
					projectionType==CPRTType.AdaptivePannini||
					projectionType==CPRTType.Stereospherical;
			}
		}
		public bool IsOrthographicProjection { 
			get {
				return projectionType==CPRTType.ObliqueOrthographic||projectionType==CPRTType.PseudoOrthographic;
			} 
		}

		/// <summary>
		/// Y field of view, in degrees
		/// </summary>
		public float FieldOfViewY { get { return fieldOfViewY; } }
		/// <summary>
		/// Y field of view, in degrees
		/// </summary>
		public float Aspect { get { return cam.aspect; } }
		/// <summary>
		/// Projection "observer" field of view, in degrees, for debug purpose
		/// </summary>
		public float ObserverFOV { get { return observerFOV; } }

		/// <summary>
		/// Get the approximate higher screen scaling with the current projection, ignore the Render Size Factor settings.
		/// </summary>
		public float ProjectionMaxPixelScaling {
			get {
				switch (projectionType) {
					case CPRTType.Pannini:
					case CPRTType.AdaptivePannini:
					case CPRTType.Stereospherical:
						return GetStereoProjectionCenterScaling(intensity);
					default:
						return 1.0f;
				}
			}
		}
		/// <summary>
		/// Minimal number of samples per pixel in the resulting viewport.
		/// renderSizeFactor accessor that takes into account the current projection distortion.
		/// A value of 1 ensures that the image won't be downsampled/blurry at any place (renderSizeFactor will be >1 for 
		/// non-linear projections, lower the FOV to increase performance).
		/// A value of 4 generate at least SSAAx2 at any places of the screen.
		/// Values higher than 4 aren't supported by the SSAA filter.
		/// </summary>
		public float MinPixelSampleCount {
			set { renderSizeFactor=ProjectionMaxPixelScaling*Mathf.Sqrt(value); }
			get { return CPRTToolkit.Sq(renderSizeFactor/ProjectionMaxPixelScaling); }
		}
		/// <summary>
		/// Minimal number of samples per pixel in the resulting viewport.
		/// screenshotRenderSizeFactor accessor that takes into account the current projection distortion.
		/// A value of 1 ensures that the image won't be downsampled/blurry at any place (renderSizeFactor will be >1 for 
		/// non-linear projections, lower the FOV to increase performance).
		/// A value of 4 generate at least SSAAx2 at any places of the screen.
		/// Values higher than 4 aren't supported by the SSAA filter.
		/// </summary>
		public float ScreenshotMinPixelSampleCount {
			set { screenshotRenderSizeFactor=ProjectionMaxPixelScaling*Mathf.Sqrt(value); }
			get { return CPRTToolkit.Sq(screenshotRenderSizeFactor/ProjectionMaxPixelScaling); }
		}

		/// <summary>
		/// Get the last rendering time step, returns 1/60 by default
		/// </summary>
		public float RenderingTimeStep { get { return renderingTimeStep; } }

		/// <summary>
		/// A 2D bias added to the CPRT projection, from -1.0f to 1.0f to remain in the same pixel.
		/// Can be useful for compatibility with Post-FX plugins that needs to displace the projection.
		/// </summary>
		public Vector2 ProjectionBias {
			get { return projectionBias; }
			set { projectionBias=value; }
		}





		private bool isSupported=false;

		private Shader cprtShader=null;
		private Material cprtMaterial=null;
		private RenderTexture cprtTex=null;
		private Camera cam;
		private int rtBaseW=0,rtBaseH=0,rtW=0,rtH=0,rtMSAA;
		private int lastCamPixelW=1,lastCamPixelH=1;
		private float widthAperture;//in radians
		private float fieldOfViewY=90.0f;
		private float observerFOV=1.0f;//do not use except for debug display
		private const float observerFarClipPlane=64.0f;
		private bool isResolutionSupported=true;
		private DateTime lastPreCull;
		private float renderingTimeStep;
		private Vector2 projectionBias;//biais dans un pixel, entre -1.0f et 1.0f

		private float prelockNearPlane=0.0f,prelockFarPlane=0.0f;//si non 0, on a influé artificiellement le near / far plane
		private Transform prelockTransform;
		private bool CamSettingsLocked { get { return prelockNearPlane!=0.0f; } }

		private Mesh projMesh;

		private float AdaptivePanniniAngle { get { return Mathf.Asin(cam.transform.forward.normalized.y); } }

		private CPRTFrustumUpdater frustumUpdater=null;


#if CPRT_LOG_FRAMERATE
		private DateTime timestamp=DateTime.Now;
		private int framecount=0;
#endif

		bool IsDestinationRenderTexture {
			get {
				float sampleratio=cprtTex!=null?rtW/(float)rtBaseW:1.0f;

				return !(sampleratio==1.0f&&projectionType==CPRTType.SimpleOversampling&&viewportRect.x==0.0f&&viewportRect.y==0.0f);
			}
		}



		/// <summary>
		/// Called by unity at start of the game
		/// </summary>
		public void Start() {
			lastPreCull=DateTime.Now;
			renderingTimeStep=1.0f/60.0f;
			projectionBias=new Vector2(0,0);

			RefreshEffect();
			MultipleCameraQuickFixRefresh();
		}
		/// <summary>
		/// Called by unity when a parameter is changed on the camera in the editor
		/// </summary>
		public void OnValidate() {
			if (isActiveAndEnabled) {
				OnEnable();
			} else {//v1.6, if we select it in the editor
				RefreshEffect();
				RefreshViewport();
			}
		}
		public void OnEnable() {
			lastPreCull=DateTime.Now;
			renderingTimeStep=1.0f/60.0f;

			RefreshEffect();
			MultipleCameraQuickFixRefresh();
			MultipleCameraQuickFix();
			ResetCameraSettings();//v1.6
			RefreshViewport();
		}
		public void OnDisable() {
			ResetCameraSettings();//v1.6
		}

		/// <summary>
		/// Refresh shader and AntiAliasing parameters and resources. 
		/// Must be called if AntiAliasing settings or if the projection type has been changed.
		/// Called by Unity's callback OnValidate() and by Start().
		/// </summary>
		public void RefreshEffect() {
			cam=GetComponent<Camera>();

			frustumUpdater=GetComponent<CPRTFrustumUpdater>();
			if (frustumUpdater) frustumUpdater.Init(this);

			if (RefreshShader()) {
				RefreshMSAA();
			}
		}
		private bool RefreshShader() {
			rtW=rtH=0;
			if (projMesh!=null) projMesh.Clear();

			cprtShader=GetShader(projectionType);

			if (cprtShader==null) {
				Debug.LogError("CPRT Shader not found or not recognized for "+projectionType.ToString()+" projection.");
				return false;
			} else {
				return CheckResources();
			}
		}
		public Shader GetShader(CPRTType proj_type) {
			switch (proj_type) {
				case CPRTType.SimpleOversampling:
				case CPRTType.ObliqueOrthographic:
				case CPRTType.PseudoOrthographic:
					return shaderOverSampling;
				case CPRTType.UnityBuiltinFisheye:
					return shaderUnityFishEye;
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini:
				case CPRTType.Stereospherical:
					return shaderStereoShapeProj;
				default:
					Debug.Log("Unknown CPRT Projection Type");
					return null;
			}
		}
		private void RefreshMSAA() {
			if (cam!=null&&targetTexture!=null) {
				rtMSAA=targetTexture.antiAliasing;
			} else if (QualitySettings.antiAliasing>1) {
				rtMSAA=QualitySettings.antiAliasing;
			} else {
				rtMSAA=1;
			}
		}

		private void LockTemporary() {
#if UNITY_3||UNITY_4||UNITY_5_0||UNITY_5_1||UNITY_5_2||UNITY_5_3||UNITY_5_4
			bool ishdr=cam.hdr;
#else
			bool ishdr=cam.allowHDR;
#endif
			RenderTextureReadWrite colormode;
			int depthbit,aasetting;
			RenderTextureFormat format=ishdr? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;

			UnlockTemporary();

			if (targetTexture!=null) {
				//targetTexture.DiscardContents(true,true);
				depthbit=targetTexture.depth;
				format=targetTexture.format;
				colormode=targetTexture.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
				aasetting=targetTexture.antiAliasing;
			} else {
				depthbit=(int)renderTextureDepthBits;
				format=ishdr ? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;
				colormode=RenderTextureReadWrite.Default;
				aasetting=rtMSAA;
			}

#if UNITY_3||UNITY_4||UNITY_5
			if (rtW>0&&rtH>0) cprtTex=RenderTexture.GetTemporary(rtW+multipleCameraQuickFixId,rtH,depthbit,format,colormode,aasetting);
#else
			if (rtW>0&&rtH>0) {
				RenderTextureDescriptor desc=new RenderTextureDescriptor(rtW+multipleCameraQuickFixId,rtH,format,depthbit);
				if (colormode==RenderTextureReadWrite.sRGB) desc.sRGB=true;
				desc.msaaSamples=aasetting;
				desc.useMipMap=desc.autoGenerateMips=false;

				if (cprtTex==null||!cprtTex.IsCreated()||!CPRTToolkit.RTDescEquals(desc,cprtTex.descriptor)) {
					UnlockTemporary();
					cprtTex=RenderTexture.GetTemporary(desc);
				}
			}
#endif
			if (cprtTex!=null&&cprtTex.IsCreated()) {
				isResolutionSupported=true;
			} else if (rtW>0&&rtH>0) {
				isResolutionSupported=cprtTex!=null ? cprtTex.Create() : false;
				if (isResolutionSupported) isResolutionSupported=cprtTex.IsCreated();
				if (!isResolutionSupported) {
					Debug.LogError("CPRT Error : Render Texture creation not supported, try a lower resolution than "+rtW+"x"+rtH+" ?");
					RenderTexture.ReleaseTemporary(cprtTex);
					cprtTex=null;
				}
			}
			if (cprtTex!=null) cprtTex.name="CPRT_RenderTexture for "+name+" "+rtW+"x"+rtH+" (fix="+multipleCameraQuickFixId+")";

			cam.targetTexture=cprtTex;

#if CPRT_LOG_BUFFERS
			Debug.Log(name+" Rendering into "+cprtTex+" to finish in "+(targetTexture?targetTexture.ToString():"final screen"));
#endif
		}
		private bool UnlockTemporary() {
			bool release=false;

			MultipleCameraQuickFixReset();
			//if (targetTexture!=null) targetTexture.DiscardContents();

			if (cprtTex!=null) {
				RenderTexture.ReleaseTemporary(cprtTex);
				cprtTex=null;
				release=true;
			}

			if (cam.targetTexture!=null&&cam.targetTexture.name.Contains("CPRTTemp")) {
				RenderTexture.ReleaseTemporary(cam.targetTexture);
			}
			cam.targetTexture=null;

			return release;
		}


		
		/// <summary>
		/// Called by unity to prepare the rendering resources
		/// </summary>
		public void OnPreCull() {
			if (isActiveAndEnabled&&(frustumUpdater==null||(!frustumUpdater.isActiveAndEnabled)||frustumUpdater.CanCallPrecull)) {
				renderingTimeStep=(float)(DateTime.Now-lastPreCull).TotalSeconds;
				lastPreCull=DateTime.Now;

#if CPRT_LOG_FRAMERATE
				if ((DateTime.Now-timestamp).TotalMilliseconds>1000.0f) {
					Debug.Log("FPS = "+(framecount+1));
					framecount=0;
					timestamp=DateTime.Now;
				} else {
					framecount++;
				}
#endif
				//for an unknown reason, OnPreRender() doesn't work with deffered rendering and multiple cameras
				//it sommons this error which is not documented : "GetRenderManager().GetCurrentCameraPtr() == m_Context->m_Camera"
				//but everything works with OnPreCull
				RefreshViewport();
			}
		}
		/// <summary>
		/// Refresh the necessary resources for the perspective. 
		/// Must be called if there is any changes in Camera.RenderTarget/viewport's size/AA/HDR settings (basically after RefreshEffect()).
		/// Called by Unity's callback OnPreCull().
		/// </summary>
		public void RefreshViewport() {
			if (CheckResources()) {
				CheckViewportSize();

				if (cam.isActiveAndEnabled&&cam.rect.width>0&&cam.rect.height>0) {
					LockTemporary();
				}
			}
		}
		void ResetCameraSettings() {
			if (cam!=null) {
				if (CamSettingsLocked) {
					cam.nearClipPlane=prelockNearPlane;
					cam.farClipPlane=prelockFarPlane;
					prelockNearPlane=prelockFarPlane=0.0f;
				}

				cam.ResetAspect();
				cam.ResetWorldToCameraMatrix();
				cam.ResetProjectionMatrix();
				cam.ResetCullingMatrix();
			}
		}
		/// <summary>
		/// Checks if renderSizeFactor can be supported, and change its value if not.
		/// Automatically called before each renderings.
		/// </summary>
		public void CheckViewportSize() {
			Rect rekt=PixelRect;
			int neww,newh,newrtw,newrth;
			bool changevp=false;


			neww=(int)(rekt.width);
			newh=(int)(rekt.height);
			newrtw=(int)(neww*renderSizeFactor);
			newrth=(int)(newh*renderSizeFactor);

			if (neww!=rtBaseW||newh!=rtBaseH||newrtw!=rtW||newrth!=rtH) {
				rtBaseW=neww;
				rtBaseH=newh;

				if (neww>=newh) {
					if (newrtw>MaxRenderTextureSize) {
						renderSizeFactor=((float)MaxRenderTextureSize+0.5f)/neww;

						newrtw=MaxRenderTextureSize;
						newrth=(int)((newh/(float)neww)*newrtw);
					}
				} else {
					if (newrth>MaxRenderTextureSize) {
						renderSizeFactor*=((float)MaxRenderTextureSize+0.5f)/newh;

						newrth=MaxRenderTextureSize;
						newrtw=(int)((neww/(float)newh)*newrth);
					}
				}
				rtW=newrtw;
				rtH=newrth;

				changevp=true;
			}


			{
				float aspect=((float)rtW)/rtH;
				float lastfov=fieldOfViewY;

				cam.rect=new Rect(0.0f,0.0f,1.0f,1.0f);
				ComputeFieldOfViewY(aspect);
				cam.fieldOfView=fieldOfViewY;
				if (fieldOfViewY!=lastfov) {
					changevp=true;
				}

				if (rtW>0&&rtH>0&&cam.nearClipPlane>0.0f&&cam.farClipPlane>cam.nearClipPlane) {//v1.6
					switch (projectionType) {
						case CPRTType.ObliqueOrthographic: { //if (projectionType==CPRTType.ObliqueOrthographic&&cam.orthographic) {
								if (cam.orthographicSize!=0.0f) {//v1.6
									Matrix4x4 mat;
									float factor=1.0f/cam.orthographicSize;

									mat=cam.projectionMatrix;

									mat.m02=obliqueBias.x*factor/Aspect;
									mat.m12=obliqueBias.y*factor;
									mat.m03=obliqueZeroDistance*mat.m02;
									mat.m13=obliqueZeroDistance*mat.m12;

									cam.projectionMatrix=mat;
								}
							}
							break;
						case CPRTType.PseudoOrthographic: {
								Matrix4x4 mat;

								if (orthographicSize!=0.0f) {
									if (!CamSettingsLocked) {
										prelockFarPlane=cam.farClipPlane;
										prelockNearPlane=cam.nearClipPlane;
										cam.farClipPlane=(prelockFarPlane+perspectiveOffset)*CPRTPseudoOrthographicsSettings.ShadowDistanceFactor;
										//near computed later in OnPreRender because of a bug
									}

									mat=Matrix4x4.Inverse(Matrix4x4.TRS(cam.transform.position-transform.forward.normalized*perspectiveOffset,cam.transform.rotation,new Vector3(1,1,-1)));
									cam.worldToCameraMatrix=mat;
							
									mat=Matrix4x4.Perspective(fieldOfViewY,((float)rtW)/rtH,cam.nearClipPlane,cam.farClipPlane);
									cam.projectionMatrix=mat;

									cam.cullingMatrix=cam.projectionMatrix*cam.worldToCameraMatrix;
								}
							}
							break;
						default://else if (projectionType!=CPRTType.ObliqueOrthographic&&!cam.orthographic) {
							cam.projectionMatrix=Matrix4x4.Perspective(fieldOfViewY,aspect,cam.nearClipPlane,cam.farClipPlane);
							cam.aspect=aspect;

							break;
					}

					if (projectionBias.x!=0.0f||projectionBias.y!=0.0f) {
						Matrix4x4 mat=cam.projectionMatrix;
						float yaw=      projectionBias.x*(0.5f/rtW)*CPRTToolkit.GetFovX(cam);
						float pitch=    projectionBias.y*(0.5f/rtH)*CPRTToolkit.GetFovY(cam);
						//Vector3 pos=new Vector3(-0.5f,0.5f,0.0f);
						Vector3 pos=new Vector3(0.0f,0.0f,0.0f);

						mat=(Matrix4x4.Translate(-pos)*Matrix4x4.LookAt(Vector3.zero+pos,CPRTToolkit.SpherePoint(pitch,yaw)+pos,Vector3.up))*mat;

						cam.projectionMatrix=mat;
					}
				}
			}

			if (changevp) UpdateProjectionMeshes();
		}
		public void ComputeFinalViewportSize(out int w,out int h) {
			if (targetTexture!=null) {
				w=targetTexture.width;
				h=targetTexture.height;
			} else {
				if (cprtTex!=null) {
					w=Screen.width;
					h=Screen.height;
				} else {
					w=cam.pixelWidth;
					h=cam.pixelHeight;
				}
			}
		}
		public void ComputeFieldOfViewY(float aspect) {
			if (projectionType!=CPRTType.PseudoOrthographic) {
				switch (fieldOfViewSetting) {
					case FOVSettingType.Vertical:
						fieldOfViewY=fieldOfView;
						break;
					case FOVSettingType.Horizontal:
						fieldOfViewY=CPRTToolkit.GetFovY(fieldOfView*Mathf.Deg2Rad,aspect)*Mathf.Rad2Deg;
						break;
					case FOVSettingType.Diagonal:
						fieldOfViewY=CPRTToolkit.GetFovYFromDiagAspect(fieldOfView*Mathf.Deg2Rad,aspect)*Mathf.Rad2Deg;
						break;
				}
			} else {//if (projectionType==CPRTType.PseudoOrthographic)
				fieldOfViewY=Mathf.Atan(orthographicSize/(perspectiveOffset+(CamSettingsLocked ? prelockNearPlane+prelockFarPlane : cam.nearClipPlane+cam.farClipPlane)*0.5f))*Mathf.Rad2Deg*2.0f;
			}
		}


		public void OnPreRender() {
			if (projectionType==CPRTType.PseudoOrthographic&&CamSettingsLocked) {
				cam.nearClipPlane+=perspectiveOffset;
				cam.projectionMatrix=Matrix4x4.Perspective(fieldOfViewY,((float)rtW)/rtH,cam.nearClipPlane,cam.farClipPlane);
			}
		}



		/// <summary>
		/// Return true if the effect is compatible with the system.
		/// Refresh the post-processing shader if needed.
		/// </summary>
		public bool CheckResources() {
			if (projectionType!=(CPRTType)0) {
				{//check if the system can support the shaders
					isSupported=true;
#if UNITY_3||UNITY_4||UNITY_5_0||UNITY_5_1||UNITY_5_2||UNITY_5_3||UNITY_5_4
					if (!SystemInfo.supportsRenderTextures) {
						Debug.LogError("CPRT : current system doesn't supports RenderTextures.");
						isSupported=false;
					}
#endif
					if (!SystemInfo.supportsImageEffects) {
						Debug.LogError("CPRT : current system doesn't supports ImageEffects.");
						isSupported=false;
					}
				}

				if (isSupported) {//control shader and create material 
					if (cprtShader==null) {
						Debug.LogError("Missing CPRT shader");
						isSupported=false;
					} else {
						if (!cprtShader.isSupported) {
							Debug.LogError("CPRT shader isn't supported on this platform.");
							isSupported=false;
						} else {
							if (cprtMaterial==null) {
								cprtMaterial=new Material(cprtShader);
								cprtMaterial.hideFlags=HideFlags.DontSave;
							} else {
								cprtMaterial.shader=cprtShader;
							}
						}
					}
				}

				if (!isSupported) {
					Debug.LogWarning("CPRT has been disabled as it's not supported on the current platform.");
					//enabled=false;
				}
				return isSupported;
			} else {
				isSupported=false;
				return false;
			}
		}

		private void UpdateProjectionMeshes() {
			widthAperture=CPRTToolkit.GetFovX(fieldOfViewY*Mathf.Deg2Rad,Aspect);//CPRTToolkit.GetFovX(cam);

			if (projMesh==null) {
				projMesh=new Mesh();
			} else {
				//projMesh.Clear(true);//not needed as long as SetTriangles replace them all
			}

			switch (projectionType) {
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini: {
					const float topdown=32.0f;
					float maxaperture=projectionType==CPRTType.AdaptivePannini?widthAperture*1.5f:widthAperture;
					float startangle=-maxaperture*0.5f;
					float anglefactor=maxaperture/projectionPrecision;
					int vertcount=(projectionPrecision+1)*2;
					int idxcount=(projectionPrecision)*6,quadid;
					List<Vector3> verts=new List<Vector3>(vertcount);
					List<int> tris=new List<int>(idxcount);
					bool addborders=true;


					if (addborders) {
						Vector3[] miniverts=new Vector3[] {
								new Vector3(-1,-topdown,-1),
								new Vector3(1,-topdown,-1),
								new Vector3(-1,-topdown,2),
								new Vector3(1,-topdown,2),
								new Vector3(-1,topdown,-1),
								new Vector3(1,topdown,-1),
								new Vector3(-1,topdown,2),
								new Vector3(1,topdown,2)};

						verts.AddRange(miniverts);
						tris.AddRange(new int[] {
								0,1,2 , 1,2,3,
								4,5,6 , 5,6,7,
								0,1,4 , 4,1,5,
								2,0,4 , 2,4,6,
								1,3,5 , 5,3,7,
								});
						quadid=miniverts.Length;
					} else {
						quadid=0;
					}
					for (int i = 0;i<=projectionPrecision;i++) {
						float angle=startangle+i*anglefactor;

						verts.Add(new Vector3(Mathf.Sin(angle),topdown,Mathf.Cos(angle)));
						verts.Add(new Vector3(Mathf.Sin(angle),-topdown,Mathf.Cos(angle)));

						if (i<projectionPrecision) {
							tris.AddRange(new int[6] { quadid,quadid+1,quadid+2,quadid+2,quadid+1,quadid+3 });
							quadid+=2;
						}
					}

					projMesh.SetVertices(verts);
					projMesh.SetTriangles(tris,0,false);
				}
				break;
				case CPRTType.Stereospherical: {
					int vertcount=CPRTToolkit.Sq(projectionPrecision+1);
					int idxcount=CPRTToolkit.Sq(projectionPrecision)*6;
					int idxrow=projectionPrecision+1;
					Vector2 startangle=new Vector2(-widthAperture*0.5f,-fieldOfViewY*Mathf.Deg2Rad*0.5f);
					Vector2 anglefactor=new Vector2(widthAperture/projectionPrecision,fieldOfViewY*Mathf.Deg2Rad/projectionPrecision);
					List<Vector3> verts=new List<Vector3>(vertcount);
					List<int> tris=new List<int>(idxcount);


					for (int y = 0;y<=projectionPrecision;y++) {
						for (int x = 0;x<=projectionPrecision;x++) {
							verts.Add(CPRTToolkit.SpherePoint(startangle.y+y*anglefactor.y,startangle.x+x*anglefactor.x));

							if (x<projectionPrecision&&y<projectionPrecision) {
								tris.AddRange(new int[6] { x+y*idxrow,x+1+y*idxrow,x+(y+1)*idxrow,x+(y+1)*idxrow,x+1+y*idxrow,x+1+(y+1)*idxrow });
							}
						}
					}


					projMesh.SetVertices(verts);
					projMesh.SetTriangles(tris,0,false);
				}
				break;
			}
		}


		/// <summary>
		/// Called by unity when drawing the frame (post-processing)
		/// </summary>
		public void OnRenderImage(RenderTexture source,RenderTexture destination) {
			float sampleratio=cprtTex!=null?rtW/(float)rtBaseW:1.0f;
			Rect rekt=PixelRect;

#if CPRT_LOG_BUFFERS
			Debug.Log("OnRenderImage: "+name+" : source="+source+" destination="+destination+" finalRT="+targetTexture);
#endif


			if ((isActiveAndEnabled&&isSupported)&&IsDestinationRenderTexture) {
				int shaderpass=0;
				float intensityfactor=IntensityFactor;

				cprtMaterial.SetVector("Intensity",new Vector4(intensityfactor,intensityfactor));
				cprtMaterial.SetFloat("SampleRatio",sampleratio);
				cprtMaterial.SetVector("SampleSize",new Vector2(1.0f/(float)rtW,1.0f/(float)rtH));
				cprtMaterial.SetFloat("SampleAspect",rtW/(float)rtH);
				cprtMaterial.SetFloat("SubSampleDistance",1.0f/(filterSharpen));


				GL.PushMatrix();


				//if (showPixelMask) {
				//	Graphics.SetRenderTarget(source);
				//	GL.Viewport(new Rect(0,0,source.width,source.height));
				//	//GL.sRGBWrite=
				//
				//	DrawScreen(1);
				//}

				//if (sampleratio==1.0f&&projectionType==CPRTType.SimpleOversampling) {
				//	source.filterMode=FilterMode.Point;
				//	shaderpass=0;
				//} else {
					switch (filterMode) {
						case CPRTFilterMode.NearestNeightboor:
							source.filterMode=FilterMode.Point;
							shaderpass=0;
							break;
						case CPRTFilterMode.SimpleBilinear:
							source.filterMode=FilterMode.Bilinear;
							shaderpass=0;
							break;
						case CPRTFilterMode.DistordedBilinear:
							source.filterMode=FilterMode.Point;
							shaderpass=1;
							break;
						case CPRTFilterMode.DistordedBilinearOptimized:
							source.filterMode=FilterMode.Point;
							shaderpass=2;
							break;
						case CPRTFilterMode.UniformBilinear:
							source.filterMode=FilterMode.Bilinear;
							shaderpass=sampleratio==2.0f&&(!IsNonLinearProjection) ? 0 : 3;//optimisation : UniformBilinear doesn't change anything on SSAAx2 in SimpleOversampling
							break;
					}
				//}

				source.SetGlobalShaderProperty("_MainTex");
				Graphics.SetRenderTarget(targetTexture);
				GL.Viewport(rekt);
				//GL.sRGBWrite=

				DrawScreen(shaderpass);


				GL.PopMatrix();
			} else {
				Graphics.Blit(source,targetTexture);
			}

			//réactive le contrôle des réglage de camera de unity
			ResetCameraSettings();

			UnlockTemporary();
		}



		private void DrawScreen(int pass) {
			bool drawintexture=targetTexture!=null;

			switch (projectionType) {
				case CPRTType.SimpleOversampling:
				case CPRTType.ObliqueOrthographic:
				case CPRTType.PseudoOrthographic:
				case CPRTType.UnityBuiltinFisheye: {
					cprtMaterial.SetPass(pass);
					GL.LoadOrtho();
					GL.Begin(GL.QUADS);
					GL.TexCoord2(0.0f,drawintexture ? 1.0f : 0.0f); GL.Vertex3(0.0f,0.0f,0.1f);
					GL.TexCoord2(0.0f,drawintexture ? 0.0f : 1.0f); GL.Vertex3(0.0f,1.0f,0.1f);
					GL.TexCoord2(1.0f,drawintexture ? 0.0f : 1.0f); GL.Vertex3(1.0f,1.0f,0.1f);
					GL.TexCoord2(1.0f,drawintexture ? 1.0f : 0.0f); GL.Vertex3(1.0f,0.0f,0.1f);
					GL.End();
				}
				break;
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini: {
					Matrix4x4 observervpmatrix;
					Matrix4x4 paintervpmatrix=Matrix4x4.Perspective(fieldOfViewY,Aspect,cam.nearClipPlane,cam.farClipPlane);//proj
					float intensityfactor=IntensityFactor;
					float offz=intensityfactor*intensity;


					if (projectionType==CPRTType.AdaptivePannini) {
						Vector3 obsat,paintat;
						float angle=AdaptivePanniniAngle;
						float centerscaling=GetStereoProjectionCenterScaling(intensityfactor*intensity);

						paintat=new Vector3(0.0f,Mathf.Sin(angle),Mathf.Cos(angle));//painter looks through angle
						paintervpmatrix=paintervpmatrix*CPRTToolkit.LookAtRH(Vector3.zero,paintat,Vector3.up);//proj * view

						obsat=new Vector3(0.0f,Mathf.Abs(angle)<Mathf.PI*0.5f ? Mathf.Tan(angle) : Mathf.Sign(angle)*16384.0f,1.0f);//observer looks painter center
						observervpmatrix=CPRTToolkit.LookAtRH(new Vector3(0,0,-offz),obsat,Vector3.up);//view (observerviewmatrix)
						observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeAdaptivePaniniProjFOV(widthAperture,Aspect,-offz,intensityfactor,centerscaling);
						observervpmatrix=GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane),drawintexture)*observervpmatrix;//proj * view
					} else {
						paintervpmatrix=paintervpmatrix*CPRTToolkit.LookAtRH();//proj * view

						observervpmatrix=CPRTToolkit.LookAtRH(new Vector3(0,0,-offz),Vector3.forward,Vector3.up);//view
						observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputePaniniProjFOV(widthAperture,Aspect,-offz);
						observervpmatrix=GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane),drawintexture)*observervpmatrix;//proj * view
					}

					cprtMaterial.SetMatrix("ObserverViewProj",observervpmatrix);
					cprtMaterial.SetMatrix("PainterViewProj",paintervpmatrix);

					//GL.LoadProjectionMatrix(observermatrix);
					GL.Clear(true,true,new Color(0.0f,0.4f,0.333f));
					if (projectionWireframe) GL.wireframe=true;
					cprtMaterial.SetPass(pass);
					GL.modelview=Matrix4x4.identity;
					Graphics.DrawMeshNow(projMesh,Matrix4x4.identity,0);//observerviewmatrix,0);

					if (projectionWireframe) {
						GL.LoadOrtho();
						GL.Begin(GL.LINES);
						GL.Vertex3(-1.0f,-1.0f,1.0f); GL.Vertex3(1.0f,1.0f,1.0f); GL.Vertex3(-1.0f,1.0f,1.0f); GL.Vertex3(1.0f,-1.0f,1.0f);
						GL.Vertex3(-1.0f,-1.0f,0.0f); GL.Vertex3(1.0f,1.0f,0.0f); GL.Vertex3(-1.0f,1.0f,0.0f); GL.Vertex3(1.0f,-1.0f,0.0f);
						GL.End();
						GL.wireframe=false;
					}
				}
				break;
				case CPRTType.Stereospherical: {
					Matrix4x4 observerprojmatrix,observerviewmatrix;
					Matrix4x4 painterprojmatrix=Matrix4x4.Perspective(fieldOfViewY,Aspect,cam.nearClipPlane,cam.farClipPlane);//cam.projectionMatrix;
					float offz=IntensityFactor*intensity;

					observerviewmatrix=CPRTToolkit.LookAtRH(new Vector3(0.0f,0.0f,-offz),Vector3.forward,Vector3.up);

					observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeStereosphericalProjFOV(widthAperture,fieldOfViewY*Mathf.Deg2Rad,Aspect,-offz);
					observerprojmatrix=GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane),drawintexture);
					//orthographic fisheye
					//float orthow=0.5f*Mathf.Tan(0.5f*CPRTToolkit.GetFovX(observerFOV,Aspect));
					//float orthoh=0.5f*Mathf.Tan(0.5f*observerFOV);
					//observerprojmatrix=Matrix4x4.Ortho(orthow,-orthow,orthoh,-orthoh,0.1f,2.0f);

					cprtMaterial.SetMatrix("ObserverViewProj",observerprojmatrix*observerviewmatrix);
					cprtMaterial.SetMatrix("PainterViewProj",painterprojmatrix*CPRTToolkit.LookAtRH());

					//GL.LoadProjectionMatrix(observermatrix);
					GL.Clear(true,true,new Color(0.0f,0.4f,0.333f));
					if (projectionWireframe) GL.wireframe=true;
					cprtMaterial.SetPass(pass);
					GL.modelview=Matrix4x4.identity;
					Graphics.DrawMeshNow(projMesh,observerviewmatrix,0);//,observerviewmatrix,0);
					if (projectionWireframe) GL.wireframe=false;
				}
				break;
			}
		}


		private float GetStereoProjectionCenterScaling(float _intensity) {
			const float delta=0.01f;//10-25px delta
			CPRTType projtype=projectionType==CPRTType.AdaptivePannini?CPRTType.Pannini:projectionType;
			Ray rayproj=ViewportPointToRay(new Vector3(0.5f+delta,0.5f+delta,1.0f),projtype);
			Ray rayprojorigin=ViewportPointToRay(new Vector3(0.5f,0.5f,1.0f),projtype);
			Ray raylinear=cam.ViewportPointToRay(new Vector3(0.5f+delta,0.5f+delta,1.0f));
			Ray rayorigin=cam.ViewportPointToRay(new Vector3(0.5f,0.5f,1.0f));

			return Vector3.Angle(raylinear.direction,rayorigin.direction)/Vector3.Angle(rayproj.direction,rayprojorigin.direction);
		}
		private float GetIntensityFactor(CPRTType proj_type) {
			if (proj_type==CPRTType.AdaptivePannini) {
				float miny=(1.0f-adaptiveTolerance)*Mathf.Cos(Mathf.Deg2Rad*0.5f*(180.0f-fieldOfViewY));
				float i=(Mathf.Abs(cam.transform.up.y)-miny)/(1.0f-miny);

				if (isAdaptiveAutomatic) {
					i=i<=0.0f ? 0.0f : (0.5f-0.5f*Mathf.Cos(2.0f*Mathf.Asin(i)));
				} else {
					//finalIntensity=finalIntensity<=0.0f ? 0.0f : (intensity*Mathf.Pow(finalIntensity,adaptivePower));
					i=i<=0.0f ? 0.0f : (Mathf.Pow(i,adaptivePower));
				}

				return i;
			} else {
				return 1.0f;
			}
		}


		/// <summary>
		/// Select a value of renderSizeFactor that avoid pixelization (all rendered pixels will never 
		/// be bigger than one screen pixel).
		/// <paramref name="beautify_power">A value of 1.06f can help giving a sharper result at the cost of performance.</paramref>
		/// </summary>
		public void AutoRenderSizeFactor(float beautify_power=1.0f) {
			switch (projectionType) {
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini:
				case CPRTType.Stereospherical: {
					renderSizeFactor=Mathf.Pow(ProjectionMaxPixelScaling,beautify_power);//power is used to make it a bit more beautiful
					if (renderSizeFactor>MaxRenderSizeFactor) {
						renderSizeFactor=MaxRenderSizeFactor;
						Debug.LogWarning("Projection scale the viewport too much, some pixel will be downsampled.");
					} else if (renderSizeFactor<1.0f) {
						Debug.LogWarning("Abnormal projection pixel scaling (WTF???).");
					}
				}
				break;
				case CPRTType.SimpleOversampling:
				case CPRTType.ObliqueOrthographic:
				case CPRTType.PseudoOrthographic:
					renderSizeFactor=1.0f;
					break;
				default:
					Debug.LogError("Projection scale not computable.");
					break;
			}
		}
		[Obsolete("AutoOversamplingFactor is now deprecated, use MinPixelSampleCount or AutoRenderSizeFactor instead.")]
		public void AutoOversamplingFactor() { AutoRenderSizeFactor(); }
		/// <summary>
		/// Select a value of projectionPrecision that minimize projection distortion.
		/// </summary>
		public void AutoProjectionPrecision() {
			switch (projectionType) {
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini:
					projectionPrecision=CPRTToolkit.Clamp((int)(64*Mathf.Sqrt(1.2f*(ProjectionMaxPixelScaling-1.0f))),MinProjectionPrecision,32);//(int)(32*ProjectionMaxPixelScaling);//64 default
					break;
				case CPRTType.Stereospherical:
					projectionPrecision=CPRTToolkit.Clamp((int)(64*Mathf.Sqrt(1.0f*(ProjectionMaxPixelScaling-1.0f))),MinProjectionPrecision,32);//(int)(32*ProjectionMaxPixelScaling);//64 default
					break;
			}
		}



		/// <summary>
		/// Manually call for rendering
		/// </summary>
		public void Render() {
			RefreshViewport();
			cam.Render();
		}
		/// <summary>
		/// Take a screen-independant screenshot and save it to a file.
		/// Uses screenshotRenderSizeFactor, screenshotWidth and screenshotHeight.
		/// To enable better screenshot quality settings or use rendering accumulation, refer to screenshotPassImproveSSAA and 
		/// screenshotPassCount attributes.
		/// </summary>
		public void OffsreenScreenshot(string path) {
			RenderTexture screenshotrt=new RenderTexture(screenshotWidth,screenshotHeight,(int)renderTextureDepthBits,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default) {
				name="CPRTScreenshot",
				autoGenerateMips=false
			};

			if (!screenshotrt.Create()) {
				Debug.LogError("Unable to create screenshot's render texture (too high resolution ?). Final resolution was "+screenshotrt.width+"x"+screenshotrt.height);
			} else {
				Texture2D screenshot=new Texture2D(screenshotWidth,screenshotHeight,TextureFormat.RGB24,false);
				RenderTexture lastrt=RenderTexture.active;

				if (screenshotPassImproveSSAA&&screenshotPassCount!=ScreenshotPassCount.Normal) {
					byte[] resultdata=null;

					{
						ushort[] sum=null;

						{
							Texture2D subscreenshot=new Texture2D(screenshotWidth,screenshotHeight,TextureFormat.RGB24,false);

							for (int i = 0;i<(int)(screenshotPassCount);i++) {
								projectionBias=GetScreenshotMultipassBiasPattern(i);

								RenderToTexture(screenshotrt,screenshotRenderSizeFactor,(int)screenshotPassCount);

								RenderTexture.active=screenshotrt;
								subscreenshot.ReadPixels(new Rect(0,0,screenshotWidth,screenshotHeight),0,0);

								{
									byte[] subdata=subscreenshot.GetRawTextureData();

									if (sum==null) sum=new ushort[subdata.Length];
									for (int x = 0, ft = sum.Length;x<ft;x++) {
										sum[x]+=subdata[x];
									}
								}
							}
						}

						{//blending samples
							float normalizer=1.0f/(int)(screenshotPassCount);

							resultdata=new byte[sum.Length];

							for (int x = 0, ft = sum.Length;x<ft;x++) {
								resultdata[x]=(byte)(sum[x]*normalizer);
							}
						}
					}

					screenshot.LoadRawTextureData(resultdata);
				} else {
					RenderToTexture(screenshotrt,screenshotRenderSizeFactor,(int)screenshotPassCount,true);

					RenderTexture.active=screenshotrt;
					screenshot.ReadPixels(new Rect(0,0,screenshotWidth,screenshotHeight),0,0);
				}

				RenderTexture.active=lastrt;
				screenshotrt.Release();
				screenshotrt=null;

				{
					FileStream file=new FileStream(path,FileMode.Create,FileAccess.Write);
					byte[] screenshotdata=screenshot.EncodeToPNG();

					file.Write(screenshotdata,0,screenshotdata.Length);
					file.Close();
				}
				screenshot=null;

				projectionBias=new Vector2(0,0);
				RefreshViewport();
				//gameObject.SendMessage("FixedUpdate",SendMessageOptions.DontRequireReceiver);
				//gameObject.SendMessage("Update",SendMessageOptions.DontRequireReceiver);
				//gameObject.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
				//Render();
			}
		}
		private Vector2 GetScreenshotMultipassBiasPattern(int sample_idx) {
			switch (screenshotPassCount) {
				case ScreenshotPassCount.x2: return screenshotSamplesX2[sample_idx];
				case ScreenshotPassCount.x4: return screenshotSamplesX4[sample_idx];
				case ScreenshotPassCount.x8: return screenshotSamplesX8[sample_idx];
				case ScreenshotPassCount.x16: return screenshotSamplesX16[sample_idx];
				case ScreenshotPassCount.Normal: default: return new Vector2(0,0);
			}
		}

		/// <summary>
		/// Manually Render a frame in the specified render texture.
		/// You can have to call RefreshViewport after this method if it's executed between the Unity OnPreCull and the post-processing.
		/// Uses the default Render Size Factor.
		/// </summary>
		public void RenderToTexture(RenderTexture tex) {
			RenderToTexture(tex,renderSizeFactor);
		}
		/// <summary>
		/// Manually Render a frame in the specified render texture.
		/// You may have to call RefreshViewport after this method if it's executed between the Unity OnPreCull and the 
		/// post-processing.
		/// Can use successive passes if an effect uses rendering accumulation (as SEGI do for collecting samples).
		/// <paramref name="pass_count">Number of successive passes.</paramref>
		/// <paramref name="passes_call_update">Does camera update methods are called for each passes?</paramref>
		/// </summary>
		public void RenderToTexture(RenderTexture tex,float render_size_factor,int pass_count=1,bool passes_call_update=false) {
			RenderTexture lastcamrt=cam.targetTexture;
			RenderTexture lastrt=targetTexture;
			float lastosf=renderSizeFactor;
			Rect lastcamvp=cam.rect;
			Rect lastvp=viewportRect;
			bool hasreleased;


			hasreleased=UnlockTemporary();
			renderSizeFactor=render_size_factor;
			cam.targetTexture=null;
			targetTexture=tex;
			cam.rect=viewportRect=new Rect(0,0,1,1);

#if CPRT_LOG_BUFFERS
			Debug.Log("screenshot");
#endif
			
			RefreshViewport();
			for (int i = 0;i<pass_count;i++) {
				if (passes_call_update) {
					gameObject.SendMessage("FixedUpdate",SendMessageOptions.DontRequireReceiver);
					gameObject.SendMessage("Update",SendMessageOptions.DontRequireReceiver);
					gameObject.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
				}
				Render();
			}

#if CPRT_LOG_BUFFERS
			Debug.Log("end screenshot");
#endif

			cam.targetTexture=null;//lastcamrt;
			targetTexture=lastrt;
			renderSizeFactor=lastosf;
			cam.rect=lastcamvp;
			viewportRect=lastvp;
			if (hasreleased) RefreshViewport();
		}



		/// <summary>
		/// Returns a normalized vector that go from the camera through a screen point. 
		/// Take into account the current perspective system.
		/// </summary>
		/// <param name="screen_position">Screen point in pixel coordinates, origin at upper-left.</param>
		public Ray ScreenPointToRay(Vector2 screen_position) {
			switch (projectionType) {
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini:
				case CPRTType.Stereospherical:
					return ViewportPointToRay(new Vector2(
						-viewportRect.x*2.0f+screen_position.x/(rtBaseW-1),
						-viewportRect.y*2.0f+screen_position.y/(rtBaseH-1)));
				case CPRTType.SimpleOversampling:
					return cam.ScreenPointToRay(new Vector2(screen_position.x,screen_position.y));
				default:
					throw new NotImplementedException("CPRT picking in '"+projectionType+"' isn't implemented.");
			}
		}
		/// <summary>
		/// Returns a normalized vector that go from the camera through a viewport point. 
		/// Take into account the correct perspective system.
		/// </summary>
		/// <param name="screen_position">Viewport point point in [0.0 -> 1.0] coordinates, origin at upper-left.</param>
		public Ray ViewportPointToRay(Vector2 vp_pos) {
			return ViewportPointToRay(vp_pos,projectionType);
		}
		private Ray ViewportPointToRay(Vector2 vp_pos,CPRTType proj_type) {
			if (!isActiveAndEnabled) proj_type=CPRTType.SimpleOversampling;
			switch (proj_type) {
				case CPRTType.Stereospherical: {
					float offz=GetIntensityFactor(proj_type)*intensity;

					if (offz>0.0f) {
						Vector3 dir=new Vector3(vp_pos.x*2.0f-1.0f,vp_pos.y*2.0f-1.0f,1.0f);
						Matrix4x4 vpmat;


						vpmat=CPRTToolkit.LookAtRH(new Vector3(0.0f,0.0f,-offz),Vector3.forward,Vector3.up);

						observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeStereosphericalProjFOV(widthAperture,fieldOfViewY*Mathf.Deg2Rad,Aspect,-offz);
						vpmat=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*vpmat;//viewproj = proj * view


						{//observer now paints on a sphere
							dir=vpmat.inverse.MultiplyPoint(dir);

							dir.z+=offz;
							dir.Normalize();

							if (offz==1.0f) {//projection on a sphere (chord finding)
								dir*=2.0f*dir.z;//dir*=2.0f*Mathf.Sin((Mathf.PI-2.0f*Mathf.Acos(dir.z))*0.5f);
							} else {//projection on a sphere (raytrace)*/
								dir*=dir.z*offz+Mathf.Sqrt(1-CPRTToolkit.Sq(dir.x*offz)-CPRTToolkit.Sq(dir.y*offz));//dir*=(dir.z*d+Mathf.Sqrt(dir.x*dir.x+dir.y*dir.y+dir.z*dir.z-CPRTToolkit.Sqr(dir.x*d)-CPRTToolkit.Sqr(dir.y*d)))/(CPRTToolkit.Sqr(dir.x)+CPRTToolkit.Sqr(dir.y)+CPRTToolkit.Sqr(dir.z));
							}

							dir.z-=offz;
						}

						dir.x=-dir.x;//???
						dir=transform.TransformDirection(dir).normalized;

						return new Ray(transform.position,dir);
					} else {
						return cam.ViewportPointToRay(vp_pos);
					}
				}
				case CPRTType.Pannini: {
					float offz=GetIntensityFactor(proj_type)*intensity;

					if (offz>0.0f) {
						Vector3 dir=new Vector3(vp_pos.x*2.0f-1.0f,vp_pos.y*2.0f-1.0f,1.0f);
						Matrix4x4 vpmat;


						vpmat=CPRTToolkit.LookAtRH(new Vector3(0,0,-offz),Vector3.forward,Vector3.up);//view

						observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputePaniniProjFOV(widthAperture,Aspect,-offz);
						vpmat=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*vpmat;//viewproj = proj * view


						{//observer now paints on a cylinder
							dir=vpmat.inverse.MultiplyPoint(dir);
							dir.z+=offz;

							{//projection on a y aligned cylinder (raytrace)
								Vector2 flatdir=new Vector2(dir.x,dir.z).normalized;

								flatdir*=flatdir.y*offz+Mathf.Sqrt(1-CPRTToolkit.Sq(flatdir.x*offz));

								dir=new Vector3(flatdir.x,dir.y*flatdir.y/dir.z,flatdir.y-offz);//dir.z-=d;
							}
						}

						dir.x=-dir.x;//???
						dir=transform.TransformDirection(dir).normalized;

						return new Ray(transform.position,dir);
					} else {
						return cam.ViewportPointToRay(vp_pos);
					}
				}
				case CPRTType.AdaptivePannini: {
					float intensityfactor=GetIntensityFactor(proj_type);
					float offz=intensityfactor*intensity;

					if (offz>0.0f) {
						Vector3 dir=new Vector3(vp_pos.x*2.0f-1.0f,vp_pos.y*2.0f-1.0f,1.0f);
						Matrix4x4 observervpmatrix,paintervpmatrix;
						Matrix4x4 painterprojmatrix=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane);
						Vector3 obspos=new Vector3(0,0,-offz);

						{
							Vector3 obsat,paintat;
							float angle=AdaptivePanniniAngle;
							float centerscaling=GetStereoProjectionCenterScaling(offz);

							paintat=new Vector3(0.0f,Mathf.Sin(angle),Mathf.Cos(angle));//painter looks through angle
							paintervpmatrix=CPRTToolkit.LookAtRH(Vector3.zero,paintat,Vector3.up);//view
							paintervpmatrix=painterprojmatrix*paintervpmatrix;//proj * view

							obsat=new Vector3(0.0f,Mathf.Abs(angle)<Mathf.PI*0.5f ? Mathf.Tan(angle) : Mathf.Sign(angle)*16384.0f,1.0f);//observer looks painter center
							observervpmatrix=CPRTToolkit.LookAtRH(obspos,obsat,Vector3.up);//view
							observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeAdaptivePaniniProjFOV(widthAperture,Aspect,-offz,intensityfactor,centerscaling);
							observervpmatrix=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*observervpmatrix;//proj * view
						}

						{//observer now paints on a cylinder
							dir=observervpmatrix.inverse.MultiplyPoint(dir);
							dir.z+=offz;

							{//projection on a y aligned cylinder (raytrace)
								Vector2 flatdir=new Vector2(dir.x,dir.z).normalized;

								flatdir*=flatdir.y*offz+Mathf.Sqrt(1-CPRTToolkit.Sq(flatdir.x*offz));

								dir=new Vector3(flatdir.x,dir.y*flatdir.y/dir.z,flatdir.y-offz);//dir.z-=d;
							}
						}

						dir=paintervpmatrix.MultiplyPoint(dir);
						dir=painterprojmatrix.inverse.MultiplyPoint(dir);
						dir.z=Mathf.Abs(dir.z);//???

						dir=transform.TransformDirection(dir).normalized;

						return new Ray(transform.position,dir);
					} else {
						return cam.ViewportPointToRay(vp_pos);
					}
				}
				case CPRTType.SimpleOversampling:
					return cam.ViewportPointToRay(vp_pos);
				default:
					throw new NotImplementedException("CPRT picking in '"+projectionType+"' isn't implemented.");
			}
		}

		/// <summary>
		/// Equivalent of Camera.WorldToViewportPoint, but it takes into account the current perspective system.
		/// </summary>
		/// <param name="screen_position">Viewport point point in [0.0 -> 1.0] coordinates, origin at bottom-left. Z is basically the z distance from the Camera, if negative, w_pos is behind.</param>
		public Vector3 WorldToViewportPoint(Vector3 w_pos) {
			return WorldToViewportPoint(w_pos,projectionType);
		}
		/// <summary>
		/// Equivalent of Camera.WorldToViewportPoint, but it takes into account the given perspective type.
		/// </summary>
		/// <param name="screen_position">Viewport point point in [0.0 -> 1.0] coordinates, origin at bottom-left. Z is basically the z distance from the Camera, if negative, w_pos is behind.</param>
		private Vector3 WorldToViewportPoint(Vector3 w_pos,CPRTType proj_type) {
			if (!isActiveAndEnabled) proj_type=CPRTType.SimpleOversampling;
			switch (proj_type) {
				case CPRTType.Stereospherical: {
					float offz=GetIntensityFactor(proj_type)*intensity;

					if (offz>0.0f) {
						Vector3 linvp2=cam.transform.InverseTransformPoint(w_pos),dir2;//its an opti : linvp2=paintervpmat.inverse.MultiplyPoint(cam.WorldToViewportPoint(w_pos).wy*2-1)
						Matrix4x4 observervpmatrix;

						observervpmatrix=CPRTToolkit.LookAtRH(new Vector3(0.0f,0.0f,-offz),Vector3.forward,Vector3.up);
						observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeStereosphericalProjFOV(widthAperture,fieldOfViewY*Mathf.Deg2Rad,Aspect,-offz);//*Mathf.Deg2Rad ajouté le 06/05/2021
							observervpmatrix=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*observervpmatrix;//viewproj = proj * view

						{//observer now paints on a sphere
							dir2=linvp2.normalized;

							dir2=observervpmatrix.MultiplyPoint(dir2);
						}

						return new Vector3(dir2.x*-0.5f+0.5f,dir2.y*0.5f+0.5f,linvp2.z);
					} else {
						return cam.WorldToViewportPoint(w_pos);
					}
				}
				case CPRTType.Pannini:
				case CPRTType.AdaptivePannini: {
					float intensityfactor=GetIntensityFactor(proj_type);
					float offz=intensityfactor*intensity;

					if (offz>0.0f) {
						Vector3 linvp2=cam.transform.InverseTransformPoint(w_pos),dir2;//its an opti : linvp2=paintervpmat.inverse.MultiplyPoint(cam.WorldToViewportPoint(w_pos).wy*2-1)
						Matrix4x4 observervpmatrix;

						if (projectionType==CPRTType.AdaptivePannini) {
							Vector3 obsat;
							float angle=AdaptivePanniniAngle;
							float centerscaling=GetStereoProjectionCenterScaling(intensityfactor*intensity);
							Matrix2x2 painterviewmatrix;

							painterviewmatrix=new Matrix2x2(-angle);//painter looks through angle
							linvp2=painterviewmatrix.MultiplyZY(linvp2);

							obsat=new Vector3(0.0f,Mathf.Abs(angle)<Mathf.PI*0.5f ? Mathf.Tan(angle) : Mathf.Sign(angle)*16384.0f,1.0f);//observer looks painter center
							observervpmatrix=CPRTToolkit.LookAtRH(new Vector3(0,0,-offz),obsat,Vector3.up);//view (observerviewmatrix)
							observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputeAdaptivePaniniProjFOV(widthAperture,Aspect,-offz,intensityfactor,centerscaling);
							observervpmatrix=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*observervpmatrix;//proj * view
						} else {
							observervpmatrix=CPRTToolkit.LookAtRH(new Vector3(0,0,-offz),Vector3.forward,Vector3.up);//view
							observerFOV=Mathf.Rad2Deg*CPRTToolkit.ComputePaniniProjFOV(widthAperture,Aspect,-offz);
							observervpmatrix=Matrix4x4.Perspective(observerFOV,Aspect,0.0625f,observerFarClipPlane)*observervpmatrix;//proj * view
						}

						{//observer now paints on a sphere
							dir2=linvp2/(new Vector2(linvp2.x,linvp2.z)).magnitude;

							dir2=observervpmatrix.MultiplyPoint(dir2);
						}

						return new Vector3(dir2.x*-0.5f+0.5f,dir2.y*0.5f+0.5f,linvp2.z);
					} else {
						return cam.WorldToViewportPoint(w_pos);
					}
				}
				case CPRTType.SimpleOversampling:
					return cam.WorldToViewportPoint(w_pos);
				default:
					throw new NotImplementedException("CPRT picking in '"+projectionType+"' isn't implemented.");
			}
		}

		#region MULTIPLECAMERAQUICKFIX
		/// <summary>
		/// INTERNAL This variable is used for a quick fix to maintain multiple camera (up to 32) even if Unity has a bug with multiple camera of different render texture size (this bug is reported)
		/// </summary>
		private static int multipleCameraQuickFixId=0;
		/// <summary>
		/// INTERNAL This variable is used for a quick fix to maintain multiple camera (up to 32) even if Unity has a bug with multiple camera of different render texture size (this bug is reported)
		/// </summary>
		private Rect multipleCameraQuickFixOldViewport=new Rect(0,0,1,1);
		/// <summary>
		/// INTERNAL This variable is used for a quick fix to maintain multiple camera (up to 32) even if Unity has a bug with multiple camera of different render texture size (this bug is reported)
		/// </summary>
		private bool multipleCameraQuickFixEnable=false;
		/// <summary>
		/// INTERNAL This variable is used for a quick fix to maintain multiple camera (up to 32) even if Unity has a bug with multiple camera of different render texture size (this bug is reported)
		/// </summary>
		private const int multipleCameraQuickFixMaxCameras=128;

		private void MultipleCameraQuickFixRefresh() {
			if (multipleCameraQuickFixEnable) MultipleCameraQuickFixReset();

			multipleCameraQuickFixOldViewport=cam.rect;
			ComputeFinalViewportSize(out lastCamPixelW,out lastCamPixelH);
		}
		private void MultipleCameraQuickFix() {
			if (multipleCameraQuickFixEnable) MultipleCameraQuickFixReset();

			{
				Rect rekt=new Rect(0.0f,0.0f,1.0f,1.0f);// cam.rect;
				Matrix4x4 mat=cam.projectionMatrix;

				multipleCameraQuickFixEnable=true;

				ComputeFinalViewportSize(out lastCamPixelW,out lastCamPixelH);

				rekt.width=1.0f+(lastCamPixelW>1 ? (2.0f*((float)multipleCameraQuickFixId)/lastCamPixelW) : 0.0f);
				multipleCameraQuickFixId++;
				if (multipleCameraQuickFixId>multipleCameraQuickFixMaxCameras) multipleCameraQuickFixId=0;

				multipleCameraQuickFixOldViewport=new Rect(0.0f,0.0f,1.0f,1.0f);// cam.rect;
				cam.rect=rekt;
				cam.projectionMatrix=mat;
			}
		}
		private void MultipleCameraQuickFixReset() {
			cam.rect=multipleCameraQuickFixOldViewport;
			multipleCameraQuickFixEnable=false;
			multipleCameraQuickFixId=0;
		}

		private void LateUpdate() {
			int oldpxw=lastCamPixelW,oldpxh=lastCamPixelH;

			ComputeFinalViewportSize(out lastCamPixelW,out lastCamPixelH);

			if (oldpxw!=lastCamPixelW||oldpxh!=lastCamPixelH) {
				MultipleCameraQuickFixRefresh();
			}

			MultipleCameraQuickFix();
		}
		#endregion

		#region AASAMPLES
		private readonly Vector2[] screenshotSamplesX2=new Vector2[2] {//+30° segment
			new Vector2(0.5f,0.8660254f)*0.5f,new Vector2(-0.5f,-0.8660254f)*0.5f,
		};
		private readonly Vector2[] screenshotSamplesX4=new Vector2[4] {//+15° square
			new Vector2(0.304810f,0.952412f)*0.5f,new Vector2(-0.304810f,-0.952412f)*0.5f,
			new Vector2(-0.952412f,0.304810f)*0.5f,new Vector2(0.952412f,-0.304810f)*0.5f,
		};
		private readonly Vector2[] screenshotSamplesX8=new Vector2[8] {//well distributed random
			new Vector2(    -0.067f,    0.01f),
			new Vector2(    -0.555f,    0.53f),
			new Vector2(    0.955f,     -0.96f),
			new Vector2(    0.695f,     0.32f),
			new Vector2(    0.19f,      0.77f),
			new Vector2(    0.43f,      -0.5f),
			new Vector2(    -0.333f,    -0.71f),
			new Vector2(    -0.79f,     -0.24f),
		};
		private readonly Vector2[] screenshotSamplesX16=new Vector2[16] {//well distributed random
			new Vector2(    0.5f,       0.58f),
			new Vector2(    0.365f,     0.08f),
			new Vector2(    0.035f,     0.8f),
			new Vector2(    -0.1f,      0.32f),
			new Vector2(    0.09f,      -0.725f),
			new Vector2(    -0.055f,    -0.26f),
			new Vector2(    0.5f,       -0.37f),
			new Vector2(    0.866f,     -0.06f),
			new Vector2(    0.985f,     -0.56f),
			new Vector2(    0.617f,     -0.962f),
			new Vector2(    -0.47f,     0.65f),
			new Vector2(    -0.855f,    0.95f),
			new Vector2(    -0.935f,    0.455f),
			new Vector2(    -0.582f,    0.13f),
			new Vector2(    -0.515f,    -0.36f),
			new Vector2(    -0.37f,     -0.894f),
		};
		/*private readonly Vector2[] screenshotSamplesX8=new Vector2[] {//checker x8 (not enough random)
			new Vector2(-0.75f,0.75f),	new Vector2(-0.25f,0.25f),
			new Vector2(0.75f,-0.75f),	new Vector2(0.25f,-0.25f),
			new Vector2(0.25f,0.75f),	new Vector2(0.75f,0.25f),
			new Vector2(-0.25f,-0.75f),	new Vector2(-0.75f,-0.25f),
		};
		private readonly Vector2[] screenshotSamplesX16=new Vector2[] {//grid x16 (not enough random)
			new Vector2(-0.75f,	-0.75f),	new Vector2(-0.25f,	-0.75f),
			new Vector2(0.75f,	-0.75f),	new Vector2(0.25f,	-0.75f),
			new Vector2(-0.75f,	-0.25f),	new Vector2(-0.25f,	-0.25f),
			new Vector2(0.75f,	-0.25f),	new Vector2(0.25f,	-0.25f),
			new Vector2(-0.75f,	0.25f),		new Vector2(-0.25f,	0.25f),
			new Vector2(0.75f,	0.25f),		new Vector2(0.25f,	0.25f),
			new Vector2(-0.75f,	0.75f),		new Vector2(-0.25f,	0.75f),
			new Vector2(0.75f,	0.75f),		new Vector2(0.25f,	0.75f),
		};*/
		/*private readonly Vector2[] screenshotSamplesX32=new Vector2[32] {//if needed//32x checker (not enough random)
			new Vector2(-0.875f,-0.875f),new Vector2(-0.375f,-0.875f),
			new Vector2(-0.625f,-0.625f),new Vector2(-0.125f,-0.625f),
			new Vector2(-0.875f,-0.375f),new Vector2(-0.375f,-0.375f),
			new Vector2(-0.625f,-0.125f),new Vector2(-0.125f,-0.125f),
			new Vector2(0.125f,-0.875f),new Vector2(0.625f,-0.875f),
			new Vector2(0.375f,-0.625f),new Vector2(0.875f,-0.625f),
			new Vector2(0.125f,-0.375f),new Vector2(0.625f,-0.375f),
			new Vector2(0.375f,-0.125f),new Vector2(0.875f,-0.125f),

			new Vector2(0.875f,0.875f),new Vector2(0.375f,0.875f),
			new Vector2(0.625f,0.625f),new Vector2(0.125f,0.625f),
			new Vector2(0.875f,0.375f),new Vector2(0.375f,0.375f),
			new Vector2(0.625f,0.125f),new Vector2(0.125f,0.125f),
			new Vector2(-0.125f,0.875f),new Vector2(-0.625f,0.875f),
			new Vector2(-0.375f,0.625f),new Vector2(-0.875f,0.625f),
			new Vector2(-0.125f,0.375f),new Vector2(-0.625f,0.375f),
			new Vector2(-0.375f,0.125f),new Vector2(-0.875f,0.125f),
		};*/
		#endregion

	}
}



