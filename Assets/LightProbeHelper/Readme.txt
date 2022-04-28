SETTING UP LIGHT PROBE GENERATOR
1.) First, create your light probe group (GameObject -> Create Empty, then Component -> Rendering -> Light Probe Group)
2.) Attach the LightProbeGenerator component (Component -> Light Probe Helper -> Light Probe Generator)
3.) Set how you want the placement algorithm to work (Grid or Random)

GENERATING LIGHT PROBES
1.) ALWAYS remember to delete light probes before generating new ones (Select All -> Delete Selected)
2.) Set the volumes you want the light probes to occupy (as well as either subdivisions or the number of light probes within the volume, depending on the selected placement algorithm).
3.) Hit 'Generate'
4.) Bake your lightmap

Original by http://forum.unity3d.com/members/51935-PhobicGunner
Edited By Svetoslav Iliev (http://www.fos4o.net)
Edited by Tatum Kirchner (https://github.com/TatumKirchner)

Added:
	- Svetoslav Iliev
		* Gizmos for position and scale of the regions (Go to editor move mode to move and in editor scale mode to scale the boxes)
		* Undo
	- Tatum Kirchner
		* Updated for Unity 2021
			- Replaced depreciated code
		* Added rotation functionality
		* Added bounding box resizing handles