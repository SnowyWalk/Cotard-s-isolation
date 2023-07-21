
    Camera Projection Rendering Toolkit
	           Version 1.6


--------------------------------------------------
                  Introduction
--------------------------------------------------

Thank you for purchasing Camera Projection Rendering Toolkit (CPRT). This Unity plugin will help you to :
	- achieve wide field of view perspective with advanced perspective techniques (up to 160°),
	- add Super-Sampling Anti-Aliasing (SSAA),
	- use oblique orthographic projections as in classic RPG or strategy games,
	- take high resolution screenshots.

It's intended for version after Unity 5_3_0, but the sample scenes are only compatible with Unity 5_6_1.


--------------------------------------------------
             ChangeLog
--------------------------------------------------

-1.6.2
	- Removed a bug in WorldToViewportPoint in Stereospheric mode : the returned coordinate were wrong.
	- Created a new sample scene "Stacked Rendering" which demonstrate :
		- the rendering by multiple camera which stack their results, 
		- the use of CPRT.WorldToViewportPoint in this context.

-1.6
	- Added the pseudo orthographic projection which mimics an orthographic projection with a perspective in order to allow deferred rendering with an orthographic projection.
	- removed a bug when disabling the CPRT component.
	- removed bugs in the CPRT editor.

-1.5.1 :
	- Improved CPRT compatibility with various external post-process plugins
	- Added an optional CPRTFrustumUpdater script : 
		- Some external plugins can interact with the camera during the "OnPreCull" Unity callback, and may be in conflict with CPRT. CPRTFrustumUpdater allows to reorder the CPRT camera handling, and should typically be first so the other scripts can work on CPRT camera settings.
	- Oversampling setting interface has been slitghly reworked : 
		- You'll likely need to reconfigure the oversampling in your scenes,
		- To make the setting clearer you now have two sliders : you can either set the render size multiplier or the Min Pixel Samples Count (minimal oversampling in the screen, 1 ensures that the image won't be downsampled at any place even with non-linear projection, 4 produce SSAAx2),
		- programatically, oversamplingFactor stays unchanged but is deprecated, please use renderSizeFactor instead from now on. The minimal oversampling in the screen can be set with the MinPixelSampleCount property,
		- Those changes applies to screenshot settings.
-1.5 :
	- New "Screenshot Pass Count" settings : 
		- Use screenshotPassCount to do successive rendering to help troubleshooting post-processing effects which accumuate samples over frames (like some ambient occlusion or global illumination effects do).
		- And set screenshotPassImproveSSAA to true to gather more antialiasing samples (for a maximum of 64 samples with SSAAx2 enabled).
	- fixed a compatibility issue with SonicEther's SEGI effect.
	- enabling / disabling the script should no longer make the camera unusable.
	- FOV can now be changed without calling RefreshEffect and RefreshViewport.
	- added a CPRT.WorldToViewportPoint method (equivalent of Camera.WorldToViewportPoint), it will help you to project a world point into the screen.

-1.4.2 :
	- Fixed some errors with Unity 2017.3.f03
	- Shader file are now found by default
	- Added support for OnEnable/OnDisable event, so the script can be disabled during runtime. 
		Known issue : There are still issues with multiple camera OnDisable handling.
-1.4.1 :
	- Hotfix : the mouse coordinates was inverted with Stereospheric and Pannini picking
- 1.4 :
	- Added support for AdaptivePannini camera-picking.
	- Adaptive Pannini has been fixed to keep viewport center at screen center, and to feel more natural.
	- CPRT control panel don't overstep the inspector width anymore.

- v1.3
	- Added support for camera-picking :
		- ScreenPointToRay and ViewportPointToRay are two new method that can help you to pick object from your camera while taking into account the projection distortion. Work with stereospheric and pannini but not with AdaptivePannini yet.
		- New showcase scene "MultiCamera Picking"
	- New script "AutoOversamplingScript" which adapt oversampling quality in real-time in accordance with the framerate.
	- Added Diagonal FOV setting (real-life camera common characteristic)
	- New property "RenderingFramerate" which gives the duration between two actual rendering.

- v1.2
	- fixed bugs when releasing executable with the plugin
	- made the documentation more user-friendly
	- added a TempRenderTextureManager.cs script that can help you to setup post-CPRT effects
	- added two test scenes
		- Surveillance Screens: demonstrate how to setup realtime render texture
		- Post-CPRT FX: demonstrate how to setup a post-CPRT chromatic aberration effect

- v1.1
	- added Horizontal FOV setting,
	- fixed some issues with render textures, you can no longer use the classic "Camera.targetTexture" parameter, you must use the script's "targetTexture" one,
	- fixed a memory leak and did some optimisations.

- v1.0
	- initial release.


--------------------------------------------------
             Contents of the package
--------------------------------------------------

 - full C# and shader code

 - PDF Documentation

 - sample scenes with presets
	
	- You must import some unity standard assets to make them work :
		- Asset -> Import Package -> Characters

 - models, textures and sources visible in screenshots :

      - a voxel floating floating island model, sources made with magica voxel, and Maya LT (for lightmap uvs)

      - a "dojo" style room model & textures, sources made with gimp and unity


---------------------------------------------------------------
                         License
            Camera Projection Rendering Toolkit
---------------------------------------------------------------

This software is distributed by the Unity Asset Store, so it is licensed under the Unity Asset Store EULA (on https://unity3d.com/fr/legal/as_terms or next to this file).

--------------------------------------------------
                Author & contact
--------------------------------------------------

Melvin REY : melcx@hotmail.fr
http://refoldedgames.com/