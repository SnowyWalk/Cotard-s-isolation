using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


/*
 * Autor : Melvin REY
 * For any request/question, contact me at refoldedgames@gmail.com
 */


namespace CameraProjectionRenderingToolkit {

	/// <summary>
	/// Some functions that are useful for the custom perspective projection system. Matrix convention is Row-Major.
	/// </summary>
	public static class CPRTToolkit {

		/// <summary>
		/// Get the X field of view of a camera in radians
		/// </summary>
		/// <param name="fov_y">Y field of view in radians (Camera.fieldOfView)</param>
		/// <param name="aspect">Viewport Width/height, number given by Camera.aspect</param>
		public static float GetFovX(float fov_y,float aspect) {
			return ACotan(Cotan(fov_y*0.5f)/aspect)*2;
		}
		/// <summary>
		/// Get the X field of view of a camera in radians
		/// </summary>
		public static float GetFovX(Camera cam) {
			return ACotan(cam.projectionMatrix.m00)*2;
		}
		/// <summary>
		/// Get the Y field of view of a camera (number given by Camera.fieldOfView) in radians
		/// </summary>
		/// <param name="fov_x">Y aperture in radians</param>
		/// <param name="aspect">Width/height, number given by Camera.aspect</param>
		public static float GetFovY(float fov_x,float aspect) {
			return ACotan(Cotan(fov_x*0.5f)*aspect)*2;
		}
		/// <summary>
		/// Get the Y field of view of a camera in radians
		/// </summary>
		public static float GetFovY(Camera cam) {
			return cam.fieldOfView*Mathf.Deg2Rad;
		}
		/// <summary>
		/// Get the diagonal FOV in radians from the x/y FOV in radians
		/// </summary>
		public static float GetFovDiag(float fov_x,float fov_y) {
			float x=Mathf.Tan(fov_x*0.5f);
			float y=Mathf.Tan(fov_y*0.5f);

			return Mathf.Atan(Mathf.Sqrt(Sq(x)+Sq(y)))*2.0f;
		}
		/// <summary>
		/// Get the diagonal FOV in radians
		/// </summary>
		public static float GetFovDiag(Camera cam) {
			float x=1.0f/cam.projectionMatrix.m00;
			float y=1.0f/cam.projectionMatrix.m11;

			return Mathf.Atan(Mathf.Sqrt(Sq(x)+Sq(y)))*2.0f;
		}
		/// <summary>
		/// Get the diagonal FOV in radians
		/// </summary>
		public static float GetFovDiagFromYAspect(float fov_y,float aspect) {
			float x=-aspect*Mathf.Tan(fov_y*0.5f);
			float y=Mathf.Tan(fov_y*0.5f);

			return Mathf.Atan(Mathf.Sqrt(Sq(x)+Sq(y)))*2.0f;
		}
		/// <summary>
		/// Get the diagonal FOV in radians
		/// </summary>
		public static float GetFovYFromDiagAspect(float fov_diag,float aspect) {
			return Mathf.Abs(2*Mathf.Atan(Mathf.Tan(fov_diag/2)/Mathf.Sqrt(Sq(aspect)+1)));
		}

		/// <summary>
		/// Returns the Y field of view (in radians) for a correct zoom for the Pannini "observer" projection.
		/// </summary>
		/// <param name="fov_x">X field of view of the camera</param>
		/// <param name="camera_aspect">Width/height, number given by Camera.aspect</param>
		/// <param name="z_offset">Z position of the "observer"</param>
		public static float ComputePaniniProjFOV(float fov_x,float camera_aspect,float z_offset) {
			Vector3 mapcorner;
			float anglex=fov_x*0.5f;

			mapcorner=new Vector3(Mathf.Sin(anglex),0.0f,Mathf.Cos(anglex)-z_offset);
			return GetFovY(2.0f*Mathf.Abs(Mathf.Atan2(mapcorner.x,mapcorner.z)),camera_aspect);
		}
		/// <summary>
		/// Returns the Y field of view (in radians) for a correct zoom for the Pannini "observer" projection.
		/// </summary>
		/// <param name="painter_proj_matrix">"Painter" projection matrix (rotated base matrix).</param>
		/// <param name="camera_aspect">Width/height, number given by Camera.aspect</param>
		/// <param name="z_offset">Z position of the "observer"</param>
		/// <param name="intensity_factor">Adaptive Pannini intensity factor (0->1)</param>
		/// <param name="center_scaling">How much the center of the screen get scaled when intensity factor==1.</param>
		public static float ComputeAdaptivePaniniProjFOV(float fov_x,float camera_aspect,float z_offset,float intensity_factor,float center_scaling) {
			Vector3 mapcorner;
			float anglex=fov_x*0.5f;
			float yfov;

			mapcorner=new Vector3(Mathf.Sin(anglex),0.0f,Mathf.Cos(anglex)-z_offset);
			yfov=GetFovY(2.0f*Mathf.Abs(Mathf.Atan2(mapcorner.x,mapcorner.z)),camera_aspect);

			//return Mathf.Lerp(yfov/(center_scaling),yfov,intensity_factor);
			return Mathf.Lerp(yfov/(0.5f+0.5f*center_scaling),yfov,intensity_factor);
		}
		/// <summary>
		/// Returns the Y field of view (in radians) for a correct zoom for the Stereosphercal "observer" projection.
		/// (incompatible with biased matrices)
		/// </summary>
		/// <param name="fov_x">X field of view of the camera</param>
		/// <param name="camera_aspect">Width/height, number given by Camera.aspect</param>
		/// <param name="z_offset">Z position of the "observer" (projection_matrix.m23) = opposite of projection intensity</param>
		public static float ComputeStereosphericalProjFOV(float fov_x,float fov_y,float camera_aspect,float z_offset) {
			Vector3 mapcorner;

			mapcorner.x=Mathf.Tan(fov_x*0.5f);
			mapcorner.y=Mathf.Tan(fov_y*0.5f);
			mapcorner.z=1.0f;
			mapcorner.Normalize();
			mapcorner.z-=z_offset;
			
			return 2.0f*Mathf.Abs(Mathf.Atan2(mapcorner.y,mapcorner.z));
		}
		/*/// <summary>
		/// DEPRECATED (incompatible with biased matrices)
		/// Returns the Y field of view (in radians) for a correct zoom for the Stereosphercal "observer" projection.
		/// </summary>
		/// <param name="camera_proj_matrix">Base camera projection matrix.</param>
		/// <param name="camera_aspect">Width/height, number given by Camera.aspect</param>
		/// <param name="z_offset">Z position of the "observer" (projection_matrix.m23) = opposite of projection intensity</param>
		public static float ComputeStereosphericalProjFOV(Matrix4x4 camera_proj_matrix,float camera_aspect,float z_offset) {
			Matrix4x4 invpainter=camera_proj_matrix.inverse;
			Vector3 mapcorner;

			mapcorner=VectorAbs(invpainter.MultiplyPoint(new Vector3(1,1,1)).normalized);
			mapcorner.z-=z_offset;

			return 2.0f*Mathf.Abs(Mathf.Atan2(mapcorner.y,mapcorner.z));
		}*/


		/// <summary>
		/// returns cotangent of x.
		/// </summary>
		/// <param name="x">angle in radian</param>
		public static float Cotan(float x) {
			return Mathf.Tan((float)(Math.PI*0.5)-x);
		}
		/// <summary>
		/// returns inverse cotangent (cotan^-1) of x in radians.
		/// </summary>
		/// <param name="x">cotangent</param>
		public static float ACotan(float x) {
			return -Mathf.Atan(x)+(float)(Math.PI*0.5);
		}
		
		/// <summary>
		/// Returns x clamped by min and max.
		/// </summary>
		public static float Clamp(float x,float min,float max) {
			return x>max ? max : (x<min ? min : x);
		}
		/// <summary>
		/// Returns x clamped by min and max.
		/// </summary>
		public static int Clamp(int x,int min,int max) {
			return x>max ? max : (x<min ? min : x);
		}
		
		/// <summary>
		/// Returns square of x.
		/// </summary>
		public static int Sq(int x) {
			return x*x;
		}
		/// <summary>
		/// Returns square of x.
		/// </summary>
		public static float Sq(float x) {
			return x*x;
		}
		/// <summary>
		/// Returns cube of x.
		/// </summary>
		public static int Pow3(int x) {
			return x*x*x;
		}
		/// <summary>
		/// Returns cube of x.
		/// </summary>
		public static float Pow3(float x) {
			return x*x*x;
		}

		/// <summary>
		/// Returns a point on a 1 radius sphere (yaw is applied first).
		/// </summary>
		/// <param name="pitch">X axis rotation in radians.</param>
		/// <param name="yaw">Y axis rotation in radians.</param>
		public static Vector3 SpherePoint(float pitch,float yaw) {
			return new Vector3(Mathf.Sin(yaw)*Mathf.Cos(Mathf.Abs(pitch)),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(Mathf.Abs(pitch)));
		}
		/// <summary>
		/// Returns a point on a 1 radius sphere (yaw is applied first).
		/// </summary>
		/// <param name="pitch_yaw">X/Y axis rotation in radians.</param>
		public static Vector3 SpherePoint(Vector2 pitch_yaw) {
			return new Vector3(Mathf.Sin(pitch_yaw.y)*Mathf.Cos(Mathf.Abs(pitch_yaw.x)),Mathf.Sin(pitch_yaw.x),Mathf.Cos(pitch_yaw.y)*Mathf.Cos(Mathf.Abs(pitch_yaw.x)));
		}

		/// <summary>
		/// Returns a vector transformed (multiplied) by the matrix (Matrix4x4.MultiplyPoint Vector4 equivalent)
		/// </summary>
		public static Vector4 Vector4Transform(Matrix4x4 matrix,Vector4 vec) {
			Vector4 tformed=new Vector4(
				vec.x*matrix.m00+vec.y*matrix.m01+vec.z*matrix.m02+vec.w*matrix.m03,
				vec.x*matrix.m10+vec.y*matrix.m11+vec.z*matrix.m12+vec.w*matrix.m13,
				vec.x*matrix.m20+vec.y*matrix.m21+vec.z*matrix.m22+vec.w*matrix.m23,
				vec.x*matrix.m30+vec.y*matrix.m31+vec.z*matrix.m32+vec.w*matrix.m33);

			return tformed;
		}
		/// <summary>
		/// Returns a vector transformed (multiplied) by the matrix and apply perspective on it.
		/// When vec.w==1.0f, it's equivalent to Matrix4x4.MultiplyPoint.
		/// When vec.w==0.0f, it transforms a vector.
		/// </summary>
		public static Vector3 Vector4TransformPerspective(Matrix4x4 matrix,Vector4 vec) {
			float invw=1.0f/(vec.x*matrix.m30+vec.y*matrix.m31+vec.z*matrix.m32+vec.w*matrix.m33);
			Vector3 tformed=new Vector3(
				(vec.x*matrix.m00+vec.y*matrix.m01+vec.z*matrix.m02+vec.w*matrix.m03)*invw,
				(vec.x*matrix.m10+vec.y*matrix.m11+vec.z*matrix.m12+vec.w*matrix.m13)*invw,
				(vec.x*matrix.m20+vec.y*matrix.m21+vec.z*matrix.m22+vec.w*matrix.m23)*invw);

			return tformed;
		}

		/// <summary>
		/// Returns the vector 'vec' with positive coordinates.
		/// </summary>
		public static Vector3 VectorAbs(Vector3 vec) {
			if (vec.x<0) vec.x=-vec.x;
			if (vec.y<0) vec.y=-vec.y;
			if (vec.z<0) vec.z=-vec.z;

			return vec;
		}

		
		/// <summary>
		/// Create a matrix that rotate around X axis.
		/// </summary>
		/// <param name="angle">angle in radians.</param>
		public static Matrix4x4 RotationX(float angle) {
			Matrix4x4 mat=Matrix4x4.zero;

			mat.m00=1.0f;
			mat.m12=Mathf.Sin(angle);
			mat.m22=Mathf.Cos(angle);
			mat.m11=mat.m22;
			mat.m21=-mat.m12;
			mat.m33=1.0f;

			return mat;
		}
		/// <summary>
		/// Create a matrix that rotate around Y axis.
		/// </summary>
		/// <param name="angle">angle in radians.</param>
		public static Matrix4x4 RotationY(float angle) {
			Matrix4x4 mat=new Matrix4x4();

			mat.m00=Mathf.Cos(angle);
			mat.m20=Mathf.Sin(angle);
			mat.m11=1.0f;
			mat.m02=-mat.m20;
			mat.m22=mat.m00;
			mat.m33=1.0f;

			return mat;
		}
		/// <summary>
		/// Create a matrix that rotate around Z axis.
		/// </summary>
		/// <param name="angle">angle in radians.</param>
		public static Matrix4x4 RotationZ(float angle) {
			Matrix4x4 mat=Matrix4x4.zero;

			mat.m01=Mathf.Sin(angle);
			mat.m11=Mathf.Cos(angle);
			mat.m10=mat.m11;
			mat.m20=-mat.m01;
			mat.m22=1.0f;
			mat.m33=1.0f;

			return mat;
		}
		/// <summary>
		/// Produce a view matrix to have a camera that views the world from a point, looking at an other, with
		/// a specified up vector.
		/// Uses the right handed convention (Unity default).
		/// </summary>
		public static Matrix4x4 LookAtRH(Vector3 pos,Vector3 at,Vector3 up) {
			Vector3 nx,ny,nz;
			Matrix4x4 mat;

			nz=(pos-at).normalized;
			if (nz.sqrMagnitude==0.0f) nz=Vector3.forward;
			nx=Vector3.Cross(up,nz).normalized;
			if (nx.sqrMagnitude==0.0f) nx=Vector3.left;
			ny=Vector3.Cross(nz,nx);

			mat.m00=nx.x;					mat.m10=ny.x;					mat.m20=nz.x;					mat.m30=0.0f;
			mat.m01=nx.y;					mat.m11=ny.y;					mat.m21=nz.y;					mat.m31=0.0f;
			mat.m02=nx.z;					mat.m12=ny.z;					mat.m22=nz.z;					mat.m32=0.0f;
			mat.m03=-Vector3.Dot(nx,pos);	mat.m13=-Vector3.Dot(ny,pos);	mat.m23=-Vector3.Dot(nz,pos);	mat.m33=1.0f;

			return mat;
		}
		public static Matrix4x4 LookAtRH() {
			return LookAtRH(Vector3.zero,Vector3.forward,Vector3.up);
		}
		/// <summary>
		/// Produce a view matrix to have a camera that views the world from a point, looking at an other, with
		/// a specified up vector.
		/// Uses the left handed convention.
		/// </summary>
		public static Matrix4x4 LookAtLH(Vector3 pos,Vector3 at,Vector3 up) {
			Vector3 nz=(at-pos).normalized;
			Vector3 nx=Vector3.Cross(up,nz).normalized;
			Vector3 ny=Vector3.Cross(nz,nx);
			Matrix4x4 mat;

			nz=(at-pos).normalized;
			if (nz.sqrMagnitude==0.0f) nz=Vector3.forward;
			nx=Vector3.Cross(up,nz).normalized;
			if (nx.sqrMagnitude==0.0f) nx=Vector3.right;
			ny=Vector3.Cross(nz,nx);

			mat.m00=nx.x;					mat.m10=ny.x;					mat.m20=nz.x;					mat.m30=0.0f;
			mat.m01=nx.y;					mat.m11=ny.y;					mat.m21=nz.y;					mat.m31=0.0f;
			mat.m02=nx.z;					mat.m12=ny.z;					mat.m22=nz.z;					mat.m32=0.0f;
			mat.m03=-Vector3.Dot(nx,pos);	mat.m13=-Vector3.Dot(ny,pos);	mat.m23=-Vector3.Dot(nz,pos);	mat.m33=1.0f;

			return mat;
		}
		public static Matrix4x4 LookAtLH() {
			return LookAtLH(Vector3.zero,Vector3.forward,Vector3.up);
		}


#if !(UNITY_3||UNITY_4||UNITY_5)
		public static bool RTDescEquals(RenderTextureDescriptor desc1,RenderTextureDescriptor desc2) {
			return
				desc1.autoGenerateMips==desc2.autoGenerateMips&&
				desc1.useMipMap==desc2.useMipMap&&
				desc1.width==desc2.width&&
				desc1.colorFormat==desc2.colorFormat&&
				desc1.depthBufferBits==desc2.depthBufferBits&&
				desc1.msaaSamples==desc2.msaaSamples&&
				desc1.sRGB==desc2.sRGB;
		}
#endif
	}


	public struct Matrix2x2 {
		public float nx_x,nx_y;
		public float ny_x,ny_y;
		
		public Matrix2x2(float _nx_x,float _nx_y,float _ny_x,float _ny_y) {
			nx_x=_nx_x;
			nx_y=_nx_y;
			ny_x=_ny_x;
			ny_y=_ny_y;
		}
		public Matrix2x2(float angle) {
			nx_x=Mathf.Cos(angle);
			ny_x=Mathf.Sin(angle);
			nx_y=-ny_x;
			ny_y=nx_x;
		}
		
		public Vector2 Multiply(Vector2 u) {
			return new Vector2(
				u.x*nx_x+u.y*ny_x,
				u.x*nx_y+u.y*ny_y);
		}
		public Vector3 MultiplyXY(Vector3 u) {
			return new Vector3(
				u.x*nx_x+u.y*ny_x,
				u.x*nx_y+u.y*ny_y,
				u.z);
		}
		public Vector3 MultiplyZY(Vector3 u) {
			return new Vector3(
				u.x,
				u.z*nx_y+u.y*ny_y,
				u.z*nx_x+u.y*ny_x);
		}
		public Vector3 MultiplyXZ(Vector3 u) {
			return new Vector3(
				u.x*nx_x+u.z*ny_x,
				u.y,
				u.x*nx_y+u.z*ny_y);
		}
		public Vector3 MultiplyZX(Vector3 u) {
			return new Vector3(
				u.x*nx_y+u.z*ny_y,
				u.y,
				u.x*nx_x+u.z*ny_x);
		}

		public float Determinant() {
			return nx_x*ny_y-nx_y*ny_x;
		}
		public Matrix2x2 Inverse() {
			float factor=1.0f/Determinant();
			return new Matrix2x2(
				ny_y*factor,nx_y*-factor,
				ny_x*-factor,nx_x*factor);
		}

		public override string ToString() {
			return "("+nx_x+", "+nx_y+" | "+ny_x+", "+ny_y+")";
		}
	}


	public static class CPRTPseudoOrthographicsSettings {
		public const float DefaultToleranceFactor=0.03f;
		public const float ShadowDistanceFactor=(1.0f/0.75f);


		[Flags]
		public enum CheckResult : uint {
			EverythingsOk=0,
			ShadowDistanceTooLow=1,ShadowDistanceTooHigh=2,OnlyOneShadowCascadeRequired=4,
			//CamFarClipPlaneTooLow=8,CamFarClipPlaneTooHigh=16,
		};

		
		public static CPRT[] GetAllPseudoOrthoCams() {
			CPRT[] orthos;

			{
				List<CPRT> allcprts=new	List<CPRT>(GameObject.FindObjectsOfType<CPRT>());
				List<CPRT> ortholist;

				if (allcprts.Count==0) return null;
				ortholist=new List<CPRT>(allcprts.Count);
			
				foreach (CPRT cprt in allcprts) {
					if (cprt.projectionType==CPRT.CPRTType.PseudoOrthographic) ortholist.Add(cprt);
				}

				orthos=ortholist.ToArray();
			}

			return orthos;
		}

		/// <summary>
		/// Checks the settings of every pseudo-ortho cameras in the scene and report any issue.
		/// </summary>
		public static CheckResult CheckSettings(float tolerance_factor=DefaultToleranceFactor) {
			return CheckSettings(GetAllPseudoOrthoCams(),tolerance_factor);
		}
		/// <summary>
		/// Checks the settings of the given pseudo-ortho cameras and report any issue.
		/// </summary>
		public static CheckResult CheckSettings(CPRT[] cams_cprt,float tolerance_factor=DefaultToleranceFactor) {
			if (cams_cprt!=null&&cams_cprt.Length>0) {
				CheckResult flags=CheckResult.EverythingsOk;
				bool notenoughshaddist=false;
				bool shaddistok=false;
			
				foreach (CPRT cprt in cams_cprt) {
					CheckResult camflags=CheckSettings(cprt,tolerance_factor);

					flags|=camflags&(CheckResult.OnlyOneShadowCascadeRequired/*|CheckResult.CamFarClipPlaneTooLow|CheckResult.CamFarClipPlaneTooHigh*/);

					notenoughshaddist|=(camflags&CheckResult.ShadowDistanceTooLow)==CheckResult.ShadowDistanceTooLow;
					shaddistok|=(camflags&(CheckResult.ShadowDistanceTooLow|CheckResult.ShadowDistanceTooHigh))==CheckResult.EverythingsOk;
				}

				if (notenoughshaddist) {
					flags|=CheckResult.ShadowDistanceTooLow;
				} else if (!shaddistok) {
					flags|=CheckResult.ShadowDistanceTooHigh;
				}

				return flags;
			} else {
				return CheckResult.EverythingsOk;
			}
		}
		/// <summary>
		/// Checks the settings of the given pseudo-ortho camera and report any issue.
		/// If multiple camera are to be taken into account, use another signature.
		/// </summary>
		public static CheckResult CheckSettings(CPRT cam_cprt,float tolerance_factor=DefaultToleranceFactor) {
			//Camera cam=cam_cprt.GetComponent<Camera>();
			CheckResult flags=CheckResult.EverythingsOk;
			float shadowdistmin=cam_cprt.perspectiveOffset*ShadowDistanceFactor*(1.0f-tolerance_factor);
			float shadowdistmax=cam_cprt.perspectiveOffset*ShadowDistanceFactor*(1.0f+tolerance_factor);

			if (QualitySettings.shadowCascades>1) flags|=CheckResult.OnlyOneShadowCascadeRequired;
			if (QualitySettings.shadowDistance<shadowdistmin) flags|=CheckResult.ShadowDistanceTooLow;
			if (QualitySettings.shadowDistance>shadowdistmax) flags|=CheckResult.ShadowDistanceTooHigh;
			//if (cam.farClipPlane<shadowdistmin) flags|=CheckResult.CamFarClipPlaneTooLow;
			//if (cam.farClipPlane>shadowdistmax) flags|=CheckResult.CamFarClipPlaneTooHigh;

			return flags;
		}

		
		/// <summary>
		/// Corrects the settings of every pseudo-ortho cameras in the scene if there is any issue.
		/// Affects QualitySettings.shadowCascades, QualitySettings.shadowDistance.
		/// </summary>
		public static void Correct(float tolerance_factor=DefaultToleranceFactor) {
			Correct(GetAllPseudoOrthoCams(),tolerance_factor);
		}
		/// <summary>
		/// Corrects the settings of the given pseudo-ortho cameras if there is any issue.
		/// Affects QualitySettings.shadowCascades, QualitySettings.shadowDistance.
		/// </summary>
		public static void Correct(CPRT[] cams_cprt,float tolerance_factor=DefaultToleranceFactor) {
			if (cams_cprt!=null&&cams_cprt.Length>0&&CheckSettings(cams_cprt,tolerance_factor)!=CheckResult.EverythingsOk) {
				float maxshaddist=0.0f;
				
				foreach (CPRT cprt in cams_cprt) {
					maxshaddist=Mathf.Max(maxshaddist,cprt.perspectiveOffset*ShadowDistanceFactor);
					//cprt.GetComponent<Camera>().farClipPlane=cprt.perspectiveOffset*ShadowDistanceFactor;
				}

				QualitySettings.shadowCascades=Mathf.Min(1,QualitySettings.shadowCascades);
				QualitySettings.shadowDistance=maxshaddist;
			}
		}
		/// <summary>
		/// Corrects the settings of the given pseudo-ortho camera if there is any issue.
		/// If multiple camera are to be taken into account, use another signature.
		/// Affects QualitySettings.shadowCascades, QualitySettings.shadowDistance.
		/// </summary>
		public static void Correct(CPRT cam_cprt,float tolerance_factor=DefaultToleranceFactor) {
			if (CheckSettings(cam_cprt,tolerance_factor)!=CheckResult.EverythingsOk) {
				//cam_cprt.GetComponent<Camera>().farClipPlane=cam_cprt.perspectiveOffset*ShadowDistanceFactor;
				QualitySettings.shadowCascades=Mathf.Min(1,QualitySettings.shadowCascades);
				QualitySettings.shadowDistance=cam_cprt.perspectiveOffset*ShadowDistanceFactor;
			}
		}
	}


#if UNITY_EDITOR
	/// <summary>
	/// Editor functionnalities of class CameraProjectionRenderingToolkit
	/// </summary>
	[CustomEditor(typeof(CPRT))]
	[CanEditMultipleObjects]
	public class CPRTEditor : Editor {

		private static bool adaptationSettingsVisible=false;
		private static bool shaderSettingsVisible=false;
		private static bool debugSettingsVisible=false;
		private static bool screenshotMenuVisible=false;

		private bool renderSizeFactorMustApply=false;
		private float renderSizeFactorNewValue=1.0f;
		private const float renderSizeFactorMustApplyCeil=4096;



		public override void OnInspectorGUI() {
			CPRT[] cprts=Array.ConvertAll(serializedObject.targetObjects,(r => (CPRT)r));
			CPRT firstcprt=(CPRT)serializedObject.targetObject;
			bool messageout,buttonclicked;
			const int hspace=10;//10



			EditorGUILayout.PropertyField(serializedObject.FindProperty("projectionType"));


			if (serializedObject.isEditingMultipleObjects) {
				foreach (CPRT cprt in cprts) {
					if (!firstcprt.IsInit) {
						GUILayout.Label("Not initialized");
						return;
					}
					if (cprt.projectionType!=firstcprt.projectionType) {
						EditorGUILayout.HelpBox("Multiediting with different projection types isn't supported.",MessageType.Warning);
						serializedObject.ApplyModifiedProperties();
						return;
					}
				}
			}
			if (firstcprt.projectionType==CPRT.CPRTType.Pannini||
					firstcprt.projectionType==CPRT.CPRTType.AdaptivePannini||
					firstcprt.projectionType==CPRT.CPRTType.Stereospherical||
					firstcprt.projectionType==CPRT.CPRTType.ObliqueOrthographic||
					firstcprt.projectionType==CPRT.CPRTType.PseudoOrthographic) {
				bool fix=false;
				bool orthoshouldbe=(firstcprt.projectionType==CPRT.CPRTType.ObliqueOrthographic);

				foreach (CPRT cprt in cprts) {
					Camera cam=cprt.GetComponent<Camera>();

					if (cam.orthographic!=orthoshouldbe) {
						GUILayout.BeginHorizontal();

						EditorGUILayout.HelpBox("Camera should be "+
							(orthoshouldbe?"orthographic":"non-orthographic")+
							" for this projection type.",MessageType.Warning);
						if (GUILayout.Button("Fix now !")) {
							fix=true;
						}

						GUILayout.EndHorizontal();

						break;
					}
				}

				if (fix) {
					foreach (CPRT cprt in cprts) {
						cprt.GetComponent<Camera>().orthographic=orthoshouldbe;
					}
				}

				switch (firstcprt.projectionType) {
					case CPRT.CPRTType.ObliqueOrthographic:
						GUILayout.Space(10);

						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Isometric",GUILayout.Width(78))) {
							foreach (CPRT cprt in cprts) {
								cprt.obliqueBias=new Vector2(0,0.1709f);
								cprt.transform.rotation=Quaternion.Euler(new Vector3(45.0f,45.0f+90.0f*Mathf.Round((cprt.transform.rotation.eulerAngles.y-45.0f)/90.0f),0));
								cprt.RefreshViewport();
							}
						}
						if (GUILayout.Button("Top-Down",GUILayout.Width(78))) {
							foreach (CPRT cprt in cprts) {
								cprt.obliqueBias=new Vector2(0,1);
								cprt.transform.rotation=Quaternion.Euler(new Vector3(90.0f,90.0f*Mathf.Round((cprt.transform.rotation.eulerAngles.y-45.0f)/90.0f),0));
								cprt.RefreshViewport();
							}
						}
						if (GUILayout.Button("Cavalier",GUILayout.Width(78))) {
							foreach (CPRT cprt in cprts) {
								cprt.obliqueBias=new Vector2(0.707106f,-0.707106f);
								cprt.transform.rotation=Quaternion.Euler(new Vector3(0.0f,90.0f*Mathf.Round((cprt.transform.rotation.eulerAngles.y-45.0f)/90.0f),0));
								cprt.RefreshViewport();
							}
						}
						if (GUILayout.Button("Military",GUILayout.Width(78))) {
							foreach (CPRT cprt in cprts) {
								cprt.obliqueBias=new Vector2(0,1);
								cprt.transform.rotation=Quaternion.Euler(new Vector3(90.0f,45.0f+90.0f*Mathf.Round((cprt.transform.rotation.eulerAngles.y-45.0f)/90.0f),0));
								cprt.RefreshViewport();
							}
						}
						GUILayout.EndHorizontal();

				
						EditorGUILayout.PropertyField(serializedObject.FindProperty("obliqueBias"),new GUIContent("Oblique Bias"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("obliqueZeroDistance"),new GUIContent("Zero Bias Distance"));

						break;
					case CPRT.CPRTType.PseudoOrthographic: {
							CPRT[] pseudoorthos=CPRTPseudoOrthographicsSettings.GetAllPseudoOrthoCams();
							CPRTPseudoOrthographicsSettings.CheckResult r=CPRTPseudoOrthographicsSettings.CheckSettings(pseudoorthos);

							EditorGUILayout.PropertyField(serializedObject.FindProperty("orthographicSize"),new GUIContent("Orthographic size"));
							EditorGUILayout.PropertyField(serializedObject.FindProperty("perspectiveOffset"),new GUIContent("Perspective offset"));
						
							if (r!=CPRTPseudoOrthographicsSettings.CheckResult.EverythingsOk) {
								GUILayout.BeginHorizontal();

								EditorGUILayout.HelpBox("The following problems seems to appear for a correct rendering : "+r,MessageType.Warning);
								if (GUILayout.Button("Fix now !")) {
									CPRTPseudoOrthographicsSettings.Correct(pseudoorthos);
								}

								GUILayout.EndHorizontal();
							}

						}
						break;
				}
			}

			
			messageout=false;
			foreach (CPRT cprt in cprts) {
				List<MonoBehaviour> list=new List<MonoBehaviour>(cprt.GetComponents<MonoBehaviour>());
				bool cprtpassed=false;

				foreach (MonoBehaviour script in list) {
					if (script!=null) {
						if (cprtpassed) {
							if (script.GetType().ToString().IndexOf("Effect")>=0) {
								EditorGUILayout.HelpBox("All image effects must be BEFORE this one (Click on the gear at top-right -> Move Down).",MessageType.Warning);
								messageout=true;
								break;
							}
						} else {
							if (script.GetType()==typeof(CPRT)) cprtpassed=true;
						}
					}
				}

				if (messageout) break;
			}



			if (!firstcprt.IsOrthographicProjection) {
				GUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("fieldOfView"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("fieldOfViewSetting"),new GUIContent(""),GUILayout.Width(70));
				GUILayout.EndHorizontal();
			}


			if (firstcprt.projectionType==CPRT.CPRTType.UnityBuiltinFisheye||
					firstcprt.projectionType==CPRT.CPRTType.Pannini||
					firstcprt.projectionType==CPRT.CPRTType.AdaptivePannini||
					firstcprt.projectionType==CPRT.CPRTType.Stereospherical) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty("intensity"));
			}

			if (firstcprt.projectionType==CPRT.CPRTType.AdaptivePannini) {
				if (adaptationSettingsVisible=EditorGUILayout.Foldout(adaptationSettingsVisible,"Projection adaptation settings")) {
					if (hspace>0) {
						GUILayout.BeginHorizontal();
						GUILayout.Space(hspace);
						GUILayout.BeginVertical();
					}

					EditorGUILayout.PropertyField(serializedObject.FindProperty("isAdaptiveAutomatic"));

					if (serializedObject.isEditingMultipleObjects||!firstcprt.isAdaptiveAutomatic) {
						EditorGUILayout.PropertyField(serializedObject.FindProperty("adaptivePower"));
					}
					EditorGUILayout.PropertyField(serializedObject.FindProperty("adaptiveTolerance"));

					if (hspace>0) {
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
					}
				}
			}


			{
				GUILayout.Space(10);
				GUILayout.Label("Rendering :");

				{//tab
					int maxrendersize=0;

					foreach (CPRT cprt in cprts) {
						int w,h;
						cprt.ComputeFinalViewportSize(out w,out h);	
						maxrendersize=Mathf.Max(maxrendersize,(int)(w*cprt.viewportRect.width),(int)(h*cprt.viewportRect.height));
					}


					if (hspace>0) {
						GUILayout.BeginHorizontal();
						GUILayout.Space(hspace);
						GUILayout.BeginVertical();
					}

				
					{
						float ssaatoover=firstcprt.ProjectionMaxPixelScaling;
						SerializedProperty prop=serializedObject.FindProperty("renderSizeFactor");
						bool multiplevalues=prop.hasMultipleDifferentValues;
						float overvalue=renderSizeFactorMustApply?renderSizeFactorNewValue:firstcprt.renderSizeFactor,newovervalue=overvalue;
						bool changeover=false;



						if (multiplevalues) {
							float r;

							if (float.TryParse(EditorGUILayout.TextField(new GUIContent("Render Size Factor"),"-"),out r)) {
								newovervalue=Mathf.Clamp(r,CPRT.MinRenderSizeFactor,ssaatoover*2.0f);
								changeover=true;
							}
							if (firstcprt.IsNonLinearProjection&&float.TryParse(EditorGUILayout.TextField(new GUIContent("Min Pixel Samples"),"-"),out r)) {
								newovervalue=Mathf.Clamp(Mathf.Sqrt(r)*ssaatoover,CPRT.MinRenderSizeFactor,ssaatoover*2.0f);
								changeover=true;
							}
						} else {
							//if (firstcprt.IsNonLinearProjection) {
								float newssaavalue,ssaavalue=0.01f*Mathf.Floor(100.0f*CPRTToolkit.Sq(overvalue/ssaatoover)+0.5f);
								float minover=CPRT.MinRenderSizeFactor;
								float minssaa=CPRTToolkit.Sq(CPRT.MinRenderSizeFactor/ssaatoover);
								
								newovervalue=EditorGUILayout.Slider("Render Size Factor",overvalue,minover,ssaatoover*2.0f);
								newssaavalue=EditorGUILayout.Slider("Min Pixel Samples",ssaavalue,minssaa,4.0f);

								if (Math.Abs(ssaavalue-newssaavalue)>0.001f) {//v1.6
									newssaavalue=0.01f*Mathf.Floor(100.0f*newssaavalue+0.5f);
									newovervalue=Mathf.Sqrt(newssaavalue)*ssaatoover;
								} 
							//} else {
							//	newovervalue=EditorGUILayout.Slider("Render Size Factor",overvalue,CPRT.MinRenderSizeFactor,ssaatoover*2.0f);
							//}

							if (newovervalue!=overvalue) changeover=true;
						}


						if (renderSizeFactorMustApply) {
							bool apply=newovervalue*maxrendersize<renderSizeFactorMustApplyCeil;

							renderSizeFactorNewValue=newovervalue;

							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Apply (GPU Heavy)",GUILayout.Width(150))) apply=true;
							GUILayout.EndHorizontal();

							if (apply) {
								if (Event.current.type==EventType.Used) {
									prop.floatValue=newovervalue;
									renderSizeFactorMustApply=false;
								}
							}
						} else {
							if (changeover&&Event.current.type==EventType.Used) {
								if (newovervalue*maxrendersize<renderSizeFactorMustApplyCeil) {
									prop.floatValue=newovervalue;
								} else {
									renderSizeFactorNewValue=newovervalue;
									renderSizeFactorMustApply=true;
								}
							}
						}
						GUILayout.Space(10);
					}


					EditorGUILayout.PropertyField(serializedObject.FindProperty("viewportRect"),new GUIContent("Viewport Rect"));
					if (!serializedObject.isEditingMultipleObjects) {
						/*EditorGUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();*/
						EditorGUILayout.LabelField("Rendering resolution : "+firstcprt.RenderTargetWidth+"x"+firstcprt.RenderTargetHeight);
						//EditorGUILayout.EndHorizontal();
					}
					GUILayout.Space(5);


					{
						EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTexture"));
						bool problem=false;

						foreach (CPRT cprt in cprts) {
							if (cprt.GetComponent<Camera>().targetTexture) {
								problem=true;
							}
						}

						if (problem) {
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.HelpBox("You must use THIS target texture instead of the camera one.",MessageType.Error);
							if (GUILayout.Button("Transfer settings now")) {
								foreach (CPRT cprt in cprts) {
									Camera cam=cprt.GetComponent<Camera>();

									cprt.targetTexture=cam.targetTexture;
									cam.targetTexture=null;
									cam.rect=new Rect(0,0,1,1);
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}


					buttonclicked=false;
					foreach (CPRT cprt in cprts) {
						Camera cam=cprt.GetComponent<Camera>();

						if (cam.rect!=new Rect(0,0,1,1)) {
							GUILayout.BeginHorizontal();
							EditorGUILayout.HelpBox("The viewport-rect settings of the Unity Camera has been modified, but when this effect is activated, you should work with the plugin viewport-rect settings (click the button to transfer the values).",MessageType.Error);
							if (GUILayout.Button("Transfer settings now")) {
								buttonclicked=true;
							}
							GUILayout.EndHorizontal();
							break;
						}
					}
					if (buttonclicked) {
						foreach (CPRT cprt in cprts) {
							Camera cam=cprt.GetComponent<Camera>();

							if (cam.rect!=new Rect(0,0,1,1)) {
								cprt.viewportRect=cam.rect;
								cam.rect=new Rect(0,0,1,1);
								cprt.OnValidate();
							}
						}
					}


					EditorGUILayout.PropertyField(serializedObject.FindProperty("filterMode"),new GUIContent("Filter"));
					if (firstcprt.filterMode==CPRT.CPRTFilterMode.DistordedBilinear||
							firstcprt.filterMode==CPRT.CPRTFilterMode.DistordedBilinearOptimized||
							firstcprt.filterMode==CPRT.CPRTFilterMode.UniformBilinear) {
						EditorGUILayout.PropertyField(serializedObject.FindProperty("filterSharpen"),new GUIContent("Filter Sharpening"));
					}
					if ((!firstcprt.IsNonLinearProjection||firstcprt.projectionType==CPRT.CPRTType.UnityBuiltinFisheye) &&
							(firstcprt.filterMode==CPRT.CPRTFilterMode.DistordedBilinear||
							firstcprt.filterMode==CPRT.CPRTFilterMode.DistordedBilinearOptimized)) {
						EditorGUILayout.HelpBox("This filter mode isn't intended for this projection, it prevents downsampled regions (because of distortion).",MessageType.Warning);
					}


					EditorGUILayout.PropertyField(serializedObject.FindProperty("renderTextureDepthBits"),new GUIContent("Render Depth Bits"));


					GUILayout.Space(5);


					if (firstcprt.projectionType==CPRT.CPRTType.Pannini||
							firstcprt.projectionType==CPRT.CPRTType.AdaptivePannini||
							firstcprt.projectionType==CPRT.CPRTType.Stereospherical) {
						EditorGUILayout.PropertyField(serializedObject.FindProperty("projectionPrecision"),new GUIContent("Mesh precision"));
					}


					GUILayout.Space(10);


					if (hspace>0) {
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
					}
				}
			}


			//Screenshots!
			if ((!serializedObject.isEditingMultipleObjects)
							&&(screenshotMenuVisible=EditorGUILayout.Foldout(screenshotMenuVisible,"Screen Independant Screenshot"))) {
				int mintexsize=(int)(CPRT.MaxRenderTextureSize/Math.Max(1.0f,firstcprt.screenshotRenderSizeFactor));
				float ssaatoover=firstcprt.ProjectionMaxPixelScaling;
				float newssaavalue,ssaavalue=0.01f*Mathf.Floor(100.0f*CPRTToolkit.Sq(firstcprt.screenshotRenderSizeFactor/ssaatoover)+0.5f);

				if (hspace>0) {
					GUILayout.BeginHorizontal();
					GUILayout.Space(hspace);
					GUILayout.BeginVertical();
				}


				GUILayout.BeginHorizontal();
				GUILayout.Label("Screenshot size : ");
				firstcprt.screenshotWidth=Math.Min(mintexsize,EditorGUILayout.IntField(firstcprt.screenshotWidth));
				firstcprt.screenshotHeight=Math.Min(mintexsize,EditorGUILayout.IntField(firstcprt.screenshotHeight));
				GUILayout.EndHorizontal();

		
				GUILayout.Space(5);


				EditorGUILayout.Slider(serializedObject.FindProperty("screenshotRenderSizeFactor"),CPRT.MinRenderSizeFactor,ssaatoover*2.0f,new GUIContent("Render Size Factor : "));
				newssaavalue=EditorGUILayout.Slider("Min Pixel Samples",ssaavalue,CPRTToolkit.Sq(CPRT.MinRenderSizeFactor/ssaatoover),4.0f);
				if (ssaavalue!=newssaavalue) {
					newssaavalue=0.01f*Mathf.Floor(100.0f*newssaavalue+0.5f);
					firstcprt.screenshotRenderSizeFactor=Mathf.Min(Mathf.Sqrt(newssaavalue)*ssaatoover,ssaatoover*2.0f);
				} 


				GUILayout.Space(5);


				EditorGUILayout.PropertyField(serializedObject.FindProperty("screenshotPassCount"),new GUIContent("Pass count : "));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("screenshotPassImproveSSAA"),new GUIContent("Improve SSAA ?"));


				GUILayout.Space(10);


				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Take Screenshot !",GUILayout.Width(140),GUILayout.Height(32))) {
					string path="Screenshot "+DateTime.Now.ToShortDateString().Replace('/','-')+' '+DateTime.Now.ToShortTimeString().Replace(':','h')+".png";
						
					path=EditorUtility.SaveFilePanel("Save screenshot as PNG","",path,"png");

					if (!string.IsNullOrEmpty(path)) firstcprt.OffsreenScreenshot(path);
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();


				GUILayout.Space(10);


				if (hspace>0) {
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				}
			}


			{//Shaders!
				bool shaderbug=firstcprt.shaderOverSampling==null||
						firstcprt.shaderStereoShapeProj==null||
						firstcprt.shaderUnityFishEye==null;

				if (shaderSettingsVisible=EditorGUILayout.Foldout(shaderbug||shaderSettingsVisible,"Shaders")) {
					if (hspace>0) {
						GUILayout.BeginHorizontal();
						GUILayout.Space(hspace);
						GUILayout.BeginVertical();
					}

					if (shaderbug&&GUILayout.Button("Auto fill Shaders",GUILayout.Width(120))) {
						serializedObject.FindProperty("shaderOverSampling").objectReferenceValue=Shader.Find("CPRT/OverSamplingShader");
						serializedObject.FindProperty("shaderStereoShapeProj").objectReferenceValue=Shader.Find("CPRT/StereoShapeProjShader");
						serializedObject.FindProperty("shaderUnityFishEye").objectReferenceValue=Shader.Find("CPRT/UnityFisheyeShader");
					}

					EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderOverSampling"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderStereoShapeProj"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("shaderUnityFishEye"));

					if (hspace>0) {
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
				}
			}

			if (firstcprt.projectionType==CPRT.CPRTType.Pannini||
					firstcprt.projectionType==CPRT.CPRTType.AdaptivePannini||
					firstcprt.projectionType==CPRT.CPRTType.Stereospherical) {
				if (debugSettingsVisible=EditorGUILayout.Foldout(debugSettingsVisible,"Debug settings")) {
					if (hspace>0) {
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(hspace);
						EditorGUILayout.BeginVertical();
					}


					EditorGUILayout.PropertyField(serializedObject.FindProperty("projectionWireframe"),new GUIContent("Show proj mesh"));
					

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Cam X FOV : "+(CPRTToolkit.GetFovX(firstcprt.GetComponent<Camera>())*Mathf.Rad2Deg)+"°");
					EditorGUILayout.LabelField("Cam Y FOV : "+(firstcprt.GetComponent<Camera>().fieldOfView)+"°");
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Cam Aspect : "+firstcprt.GetComponent<Camera>().aspect);
					EditorGUILayout.LabelField("Obs Y FOV : "+firstcprt.ObserverFOV);
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Cam Diag FOV : "+(CPRTToolkit.GetFovDiag(firstcprt.GetComponent<Camera>())*Mathf.Rad2Deg)+"°");
					EditorGUILayout.LabelField("Obs Diag FOV : "+(CPRTToolkit.GetFovDiagFromYAspect(firstcprt.ObserverFOV*Mathf.Deg2Rad,firstcprt.Aspect)*Mathf.Rad2Deg)+"°");
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Max px scaling : "+firstcprt.renderSizeFactor/firstcprt.ProjectionMaxPixelScaling);
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Final intensity : "+firstcprt.IntensityFactor);
					EditorGUILayout.LabelField("RT MSAA : "+firstcprt.RenderTargetMSAA);
					EditorGUILayout.EndHorizontal();


					if (hspace>0) {
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
					}
				}
			}


			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}