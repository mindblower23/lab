using Godot;
using System;

public partial class WorldEnvironment : Godot.WorldEnvironment
{
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private float _currentGlow = 1.0f;
	private float _targetGlow = 1.0f;
	private float _timeToNextFlicker = 0.0f;

	private const float GlowMin = 0.05f;
	private const float GlowMax = 0.15f;
	private const float MinInterval = 0.04f;
	private const float MaxInterval = 0.14f;
	private const float Smoothing = 0.2f;

	public override void _Ready()
	{
		_rng.Randomize();
		_targetGlow = _currentGlow = 1.0f;
		_timeToNextFlicker = _rng.RandfRange(MinInterval, MaxInterval);
	}

	public override void _Process(double delta)
	{
		var env = Environment;
		if (env == null)
			return;

		_timeToNextFlicker -= (float)delta;
		if (_timeToNextFlicker <= 0.0f)
		{
			_targetGlow = _rng.RandfRange(GlowMin, GlowMax);
			_timeToNextFlicker = _rng.RandfRange(MinInterval, MaxInterval);
		}

		_currentGlow = Mathf.Lerp(_currentGlow, _targetGlow, Smoothing);

		// Use the Environment property in Godot 4 to control global glow intensity
		env.GlowIntensity = _currentGlow;
	}
}
