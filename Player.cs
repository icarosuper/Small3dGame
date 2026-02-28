using Godot;

namespace Teste;

public partial class Player : CharacterBody3D
{
	// Health
	private const int Damage = 1;
	private const int MaxHealth = 3;
	private const int MaxStamina = 3;
	private int _health = MaxHealth;
	private int _stamina = MaxStamina;
	
	// Movement
	private const float AccelerationMultiplier = 0.8f;
	private const float DecelerationMultiplier = 1.2f;
	private const float MovementMaxSpeed = 5f;
	
	// Combat
	private bool _playerIsAttacking;
	private double _attackTimer;
	private const double AttackDuration = .5f;
	private const double GracePeriodTimerMax = .5;
	private double _gracePeriodTimer;
	private bool _isInGracePeriod;
	
	// Nodes
	private StandardMaterial3D _swordMaterial;
	private Area3D _swordAttackArea;
	private CollisionShape3D _swordAttackCollision;
	
	// State Machine
	private enum PlayerState { Normal, Attacking, Damaged, LowStamina, Dash }
	private PlayerState _state = PlayerState.Normal;

	public override void _Ready()
	{
	   GetSwordMaterial();
	   GetSwordAttackArea();
	   GetSwordAttackCollision();
	}

	private void GetSwordMaterial()
	{
		var swordMesh = GetNode<MeshInstance3D>("Sword/SwordMesh");
		_swordMaterial = new StandardMaterial3D();
		swordMesh.SetSurfaceOverrideMaterial(0, _swordMaterial);
	}

	private void GetSwordAttackArea()
	{
		_swordAttackArea = GetNode<Area3D>("Sword/SwordAttackArea");
	}

	private void GetSwordAttackCollision()
	{
		_swordAttackCollision = GetNode<CollisionShape3D>("Sword/SwordAttackArea/SwordAttackCollision");
	}

	public override void _PhysicsProcess(double delta)
	{
	   CheckMovement();
	   CheckAttack(delta);
	   
	   if (_isInGracePeriod)
	   {
		   _gracePeriodTimer -= delta;
		   if (_gracePeriodTimer <= 0)
			   _isInGracePeriod = false;
	   }
	}

	private void CheckAttack(double delta)
	{
	   if (_playerIsAttacking)
	   {
		   ContinueAttack(delta);
		   CheckForHit();
	   }
	   else if (Input.IsMouseButtonPressed(MouseButton.Left))
		  BeginAttack();
	   
	   ColorSword();
	}

	private void CheckForHit()
	{
		foreach (var body in _swordAttackArea.GetOverlappingBodies())
		{
			var isEnemy = body.IsInGroup("enemies");
			if (isEnemy && _playerIsAttacking)
				body.Call("TakeDamage", Damage);
		}
	}

	private void ContinueAttack(double delta)
	{
	   _attackTimer -= delta;
	   
	   if (_attackTimer <= 0)
	   {
		   _playerIsAttacking = false;
		   SetSwordCollisionEnabled(false);
	   }
	}

	private void BeginAttack()
	{
	   _playerIsAttacking = true;
	   _attackTimer = AttackDuration;
	   
	   SetSwordCollisionEnabled(true);
	}

	private void SetSwordCollisionEnabled(bool enabled)
	{
		_swordAttackCollision.Disabled = !enabled;
	}

	private void ColorSword()
	{
	   _swordMaterial.AlbedoColor = _playerIsAttacking ? Colors.Red : Colors.Green;
	}

	private void CheckMovement()
	{
		var velocity = Velocity;

		var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		var forward = -GlobalTransform.Basis.Z;
		var right = GlobalTransform.Basis.X;

		var direction = (right * input.X + forward * -input.Y);

		if (direction != Vector3.Zero)
		{
			var newVelocityX = velocity.X + direction.X * AccelerationMultiplier;
			var newVelocityZ = velocity.Z + direction.Z * AccelerationMultiplier;

			if (Mathf.Abs(newVelocityX) < MovementMaxSpeed) velocity.X = newVelocityX;
			if (Mathf.Abs(newVelocityZ) < MovementMaxSpeed) velocity.Z = newVelocityZ;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, AccelerationMultiplier * DecelerationMultiplier);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, AccelerationMultiplier * DecelerationMultiplier);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void TakeDamage(int damage)
	{
		if (_isInGracePeriod)
			return;

		_health -= damage;
		GD.Print("Player took damage. Health: " + _health);
		if (_health <= 0)
			GameOver();
		
		// TODO: Color player

		_isInGracePeriod = true;
		_gracePeriodTimer = GracePeriodTimerMax;
	}

	private static void GameOver()
	{
		GD.Print("Game Over");
	}
}
