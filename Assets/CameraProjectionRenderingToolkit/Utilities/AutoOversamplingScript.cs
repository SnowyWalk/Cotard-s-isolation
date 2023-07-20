using UnityEngine;
using CameraProjectionRenderingToolkit;
using System;


public class AutoOversamplingScript : MonoBehaviour {
	public int fpsQualityIncreasing=60;//ideal fps, we can increase quality
	public int fpsQualityDecreasing=55;//subpar fps, we can decrease quality
	public float qualityUpdateTime=1.5f;
	public int qualityUpdateMargin=2;//useful for ignoring quality changing performance overhead : 0 -> 4

	private float renderingFPS=60;
	//private float lastRenderingFPS=60;
	private int frameCount=0;
	private DateTime lastTime;
	private CPRT cprt;
	private int detectTime=1;


	private void Start() {
		lastTime=DateTime.Now;
		cprt=GetComponent<CPRT>();
	}


	private void OnPreCull() {
		if (detectTime>0) {
			if (detectTime<qualityUpdateMargin) {
				lastTime=DateTime.Now;
				frameCount=0;
				detectTime=0;
			} else {
				detectTime++;
			}
		} else {//detectTime==0, margin has been applied, waiting for quality update
			frameCount++;


			if ((DateTime.Now-lastTime).TotalSeconds>qualityUpdateTime) {
				float minOversample=cprt.ProjectionMaxPixelScaling;
				float maxOversample=2.0f*minOversample;

				renderingFPS=frameCount/qualityUpdateTime;
				if (renderingFPS<fpsQualityDecreasing) {
					if (cprt.renderSizeFactor>minOversample) {
						cprt.renderSizeFactor-=0.1f;//decrease renderSizeFactor setting
						if (cprt.renderSizeFactor<minOversample) cprt.renderSizeFactor=minOversample;
					}
				} else if (renderingFPS>fpsQualityIncreasing) {
					if (cprt.renderSizeFactor<maxOversample) {
						cprt.renderSizeFactor+=0.1f;//decrease renderSizeFactor setting
						if (cprt.renderSizeFactor>maxOversample) cprt.renderSizeFactor=maxOversample;
					}
				}
				//lastRenderingFPS=renderingFPS;
				detectTime=1;
			}
		}
	}
}
