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

// Models the tutorial that is played at the begining of the game
public partial class Tutorial : CanvasLayer {

	// Text XML constants
	private const string TUTO_FILENAME = "tutorial.xml";
	private const string TUTO_TEXT_GROUP = "tutorial_text";
	private const string INFO_BUBBLE_GROUP = "info_bubble";
	private const int INFO_BUBBLE_START_IDX = 2;

	// TextController reference set by the game loop
	private TextController TC;

	// Context for language updates
	private Context C;
	
	// Continuation button: advances the tutorial
	private Button B;

	// Contains the current tutorial text
	private RichTextLabel L;

	// The index of the current tutorial text
	private int TutoIdx = 0;
	private int MaxTutoIdx = 0;
	
	// Animation player
	private AnimationPlayer AP;

	// Array containing the various info bubbles and their texts
	private List<(NinePatchRect, RichTextLabel)> IBs;

	// ==================== GODOT Method Overrides ====================
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch the text controller and context
		TC = GetNode<TextController>("../TextController");
		C = GetNode<Context>("/root/Context");

		// Fetch other nodes
		B = GetNode<Button>("TutoPopUp/ColorRect/Button");
		AP = GetNode<AnimationPlayer>("AnimationPlayer");
		L = GetNode<RichTextLabel>("TutoPopUp/ColorRect/Text");

		// Build out the info bubble list
		IBs = new () {
			(GetNode<NinePatchRect>("InfoBubble"), GetNode<RichTextLabel>("InfoBubble/ColorRect/Text")),
			(GetNode<NinePatchRect>("InfoBubble2"), GetNode<RichTextLabel>("InfoBubble2/ColorRect/Text")),
			(GetNode<NinePatchRect>("InfoBubble3"), GetNode<RichTextLabel>("InfoBubble3/ColorRect/Text")),
			(GetNode<NinePatchRect>("InfoBubble4"), GetNode<RichTextLabel>("InfoBubble4/ColorRect/Text"))
		};

		// Initialize the tutorial text
		L.Text = TC._GetText(TUTO_FILENAME, TUTO_TEXT_GROUP, TutoIdx.ToString());
		
		// Connect Button Callback
		B.Pressed += _OnButtonPressed;	
		C.UpdateLanguage += _OnLanguageUpdate;

		// Set the max tutorial idx
		MaxTutoIdx = TC._GetNTexts(TUTO_FILENAME, TUTO_TEXT_GROUP);
	}

	// ==================== Interaction Callbacks ====================

	// Reset the tutorial
	public void _Reset() {
		TutoIdx = 0;

		// Hide all bubbles
		foreach(var bb in IBs) {
			bb.Item1.Hide();
		}

		// Initialize the tutorial text
		L.Text = TC._GetText(TUTO_FILENAME, TUTO_TEXT_GROUP, TutoIdx.ToString());
		Show();

	}

	// Updates labels when the language changes
	public void _OnLanguageUpdate() {
		// Update the text on the screen if the tutorial is still shown
		if(TutoIdx < MaxTutoIdx) {
			L.Text = TC._GetText(TUTO_FILENAME, TUTO_TEXT_GROUP, TutoIdx.ToString());
		}
	}

	// When the continuation button is pressed, advance the tutorial
	// If the current tutorial index is out of range, then our tutorial is done
	public void _OnButtonPressed() {
		// Update the tutorial index
		if(++TutoIdx < MaxTutoIdx) {
			// Update the tutorial text
			L.Text = TC._GetText(TUTO_FILENAME, TUTO_TEXT_GROUP, TutoIdx.ToString());

			// Check for info bubbles
			if(TutoIdx >= INFO_BUBBLE_START_IDX) {
				// Compute the info bubble's idx
				int bubble_idx = TutoIdx - INFO_BUBBLE_START_IDX;

				// Set the bubble's text
				SetInfoBubbleText(IBs[bubble_idx].Item2, bubble_idx);

				// Show the bubble and hide all others
				foreach(var bb in IBs) {
					bb.Item1.Hide();
				}
				IBs[bubble_idx].Item1.Show();
			}
		} else {
			// If we are out of range, we can hide the tutorial
			Hide();
		}
	}

	// ==================== Internal Helpers ====================

	// Sets the text of a given info bubble to what's present in the xml file
	// Requires a reference to the infobubble text and it's id
	private void SetInfoBubbleText(RichTextLabel _ibt, int id) {
		// Sanity check: given id is in range
		if(id >= TC._GetNTexts(TUTO_FILENAME, INFO_BUBBLE_GROUP)) {
			throw new ArgumentException("Given ID is out of range!!");
		}

		// Fetch the text and set in the bubble's label
		_ibt.Text = TC._GetText(TUTO_FILENAME, INFO_BUBBLE_GROUP, id.ToString());
	}
}
