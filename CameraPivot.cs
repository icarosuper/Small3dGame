using Godot;

namespace Teste;

public partial class CameraPivot : Node3D
{
	[Export] public float Sensitivity = 0.003f;
	[Export] public float MinPitch = -60f;
	[Export] public float MaxPitch = 60f;

	private float _pitch = 0f;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			// rotação horizontal gira o Player, não o pivot
			GetParent<Node3D>().RotateY(-motion.Relative.X * Sensitivity);

			// rotação vertical continua no pivot
			_pitch -= motion.Relative.Y * Sensitivity;
			_pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(MinPitch), Mathf.DegToRad(MaxPitch));
			Rotation = new Vector3(_pitch, 0, 0);
		}

		if (@event is InputEventKey key && key.Keycode == Key.Escape)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}
}
