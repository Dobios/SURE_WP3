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
using System.Collections.Generic;

// Represents the policy window containing all policy choices
// TODO: This requires implementing policies, which is a tricky task that will require a ton of work.
public partial class PolicyWindow : CanvasLayer {

	private ColorRect P;
	private AnimationPlayer AP;

	private List<Button> PolicyButtons;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		P = GetNode<ColorRect>("ColorRect");
		P.GuiInput += _OnPanelGuiInput;
		AP = GetNode<AnimationPlayer>("AnimationPlayer");
	}
	
	public void _PlayAnim(string Anim, bool forward = true) {
		if (forward) {
			AP.Play(Anim);
		} else {
			AP.PlayBackwards(Anim);
		}
			
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
	}

	// ==================== Interaction Callbacks ====================

	// Hides the window if the panel is pressed
	public void _OnPanelGuiInput(InputEvent input) {
		if(input.GetType() == new InputEventMouseButton().GetType())
			Hide();
	} 
}
