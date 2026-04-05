using Godot;
using System;

public partial class TerrainArea : Node3D
{
	public enum TerrainType
	{
		Grass,
		Sand,
		Water
	}
	[Export] public TerrainType Type { get; private set; } = TerrainType.Grass;
	private Area3D _triggerArea;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_triggerArea = GetNode<Area3D>("Area3D");
		_triggerArea.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player)
		{
			player.ShoutOutTerrainType(Type);
		}
		//GD.Print("Body entered terrain area: " + body.Name + " on terrain type: " + Type);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
