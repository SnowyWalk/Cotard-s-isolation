Shader "CPRT/StereoShapeProjShader" {
	Properties{
		_MainTex("Base (RGB)", 2D)="" {}
	}


		// Shader code pasted into all further CGPROGRAM blocks
CGINCLUDE
#include "UnityCG.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		float4 lclpos : TEXCOORD0;
	};

	sampler2D _MainTex;

	float4x4 ObserverViewProj;
	float4x4 PainterViewProj;
	float2 SampleSize;
	float2 Intensity;
	float SampleRatio;
	float SampleAspect;
	float SubSampleDistance;




	float4 ComputeScreenCoords(float4 lclpos) {
		return mul(PainterViewProj,lclpos);
	}
	float2 FinalizeScreenCoords(float4 coords) {
		return ((coords.xy)/coords.w)*float2(0.5f,0.5f)+float2(0.5f,0.5f);//,1.0f+coords.w);
	}


	v2f vert(appdata_img v) {
		v2f o;

		o.pos=mul(ObserverViewProj,v.vertex);
		o.lclpos=ComputeScreenCoords(v.vertex.xyzw);

		return o;
	}

	half4 fragOneSampleLinear(v2f i): SV_Target{
		return tex2D(_MainTex,FinalizeScreenCoords(i.lclpos));
	}

	static const float2 invsamplesize=1.0f/SampleSize;
	static const float samplesizem=length(SampleSize);
	half4 fragFourSamplesDistorded(v2f i):SV_Target{
		float2 coords=FinalizeScreenCoords(i.lclpos)-SampleSize*0.5f;
		float2 dcdx=ddx(coords)*SubSampleDistance*0.5f;
		float2 dcdy=ddy(coords)*SubSampleDistance*0.5f;
		half4 color;


		{//bilinear interp
			half2 bli=coords*invsamplesize-floor(coords*invsamplesize);

			color=tex2D(_MainTex,(coords))*(1-bli.x)*(1-bli.y)+
				tex2D(_MainTex,(coords+float2(SampleSize.x,0)))*(bli.x)*(1-bli.y)+
				tex2D(_MainTex,(coords+float2(0,SampleSize.y)))*(1-bli.x)*(bli.y)+
				tex2D(_MainTex,(coords+SampleSize))*(bli.x)*(bli.y);
		}

		{//x distortion compensation
			float d=length(dcdx);

			if (d>SampleSize.x) {
				half xtd=min(1.0f,(d-SampleSize.x)/samplesizem);

				color=color*(1-xtd*0.66666667f)+xtd*0.33333333f*(
					tex2D(_MainTex,coords+dcdx)+
					tex2D(_MainTex,coords-dcdx));
			}
		}

		{//y distortion compensation
			float d=length(dcdy);

			if (d>SampleSize.y) {
				half xtd=min(1.0f,(d-SampleSize.y)/samplesizem);

				color=color*(1-xtd*0.66666667f)+xtd*0.33333333f*(
					tex2D(_MainTex,coords+dcdy)+
					tex2D(_MainTex,coords-dcdy));
			}
		}

		return color;
	}
	half4 fragFourSamplesDistordedOptimised(v2f i):SV_Target{
		float2 coords=FinalizeScreenCoords(i.lclpos)-SampleSize*0.5f;
		half4 color;


		{//bilinear interp
			half2 bli=coords*invsamplesize-floor(coords*invsamplesize);

			color=tex2D(_MainTex,(coords))*(1-bli.x)*(1-bli.y)+
				tex2D(_MainTex,(coords+float2(SampleSize.x,0)))*(bli.x)*(1-bli.y)+
				tex2D(_MainTex,(coords+float2(0,SampleSize.y)))*(1-bli.x)*(bli.y)+
				tex2D(_MainTex,(coords+SampleSize))*(bli.x)*(bli.y);
		}

		{//x distortion compensation
			float2 dcdx=ddx(coords)*SubSampleDistance*0.5f;
			float d=length(dcdx);

			if (d>SampleSize.x) {
				half xtd=min(1.0f,(d-SampleSize.x)/samplesizem);

				color=color*(1-xtd*0.66666667f)+xtd*0.33333333f*(
					tex2D(_MainTex,coords+dcdx)+
					tex2D(_MainTex,coords-dcdx));
			}
		}

		return color;
	}

	half4 fragFourSamplesBilinear(v2f i): SV_Target{
		float2 coords=FinalizeScreenCoords(i.lclpos);
		float2 dcdx=ddx(coords)*SubSampleDistance*0.33333333f;
		float2 dcdy=ddy(coords)*SubSampleDistance*0.33333333f;
		float4 color;

		color=	tex2D(_MainTex,coords-dcdx-dcdy)+
				tex2D(_MainTex,coords+dcdx-dcdy)+
				tex2D(_MainTex,coords-dcdx+dcdy)+
				tex2D(_MainTex,coords+dcdx+dcdy);

		return color*0.25f;
	}
ENDCG

	Subshader {
		Pass{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragOneSampleLinear
			ENDCG
		}
		Pass{
			ZTest Always Cull Off ZWrite Off //Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragFourSamplesDistorded
			ENDCG
		}
		Pass{
			ZTest Always Cull Off ZWrite Off //Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragFourSamplesDistordedOptimised
			ENDCG
		}
		Pass{
			ZTest Always Cull Off ZWrite Off //Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragFourSamplesBilinear
			ENDCG
		}
	}

	Fallback off

} // shader



  /*tex2D() equivalent :
  float2 coords=i.uv-SampleSize*0.5f;
  float2 bli=coords*invsamplesize-floor(coords*invsamplesize);
  float4 color=tex2D(_MainTex, (coords))*(1-bli.x)*(1-bli.y)+
  tex2D(_MainTex,(coords+float2(SampleSize.x,0)))*(bli.x)*(1-bli.y)+
  tex2D(_MainTex,(coords+float2(0,SampleSize.y)))*(1-bli.x)*(bli.y)+
  tex2D(_MainTex,(coords+SampleSize))*(bli.x)*(bli.y);

  return color;
  */