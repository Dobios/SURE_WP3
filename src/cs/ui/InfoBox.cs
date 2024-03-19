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

// Represents the UI box containing additional information about a certain resource
public partial class InfoBox : Control {

	private const int N_LABELS = 5;

	// Various text labels
	private Label Text0;
	private Label Text1;
	private Label Text2;

	// Various N labels
	private Label N0;
	private Label N1;
	private Label N2;
	private Label NOvMax;

	private Label[] labels;
	
	private TextureButton InfoButton;
	private Label InfoText;
	private Label InfoText2;
	private Label InfoN;
	private Label InfoN2;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		labels = new Label[N_LABELS];
		
		// Create label array by fetching nodes
		labels[0] = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/n_ov_max");
		labels[1] = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/Text0");
		labels[2] = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/Text0/n");
		labels[3] = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/Text1");
		labels[4] = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/Text1/n1");
		//labels[5] = GetNode<Label>("Control/TextContainer/Text2");
		//labels[6] = GetNode<Label>("Control/nContainer/n2");
		
		InfoButton = GetNode<TextureButton>("MoreInfo");
		InfoText = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/InfoText");
		InfoText2 = GetNode<Label>("MarginContainer/MarginContainer/VBoxContainer/InfoText2");
	
		
		InfoButton.Pressed += _OnMoreInfoPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
	}

	// ==================== InfoBox Update API ====================

	// Only display Labels that contain information
	public void _ShowOnlyFilled() {
		// Start by showing the entire box
		Show();

		// Hide all empty labels
		foreach(var l in labels) {
			if(l.Text != "") {
				l.Show();
			} else {
				l.Hide();
			}
		}
	}

	// Fill in the information labels
	// params: varargs in the form of N/Max, T0, N0, T1, N1, T2, N2
	// If parameters are omitted than the label will not be shown
	public void _SetInfo(params string[] ts) {
		int l = ts.Length;
		// Set fields if they exist
		int i = 0;
		for(i = 0; i < l && i < labels.Length; ++i) {
			labels[i].Text = ts[i];
		}
		// Fill in what's left with empty strings
		for(; i < labels.Length; ++i) {
			labels[i].Text = "";
		}
	}
	
	public void _OnMoreInfoPressed() {
		InfoText.Visible = !InfoText.Visible;
		InfoText2.Visible = !InfoText2.Visible;
		labels[0].Visible = !labels[0].Visible;
	}
}
