using Godot;
using System;

public partial class Driver : Node
{
	public override void _Ready()
	{
		var world = new AutumnWorldStreamer
		{
			ChunkSize = 120f,
			RiverWidth = 16f,
			GridRadius = 2,

			NightMode = true,
			EnableSSR = true,
			EnableGlow = true,

			TreesPerChunk = 300,
			MinTreeHeight = 2.6f,
			MaxTreeHeight = 5.2f,
			TreeRadiusScale = new Vector2(0.8f, 1.35f),
			AddCanopySecondLobe = true,
			RiverBuffer = 1.2f,
			BridgeEveryNRows = 4,

			WaterWaveAmp = 0.06f,
			WaterWaveFreq = 1.6f,
			WaterWaveSpeed = 0.35f,
			WaterRoughness = 0.08f,
			WaterFresnelPower = 5.0f
		};
		AddChild(world);
		
		// 2) Add the camera with arrow-key controls
		var cam = new CameraArrowController
		{
			MoveSpeed = 12f,
			SprintMultiplier = 2.0f,
			LookSensitivity = 0.25f,
			CaptureMouse = true,
			DebugInput = true // set to false once verified
		};
		AddChild(cam);
		cam.Position = new Vector3(0f, 6f, 0f);
		cam.Current = true;
		cam.MakeCurrent();
		cam.CallDeferred(Camera3D.MethodName.MakeCurrent);

		// Optional: if another Camera3D exists and is current, force it off
		// (Useful if you still have a streamer that spawns its own camera.)
		foreach (var child in GetTree().GetNodesInGroup("cameras"))
		{
			if (child is Camera3D other && other != cam && other.Current)
				other.Current = false;
		}
	}
}
