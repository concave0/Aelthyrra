using Godot;
using System;

public partial class SpiritCharacter : Node3D
{
	[Export]
	public Texture2D SpriteTexture { get; set; } = null;

	[Export]
	public Color SpiritColor { get; set; } = new Color(0.5f, 0.9f, 1.0f, 0.9f);

	[Export]
	public Vector2 Size { get; set; } = new Vector2(1f, 1f);

	[Export]
	public float BobAmplitude { get; set; } = 0.25f;

	[Export]
	public float BobSpeed { get; set; } = 1.5f;
	
	[Export]
	public string PathToSpirit { get; set; } = "";

	[Export]
	public float PulseSpeed { get; set; } = 2.0f;

	[Export]
	public bool FaceCamera { get; set; } = true;

	// If you prefer to create & edit a ShaderMaterial in the editor, assign it here.
	[Export]
	public ShaderMaterial EditorMaterial { get; set; } = null;

	private MeshInstance3D _quad;
	private ShaderMaterial _material;
	private float _bobTime = 0f;
	private float _pulseTime = 0f;

	public override void _Ready()
	{
		// Create quad
		_quad = new MeshInstance3D();
		var quadMesh = new QuadMesh();
		quadMesh.Size = Size;
		_quad.Mesh = quadMesh;

		
		if (EditorMaterial != null)
		{
			_material = (ShaderMaterial)EditorMaterial.Duplicate();
		}
		else
		{
			if (!string.IsNullOrWhiteSpace(PathToSpirit))
			{
				Shader shader = null;
				try
				{
					shader = GD.Load<Shader>(PathToSpirit);
					if (shader == null)
					{
						// GD.Load returned null without throwing — treat this as a load failure
						throw new InvalidOperationException($"GD.Load<Shader> returned null for path '{PathToSpirit}'. Verify the path and that the shader resource is valid.");
					}
				}
				catch (Exception ex)
				{
					// Wrap the exception to provide clearer context to the caller
					throw new InvalidOperationException($"Failed to load shader at '{PathToSpirit}'. See inner exception for details.", ex);
				}

				// If we get here, shader was loaded successfully
				_material = new ShaderMaterial { Shader = shader };
			}
			else
			{
				// Fail fast: neither an EditorMaterial nor a shader path was provided.
				throw new InvalidOperationException(
					"SpiritCharacter: no material available. " +
					"Assign a ShaderMaterial to EditorMaterial, or set PathToSpirit to the shader resource path (e.g. 'res://shaders/spirit.gdshader')."
				);
			}
		}
		
		// Set initial shader params (color, texture)
		_material.SetShaderParameter("albedo_color", SpiritColor);
		_material.SetShaderParameter("emission_energy", 1.2f);
		if (SpriteTexture != null)
			_material.SetShaderParameter("albedo_texture", SpriteTexture);

		_quad.MaterialOverride = _material;
		AddChild(_quad);

		// Optional quick camera helper (only add if no Camera3D exists in the viewport)
		var hasCamera = GetViewport().GetCamera3D() != null;
		if (!hasCamera)
		{
			var cam = new Camera3D();
			AddChild(cam);
			cam.Position = new Vector3(0f, 1.5f, -4f);
			cam.LookAtFromPosition(cam.Position, GlobalPosition, Vector3.Up);
			cam.Current = true;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		_bobTime += dt * BobSpeed;
		float bobY = Mathf.Sin(_bobTime) * BobAmplitude;

		_pulseTime += dt * PulseSpeed;
		float pulse = 0.9f + 0.4f * Mathf.Sin(_pulseTime);

		// Update shader emission param
		_material.SetShaderParameter("emission_energy", pulse);

		_quad.Position = new Vector3(0f, bobY, 0f);

		if (FaceCamera)
		{
			var cam = GetViewport().GetCamera3D();
			if (cam != null)
			{
				_quad.LookAtFromPosition(_quad.GlobalPosition, cam.GlobalPosition, Vector3.Up);
			}
		}
	}
}
