using System;
using Godot;

namespace Teste;

public partial class Enemy : CharacterBody3D
{
	// Health
	private const int MaxHealth = 2;
	private const int Damage = 1;
	private int _health = MaxHealth;
	
	// Combat
	private const double GracePeriodTimerMax = .5;
	private double _gracePeriodTimer;
	private bool _isInGracePeriod;

	// Nodes
	private Player _player;
	private StandardMaterial3D _enemyMaterial;
	private Area3D _enemyAttackArea;
	private CollisionShape3D _enemyAttackCollision;

	// Walk
	private const double MinWalkTimerMax = 1;
	private const double WalkDirectionModifierTimerMax = .8;
	private const int WalkDirectionModifierRange = 30;
	private const float BaseMovementSpeed = 2f;
	private double _walkDirectionModifierTimer;
	private float _walkDirectionModifier;
	private double _minWalkTimer;

	// Lunge
	private const double LungeCooldownTimerMax = 2;
	private const double LungeTimerMax = 0.3;
	private const double PrepareLungeTimerMax = 0.8;
	private const float LungeMinDistance = 4f;
	private const float LungeSpeed = 14f;
	private double _lungeCooldownTimer;
	private double _lungeTimer;
	private double _prepareLungeTimer;
	private Vector3 _lungeDirection;
	
	// State Machine
	private enum EnemyState { Walking, PrepareLunge, Lunging, LungeCooldown }
	private EnemyState _state = EnemyState.Walking;

	public override void _Ready()
	{
		_player = GetNode<Player>("../Player");
		GetEnemyMaterial();
		_enemyAttackArea = GetNode<Area3D>("EnemyAttackArea");
		_enemyAttackCollision = GetNode<CollisionShape3D>("EnemyAttackArea/EnemyAttackCollision");
	}
	
	private void GetEnemyMaterial()
	{
		var enemyMesh = GetNode<MeshInstance3D>("EnemyMesh");
		_enemyMaterial = new StandardMaterial3D();
		enemyMesh.SetSurfaceOverrideMaterial(0, _enemyMaterial);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isInGracePeriod)
		{
			_gracePeriodTimer -= delta;
			if (_gracePeriodTimer <= 0)
			{
				_isInGracePeriod = false;
				ColorEnemy(Colors.White);
			}
		}
		
		switch (_state)
		{
			case EnemyState.Walking:
				HandleWalkingState(delta);
				break;

			case EnemyState.PrepareLunge:
				HandlePrepareLungeState(delta);
				break;

			case EnemyState.Lunging:
				HandleLungingState(delta);
				break;

			case EnemyState.LungeCooldown:
				HandleLungeCooldownState(delta);
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(_state), _state, "_state must be in EnemyState");
		}
	}

	private void AdvanceState()
	{
		switch (_state)
		{
			case EnemyState.Walking:
				StartPrepareLungeState();
				break;
			
			case EnemyState.PrepareLunge:
				StartLungingState();
				break;
			
			case EnemyState.Lunging:
				StartLungeCooldownState();
				break;
			
			case EnemyState.LungeCooldown:
				StartWalkingState();
				break;
			
			default:
				throw new ArgumentOutOfRangeException(nameof(_state), _state, "_state must be in EnemyState");
		}
	}

	private void StartWalkingState()
	{
		_minWalkTimer = MinWalkTimerMax;
		_state = EnemyState.Walking;
		ColorEnemy(Colors.White);
	}

	private void StartLungeCooldownState()
	{
		_lungeCooldownTimer = LungeCooldownTimerMax;
		_enemyAttackCollision.Disabled = true;
		_state = EnemyState.LungeCooldown;
		ColorEnemy(Colors.Gray);
	}

	private void StartLungingState()
	{
		_lungeDirection = GetPlayerDirection();
		_lungeTimer = LungeTimerMax;
		_enemyAttackCollision.Disabled = false;
		_state = EnemyState.Lunging;
		ColorEnemy(Colors.Red);
	}

	private void StartPrepareLungeState()
	{
		_prepareLungeTimer = PrepareLungeTimerMax;
		_state = EnemyState.PrepareLunge;
		ColorEnemy(Colors.Yellow);
	}

	private void ColorEnemy(Color newColor)
	{
		_enemyMaterial.AlbedoColor = newColor;
	}

	private void HandleLungeCooldownState(double delta)
	{
		_lungeCooldownTimer -= delta;
		if (_lungeCooldownTimer <= 0)
			AdvanceState();
	}

	private void HandleLungingState(double delta)
	{
		Velocity = _lungeDirection * LungeSpeed; // TODO: Trocar por aceleração
		MoveAndSlide();
		
		CheckPlayerHit();

		_lungeTimer -= delta;
		if (_lungeTimer <= 0)
			AdvanceState();
	}

	private void CheckPlayerHit()
	{
		foreach (var body in _enemyAttackArea.GetOverlappingBodies())
		{
			var isPlayer = body.IsInGroup("player");
			if (isPlayer)
				body.Call("TakeDamage", Damage);
		}
	}

	private void HandlePrepareLungeState(double delta)
	{
		// TODO: Terminar de implementar desaceleração
		var velocity = Velocity;
		
		velocity.X = Mathf.MoveToward(velocity.X, 0, 1f);
		velocity.Y = Mathf.MoveToward(velocity.X, 0, 1f);
		
		Velocity = velocity;
		
		MoveAndSlide();
		LookAtPlayer();
		
		_prepareLungeTimer -= delta;
		if (_prepareLungeTimer <= 0)
			AdvanceState();
	}

	private void LookAtPlayer()
	{
		LookAt(GetPlayerDirection(), Vector3.Up);
	}

	private void HandleWalkingState(double delta)
	{
		LookAtPlayer();
		Walk(delta);
		
		_minWalkTimer -= delta;
		if (_minWalkTimer <= 0)
			TryStartLunge();
	}

	private void TryStartLunge()
	{
		var distanceToPlayer = GetPlayerDistance();
		if (distanceToPlayer <= LungeMinDistance)
			AdvanceState();
	}

	private void BeginLunge()
	{
		_prepareLungeTimer = PrepareLungeTimerMax;
		_state = EnemyState.PrepareLunge;
	}

	private void HandleWalkDirectionModifier(double delta)
	{
		if (_walkDirectionModifierTimer <= 0)
		{
			var newRange = GD.RandRange(-WalkDirectionModifierRange, WalkDirectionModifierRange);
			_walkDirectionModifier = Mathf.DegToRad(newRange);
			_walkDirectionModifierTimer = WalkDirectionModifierTimerMax;
		}
		else
		{
			_walkDirectionModifierTimer -= delta;
		}
	}

	private void Walk(double delta)
	{
		HandleWalkDirectionModifier(delta);

		var direction = GetPlayerDirection().Rotated(Vector3.Up, _walkDirectionModifier);
		
		Velocity = direction * BaseMovementSpeed;
		MoveAndSlide();
	}

	private Vector3 GetPlayerDirection()
	{
		return GlobalPosition.DirectionTo(_player.GlobalPosition);
	}

	private float GetPlayerDistance()
	{
		return GlobalPosition.DistanceTo(_player.GlobalPosition);
	}

	private void TakeDamage(int damage)
	{
		if (_isInGracePeriod)
			return;

		_health -= damage;
		GD.Print("Enemy took damage. Health: " + _health);
		if (_health <= 0)
			QueueFree();
		
		ColorEnemy(Colors.Purple);

		_isInGracePeriod = true;
		_gracePeriodTimer = GracePeriodTimerMax;
	}
}
