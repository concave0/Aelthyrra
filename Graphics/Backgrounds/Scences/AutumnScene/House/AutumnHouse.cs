using Godot;
using System;
using GArray = Godot.Collections.Array;

/// <summary>
/// Procedural house with a triangular-prism roof. Uses plain C# arrays for the roof mesh
/// (compatible with Godot C# builds that don't expose PackedVector3Array/PackedInt32Array).
/// </summary>
public partial class AutumnHouse : Node3D
{
	// Overall footprint (meters)
	[Export] public float Width { get; set; } = 4.0f;
	[Export] public float Depth { get; set; } = 4.0f;
	[Export] public float Height { get; set; } = 3.2f;

	[Export] public float RoofOverhang { get; set; } = 0.35f;
	[Export] public float RoofPitchDeg { get; set; } = 35f; // degrees

	// Variation
	[Export] public ulong Seed { get; set; } = 0; // 0 => random
	[Export] public float GrayBase { get; set; } = 0.45f; // 0..1 base gray for walls
	[Export] public float GrayVariance { get; set; } = 0.08f; // +/- variance

	// Window/door counts
	[Export] public int WindowCount { get; set; } = 3;

	// Optional small details toggles
	[Export] public bool AddPorch { get; set; } = true;
	[Export] public bool AddChimney { get; set; } = true;

	private RandomNumberGenerator _rng;

	public override void _Ready()
	{
		_rng = new RandomNumberGenerator();
		if (Seed == 0UL)
			_rng.Randomize();
		else
			_rng.Seed = Seed;

		BuildHouse();
	}

	private void BuildHouse()
	{
		// Clear children (safe rebuild)
		foreach (Node child in GetChildren())
			child.QueueFree();

		// Gray palette
		float g = Mathf.Clamp(GrayBase + _rng.RandfRange(-GrayVariance, GrayVariance), 0f, 1f);
		float wallGray = g;
		float trimGray = Mathf.Clamp(g - 0.12f, 0f, 1f);
		float roofGray = Mathf.Clamp(g - 0.35f, 0f, 1f);
		float doorGray = Mathf.Clamp(g - 0.28f, 0f, 1f);
		float stoneGray = Mathf.Clamp(g - 0.45f, 0f, 1f);

		// Materials
		var wallMat = new StandardMaterial3D { AlbedoColor = new Color(wallGray, wallGray, wallGray), Roughness = 0.88f };
		var trimMat = new StandardMaterial3D { AlbedoColor = new Color(trimGray, trimGray, trimGray), Roughness = 0.85f };
		var roofMat = new StandardMaterial3D { AlbedoColor = new Color(roofGray, roofGray, roofGray), Roughness = 1.0f };
		var doorMat = new StandardMaterial3D { AlbedoColor = new Color(doorGray, doorGray, doorGray), Roughness = 0.9f };
		var stoneMat = new StandardMaterial3D { AlbedoColor = new Color(stoneGray, stoneGray, stoneGray), Roughness = 1.0f };
		var glassMat = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.14f, 0.18f), Roughness = 0.35f, Metallic = 0.0f };

		// Foundation (stone base) - a low block under the house
		var foundation = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(Width + 0.2f, 0.3f, Depth + 0.2f) },
			Position = new Vector3(0f, 0.15f, 0f),
			MaterialOverride = stoneMat
		};
		AddChild(foundation);

		// Main body (siding box)
		var body = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(Width, Height, Depth) },
			Position = new Vector3(0f, 0.3f + Height * 0.5f, 0f),
			MaterialOverride = wallMat
		};
		AddChild(body);

		// Trim: corner boards & eaves
		void AddTrimBoard(Vector3 localPos, Vector3 size, Vector3 rotDeg)
		{
			var tb = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = size },
				Position = localPos,
				RotationDegrees = rotDeg,
				MaterialOverride = trimMat
			};
			AddChild(tb);
		}

		float trimWidth = 0.08f;
		AddTrimBoard(new Vector3(-Width * 0.5f - trimWidth * 0.5f, 0.3f + Height * 0.5f, 0f), new Vector3(trimWidth, Height + 0.2f, 0.12f), Vector3.Zero);
		AddTrimBoard(new Vector3(Width * 0.5f + trimWidth * 0.5f, 0.3f + Height * 0.5f, 0f), new Vector3(trimWidth, Height + 0.2f, 0.12f), Vector3.Zero);

		// Horizontal band under roof (eaves trim)
		var eaves = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(Width + 0.02f, 0.2f, Depth + 0.02f) },
			Position = new Vector3(0f, 0.3f + Height - 0.08f, 0f),
			MaterialOverride = trimMat
		};
		AddChild(eaves);

		// ---------- ROOF (triangular prism) ----------
		float pitchRad = Mathf.DegToRad(RoofPitchDeg);
		float halfSpan = Width * 0.5f + RoofOverhang;
		float roofRise = Mathf.Tan(pitchRad) * halfSpan;
		float eaveY = 0.3f + Height;      // eave sits at top of the body (+foundation offset)
		float ridgeY = eaveY + roofRise;  // ridge height

		float halfDepth = Depth * 0.5f + RoofOverhang;

		// Build triangular prism roof mesh (uses plain arrays)
		var roofMesh = BuildRoofPrismMesh(halfSpan, roofRise, halfDepth, eaveY);
		var roofInstance = new MeshInstance3D { Mesh = roofMesh, MaterialOverride = roofMat };
		AddChild(roofInstance);

		// Small ridge beam
		var ridge = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.06f, Height = (halfDepth * 2f) + 0.12f, RadialSegments = 12 },
			Position = new Vector3(0f, ridgeY + 0.02f, 0f),
			RotationDegrees = new Vector3(90f, 0f, 0f),
			MaterialOverride = trimMat
		};
		AddChild(ridge);

		// Chimney (optional)
		if (AddChimney && _rng.Randf() < 0.7f)
		{
			var chimney = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.36f, 0.9f, 0.36f) },
				Position = new Vector3(Width * 0.18f, ridgeY - roofRise * 0.25f, Depth * 0.15f),
				MaterialOverride = stoneMat
			};
			AddChild(chimney);
		}

		// Door with step
		var doorWidth = Width * 0.32f;
		var doorHeight = Height * 0.52f;
		var door = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(doorWidth, doorHeight, 0.12f) },
			Position = new Vector3(0f, 0.3f + doorHeight * 0.5f, -(Depth * 0.5f + 0.06f)),
			MaterialOverride = doorMat
		};
		AddChild(door);

		// Door knob (small sphere)
		var knob = new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = 0.05f, RadialSegments = 12, Rings = 8 },
			Position = new Vector3(doorWidth * 0.3f, 0.3f + doorHeight * 0.5f, -(Depth * 0.5f + 0.02f)),
			MaterialOverride = trimMat
		};
		AddChild(knob);

		// Step
		var step = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(doorWidth + 0.2f, 0.12f, 0.5f) },
			Position = new Vector3(0f, 0.06f, -(Depth * 0.5f + 0.25f)),
			MaterialOverride = trimMat
		};
		AddChild(step);

		// Porch (optional)
		if (AddPorch)
		{
			var porch = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(Width * 0.6f, 0.12f, 0.8f) },
				Position = new Vector3(0f, 0.06f, -(Depth * 0.5f + 0.8f * 0.5f)),
				MaterialOverride = trimMat
			};
			AddChild(porch);

			// Porch posts
			int posts = 2;
			for (int i = 0; i < posts; i++)
			{
				float x = (i == 0) ? -Width * 0.25f : Width * 0.25f;
				var post = new MeshInstance3D
				{
					Mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = 0.6f, RadialSegments = 10 },
					Position = new Vector3(x, 0.3f, -(Depth * 0.5f + 0.8f)),
					MaterialOverride = trimMat
				};
				AddChild(post);
			}
		}

		// Windows: framed quads (glass) with a simple mullion (cross)
		int placed = 0;
		for (int i = 0; i < WindowCount && placed < WindowCount; i++)
		{
			// choose wall: front, back, left, right
			int wall = _rng.RandiRange(0, 3);
			float wx = _rng.RandfRange(-Width * 0.32f, Width * 0.32f);
			float wz = _rng.RandfRange(-Depth * 0.32f, Depth * 0.32f);
			float wy = _rng.RandfRange(0.5f + Height * 0.1f, Height * 0.65f);

			Vector3 pos = Vector3.Zero;
			Vector3 rot = Vector3.Zero;
			switch (wall)
			{
				case 0: // front
					pos = new Vector3(wx, 0.3f + wy, -(Depth * 0.5f + 0.01f));
					rot = new Vector3(0f, 180f, 0f);
					break;
				case 1: // back
					pos = new Vector3(wx, 0.3f + wy, (Depth * 0.5f + 0.01f));
					rot = Vector3.Zero;
					break;
				case 2: // left
					pos = new Vector3(-(Width * 0.5f + 0.01f), 0.3f + wy, wz);
					rot = new Vector3(0f, -90f, 0f);
					break;
				default: // right
					pos = new Vector3((Width * 0.5f + 0.01f), 0.3f + wy, wz);
					rot = new Vector3(0f, 90f, 0f);
					break;
			}

			// Frame (thin border)
			var frame = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.62f, 0.62f, 0.08f) },
				Position = pos,
				RotationDegrees = rot,
				MaterialOverride = trimMat
			};
			AddChild(frame);

			// Glass quad inset slightly
			var glass = new MeshInstance3D
			{
				Mesh = new QuadMesh { Size = new Vector2(0.48f, 0.48f) },
				Position = pos + new Vector3(0f, 0f, 0.05f),
				RotationDegrees = rot,
				MaterialOverride = glassMat
			};
			AddChild(glass);

			// Mullion cross
			var mull1 = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.02f, 0.48f, 0.02f) },
				Position = pos + new Vector3(0f, 0f, 0.06f),
				RotationDegrees = rot,
				MaterialOverride = trimMat
			};
			AddChild(mull1);

			var mull2 = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(0.48f, 0.02f, 0.02f) },
				Position = pos + new Vector3(0f, 0f, 0.06f),
				RotationDegrees = rot,
				MaterialOverride = trimMat
			};
			AddChild(mull2);

			placed++;
		}

		// Slight rotation / settlement for charm
		RotationDegrees = new Vector3(_rng.RandfRange(-1.5f, 1.5f), _rng.RandfRange(0f, 360f), _rng.RandfRange(-1.5f, 1.5f));
	}

	// Build a closed triangular prism mesh for the roof using plain C# arrays.
	private ArrayMesh BuildRoofPrismMesh(float halfSpan, float roofRise, float halfDepth, float eaveY)
	{
		// Vertices: front-left, front-right, front-ridge, back-left, back-right, back-ridge
		var v0 = new Vector3(-halfSpan, eaveY, -halfDepth);
		var v1 = new Vector3(halfSpan, eaveY, -halfDepth);
		var v2 = new Vector3(0f, eaveY + roofRise, -halfDepth);
		var v3 = new Vector3(-halfSpan, eaveY, halfDepth);
		var v4 = new Vector3(halfSpan, eaveY, halfDepth);
		var v5 = new Vector3(0f, eaveY + roofRise, halfDepth);

		Vector3[] vertsArr = new Vector3[] { v0, v1, v2, v3, v4, v5 };

		// Indices (triangles)
		int[] idxArr = new int[]
		{
			// front
			0,2,1,
			// back
			3,4,5,
			// left sloped rect -> two tris
			0,3,5,  0,5,2,
			// right sloped rect -> two tris
			1,2,5,  1,5,4,
			// underside (close bottom)
			0,1,4,  0,4,3
		};

		var arrays = new GArray();
		arrays.Resize((int)ArrayMesh.ArrayType.Max);
		// assign plain arrays — Godot will accept C# arrays here
		arrays[(int)ArrayMesh.ArrayType.Vertex] = vertsArr;
		arrays[(int)ArrayMesh.ArrayType.Index] = idxArr;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		return mesh;
	}
}
