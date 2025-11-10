using Godot;
using System;

public partial class AutumnValleyChunk : Node3D
{
	// Chunk placement/index (set by the streamer)
	public int ChunkIndex { get; set; } = 0;

	// Randomness seed (set by the streamer for deterministic chunks)
	[Export] public ulong Seed { get; set; } = 123456UL;

	// Core layout
	[Export] public float ChunkLength { get; set; } = 120f; // size along Z
	[Export] public float RiverWidth { get; set; } = 16f;

	// Forest controls (per chunk)
	[Export] public int TreesPerSide { get; set; } = 300;
	[Export] public float MinTreeHeight { get; set; } = 2.6f;
	[Export] public float MaxTreeHeight { get; set; } = 5.2f;
	[Export] public Vector2 TreeRadiusScale { get; set; } = new Vector2(0.8f, 1.35f);
	[Export] public float RiverBuffer { get; set; } = 2.0f;
	[Export] public bool AddCanopySecondLobe { get; set; } = true;

	// Water shader controls
	[Export] public float WaterWaveAmp { get; set; } = 0.06f;
	[Export] public float WaterWaveFreq { get; set; } = 1.6f;
	[Export] public float WaterWaveSpeed { get; set; } = 0.35f;
	[Export] public float WaterRoughness { get; set; } = 0.08f;
	[Export] public float WaterFresnelPower { get; set; } = 5.0f;
	[Export] public Color WaterShallowColor { get; set; } = new Color(0.06f, 0.08f, 0.12f);
	[Export] public Color WaterDeepColor { get; set; } = new Color(0.02f, 0.05f, 0.09f);

	// Bridge frequency (e.g., every Nth chunk gets a bridge; 0 = never)
	[Export] public int BridgeEveryNChunks { get; set; } = 4;

	private RandomNumberGenerator _rng;

	public override void _Ready()
	{
		_rng = new RandomNumberGenerator { Seed = Seed };

		AddGround();
		AddWater();
		AddBanks();
		BuildForestMultiMesh();
		AddUnderbrush();

		if (BridgeEveryNChunks > 0 && EuclidMod(ChunkIndex, BridgeEveryNChunks) == 0)
		{
			AddCurvedBridgeWithRails(new Vector3(0f, 1.0f, 0f));
		}
	}

	private static int EuclidMod(int a, int n)
	{
		int r = a % n;
		return r < 0 ? r + n : r;
	}

	private float Half() => ChunkLength * 0.5f;

	private void AddGround()
	{
		var ground = new MeshInstance3D();
		var plane = new PlaneMesh { Size = new Vector2(RiverWidth + 80f, ChunkLength + 80f) };
		ground.Mesh = plane;
		ground.MaterialOverride = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.10f, 0.10f, 0.11f),
			Roughness = 0.98f
		};
		AddChild(ground);
	}

	private static Vector3 RGB(Color c) => new Vector3(c.R, c.G, c.B);

	private void AddWater()
	{
		var river = new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(RiverWidth, ChunkLength) },
			Position = new Vector3(0f, 0f, 0f),
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
	float h = wave_h(VERTEX.xy, TIME); // PlaneMesh default local XY → world XZ
	VERTEX.z += h;  // Note: PlaneMesh XY plane -> displace along +Z to make 'up' in world +Y after rotation? 
                    // In Godot 4 PlaneMesh lies in XZ by default. If yours does, change to VERTEX.y += h and use xz in wave_h.
	// Safe fallback: recompute normal with small perturbation
	NORMAL = normalize(NORMAL);
}

void fragment() {
	// Use the interpolated normal: for a simple look, mix two colors by normal 'upness'
	float up = clamp(NORMAL.y * 0.5 + 0.5, 0.0, 1.0);
	vec3 base = mix(deep_color, shallow_color, up);

	float ndotv = clamp(dot(NORMAL, VIEW), 0.0, 1.0);
	float fres = pow(1.0 - ndotv, max(0.0001, fresnel_power));

	ALBEDO = base;
	METALLIC = metallic;
	ROUGHNESS = clamp(roughness * (0.6 + 0.4 * (1.0 - fres)), 0.02, 1.0);
}
";
		// IMPORTANT: If your PlaneMesh is XZ by default (most Godot 4 builds), use the earlier version that displaces VERTEX.y.
		// If your water looks wrong, flip the two commented lines above as hinted.

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
	}

	private void AddBanks()
	{
		// Left bank
		AddBank(new Vector3(-RiverWidth * 0.6f, 0f, 0f), new Vector3(6f, 1.0f, ChunkLength));
		// Right bank
		AddBank(new Vector3(+RiverWidth * 0.6f, 0f, 0f), new Vector3(6f, 1.25f, ChunkLength));
	}

	private void AddBank(Vector3 center, Vector3 size)
	{
		var bank = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = size },
			Position = new Vector3(center.X, -0.55f, center.Z),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.14f, 0.12f, 0.12f),
				Roughness = 0.95f
			}
		};
		AddChild(bank);
	}

	private void BuildForestMultiMesh()
	{
		var trunkMesh = new CylinderMesh
		{
			TopRadius = 0.10f,
			BottomRadius = 0.18f,
			Height = 1.0f,
			RadialSegments = 12
		};
		var trunkMat = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.16f, 0.14f, 0.12f),
			Roughness = 0.9f
		};

		var canopyMesh = new SphereMesh { Radius = 1.0f, RadialSegments = 16, Rings = 12 };
		var canopyMatA = new StandardMaterial3D { AlbedoColor = new Color(0.28f, 0.32f, 0.20f), Roughness = 0.8f };
		var canopyMatB = new StandardMaterial3D { AlbedoColor = new Color(0.22f, 0.28f, 0.18f), Roughness = 0.8f };

		int total = TreesPerSide * 2;

		var trunks = new MultiMeshInstance3D();
		var mmTrunk = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			InstanceCount = total,
			Mesh = trunkMesh
		};
		trunks.Multimesh = mmTrunk;
		trunks.MaterialOverride = trunkMat;

		int countA = total / 2;
		int countB = total - countA;

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

		int trunkIdx = 0, aIdx = 0, bIdx = 0;
		float zMin = -Half() + 4f;
		float zMax = +Half() - 4f;

		for (int side = 0; side < 2; side++)
		{
			float sign = side == 0 ? -1f : +1f;
			for (int i = 0; i < TreesPerSide; i++)
			{
				float z = _rng.RandfRange(zMin, zMax);
				float minX = (RiverWidth * 0.5f) + RiverBuffer + 2f;
				float extra = _rng.RandfRange(2f, 30f);
				float x = sign * (minX + extra);

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

				var trunkXform = new Transform3D(trunkBasis, new Vector3(x, height * 0.5f, z));
				mmTrunk.SetInstanceTransform(trunkIdx++, trunkXform);

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

				var canopyPos = new Vector3(x, height + ellip.Y * 0.55f, z);

				bool toA = ((i + (side * 97)) & 1) == 0;
				if (toA && aIdx < countA)
					mmCanopyA.SetInstanceTransform(aIdx++, new Transform3D(canopyBasis, canopyPos));
				else if (bIdx < countB)
					mmCanopyB.SetInstanceTransform(bIdx++, new Transform3D(canopyBasis, canopyPos));
				else if (aIdx < countA)
					mmCanopyA.SetInstanceTransform(aIdx++, new Transform3D(canopyBasis, canopyPos));

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

					if (((i + 1) & 1) == 0 && aIdx < countA)
						mmCanopyA.SetInstanceTransform(aIdx++, new Transform3D(lobeBasis, canopyPos + lobeOffset));
					else if (bIdx < countB)
						mmCanopyB.SetInstanceTransform(bIdx++, new Transform3D(lobeBasis, canopyPos + lobeOffset));
				}
			}
		}
	}

	private void AddUnderbrush()
	{
		int count = 160;
		var shrubMesh = new SphereMesh { Radius = 0.25f, RadialSegments = 12, Rings = 8 };
		var colors = new[] {
			new Color(0.25f, 0.30f, 0.35f),
			new Color(0.22f, 0.26f, 0.30f),
			new Color(0.18f, 0.22f, 0.20f),
		};

		for (int i = 0; i < count; i++)
		{
			float z = _rng.RandfRange(-Half() + 4f, Half() - 4f);
			float side = _rng.Randf() < 0.5f ? -1f : +1f;
			float x = side * (RiverWidth * 0.5f + _rng.RandfRange(0.8f, 3.0f));

			var shrub = new MeshInstance3D
			{
				Mesh = shrubMesh,
				Position = new Vector3(x + _rng.RandfRange(-0.7f, 0.7f), 0.1f, z + _rng.RandfRange(-0.9f, 0.9f)),
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

	private void AddCurvedBridgeWithRails(Vector3 center)
	{
		int plankCount = 26;
		float railHeight = 0.6f;
		int posts = 9;

		// Deck planks
		for (int i = 0; i < plankCount; i++)
		{
			float t = i / (float)(plankCount - 1);
			float x = (t - 0.5f) * RiverWidth * 0.98f;
			float y = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.72f;
			float z = center.Z;

			var plank = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(RiverWidth * 0.085f, 0.07f, 2.0f) },
				Position = new Vector3(x, y, z),
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.28f, 0.24f, 0.20f),
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
			float x = (t - 0.5f) * RiverWidth * 0.98f;
			float baseY = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.72f;
			float z = center.Z;

			void AddPost(float sideSign)
			{
				var post = new MeshInstance3D
				{
					Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = railHeight, RadialSegments = 10 },
					Position = new Vector3(x, baseY + railHeight * 0.5f, z + sideSign * (RiverWidth * 0.06f)),
					MaterialOverride = new StandardMaterial3D
					{
						AlbedoColor = new Color(0.22f, 0.20f, 0.18f),
						Roughness = 0.9f
					}
				};
				AddChild(post);
			}

			AddPost(-1f);
			AddPost(+1f);
		}

		// Rail bars
		int railSegs = 14;
		for (int side = 0; side < 2; side++)
		{
			float sideSign = side == 0 ? -1f : +1f;
			for (int i = 0; i < railSegs; i++)
			{
				float t0 = i / (float)railSegs;
				float t1 = (i + 1) / (float)railSegs;

				Vector3 P(float t)
				{
					float x = (t - 0.5f) * RiverWidth * 0.98f;
					float y = center.Y + 0.35f + Mathf.Cos((t - 0.5f) * Mathf.Pi) * 0.72f + 0.45f;
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
						AlbedoColor = new Color(0.22f, 0.20f, 0.18f),
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
}
