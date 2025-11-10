using Godot;
using System;

public partial class AutumnWorldChunk : Node3D
{
	// Grid coordinate (set by the streamer)
	public int GridX { get; set; }
	public int GridZ { get; set; }

	// Deterministic seed per chunk (set by the streamer)
	[Export] public ulong Seed { get; set; } = 123456UL;

	// Chunk/world params
	[Export] public float ChunkSize { get; set; } = 120f; // chunk size along X and Z
	[Export] public float RiverWidth { get; set; } = 16f; // world river band width centered at world X=0

	// Forest controls
	[Export] public int TreesPerSide { get; set; } = 300; // total ~ this value (distributed both sides of river)
	[Export] public float MinTreeHeight { get; set; } = 2.6f;
	[Export] public float MaxTreeHeight { get; set; } = 5.2f;
	[Export] public Vector2 TreeRadiusScale { get; set; } = new Vector2(0.8f, 1.35f);
	[Export] public bool AddCanopySecondLobe { get; set; } = true;

	// How far from the river trees are kept away (to avoid spawning in water)
	[Export] public float RiverBuffer { get; set; } = 1.2f;

	// Water shader controls
	[Export] public float WaterWaveAmp { get; set; } = 0.06f;
	[Export] public float WaterWaveFreq { get; set; } = 1.6f;
	[Export] public float WaterWaveSpeed { get; set; } = 0.35f;
	[Export] public float WaterRoughness { get; set; } = 0.08f;
	[Export] public float WaterFresnelPower { get; set; } = 5.0f;
	[Export] public Color WaterShallowColor { get; set; } = new Color(0.06f, 0.08f, 0.12f);
	[Export] public Color WaterDeepColor { get; set; } = new Color(0.02f, 0.05f, 0.09f);

	// Bridges: place a bridge in river-column chunks every N rows
	[Export] public int BridgeEveryNRows { get; set; } = 4;
	
	[Export] public int HousesPerChunk { get; set; } = 2;   // tune per chunk
	[Export] public float HouseMinDistanceFromRiver { get; set; } = 3.0f;

	private RandomNumberGenerator _rng;
	private bool _housesSpawned = false;

	public override void _Ready()
	{
		_rng = new RandomNumberGenerator { Seed = Seed };

		AddGround();
		AddRiverAndBanksIfIntersecting();
		BuildForestMultiMesh();

		SpawnHouses();

		AddUnderbrush();
		AddBridgeIfNeeded();
	}

	private float Half => ChunkSize * 0.5f;
	private Vector3 WorldCenter => new Vector3(GridX * ChunkSize, 0f, GridZ * ChunkSize);

	private static Vector3 RGB(Color c) => new Vector3(c.R, c.G, c.B);

	private void AddGround()
	{
		var ground = new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(ChunkSize + 2f, ChunkSize + 2f) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.10f, 0.10f, 0.11f),
				Roughness = 0.98f
			}
		};
		AddChild(ground);
	}

	private void AddRiverAndBanksIfIntersecting()
	{
		// Compute where the world river centerline (X=0) falls in this chunk's local space.
		float worldCenterX = WorldCenter.X;
		float localRiverX = -worldCenterX; // so that world X=0 appears at this local X
		// If the river band intersects the chunk bounds in X, draw it.
		bool intersects =
			(localRiverX + RiverWidth * 0.5f) >= -Half &&
			(localRiverX - RiverWidth * 0.5f) <= +Half;

		if (!intersects)
			return;

		// River plane runs along chunk Z, width RiverWidth in X, centered at localRiverX
		var river = new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(RiverWidth, ChunkSize) },
			Position = new Vector3(localRiverX, 0f, 0f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};

		var shader = new Shader();
		shader.Code = @"
		shader_type spatial;
		render_mode cull_disabled, specular_schlick_ggx;

		uniform vec3 deep_color = vec3(0.02, 0.05, 0.09);
		uniform vec3 shallow_color = vec3(0.06, 0.08, 0.12);
		uniform float metallic = 1.0;
		uniform float roughness = 0.08;
		uniform float wave_amp = 0.06;
		uniform float wave_freq = 1.6;
		uniform float wave_speed = 0.35;
		uniform float fresnel_power = 5.0;

		float wave_h(vec2 xz, float t) {
			return sin(xz.x * wave_freq + t * wave_speed) * wave_amp
			     + cos(xz.y * wave_freq * 1.2 + t * wave_speed * 1.3) * wave_amp;
		}

		void vertex() {
			float h = wave_h(VERTEX.xz, TIME);
			VERTEX.y += h;

			float dhdx = wave_amp * wave_freq * cos(VERTEX.x * wave_freq + TIME * wave_speed);
			float dhdz = -wave_amp * wave_freq * 1.2 * sin(VERTEX.z * wave_freq * 1.2 + TIME * wave_speed * 1.3);

			NORMAL = normalize(vec3(-dhdx, 1.0, -dhdz));
		}

		void fragment() {
			float up = clamp(NORMAL.y * 0.5 + 0.5, 0.0, 1.0);
			vec3 base = mix(deep_color, shallow_color, up);

			float ndotv = clamp(dot(NORMAL, VIEW), 0.0, 1.0);
			float fres = pow(1.0 - ndotv, max(0.0001, fresnel_power));

			ALBEDO = base;
			METALLIC = metallic;
			ROUGHNESS = clamp(roughness * (0.6 + 0.4 * (1.0 - fres)), 0.02, 1.0);
		}
		";
		var mat = new ShaderMaterial { Shader = shader };
		mat.SetShaderParameter("deep_color", RGB(WaterDeepColor));
		mat.SetShaderParameter("shallow_color", RGB(WaterShallowColor));
		mat.SetShaderParameter("metallic", 1.0f);
		mat.SetShaderParameter("roughness", WaterRoughness);
		mat.SetShaderParameter("wave_amp", WaterWaveAmp);
		mat.SetShaderParameter("wave_freq", WaterWaveFreq);
		mat.SetShaderParameter("wave_speed", WaterWaveSpeed);
		mat.SetShaderParameter("fresnel_power", WaterFresnelPower);
		river.MaterialOverride = mat;
		AddChild(river);

		// Simple banks: long boxes along Z, left and right of the river
		float bankGap = 0.4f;
		float bankWidth = 6f;

		void AddBank(float sideSign, float height)
		{
			var bank = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(bankWidth, height, ChunkSize) },
				Position = new Vector3(localRiverX + sideSign * (RiverWidth * 0.5f + bankGap + bankWidth * 0.5f), -0.55f, 0f),
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.14f, 0.12f, 0.12f),
					Roughness = 0.95f
				}
			};
			AddChild(bank);
		}

		AddBank(-1f, 1.0f);
		AddBank(+1f, 1.25f);
	}

	private void BuildForestMultiMesh()
	{
		// Prepare trunk and canopy meshes/materials
		var trunkMesh = new CylinderMesh { TopRadius = 0.10f, BottomRadius = 0.18f, Height = 1.0f, RadialSegments = 12 };
		var trunkMat = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.14f, 0.12f), Roughness = 0.9f };

		var canopyMesh = new SphereMesh { Radius = 1.0f, RadialSegments = 16, Rings = 12 };
		var canopyMatA = new StandardMaterial3D { AlbedoColor = new Color(0.28f, 0.32f, 0.20f), Roughness = 0.8f };
		var canopyMatB = new StandardMaterial3D { AlbedoColor = new Color(0.22f, 0.28f, 0.18f), Roughness = 0.8f };

		int totalTrees = TreesPerSide; // per chunk total
		if (totalTrees <= 0) return;

		// MultiMeshes
		var trunks = new MultiMeshInstance3D();
		var mmTrunk = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = totalTrees,
			Mesh = trunkMesh
		};
		trunks.Multimesh = mmTrunk;
		trunks.MaterialOverride = trunkMat;

		int countA = totalTrees / 2;
		int countB = totalTrees - countA;

		var canopiesA = new MultiMeshInstance3D();
		var mmCanopyA = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = countA,
			Mesh = canopyMesh
		};
		canopiesA.Multimesh = mmCanopyA;
		canopiesA.MaterialOverride = canopyMatA;

		var canopiesB = new MultiMeshInstance3D();
		var mmCanopyB = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = countB,
			Mesh = canopyMesh
		};
		canopiesB.Multimesh = mmCanopyB;
		canopiesB.MaterialOverride = canopyMatB;

		AddChild(trunks);
		AddChild(canopiesA);
		AddChild(canopiesB);

		int tIdx = 0, aIdx = 0, bIdx = 0;

		// Distribute random positions inside the chunk bounds (local space [-Half, +Half])
		for (int i = 0; i < totalTrees; i++)
		{
			float lx = _rng.RandfRange(-Half, Half);
			float lz = _rng.RandfRange(-Half, Half);

			// Avoid river band (world X near zero)
			float worldX = WorldCenter.X + lx;
			if (Mathf.Abs(worldX) < (RiverWidth * 0.5f + RiverBuffer))
			{
				// Push it away from water by moving X outward
				lx = (worldX < 0 ? -1f : +1f) * (Mathf.Abs(WorldCenter.X) + (RiverWidth * 0.5f + RiverBuffer) - WorldCenter.X);
				// Clamp local X to chunk bounds
				lx = Mathf.Clamp(lx, -Half + 0.5f, Half - 0.5f);
			}

			float height = _rng.RandfRange(MinTreeHeight, MaxTreeHeight);
			float rScale = _rng.RandfRange(TreeRadiusScale.X, TreeRadiusScale.Y);
			float tiltX = Mathf.DegToRad(_rng.RandfRange(-3f, 3f));
			float tiltZ = Mathf.DegToRad(_rng.RandfRange(-3f, 3f));
			float yaw = Mathf.DegToRad(_rng.RandfRange(-12f, 12f));

			var trunkBasis =
				Basis.Identity
					.Rotated(Vector3.Right, tiltX)
					.Rotated(Vector3.Forward, tiltZ)
					.Rotated(Vector3.Up, yaw)
					.Scaled(new Vector3(rScale, height, rScale));

			var trunkXform = new Transform3D(trunkBasis, new Vector3(lx, height * 0.5f, lz));
			mmTrunk.SetInstanceTransform(tIdx++, trunkXform);

			// Canopy
			float baseR = height * 0.45f * _rng.RandfRange(0.9f, 1.15f);
			var ellip = new Vector3(
				baseR * _rng.RandfRange(0.85f, 1.15f),
				baseR * _rng.RandfRange(1.0f, 1.35f),
				baseR * _rng.RandfRange(0.85f, 1.15f)
			);

			var canopyBasis =
				Basis.Identity
					.Rotated(Vector3.Right, tiltX * 0.5f)
					.Rotated(Vector3.Forward, tiltZ * 0.5f)
					.Rotated(Vector3.Up, yaw)
					.Scaled(ellip);

			var canopyPos = new Vector3(lx, height + ellip.Y * 0.55f, lz);

			bool toA = ((i + (GridX * 31 + GridZ * 17)) & 1) == 0;
			if (toA && aIdx < countA)
				mmCanopyA.SetInstanceTransform(aIdx++, new Transform3D(canopyBasis, canopyPos));
			else if (bIdx < countB)
				mmCanopyB.SetInstanceTransform(bIdx++, new Transform3D(canopyBasis, canopyPos));

			// Optional second lobe
			if (AddCanopySecondLobe)
			{
				var lobeScale = ellip * _rng.RandfRange(0.65f, 0.85f);
				var lobeBasis =
					Basis.Identity
						.Rotated(Vector3.Up, yaw + Mathf.DegToRad(_rng.RandfRange(-10f, 10f)))
						.Scaled(lobeScale);

				Vector3 lobeOffset = new Vector3(
					_rng.RandfRange(-ellip.X * 0.25f, ellip.X * 0.25f),
					_rng.RandfRange(+ellip.Y * 0.1f, +ellip.Y * 0.35f),
					_rng.RandfRange(-ellip.Z * 0.25f, ellip.Z * 0.25f)
				);

				// Use the other canopy set to avoid needing more MultiMeshes
				if (toA && bIdx < countB)
					mmCanopyB.SetInstanceTransform(bIdx++, new Transform3D(lobeBasis, canopyPos + lobeOffset));
				else if (aIdx < countA)
					mmCanopyA.SetInstanceTransform(aIdx++, new Transform3D(lobeBasis, canopyPos + lobeOffset));
			}
		}
	}

	private void AddUnderbrush()
	{
		int count = 120;
		var shrubMesh = new SphereMesh { Radius = 0.25f, RadialSegments = 12, Rings = 8 };
		var colors = new[]
		{
			new Color(0.25f, 0.30f, 0.35f),
			new Color(0.22f, 0.26f, 0.30f),
			new Color(0.18f, 0.22f, 0.20f),
		};

		for (int i = 0; i < count; i++)
		{
			float lx = _rng.RandfRange(-Half, Half);
			float lz = _rng.RandfRange(-Half, Half);

			float worldX = WorldCenter.X + lx;
			if (Mathf.Abs(worldX) < (RiverWidth * 0.5f + RiverBuffer))
				continue; // skip shrubs in water

			var shrub = new MeshInstance3D
			{
				Mesh = shrubMesh,
				Position = new Vector3(lx, 0.1f, lz),
				Scale = Vector3.One * _rng.RandfRange(0.6f, 1.6f),
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = colors[_rng.RandiRange(0, colors.Length - 1)],
					Roughness = 0.95f
				}
			};
			AddChild(shrub);
		}
	}

	private void AddBridgeIfNeeded()
	{
		// Only consider chunks that intersect river
		float worldCenterX = WorldCenter.X;
		bool nearRiver =
			Mathf.Abs(worldCenterX) <= (ChunkSize * 0.5f + RiverWidth * 0.5f);
		if (!nearRiver || BridgeEveryNRows <= 0)
			return;

		// Place a bridge every N rows in the river column(s):
		// Any chunk whose river crosses near local X and whose GridZ % N == 0
		if (Mathf.Abs(worldCenterX) <= (RiverWidth * 0.25f) && EuclidMod(GridZ, BridgeEveryNRows) == 0)
		{
			float localRiverX = -worldCenterX;
			AddCurvedBridgeWithRails(new Vector3(localRiverX, 1.0f, 0f));
		}
	}

	private static int EuclidMod(int a, int n)
	{
		int r = a % n;
		return r < 0 ? r + n : r;
	}

	private void AddCurvedBridgeWithRails(Vector3 center)
	{
		int plankCount = 24;
		float railHeight = 0.6f;
		int posts = 8;

		// Deck planks
		for (int i = 0; i < plankCount; i++)
		{
			float t = i / (float)(plankCount - 1);
			float x = center.X + (t - 0.5f) * RiverWidth * 0.95f;
			float y = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.65f;
			float z = center.Z;

			var plank = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(RiverWidth * 0.09f, 0.07f, 1.9f) },
				Position = new Vector3(x, y, z),
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.28f, 0.24f, 0.2f),
					Roughness = 0.9f
				}
			};
			plank.RotationDegrees = new Vector3(0f, 0f, (t - 0.5f) * -12f);
			AddChild(plank);
		}

		// Rail posts
		for (int i = 0; i < posts; i++)
		{
			float t = i / (float)(posts - 1);
			float x = center.X + (t - 0.5f) * RiverWidth * 0.95f;
			float baseY = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.65f;
			float z = center.Z;

			void AddPost(float sideSign)
			{
				var post = new MeshInstance3D
				{
					Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = railHeight, RadialSegments = 10 },
					Position = new Vector3(x, baseY + railHeight * 0.5f, z + sideSign * (RiverWidth * 0.06f)),
					MaterialOverride = new StandardMaterial3D
					{
						AlbedoColor = new Color(0.22f, 0.2f, 0.18f),
						Roughness = 0.9f
					}
				};
				AddChild(post);
			}

			AddPost(-1f);
			AddPost(+1f);
		}

		// Rail bars
		int railSegs = 12;
		for (int side = 0; side < 2; side++)
		{
			float sideSign = side == 0 ? -1f : +1f;
			for (int i = 0; i < railSegs; i++)
			{
				float t0 = i / (float)railSegs;
				float t1 = (i + 1) / (float)railSegs;

				Vector3 P(float t)
				{
					float x = center.X + (t - 0.5f) * RiverWidth * 0.95f;
					float y = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.65f + 0.45f;
					float z = center.Z + sideSign * (RiverWidth * 0.06f);
					return new Vector3(x, y, z);
				}

				var p0 = P(t0);
				var p1 = P(t1);
				var seg = p1 - p0;
				float len = seg.Length();

				var rail = new MeshInstance3D
				{
					Mesh = new BoxMesh { Size = new Vector3(len, 0.05f, 0.05f) },
					Position = (p0 + p1) * 0.5f,
					MaterialOverride = new StandardMaterial3D
					{
						AlbedoColor = new Color(0.22f, 0.2f, 0.18f),
						Roughness = 0.9f
					}
				};

				var dir = seg.Normalized();
				var basis = Basis.LookingAt(dir, Vector3.Up);
				rail.Transform = new Transform3D(basis, rail.Position);
				AddChild(rail);
			}
		}
	}
	
	public void SpawnHouses()
	{
		if (_housesSpawned) return;
		_housesSpawned = true;

		// Place a few houses randomly inside the chunk, avoiding river band
		for (int i = 0; i < HousesPerChunk; i++)
		{
			// Local (chunk) coordinates in [-Half, +Half]
			float lx = _rng.RandfRange(-Half + 2.0f, Half - 2.0f);
			float lz = _rng.RandfRange(-Half + 2.0f, Half - 2.0f);

			// Convert to world X to check river avoidance (river assumed near world X == 0)
			float worldX = WorldCenter.X + lx;
			if (Mathf.Abs(worldX) < (RiverWidth * 0.5f + HouseMinDistanceFromRiver))
			{
				// push house further away from river centerline
				float push = (RiverWidth * 0.5f + HouseMinDistanceFromRiver) + _rng.RandfRange(0.5f, 6.0f);
				lx = worldX < 0 ? -Half + push : Half - push;
				// clamp back to chunk
				lx = Mathf.Clamp(lx, -Half + 1.0f, Half - 1.0f);
			}

			// Slight variant sizes
			float scale = _rng.RandfRange(0.85f, 1.35f);

			// Build the house
			var house = new AutumnHouse
			{
				Width = 3.0f * scale,
				Depth = 3.2f * scale,
				Height = 2.8f * scale,
				RoofPitchDeg = _rng.RandfRange(25f, 42f),
				RoofOverhang = _rng.RandfRange(0.2f, 0.45f),
				WindowCount = _rng.RandiRange(1, 5),
				Seed = Seed ^ (ulong)((GridX * 73856093) ^ (GridZ * 19349663) ^ (i * 374761393)), // deterministic per chunk/slot
				GrayBase = Mathf.Clamp(0.45f + _rng.RandfRange(-0.15f, 0.12f), 0f, 1f)
			};

			house.Position = new Vector3(lx, 0f, lz);
			house.RotationDegrees = new Vector3(0f, _rng.RandfRange(0f, 360f), 0f);
			AddChild(house);
		}
	}
}
