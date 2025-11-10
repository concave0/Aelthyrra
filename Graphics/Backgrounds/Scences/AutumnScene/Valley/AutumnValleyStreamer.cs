using Godot;
using System;
// Alias to avoid ambiguity with System.Environment
using GdEnvironment = Godot.Environment;

public partial class AutumnValleyStreamer : Node3D
{
	// Chunk settings
	[Export] public float ChunkLength { get; set; } = 120f;
	[Export] public float RiverWidth { get; set; } = 16f;

	// Streaming window around the camera's current chunk index
	[Export] public int ChunksAhead { get; set; } = 3;
	[Export] public int ChunksBehind { get; set; } = 1;

	// Camera movement
	[Export] public float CameraSpeed { get; set; } = 6.0f;
	[Export] public Vector3 CameraStartPosition { get; set; } = new Vector3(0f, 6f, -50f);

	// Deterministic RNG base for chunks
	[Export] public ulong BaseSeed { get; set; } = 987654UL;

	// Visual style toggles
	[Export] public bool NightMode { get; set; } = true;
	[Export] public bool EnableSSR { get; set; } = true;
	[Export] public bool EnableGlow { get; set; } = true;

	// Moon disc controls
	[Export] public float MoonDistance { get; set; } = 500f;
	[Export] public float MoonAngularSizeDeg { get; set; } = 1.1f;

	// Forest/water defaults passed to chunks
	[Export] public int TreesPerSide { get; set; } = 300;
	[Export] public float MinTreeHeight { get; set; } = 2.6f;
	[Export] public float MaxTreeHeight { get; set; } = 5.2f;
	[Export] public Vector2 TreeRadiusScale { get; set; } = new Vector2(0.8f, 1.35f);
	[Export] public float RiverBuffer { get; set; } = 2.0f;
	[Export] public bool AddCanopySecondLobe { get; set; } = true;
	[Export] public int BridgeEveryNChunks { get; set; } = 4;

	[Export] public float WaterWaveAmp { get; set; } = 0.06f;
	[Export] public float WaterWaveFreq { get; set; } = 1.6f;
	[Export] public float WaterWaveSpeed { get; set; } = 0.35f;
	[Export] public float WaterRoughness { get; set; } = 0.08f;
	[Export] public float WaterFresnelPower { get; set; } = 5.0f;
	[Export] public Color WaterShallowColor { get; set; } = new Color(0.06f, 0.08f, 0.12f);
	[Export] public Color WaterDeepColor { get; set; } = new Color(0.02f, 0.05f, 0.09f);

	private Camera3D _cam;
	private DirectionalLight3D _moonLight;
	private MeshInstance3D _moonDisc;
	private QuadMesh _moonQuad;

	private readonly System.Collections.Generic.Dictionary<int, AutumnValleyChunk> _active = new();

	public override void _Ready()
	{
		// World environment (night or day)
		SetupEnvironment();

		// Light (moon or sun)
		_moonLight = new DirectionalLight3D
		{
			LightColor = NightMode ? new Color(0.75f, 0.82f, 1.0f) : new Color(1.0f, 0.95f, 0.85f),
			LightEnergy = NightMode ? 0.85f : 3.5f,
			ShadowEnabled = true
		};
		_moonLight.RotationDegrees = NightMode ? new Vector3(-15f, 25f, 0f) : new Vector3(-25f, 40f, 0f);
		AddChild(_moonLight);

		// Camera
		_cam = new Camera3D { Current = true };
		AddChild(_cam);
		_cam.Position = CameraStartPosition;
		_cam.LookAt(new Vector3(0, 1.5f, 0), Vector3.Up);

		// Moon disc (for visual + SSR reflection)
		if (NightMode)
		{
			AddMoonDisc();
			UpdateMoonDiscTransform();
		}

		// Spawn initial chunk window
		RefreshChunks(forceAll: true);
	}

	public override void _Process(double delta)
	{
		// Move camera forward along +Z (down the river)
		_cam.Translate(new Vector3(0f, 0f, CameraSpeed * (float)delta));

		// Keep streamer spawning/removing chunks based on camera position
		RefreshChunks();

		// Update moon disc to face camera and follow the light direction
		if (NightMode)
			UpdateMoonDiscTransform();
	}

	private void SetupEnvironment()
	{
		var env = new GdEnvironment { BackgroundMode = GdEnvironment.BGMode.Sky };
		env.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };

		try {
			if (NightMode)
			{
				env.AmbientLightColor = new Color(0.08f, 0.10f, 0.14f);
				env.AmbientLightEnergy = 0.4f;
				env.AmbientLightSkyContribution = 0.2f;

				env.FogEnabled = true;
				env.FogDensity = 0.028f;
				env.FogLightColor = new Color(0.42f, 0.48f, 0.62f);
				env.FogSkyAffect = 0.7f;
			}
			else
			{
				env.AmbientLightColor = new Color(0.8f, 0.8f, 0.8f);
				env.AmbientLightEnergy = 1.2f;
			}
		} catch { }

		if (EnableSSR)
		{
			try {
				env.SsrEnabled = true;
				env.SsrMaxSteps = 128;
			} catch { }
		}
		if (EnableGlow && NightMode)
		{
			try {
				env.GlowEnabled = true;
				env.GlowIntensity = 0.55f;
			} catch { }
		}

		AddChild(new WorldEnvironment { Environment = env });
	}

	private int ZToChunkIndex(float z)
	{
		// Chunks are centered at i * ChunkLength and span [center - L/2, center + L/2).
		return Mathf.FloorToInt((z + ChunkLength * 0.5f) / ChunkLength);
	}

	private void RefreshChunks(bool forceAll = false)
	{
		int center = ZToChunkIndex(_cam.GlobalPosition.Z);
		int minI = center - ChunksBehind;
		int maxI = center + ChunksAhead;

		// Spawn missing
		for (int i = minI; i <= maxI; i++)
		{
			if (forceAll || !_active.ContainsKey(i))
				SpawnChunk(i);
		}

		// Despawn out of window
		var toRemove = new System.Collections.Generic.List<int>();
		foreach (var kv in _active)
		{
			if (kv.Key < minI || kv.Key > maxI)
				toRemove.Add(kv.Key);
		}
		foreach (int idx in toRemove)
		{
			_active[idx].QueueFree();
			_active.Remove(idx);
		}
	}

	private void SpawnChunk(int index)
	{
		var chunk = new AutumnValleyChunk
		{
			ChunkIndex = index,
			ChunkLength = ChunkLength,
			RiverWidth = RiverWidth,

			TreesPerSide = TreesPerSide,
			MinTreeHeight = MinTreeHeight,
			MaxTreeHeight = MaxTreeHeight,
			TreeRadiusScale = TreeRadiusScale,
			RiverBuffer = RiverBuffer,
			AddCanopySecondLobe = AddCanopySecondLobe,
			BridgeEveryNChunks = BridgeEveryNChunks,

			WaterWaveAmp = WaterWaveAmp,
			WaterWaveFreq = WaterWaveFreq,
			WaterWaveSpeed = WaterWaveSpeed,
			WaterRoughness = WaterRoughness,
			WaterFresnelPower = WaterFresnelPower,
			WaterShallowColor = WaterShallowColor,
			WaterDeepColor = WaterDeepColor,

			Seed = BaseSeed + (ulong)(index * 10007)
		};

		// Place chunk center at world Z = index * ChunkLength
		chunk.Position = new Vector3(0f, 0f, index * ChunkLength);

		AddChild(chunk);
		_active[index] = chunk;
	}

	private void AddMoonDisc()
	{
		_moonQuad = new QuadMesh();
		_moonDisc = new MeshInstance3D { Mesh = _moonQuad };

		var shader = new Shader();
		shader.Code = @"
shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

uniform vec4 color : source_color = vec4(0.9, 0.95, 1.0, 1.0);
uniform float softness = 0.02;

void fragment() {
	vec2 uv = UV * 2.0 - 1.0;
	float r = length(uv);
	float edge = 1.0 - smoothstep(1.0 - softness, 1.0, r);
	ALBEDO = color.rgb;
	EMISSION = color.rgb * edge * 2.0;
	ALPHA = edge * color.a;
}
";
		_moonDisc.MaterialOverride = new ShaderMaterial { Shader = shader };
		AddChild(_moonDisc);
	}

	private void UpdateMoonDiscTransform()
	{
		if (_cam == null || _moonDisc == null || _moonQuad == null || _moonLight == null)
			return;

		var lightDir = -_moonLight.GlobalTransform.Basis.Z;
		var moonDir = -lightDir;

		var pos = _cam.GlobalTransform.Origin + moonDir * MoonDistance;

		float theta = Mathf.DegToRad(Mathf.Max(0.1f, MoonAngularSizeDeg));
		float size = 2f * MoonDistance * Mathf.Tan(theta * 0.5f);
		_moonQuad.Size = new Vector2(size, size);

		var basis = Basis.LookingAt((_cam.GlobalTransform.Origin - pos).Normalized(), Vector3.Up);
		_moonDisc.Transform = new Transform3D(basis, pos);
	}
}
