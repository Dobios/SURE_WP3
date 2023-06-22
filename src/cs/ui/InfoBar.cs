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

	// Line showing the target amount to reach
	private Line2D Target;

	// Label containing the name of the bar
	private Label BarName;

	// Info boc showing all of the relevant subfields of this resource
	private InfoBox Box;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch nodes
		Target = GetNode<Line2D>("Target");
		BarName = GetNode<Label>("Name");
		Box = GetNode<InfoBox>("Name/BarInfo");

		Box.Hide();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
	}

	// ==================== InfoBar Update API ====================

	// Updates the progress value of this information bar
	public void _UpdateProgress(int v) {
		// Sanity Check
		Debug.Assert(MinValue <= v && v <= MaxValue);

		// Update the progress bar's value
		Value = v;
	}

	// Updates the bar name (for localization)
	public void _UpdateBarName(string name) {
		BarName.Text = name;
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
}