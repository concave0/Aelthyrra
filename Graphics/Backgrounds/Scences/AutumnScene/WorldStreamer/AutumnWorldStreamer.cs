using Godot;
using System;
using System.Collections.Generic;
using GdEnvironment = Godot.Environment;

public partial class AutumnWorldStreamer : Node3D
{
	[Export] public int NumVisibleTrees { get; set; } = 100;
	[Export] public float TreeArcRadius { get; set; } = 18f;
	[Export] public PackedScene TreeScene { get; set; }
	[Export] public PackedScene SkyScene { get; set; } // Export your .tscn sky as a PackedScene

	// Leave your chunk and water/outdoor options here as before ...
	[Export] public float ChunkSize { get; set; } = 120f;
	[Export] public float RiverWidth { get; set; } = 16f;
	[Export] public int GridRadius { get; set; } = 2;
	[Export] public bool NightMode { get; set; } = true;
	[Export] public bool EnableSSR { get; set; } = true;
	[Export] public bool EnableGlow { get; set; } = true;
	[Export] public int TreesPerChunk { get; set; } = 320;
	[Export] public float MinTreeHeight { get; set; } = 2.6f;
	[Export] public float MaxTreeHeight { get; set; } = 5.2f;
	[Export] public Vector2 TreeRadiusScale { get; set; } = new Vector2(0.8f, 1.35f);
	[Export] public bool AddCanopySecondLobe { get; set; } = true;
	[Export] public float RiverBuffer { get; set; } = 1.2f;
	[Export] public int BridgeEveryNRows { get; set; } = 4;
	[Export] public float WaterWaveAmp { get; set; } = 0.06f;
	[Export] public float WaterWaveFreq { get; set; } = 1.6f;
	[Export] public float WaterWaveSpeed { get; set; } = 0.35f;
	[Export] public float WaterRoughness { get; set; } = 0.08f;
	[Export] public float WaterFresnelPower { get; set; } = 5.0f;
	[Export] public Color WaterShallowColor { get; set; } = new Color(0.06f, 0.08f, 0.12f);
	[Export] public Color WaterDeepColor { get; set; } = new Color(0.02f, 0.05f, 0.09f);
	[Export] public ulong BaseSeed { get; set; } = 987654321UL;

	private List<Node3D> _treePool = new();
	private DirectionalLight3D _mainLight;
	private PackedScene _treePackedScene;
	private readonly Dictionary<long, AutumnWorldChunk> _active = new();

	public override void _Ready()
	{
		SetupEnvironment();

		_mainLight = new DirectionalLight3D
		{
			LightColor = NightMode ? new Color(0.75f, 0.82f, 1.0f) : new Color(1.0f, 0.95f, 0.85f),
			LightEnergy = NightMode ? 0.85f : 3.0f,
			ShadowEnabled = true
		};
		_mainLight.RotationDegrees = NightMode
			? new Vector3(-15f, 25f, 0f)
			: new Vector3(-25f, 40f, 0f);
		AddChild(_mainLight);

		// Tree pool: use export, then fallback if needed
		_treePackedScene = TreeScene;
		if (_treePackedScene == null)
		{
			GD.Print("No TreeScene provided, using built-in generic tree as fallback.");
			_treePackedScene = CreateFallbackTreeScene();
		}

		_treePool.Clear();
		for (int i = 0; i < NumVisibleTrees; i++)
		{
			var tree = _treePackedScene.Instantiate<Node3D>();
			tree.Visible = true;
			AddChild(tree);
			_treePool.Add(tree);
		}
	}

	public override void _Process(double delta)
	{
		RefreshChunks();
		UpdateTreePool();
	}

	private void UpdateTreePool()
	{
		var camera = GetViewport()?.GetCamera3D();
		if (camera == null)
			return;

		Vector3 camPos = camera.GlobalPosition;
		float camYaw = camera.GlobalTransform.Basis.GetEuler().Y;
		float fov = Mathf.DegToRad(camera.Fov);

		for (int i = 0; i < NumVisibleTrees; i++)
		{
			float t = (i + 1) / (float)(NumVisibleTrees + 1);
			float angle = camYaw - fov / 2f + fov * t;
			Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * TreeArcRadius;
			Vector3 pos = camPos + offset + new Vector3(0, 1, 0);
			_treePool[i].GlobalPosition = pos;
			_treePool[i].Visible = true;
		}
	}

	// Fallback: generates a simple tree if none provided
	private PackedScene CreateFallbackTreeScene()
	{
		var root = new Node3D();

		var trunk = new MeshInstance3D();
		trunk.Mesh = new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.2f, Height = 1.0f };
		trunk.MaterialOverride = CreateMaterial(new Color(0.4f, 0.23f, 0.05f));
		trunk.Position = new Vector3(0, 0.5f, 0);

		var leaves = new MeshInstance3D();
		leaves.Mesh = new SphereMesh { Radius = 0.5f, Height = 1.0f };
		leaves.MaterialOverride = CreateMaterial(new Color(0, 0.85f, 0.22f));
		leaves.Position = new Vector3(0, 1.2f, 0);

		root.AddChild(trunk);
		root.AddChild(leaves);

		// Save as a PackedScene for instancing
		var scene = new PackedScene();
		scene.Pack(root);

		return scene;
	}

	private StandardMaterial3D CreateMaterial(Color color)
	{
		var mat = new StandardMaterial3D();
		mat.AlbedoColor = color;
		return mat;
	}

	// THIS IS THE KEY PART: .tscn-based sky loading!
	private void SetupEnvironment()
	{
		var env = new GdEnvironment();

		// If a sky scene is assigned, instance it as a node and extract the Sky resource
		if (SkyScene != null)
		{
			var inst = SkyScene.Instantiate();
			if (inst is WorldEnvironment worldEnv && worldEnv.Environment?.Sky != null)
			{
				env.BackgroundMode = GdEnvironment.BGMode.Sky;
				env.Sky = worldEnv.Environment.Sky;
				env.AmbientLightEnergy = worldEnv.Environment.AmbientLightEnergy;
				env.AmbientLightSkyContribution = worldEnv.Environment.AmbientLightSkyContribution;
				env.FogEnabled = worldEnv.Environment.FogEnabled;
				env.FogLightColor = worldEnv.Environment.FogLightColor;
				env.FogDensity = worldEnv.Environment.FogDensity;
				env.FogSkyAffect = worldEnv.Environment.FogSkyAffect;
				// Set SSR and Glow if desired
				if (EnableSSR)
				{
					env.SsrEnabled = true;
					env.SsrMaxSteps = 128;
				}
				if (EnableGlow && NightMode)
				{
					env.GlowEnabled = true;
					env.GlowIntensity = 0.55f;
				}
			}
			else
			{
				GD.PrintErr("SkyScene provided, but no WorldEnvironment or Sky found inside! Falling back to procedural sky.");
				// Use procedural sky fallback
				env.BackgroundMode = GdEnvironment.BGMode.Sky;
				env.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };
			}
		}
		else
		{
			// No prefab sky, use a blue procedural sky
			env.BackgroundMode = GdEnvironment.BGMode.Sky;
			env.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };
		}

		AddChild(new WorldEnvironment { Environment = env });
	}

	private (int gx, int gz) WorldToGrid(Vector3 worldPos)
	{
		int gx = Mathf.FloorToInt(worldPos.X / ChunkSize);
		int gz = Mathf.FloorToInt(worldPos.Z / ChunkSize);
		return (gx, gz);
	}

	private long PackKey(int gx, int gz) => ((long)gx << 32) ^ (uint)gz;

	private void RefreshChunks(bool forceAll = false)
	{
		Vector3 anchorPosition = GetViewport()?.GetCamera3D()?.GlobalPosition ?? GlobalPosition;
		var (cx, cz) = WorldToGrid(anchorPosition);

		int minX = cx - GridRadius;
		int maxX = cx + GridRadius;
		int minZ = cz - GridRadius;
		int maxZ = cz + GridRadius;

		// Spawn missing chunks
		for (int gz = minZ; gz <= maxZ; gz++)
		{
			for (int gx = minX; gx <= maxX; gx++)
			{
				long key = PackKey(gx, gz);
				if (forceAll || !_active.ContainsKey(key))
					SpawnChunk(gx, gz, key);
			}
		}

		// Despawn out-of-window chunks
		var toRemove = new List<long>();
		foreach (var kv in _active)
		{
			long key = kv.Key;
			int gx = (int)(key >> 32);
			int gz = (int)(key & 0xFFFFFFFF);
			if (gx < minX || gx > maxX || gz < minZ || gz > maxZ)
				toRemove.Add(key);
		}
		foreach (var key in toRemove)
		{
			_active[key].QueueFree();
			_active.Remove(key);
		}
	}

	private void SpawnChunk(int gx, int gz, long key)
	{
		var chunk = new AutumnWorldChunk
		{
			GridX = gx,
			GridZ = gz,
			ChunkSize = ChunkSize,
			RiverWidth = RiverWidth,
			TreesPerSide = TreesPerChunk,
			MinTreeHeight = MinTreeHeight,
			MaxTreeHeight = MaxTreeHeight,
			TreeRadiusScale = TreeRadiusScale,
			AddCanopySecondLobe = AddCanopySecondLobe,
			RiverBuffer = RiverBuffer,
			BridgeEveryNRows = BridgeEveryNRows,
			WaterWaveAmp = WaterWaveAmp,
			WaterWaveFreq = WaterWaveFreq,
			WaterWaveSpeed = WaterWaveSpeed,
			WaterRoughness = WaterRoughness,
			WaterFresnelPower = WaterFresnelPower,
			WaterShallowColor = WaterShallowColor,
			WaterDeepColor = WaterDeepColor,
			Seed = SeedFor(gx, gz, BaseSeed)
		};

		chunk.Position = new Vector3(gx * ChunkSize, 0f, gz * ChunkSize);
		AddChild(chunk);
		_active[key] = chunk;
	}
	private static ulong SeedFor(int gx, int gz, ulong baseSeed)
	{
		ulong x = baseSeed ^ (uint)gx * 0x9E3779B1u ^ ((ulong)(uint)gz << 32);
		return Mix64(x);
	}
	private static ulong Mix64(ulong z)
	{
		z += 0x9E3779B97F4A7C15UL;
		z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
		z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
		return z ^ (z >> 31);
	}
}

//using Godot;
//using System;
//using GdEnvironment = Godot.Environment;
//
//public partial class AutumnWorldStreamer : Node3D
//{
	//// Chunk/grid settings
	//[Export] public float ChunkSize { get; set; } = 120f;
	//[Export] public float RiverWidth { get; set; } = 16f;
	//[Export] public int GridRadius { get; set; } = 2; // (2R+1)^2 chunks active
//
	//// Visual toggles
	//[Export] public bool NightMode { get; set; } = true;
	//[Export] public bool EnableSSR { get; set; } = true;
	//[Export] public bool EnableGlow { get; set; } = true;
//
	//// Forest/water defaults passed to chunks
	//[Export] public int TreesPerChunk { get; set; } = 320;
	//[Export] public float MinTreeHeight { get; set; } = 2.6f;
	//[Export] public float MaxTreeHeight { get; set; } = 5.2f;
	//[Export] public Vector2 TreeRadiusScale { get; set; } = new Vector2(0.8f, 1.35f);
	//[Export] public bool AddCanopySecondLobe { get; set; } = true;
	//[Export] public float RiverBuffer { get; set; } = 1.2f;
	//[Export] public int BridgeEveryNRows { get; set; } = 4;
//
	//[Export] public float WaterWaveAmp { get; set; } = 0.06f;
	//[Export] public float WaterWaveFreq { get; set; } = 1.6f;
	//[Export] public float WaterWaveSpeed { get; set; } = 0.35f;
	//[Export] public float WaterRoughness { get; set; } = 0.08f;
	//[Export] public float WaterFresnelPower { get; set; } = 5.0f;
	//[Export] public Color WaterShallowColor { get; set; } = new Color(0.06f, 0.08f, 0.12f);
	//[Export] public Color WaterDeepColor { get; set; } = new Color(0.02f, 0.05f, 0.09f);
//
	//// Deterministic base seed
	//[Export] public ulong BaseSeed { get; set; } = 987654321UL;
//
	//private DirectionalLight3D _mainLight;
	//private Camera3D _cam; // resolved from Viewport’s current camera
//
	//private readonly System.Collections.Generic.Dictionary<long, AutumnWorldChunk> _active = new();
	//
	//
	//private PackedScene _treePackedScene;
//
	//// ... rest of your class ...
//
	////public override void _Ready()
	////{
		////SetupEnvironment();
		////_treePackedScene = ResourceLoader.Load<PackedScene>("res://res://Textures/tree_small_02_4k.blend/tree_small_02_4k.blend"); // change to .glb if you use that
////
		////_mainLight = new DirectionalLight3D
		////{
			////LightColor = NightMode ? new Color(0.75f, 0.82f, 1.0f) : new Color(1.0f, 0.95f, 0.85f),
			////LightEnergy = NightMode ? 0.85f : 3.0f,
			////ShadowEnabled = true
		////};
		////_mainLight.RotationDegrees = NightMode ? new Vector3(-15f, 25f, 0f) : new Vector3(-25f, 40f, 0f);
		////AddChild(_mainLight);
////
		////_cam = GetViewport()?.GetCamera3D();
	////}
	//// old copy 
	//public override void _Ready()
	//{
		//SetupEnvironment();
//
		//_mainLight = new DirectionalLight3D
		//{
			//LightColor = NightMode ? new Color(0.75f, 0.82f, 1.0f) : new Color(1.0f, 0.95f, 0.85f),
			//LightEnergy = NightMode ? 0.85f : 3.0f,
			//ShadowEnabled = true
		//};
		//_mainLight.RotationDegrees = NightMode ? new Vector3(-15f, 25f, 0f) : new Vector3(-25f, 40f, 0f);
		//AddChild(_mainLight);
//
		//// First pass: try to resolve the current camera (may be null until your Driver adds it)
		//_cam = GetViewport()?.GetCamera3D();
//
		//// Spawn initial grid once a camera exists (done in _Process when _cam is non-null)
	//}
//
	//public override void _Process(double delta)
	//{
		//// Track the current active camera each frame (handles camera switches)
		//var current = GetViewport()?.GetCamera3D();
		//if (current == null)
			//return;
//
		//if (_cam != current)
		//{
			//_cam = current;
			//RefreshChunks(forceAll: true); // camera changed; rebuild window
			//return;
		//}
//
		//RefreshChunks();
	//}
//
	//private void SetupEnvironment()
	//{
		//var env = new GdEnvironment { BackgroundMode = GdEnvironment.BGMode.Sky };
		//env.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };
//
		//try
		//{
			//// Try to load the EXR as a Texture2D:
			//var tex = ResourceLoader.Load<Texture2D>("res://Textures/SunflowersPuresky.exr");
			//if (tex != null)
			//{
				//Type panoType = typeof(Sky).Assembly.GetType("Godot.PanoramaSky");
				//if (panoType != null)
				//{
					//try
					//{
						//// Create the PanoramaSky instance dynamically via reflection
						//var panoObj = Activator.CreateInstance(panoType);
//
						//// Set "Panorama" property via reflection ("Panorama" or "panorama")
						//var prop = panoType.GetProperty("Panorama") ?? panoType.GetProperty("panorama");
						//if (prop != null && prop.CanWrite)
						//{
							//prop.SetValue(panoObj, tex);
//
							//// Assign to env.Sky using reflection (ensures no type errors)
							//var skyProp = env.GetType().GetProperty("Sky");
							//if (skyProp != null && skyProp.CanWrite)
							//{
								//skyProp.SetValue(env, panoObj);
								//GD.Print("Assigned PanoramaSky via reflection from EXR.");
							//}
							//else
							//{
								//GD.PrintErr("Failed to reflectively assign to env.Sky.");
							//}
						//}
						//else
						//{
							//GD.PrintErr("Panorama property not found or not writable in PanoramaSky type.");
						//}
					//}
					//catch (Exception ex)
					//{
						//GD.PrintErr($"Failed to create/assign PanoramaSky at runtime: {ex.Message}");
					//}
				//}
				//else
				//{
					//GD.PrintErr("PanoramaSky type is not present in C# bindings; please use a PanoramaSky .tres in the editor as fallback.");
				//}
//
				//// Optional: Apply HDR tweaks
				//env.AmbientLightEnergy = 0.6f;
				//env.AmbientLightSkyContribution = 1.0f;
				//env.SsrEnabled = true;
				//env.GlowEnabled = true;
				//env.GlowIntensity = 0.6f;
			//}
			//else if (NightMode)
			//{
				//env.AmbientLightColor = new Color(0.08f, 0.10f, 0.14f);
				//env.AmbientLightEnergy = 0.4f;
				//env.AmbientLightSkyContribution = 0.2f;
				//env.FogEnabled = true;
				//env.FogDensity = 0.028f;
				//env.FogLightColor = new Color(0.42f, 0.48f, 0.62f);
				//env.FogSkyAffect = 0.7f;
			//}
			//else
			//{
				//env.AmbientLightColor = new Color(0.8f, 0.8f, 0.8f);
				//env.AmbientLightEnergy = 1.2f;
			//}
		//}
		//catch (Exception e)
		//{
			//GD.PrintErr("SetupEnvironment: failed to load sky texture or configure environment: ", e.Message);
			//if (NightMode)
			//{
				//env.AmbientLightColor = new Color(0.08f, 0.10f, 0.14f);
				//env.AmbientLightEnergy = 0.4f;
				//env.AmbientLightSkyContribution = 0.2f;
				//env.FogEnabled = true;
				//env.FogDensity = 0.028f;
				//env.FogLightColor = new Color(0.42f, 0.48f, 0.62f);
				//env.FogSkyAffect = 0.7f;
			//}
			//else
			//{
				//env.AmbientLightColor = new Color(0.8f, 0.8f, 0.8f);
				//env.AmbientLightEnergy = 1.2f;
			//}
		//}
//
		//if (EnableSSR)
		//{
			//try { env.SsrEnabled = true; env.SsrMaxSteps = 128; } catch { }
		//}
		//if (EnableGlow && NightMode)
		//{
			//try { env.GlowEnabled = true; env.GlowIntensity = 0.55f; } catch { }
		//}
//
		//AddChild(new WorldEnvironment { Environment = env });
	//}
//
	//private (int gx, int gz) WorldToGrid(Vector3 worldPos)
	//{
		//int gx = Mathf.FloorToInt(worldPos.X / ChunkSize);
		//int gz = Mathf.FloorToInt(worldPos.Z / ChunkSize);
		//return (gx, gz);
	//}
//
	//private long PackKey(int gx, int gz) => ((long)gx << 32) ^ (uint)gz;
//
	//private void RefreshChunks(bool forceAll = false)
	//{
		//if (_cam == null) return;
		//var (cx, cz) = WorldToGrid(_cam.GlobalPosition);
//
		//int minX = cx - GridRadius;
		//int maxX = cx + GridRadius;
		//int minZ = cz - GridRadius;
		//int maxZ = cz + GridRadius;
//
		//// Spawn missing
		//for (int gz = minZ; gz <= maxZ; gz++)
		//{
			//for (int gx = minX; gx <= maxX; gx++)
			//{
				//long key = PackKey(gx, gz);
				//if (forceAll || !_active.ContainsKey(key))
					//SpawnChunk(gx, gz, key);
			//}
		//}
//
		//// Despawn out of window
		//var toRemove = new System.Collections.Generic.List<long>();
		//foreach (var kv in _active)
		//{
			//long key = kv.Key;
			//int gx = (int)(key >> 32);
			//int gz = (int)(key & 0xFFFFFFFF);
			//if (gx < minX || gx > maxX || gz < minZ || gz > maxZ)
				//toRemove.Add(key);
		//}
		//foreach (var key in toRemove)
		//{
			//_active[key].QueueFree();
			//_active.Remove(key);
		//}
	//}
	//// old copy 
	//private void SpawnChunk(int gx, int gz, long key)
	//{
		//var chunk = new AutumnWorldChunk
		//{
			//GridX = gx,
			//GridZ = gz,
			//ChunkSize = ChunkSize,
			//RiverWidth = RiverWidth,
//
			//TreesPerSide = TreesPerChunk,
			//MinTreeHeight = MinTreeHeight,
			//MaxTreeHeight = MaxTreeHeight,
			//TreeRadiusScale = TreeRadiusScale,
			//AddCanopySecondLobe = AddCanopySecondLobe,
			//RiverBuffer = RiverBuffer,
			//BridgeEveryNRows = BridgeEveryNRows,
//
			//WaterWaveAmp = WaterWaveAmp,
			//WaterWaveFreq = WaterWaveFreq,
			//WaterWaveSpeed = WaterWaveSpeed,
			//WaterRoughness = WaterRoughness,
			//WaterFresnelPower = WaterFresnelPower,
			//WaterShallowColor = WaterShallowColor,
			//WaterDeepColor = WaterDeepColor,
//
			//Seed = SeedFor(gx, gz, BaseSeed)
		//};
//
		//chunk.Position = new Vector3(gx * ChunkSize, 0f, gz * ChunkSize);
//
		//AddChild(chunk);
		//_active[key] = chunk;
	//}
	//
	////private void SpawnChunk(int gx, int gz, long key)
	////{
		////var chunk = new AutumnWorldChunk
		////{
			////GridX = gx,
			////GridZ = gz,
			////ChunkSize = ChunkSize,
			////RiverWidth = RiverWidth,
////
			////TreesPerSide = TreesPerChunk,
			////MinTreeHeight = MinTreeHeight,
			////MaxTreeHeight = MaxTreeHeight,
			////TreeRadiusScale = TreeRadiusScale,
			////AddCanopySecondLobe = AddCanopySecondLobe,
			////RiverBuffer = RiverBuffer,
			////BridgeEveryNRows = BridgeEveryNRows,
////
			////WaterWaveAmp = WaterWaveAmp,
			////WaterWaveFreq = WaterWaveFreq,
			////WaterWaveSpeed = WaterWaveSpeed,
			////WaterRoughness = WaterRoughness,
			////WaterFresnelPower = WaterFresnelPower,
			////WaterShallowColor = WaterShallowColor,
			////WaterDeepColor = WaterDeepColor,
////
			////Seed = SeedFor(gx, gz, BaseSeed)
		////};
////
		////chunk.Position = new Vector3(gx * ChunkSize, 0f, gz * ChunkSize);
////
		////AddChild(chunk);
		////_active[key] = chunk;
////
		////// --- SPAWN TREES USING TREE MODEL ---
		////if (_treePackedScene != null)
		////{
			////var rand = new RandomNumberGenerator();
			////rand.Seed = (ulong)(gx * 73856093 ^ gz * 19349663); // some variability per chunk
////
			////for (int i = 0; i < TreesPerChunk; i++)
			////{
				////Node3D treeInstance = _treePackedScene.Instantiate<Node3D>();
				////
				////// Random grid within the chunk:
				////float x = (float)rand.RandfRange(0, ChunkSize);
				////float z = (float)rand.RandfRange(0, ChunkSize);
////
				////// Optionally: set a random height/scale
				////var scale = rand.RandfRange(MinTreeHeight, MaxTreeHeight);
				////treeInstance.Scale = new Vector3(scale, scale, scale);
////
				////treeInstance.Position = chunk.Position + new Vector3(x, 0, z);
////
				////AddChild(treeInstance); // If you want trees to be children of this node (not chunk)
				////// chunk.AddChild(treeInstance); // OR add to chunk to keep things organized
			////}
		////}
		////else
		////{
			////GD.PrintErr("Tree PackedScene is null! Check your .blend import path.");
		////}
	////}
//
	//private static ulong SeedFor(int gx, int gz, ulong baseSeed)
	//{
		//ulong x = baseSeed ^ (uint)gx * 0x9E3779B1u ^ ((ulong)(uint)gz << 32);
		//return Mix64(x);
	//}
//
	//private static ulong Mix64(ulong z)
	//{
		//z += 0x9E3779B97F4A7C15UL;
		//z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
		//z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
		//return z ^ (z >> 31);
	//}
//}
