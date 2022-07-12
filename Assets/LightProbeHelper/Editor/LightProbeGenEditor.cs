using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(LightProbeGenerator))]
public class LightProbeGenEditor : Editor
{
	private BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();
	private bool _editBounds = false;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Generate"))
		{
			(target as LightProbeGenerator).GenProbes();
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Clear"))
		{
			(target as LightProbeGenerator).ClearProbes();
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Edit Bounds"))
        {
			_editBounds = !_editBounds;
        }
	}

	public void OnSceneGUI()
	{
		LightProbeGenerator gen = target as LightProbeGenerator;

		if (_editBounds)
        {
			_boundsHandle.center = gen.LightProbeVolumes.ProbeVolume.center;
			_boundsHandle.size = gen.LightProbeVolumes.ProbeVolume.size;
			_boundsHandle.wireframeColor = Color.red;
			_boundsHandle.handleColor = Color.green;
		}

		if (_editBounds)
        {
			EditorGUI.BeginChangeCheck();
			_boundsHandle.DrawHandle();
		}

		if (Tools.current == Tool.Move && !_editBounds)
		{
			Undo.RecordObject(target, "ProbeGenerator Move");

			gen.LightProbeVolumes.ProbeVolume.center = Handles.PositionHandle(
					gen.LightProbeVolumes.ProbeVolume.center, Quaternion.identity);
		}

		if (Tools.current == Tool.Rotate && !_editBounds)
		{
			Undo.RecordObject(target, "ProbeGenerator Rotation");

			gen.LightProbeVolumes.Rotation = Handles.RotationHandle(gen.LightProbeVolumes.Rotation, gen.LightProbeVolumes.ProbeVolume.center);
		}

		if (Tools.current == Tool.Scale && !_editBounds)
		{
			Undo.RecordObject(target, "ProbeGenerator Scale");

			gen.LightProbeVolumes.ProbeVolume.extents = Handles.ScaleHandle(
							gen.LightProbeVolumes.ProbeVolume.extents,
							gen.LightProbeVolumes.ProbeVolume.center,
							Quaternion.identity,
							5.0f);
		}

        if (EditorGUI.EndChangeCheck() && _editBounds)
        {
			Undo.RecordObject(target, "Change Bounds");

            Bounds newBounds = new Bounds
            {
                center = _boundsHandle.center,
                size = _boundsHandle.size
            };
            gen.LightProbeVolumes.ProbeVolume.center = newBounds.center;
			gen.LightProbeVolumes.ProbeVolume.extents = newBounds.extents;
        }
	}
}