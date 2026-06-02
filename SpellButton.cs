using Godot;
using System;

public partial class SpellButton : Button
{
	[Export] public AddView addView;
	[Export] public SpellView spellView;
	public override void _Ready()
	{
		this.Pressed += PressedButton;
	}

	public override void _Process(double delta)
	{
	}
	private void PressedButton()
	{
		spellView.Visible = true;
		addView.Visible = false;
	}
}
