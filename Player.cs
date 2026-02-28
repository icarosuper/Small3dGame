using System;
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
	private const float MovementMaxSpeed = 5f;
	private const float AccelerationMultiplier = .8f;
	private const float DecelerationMultiplier = 1.2f;
	private const float LowStaminaVelocityMultiplier = .4f;
	private const float DamagedVelocityMultiplier = .8f;
	private const double LowStaminaTimerMax = 1;
	private const double DashTimerMax = .3;
	private double _lowStaminaTimer;
	private double _dashTimer;
	private Vector3 _dashDirection;
	
	// Combat
	private const double AttackTimerMax = .5;
	private const double AttackCooldownTimerMax = .5;
	private const double DamagedStateTimerMax = .5;
	private bool _playerIsAttacking;
	private double _attackTimer;
	private bool _playerIsInAttackCooldown;
	private double _attackCooldownTimer;
	private double _damagedStateTimer;
	
	// Nodes
	private StandardMaterial3D _playerMaterial;
	private StandardMaterial3D _swordMaterial;
	private Area3D _swordAttackArea;
	private CollisionShape3D _swordAttackCollision;
	
	// State Machine
	private enum PlayerState { Normal, Damaged, LowStamina, Dash }
	private PlayerState _state = PlayerState.Normal;

	public override void _Ready()
	{
	   GetPlayerMaterial();
	   GetSwordMaterial();
	   GetSwordAttackArea();
	   GetSwordAttackCollision();
	}

	private void GetPlayerMaterial()
	{
		var playerMesh = GetNode<MeshInstance3D>("PlayerMesh");
		_playerMaterial = new StandardMaterial3D();
		playerMesh.SetSurfaceOverrideMaterial(0, _playerMaterial);
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
	   HandleCurrentState(delta);
	   HandleAttack(delta);
	}

	private void HandleCurrentState(double delta)
	{
		switch (_state)
		{
			case PlayerState.Normal:
				HandleNormalState();
				break;
			
			case PlayerState.Damaged:
				HandleDamagedState(delta);
				break;
			
			case PlayerState.LowStamina:
				HandleLowStaminaState(delta);
				break;
			
			case PlayerState.Dash:
				HandleDashState(delta);
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(_state), _state, "_state must be in PlayerState");
		}
	}

	private void ChangeState(PlayerState newState)
	{
		switch (newState)
		{
			case PlayerState.Normal:
				StartNormalState();
				break;
			
			case PlayerState.Damaged:
				StartDamagedState();
				break;
			
			case PlayerState.LowStamina:
				StartLowStaminaState();
				break;
			
			case PlayerState.Dash:
				StartDashState();
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(_state), _state, "_state must be in PlayerState");
		}
	}

	private void HandleNormalState()
	{
		HandleMovement(new MovementParams());
	}

	private void HandleDamagedState(double delta)
	{
	   HandleMovement(new MovementParams{ VelocityModifier = DamagedVelocityMultiplier});
	   
	   _damagedStateTimer -= delta;
	   if (_damagedStateTimer <= 0)
		   ChangeState(PlayerState.Normal);
	}

	private void HandleLowStaminaState(double delta)
	{
	   HandleMovement(new MovementParams { VelocityModifier = LowStaminaVelocityMultiplier});
	   
	   _lowStaminaTimer -= delta;
	   if (_lowStaminaTimer <= 0)
		   ChangeState(PlayerState.Normal);
	}

	private void HandleDashState(double delta)
	{
	   HandleMovement(new MovementParams
	   {
		   Direction = _dashDirection
	   });
	   
	   _dashTimer -= delta;
	   if (_dashTimer <= 0)
		   ChangeState(PlayerState.Normal);
	}

	private void StartNormalState()
	{
		ColorPlayer(Colors.White);
		_state = PlayerState.Normal;
	}

	private void StartDamagedState()
	{
		ColorPlayer(Colors.Purple);
		_damagedStateTimer = DamagedStateTimerMax;
		_state = PlayerState.Damaged;
	}

	private void StartLowStaminaState()
	{
		ColorPlayer(Colors.Brown);
		_lowStaminaTimer = LowStaminaTimerMax;
		_state = PlayerState.LowStamina;
	}

	private void StartDashState()
	{
		ColorPlayer(Colors.Chartreuse);
		_dashTimer = DashTimerMax;
		_dashDirection = GetMovementDirection();
		_state = PlayerState.Dash;
	}

	private void HandleAttack(double delta)
	{
		if (_playerIsAttacking)
			HandleAttackState(delta);
		else if (_playerIsInAttackCooldown)
			HandleAttackCooldownState(delta);
		else if (Input.IsMouseButtonPressed(MouseButton.Left))
			StartAttack();
	}

	private void HandleAttackState(double delta)
	{
		CheckForHit();
		
		_attackTimer -= delta;
		if (_attackTimer <= 0)
		{
			FinishAttack();
			StartAttackCooldown();
		}
	}

	private void HandleAttackCooldownState(double delta)
	{
		_attackCooldownTimer -= delta;
		if (_attackCooldownTimer <= 0)
			FinishAttackCooldown();
	}
	
	private void StartAttack()
	{
		ColorSword(Colors.Red);
		SetSwordCollisionEnabled(true);
		_attackTimer = AttackTimerMax;
		_playerIsAttacking = true;
	}

	private void FinishAttack()
	{
		SetSwordCollisionEnabled(false);
		_playerIsAttacking = false;
	}

	private void StartAttackCooldown()
	{
		ColorSword(Colors.Yellow);
		_attackCooldownTimer = AttackCooldownTimerMax;
		_playerIsInAttackCooldown = true;
	}

	private void FinishAttackCooldown()
	{
		ColorSword(Colors.White);
		_playerIsInAttackCooldown = false;
	}

	private void CheckForHit()
	{
		foreach (var body in _swordAttackArea.GetOverlappingBodies())
		{
			var isEnemy = body.IsInGroup("enemies");
			if (isEnemy)
				body.Call("TakeDamage", Damage);
		}
	}

	private void SetSwordCollisionEnabled(bool enabled)
	{
		_swordAttackCollision.Disabled = !enabled;
	}

	private void ColorSword(Color color)
	{
		_swordMaterial.AlbedoColor = color;
	}

	private void ColorPlayer(Color color)
	{
		_playerMaterial.AlbedoColor = color;
	}

	private record MovementParams
	{
		public float VelocityModifier { get; init; } = 1f;
		public float AccelerationModifier { get; init; } = 1f;
		public Vector3? Direction { get; init; }
	}

	private void HandleMovement(MovementParams movementParams)
	{
		var velocity = Velocity;
		
		var direction = movementParams.Direction ?? GetMovementDirection();

		if (direction != Vector3.Zero)
		{
			var newVelocityX = velocity.X + direction.X * AccelerationMultiplier;
			var newVelocityZ = velocity.Z + direction.Z * AccelerationMultiplier;
			
			newVelocityX *= movementParams.VelocityModifier;
			newVelocityZ *= movementParams.VelocityModifier;

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

	private Vector3 GetMovementDirection()
	{
		var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		var forward = -GlobalTransform.Basis.Z;
		var right = GlobalTransform.Basis.X;

		return right * input.X + forward * -input.Y;
	}

	private void TakeDamage(int damage)
	{
		if (_state is PlayerState.Damaged or PlayerState.Dash)
			return;

		_health -= damage;
		GD.Print("Player took damage. Health: " + _health);
		if (_health <= 0)
			GameOver();
		
		ChangeState(PlayerState.Damaged);
	}

	private static void GameOver()
	{
		GD.Print("Game Over");
	}
}
