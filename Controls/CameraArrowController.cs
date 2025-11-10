using Godot;
using System;

public partial class CameraArrowController : Camera3D
{
	[Export] public float MoveSpeed { get; set; } = 12f;
	[Export] public float SprintMultiplier { get; set; } = 2.0f;
	[Export] public float LookSensitivity { get; set; } = 0.25f; // mouse sensitivity
	[Export] public bool CaptureMouse { get; set; } = true;      // press Esc to toggle
	[Export] public bool DebugInput { get; set; } = false;       // prints when keys pressed

	private Vector2 _mouseDelta = Vector2.Zero;
	private float _yaw;   // around Y
	private float _pitch; // around X

	public override void _Ready()
	{
		EnsureArrowAndWasdActions();

		if (CaptureMouse)
			Input.MouseMode = Input.MouseModeEnum.Captured;

		// Initialize orientation from current transform
		Vector3 euler = Basis.GetEuler(); // Godot 4: returns Vector3
		_yaw = euler.Y;
		_pitch = euler.X;

		// Make sure this camera is active (also do a deferred MakeCurrent to win against late-joining cameras)
		Current = true;
		MakeCurrent();
		CallDeferred(MethodName.MakeCurrent);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion && CaptureMouse)
			_mouseDelta += motion.Relative;

		if (@event.IsActionPressed("ui_cancel")) // Esc
		{
			CaptureMouse = !CaptureMouse;
			Input.MouseMode = CaptureMouse ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Mouse look
		if (CaptureMouse)
		{
			_yaw   -= _mouseDelta.X * LookSensitivity * 0.01f;
			_pitch -= _mouseDelta.Y * LookSensitivity * 0.01f;
			_pitch = Mathf.Clamp(_pitch, -1.3f, 1.3f);
			_mouseDelta = Vector2.Zero;

			Basis = new Basis(Vector3.Up, _yaw) * new Basis(Vector3.Right, _pitch);
		}

		// Arrow/WASD movement (uses ui_* actions)
		Vector3 dir = Vector3.Zero;
		bool up    = Input.IsActionPressed("ui_up");
		bool down  = Input.IsActionPressed("ui_down");
		bool left  = Input.IsActionPressed("ui_left");
		bool right = Input.IsActionPressed("ui_right");

		if (up)    dir += -Basis.Z; // forward
		if (down)  dir +=  Basis.Z; // back
		if (left)  dir += -Basis.X; // left
		if (right) dir +=  Basis.X; // right

		// Optional vertical movement: E up, Q down
		if (Input.IsKeyPressed(Key.E)) dir += Vector3.Up;
		if (Input.IsKeyPressed(Key.Q)) dir += Vector3.Down;

		if (!Input.IsKeyPressed(Key.E) && !Input.IsKeyPressed(Key.Q))
			dir.Y = 0f;

		if (dir.Length() > 0.001f)
			dir = dir.Normalized();

		float speed = MoveSpeed * (Input.IsKeyPressed(Key.Shift) ? SprintMultiplier : 1f);
		Translate(dir * speed * dt);

		if (DebugInput && (up || down || left || right))
			GD.Print($"CameraArrowController: up={up} down={down} left={left} right={right} pos={GlobalPosition}");
	}

	private static void EnsureArrowAndWasdActions()
	{
		// Ensures both Arrow keys (ui_*) and WASD are mapped.
		void Ensure(string action)
		{
			if (!InputMap.HasAction(action))
				InputMap.AddAction(action);
		}

		void AddKey(string action, Key key, bool physical = true)
		{
			var ev = new InputEventKey();
			if (physical) ev.PhysicalKeycode = key;
			else ev.Keycode = key;
			InputMap.ActionAddEvent(action, ev);
		}

		// ui_* actions
		Ensure("ui_up");    Ensure("ui_down");  Ensure("ui_left");  Ensure("ui_right");
		Ensure("ui_cancel");

		// Add both physical and logical for robustness
		AddKey("ui_up",    Key.Up,   physical: true);
		AddKey("ui_down",  Key.Down, physical: true);
		AddKey("ui_left",  Key.Left, physical: true);
		AddKey("ui_right", Key.Right,physical: true);

		// Also map WASD to ui_* (common expectation)
		AddKey("ui_up",    Key.W, physical: true);
		AddKey("ui_down",  Key.S, physical: true);
		AddKey("ui_left",  Key.A, physical: true);
		AddKey("ui_right", Key.D, physical: true);

		// Esc for ui_cancel
		AddKey("ui_cancel", Key.Escape, physical: true);
	}
}
