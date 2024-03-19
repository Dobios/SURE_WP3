/**
	Sustainable Energy Development game modeling the Swiss energy Grid.
	Copyright (C) 2023 Università della Svizzera Italiana

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using Godot;
using System;
using System.Diagnostics;

// Represents a resource bar, specifically used for the environment status and the support status
public partial class InfoBar : ProgressBar {

	// Slider initial positions
	private const int SLIDER_BEG_X = -330;
	private const int SLIDER_END_X = -139;
	private int SLIDER_RANGE = Math.Abs(SLIDER_BEG_X - SLIDER_END_X);

	// Line showing the target amount to reach
	private Line2D Target;

	// Label containing the name of the bar
	private Label BarName;
	
	private Button BarButton;
	
	private Sprite2D Arrow;

	// Info boc showing all of the relevant subfields of this resource
	public InfoBox Box;
	
	// Energy bar check
	[Export]
	private bool EnergyBar = true;
	
	// Colors for Bars
	[Export]
	private StyleBoxTexture NormalColor;
	[Export]
	private StyleBoxTexture LowColor;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch nodes
		Target = GetNode<Line2D>("Target");
		BarName = GetNode<Label>("Name");
		Box = GetNode<InfoBox>("BarInfo");
		BarButton = GetNode<Button>("Button");
		
		NormalColor = ResourceLoader.Load("res://scenes/hud/energy_bar_green.tres") as StyleBoxTexture;
		LowColor = ResourceLoader.Load("res://scenes/hud/energy_bar_low.tres") as StyleBoxTexture;

		

		Box.Hide();
	}

	// ==================== InfoBar Update API ====================

	// Updates the progress value of this information bar
	public void _UpdateProgress(int v) {
		// Sanity Check
		Debug.Assert(MinValue <= v && v <= MaxValue);

		// Update the progress bar's value
		//Value = v;
		// Animates the value going to v in n seconds
		if (!EnergyBar) {
			v = (int)Mathf.Remap(v, 0, 100, -70, 70);
			Sprite2D Arrow = GetNode<Sprite2D>("Arrow");
			Tween tween = CreateTween();
			tween.TweenProperty(Arrow, "rotation_degrees", v, 0.5f);
		} else {
			Tween tween = CreateTween();
			tween.TweenProperty(this, "value", v, 0.8f);
		}
		

	}

	// Updates the position of the slider based on a given value
	// The given value is a percentage in [0, 1]
	public void _UpdateSlider(float v) {
		// Sanity check
		v = Math.Max(0.0f, Math.Min(1.0f, v));

		// Update the position such that it matches the given percentage
		Target.Position = new Vector2(
			SLIDER_BEG_X + (SLIDER_RANGE * v),
			Target.Position.Y
		);
	}

	// Updates the bar name (for localization)
	public void _UpdateBarName(string name) {
		BarButton.TooltipText = name;
	}

	// Updates the information of the associated info box 
	// Follows the same calling semantics as the info box:
	// params: varargs in the form of N/Max, T0, N0, T1, N1, T2, N2
	// If parameters are omitted than the label will not be shown
	public void _UpdateInfo(params string[] ts) {
		Box._SetInfo(ts);
	}

	// Displays the information related to this progress bar
	public void _DisplayInfo() {
		Box._ShowOnlyFilled();
	}

	// Hides the information realted to this progress bar
	public void _HideInfo() {
		Box.Hide();
	}
	
	// The bar can have 2 colors, one when it's low and one normal
	public void _UpdateColor(bool IsLow) {
		if (IsLow) {
			this.AddThemeStyleboxOverride("fill", LowColor);
		} else {
			
			this.AddThemeStyleboxOverride("fill", NormalColor);
		}

	}
	
	// Hides the info if the player clicks somewhere else on the map
	public override void _UnhandledInput(InputEvent E) {
		if(E is InputEventMouseButton MouseButton) {
			if(MouseButton.ButtonMask == MouseButtonMask.Left) {
				_HideInfo();
			}
		}
		}
}
