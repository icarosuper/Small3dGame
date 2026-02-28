using System;
using System.Collections.Generic;
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
	private const float LowStaminaMaxSpeed = 2f;
	private const float DamagedMaxSpeed = 4;
	private const float DashMaxSpeed = 12f;
	private const float AccelerationMultiplier = 1f;
	private const float DecelerationMultiplier = 1.5f;
	private const float DashAccelerationMultiplier = 5f;
	private const double LowStaminaTimerMax = 1;
	private const double DashTimerMax = .2;
	private const double DashCooldownTimerMax = 1;
	private double _lowStaminaTimer;
	private double _dashTimer;
	private double _dashCooldownTimer;
	private Vector3 _dashDirection;
	
	// Combat
	private const double AttackTimerMax = .5;
	private const double AttackCooldownTimerMax = .8;
	private const double DamagedStateTimerMax = 1;
	private bool _playerIsAttacking;
	private double _attackTimer;
	private bool _playerIsInAttackCooldown;
	private double _attackCooldownTimer;
	private double _damagedStateTimer;
	private List<ulong> _bodiesCheckedForDamage = [];
	
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
				HandleNormalState(delta);
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

	private void HandleNormalState(double delta)
	{
		HandleMovement();

		if (_dashCooldownTimer > 0)
		{
			_dashCooldownTimer -= delta;
			return;
		}
		
		var shiftPressed = Input.IsKeyPressed(Key.Shift);
		if (shiftPressed)
			ChangeState(PlayerState.Dash);
	}

	private void HandleDamagedState(double delta)
	{
	   HandleMovement(DamagedMaxSpeed);
	   
	   _damagedStateTimer -= delta;
	   if (_damagedStateTimer <= 0)
		   ChangeState(PlayerState.Normal);
	}

	private void HandleLowStaminaState(double delta)
	{
	   HandleMovement(LowStaminaMaxSpeed);
	   
	   _lowStaminaTimer -= delta;
	   if (_lowStaminaTimer <= 0)
		   ChangeState(PlayerState.Normal);
	}

	private void HandleDashState(double delta)
	{
	   HandleDash();
	   
	   _dashTimer -= delta;
	   if (_dashTimer <= 0)
	   {
		   _dashCooldownTimer = DashCooldownTimerMax;
		   ChangeState(PlayerState.Normal);
	   }
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

	private void HandleDash()
	{
		var velocity = Velocity;
		velocity.X = Mathf.MoveToward(velocity.X, _dashDirection.X * DashMaxSpeed, DashAccelerationMultiplier);
		velocity.Z = Mathf.MoveToward(velocity.Z, _dashDirection.Z * DashMaxSpeed, DashAccelerationMultiplier);
		Velocity = velocity;
		MoveAndSlide();
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
		_bodiesCheckedForDamage = [];
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
			var id = body.GetInstanceId();
			
			// This prevents any entity being hit more than once in a single attack
			if (_bodiesCheckedForDamage.Contains(id)) continue;
			_bodiesCheckedForDamage.Add(id);
			
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

	private void HandleMovement(float maxSpeed = MovementMaxSpeed)
	{
		var velocity = Velocity;
		var direction = GetMovementDirection();

		var playerIsAccelerating = direction != Vector3.Zero;
		if (playerIsAccelerating)
		{
			velocity.X = Mathf.MoveToward(velocity.X, direction.X * maxSpeed, AccelerationMultiplier);
			velocity.Z = Mathf.MoveToward(velocity.Z, direction.Z * maxSpeed, AccelerationMultiplier);
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
