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
using System.Diagnostics;
using System.Linq;

// Represents the generic window that will contain info about the current shock
// These appear at the end of each turn and are described in shocks.xml
public partial class Shock : CanvasLayer {

	[Signal]
	// Signals that one of the reactions has been selected
	public delegate void SelectReactionEventHandler(int id);

	[Signal]
	// Signals that a reward must be applied
	public delegate void ApplyRewardEventHandler();

	[Signal]
	public delegate void ReintroduceNuclearEventHandler()
;
	[Export]
	// Probability of getting a shock
	public int ShockProba = 70;

	// Big list of shock ids
	private List<string> SHOCKS;

	// The currently displayed shock's ID
	private string CurShock;

	// Various labels used in the shock
	private Label Title;
	private Label Text;
	private Label Result;
	private Label Reward;
	private Sprite2D Img;
	public AnimationPlayer AP;
	
	// Control node containing the reactions
	private Control Reactions;

	// Buttons for each potential reaction
	private Button R1;
	private Button R2;
	private Button R3;

	// Continue button to pass if no reaction is available
	public Button Continue;

	// Controller used to access the config files
	private ShockController SC;

	// Store the current requirements, rewards and possible reactions
	private List<Requirement> CurRequirements;
	private Reward CurReward;
	private List<Reward> CurReactions;

	// Context for persistent states
	private Context C;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch nodes
		Title = GetNode<Label>("NinePatchRect/ColorRect/Title");
		Text = GetNode<Label>("NinePatchRect/ColorRect/Text");
		Result = GetNode<Label>("NinePatchRect/ColorRect/Result");
		Reward = GetNode<Label>("NinePatchRect/ColorRect/Reward");
		Reactions = GetNode<Control>("NinePatchRect/ColorRect/Reactions");
		R1 = GetNode<Button>("NinePatchRect/ColorRect/Reactions/Button");
		R2 = GetNode<Button>("NinePatchRect/ColorRect/Reactions/Button2");
		R3 = GetNode<Button>("NinePatchRect/ColorRect/Reactions/Button3");
		Continue = GetNode<Button>("NinePatchRect/ColorRect/Continue");
		SC = GetNode<ShockController>("ShockController");
		Img = GetNode<Sprite2D>("NinePatchRect/ColorRect/Img");
		AP = GetNode<AnimationPlayer>("AnimationPlayer");

		// Fetch the context
		C = GetNode<Context>("/root/Context");

		// Set the button callbacks
		R1.Pressed += _OnR1Pressed;
		R2.Pressed += _OnR2Pressed;
		R3.Pressed += _OnR3Pressed;

		SHOCKS = new() { 
			"cold_spell", "heat_wave", "glaciers_melting", 
			"severe_weather",
			"inc_raw_cost_10", "inc_raw_cost_20", "mass_immigration",
			"pandemic", "nuc_accident"
		};
	
		// Set the initial shock
		CurShock = SHOCKS[0];
		SetFields();
	}

	// ==================== Public API ====================

	// Sets the internal current shock to a newly selected one
	public bool _SelectNewShock(MoneyData M, Energy E, Environment Env, Support S) {
		Debug.Print("CURRENT TURN: " + C._GetTurn());
		// The nuclear reintroduction must always happen at the same turn
		if(C._GetTurn() == 3) {
			Debug.Print("REINTRODUCE NUCLEAR");
			// Set the next shock
			CurShock = "nuc_reintro";

			// Update the fields to match the new shock
			SetFields(M, E, Env, S);
		} else {
			// Initialize pseudo-random number generator
			Random rnd = new ();

			Debug.Print("RANDOM SHOCK");

			// Pick a random number in the range of shock ids
			int next_idx = rnd.Next(SHOCKS.Count);

			// Pick the associated id
			string next_shock = SHOCKS[next_idx];

			// Remove the shock from the list
			SHOCKS.RemoveAt(next_idx);

			// Set the next shock
			CurShock = next_shock;

			// Update the fields to match the new shock
			SetFields(M, E, Env, S);

			// Decide whether or not to show a shock
			// There is a 50% chance of getting a shock
			if(rnd.Next(0, 100) > ShockProba) {
				return false;
			}
		}
		
		Debug.Print("SHOCKKKKKKKKK");
		return true;
	}

	// Getter for the shock's reward effects
	public Reward _GetReward() => CurReward;

	// Getter for the shock's reactions 
	public List<Reward> _GetReactions() => CurReactions;

	// Shows the shock
	// Requires the current money, energy, environment, and support levels to check the requirements
	public void _Show(MoneyData M, Energy E, Environment Env, Support S) {
		// Start by checking the current requirements to see if a reward should be displayed
		// We do this by verified that all requirements pass the checkrequirement test
		bool req_met = CurRequirements.Select(sr => 
			CheckRequirement(sr, M, E, Env, S)
		).Aggregate((acc, sr_met) => acc && sr_met);

		// If the requirement is met, then we can show the reward and apply it
		if(req_met) {
			// Start by hiding the reaction buttons
			_HideReactions();

			// Show the rewards
			Reward.Show();

			// Mark the shock as survived
			C._IncShocksSurvived();

			// Signal that a reward is applied
			// This should cause the gameloop to retrieve the reward and 
			// update the various related resources
			EmitSignal(SignalName.ApplyReward);
		}
		// Otherwise the requirements are not met, and a reaction must be selected
		else {
			// Show the Reactions
			_ShowReactions();

			// Hide the reward
			Reward.Hide();
		}

		// Show the shock itself once everything is setup
		Show();
		AP.Play("popUp");
	}

	  // Hides all of the reaction buttons
	// Should be done in the case of a reward
	public void _HideReactions() {
		Reactions.Hide();
		Continue.Show();
	}

	// Shows all of the reaction buttons
	// Should be done in the case of no reward
	public void _ShowReactions() {
		Reactions.Show();
		Continue.Hide();
	}

	// ==================== Internal Helpers ====================

	// Sets all of the fields for the shock once a new one is selected
	private void SetFields(MoneyData M, Energy E, Environment Env, Support S) {
		// Load in the corresponding image representing this shock
		string res_path = "res://assets/Icons/" + SC._GetShockImg(CurShock) + ".png"
			?? throw new Exception("Unable to fetch image path: " + CurShock.ToString());
		Img.Texture = (
			ResourceLoader.Load(res_path)
			?? throw new Exception("Unable to load resource: " + res_path)
		) as Texture2D;
		
		// Extract the name and the description and set the labels to match them
		Title.Text = SC._GetShockName(CurShock) 
			?? throw new Exception("Unable to fetch name for id: " + CurShock.ToString());
		Text.Text = SC._GetShockText(CurShock) 
			?? throw new Exception("Unable to fetch text for id: " + CurShock.ToString());

		// Set the current requirement
		CurRequirements = SC._GetRequirements(CurShock);

		// Retrieve the current reward
		CurReward = SC._GetReward(CurShock);

		// Set the reward text
		Reward.Text = CurReward.Text;

		// Retrieve the current reactions
		CurReactions = SC._GetReactions(CurShock);

		// Set the individual buttons if they have associated reactions
		if(CurReactions.Count > 0) {
			// Set the button's text and enable it
			R1.Text = CurReactions[0].Text;
			R1.Disabled = false;
			R1.Show();
			R2.Hide();
			R3.Hide();

			// Check reaction requirements
			CheckAllEffectReqs(CurReactions[0], ref R1, (M, E, Env, S));

		}
		if(CurReactions.Count > 1) {
			// Set the button's text and enable it
			R2.Text = CurReactions[1].Text;
			R2.Disabled = false;
			R2.Show();
			R3.Hide();

			// Check reaction requirements
			CheckAllEffectReqs(CurReactions[1], ref R2, (M, E, Env, S));
		}
		if(CurReactions.Count > 2) {
			// Set the button's text and enable it
			R3.Text = CurReactions[2].Text;
			R3.Disabled = false;
			R3.Show();

			// Check reaction requirements
			CheckAllEffectReqs(CurReactions[2], ref R3, (M, E, Env, S));
		}
	}

	// Checks the validity of the reaction based on current resources
	private void CheckAllEffectReqs(Reward se, ref Button react, (MoneyData, Energy, Environment, Support) res) {
		
		// We start looking at if our effect requirements are all met
		bool AllReqsMet = se.ToRequirements().Aggregate(true, (acc, req) => 
			CheckRequirement(req, res.Item1, res.Item2, res.Item3, res.Item4) && acc
		);
		// If they aren't all met, then we have to deactivate this response
		if(!AllReqsMet) {
			react.Disabled = true;
			react.Modulate = new(1, 1, 1, 0.5f);
		} else {
			react.Disabled = false;
			react.Modulate = new(1, 1, 1, 1);
		}
	}

	// Sets all of the fields for the shock once a new one is selected
	private void SetFields() {
		// Load in the corresponding image representing this shock
		string res_path = "res://assets/Icons/" + SC._GetShockImg(CurShock) + ".png"
			?? throw new Exception("Unable to fetch image path: " + CurShock.ToString());
		Img.Texture = (
			ResourceLoader.Load(res_path)
			?? throw new Exception("Unable to load resource: " + res_path)
		) as Texture2D;
		
		// Extract the name and the description and set the labels to match them
		Title.Text = SC._GetShockName(CurShock) 
			?? throw new Exception("Unable to fetch name for id: " + CurShock.ToString());
		Text.Text = SC._GetShockText(CurShock) 
			?? throw new Exception("Unable to fetch text for id: " + CurShock.ToString());

		// Set the current requirement
		CurRequirements = SC._GetRequirements(CurShock);

		// Retrieve the current reward
		CurReward = SC._GetReward(CurShock);

		// Set the reward text
		Reward.Text = CurReward.Text;

		// Retrieve the current reactions
		CurReactions = SC._GetReactions(CurShock);

		// Set the individual buttons if they have associated reactions
		if(CurReactions.Count > 0) {
			// Set the button's text and enable it
			R1.Text = CurReactions[0].Text;
			R1.Disabled = false;
			R1.Show();
			R2.Hide();
			R3.Hide();
		}
		if(CurReactions.Count > 1) {
			// Set the button's text and enable it
			R2.Text = CurReactions[1].Text;
			R2.Disabled = false;
			R2.Show();
			R3.Hide();
		}
		if(CurReactions.Count > 2) {
			// Set the button's text and enable it
			R3.Text = CurReactions[2].Text;
			R3.Disabled = false;
			R3.Show();
		}
	}

	// Checks if a given requirement is met
	private bool CheckRequirement(Requirement SR, MoneyData M, Energy E, Environment Env, Support S) =>
		SR.RT switch {
			// For energy supply, we need to be ${value} above the demand
			ResourceType.ENERGY_W => (E.SupplyWinter - E.DemandWinter) >= SR.Value,
			ResourceType.ENERGY_S => (E.SupplySummer - E.DemandSummer) >= SR.Value,
			// For environment, it's simply the environment bar that is checked
			ResourceType.ENVIRONMENT => Env.EnvBarValue() >= SR.Value,
			// Support and money are straightforward
			ResourceType.SUPPORT => S.Value >= SR.Value,
			ResourceType.MONEY => M.Money >= SR.Value,
			// This should never happen
			_ => throw new ArgumentException("Invalid Resource type was given !")
		};

	// ==================== Button Callbacks ====================

	// Reaction to the first button being pressed
	// This will trigger the effects set by the first reaction of the shock
	public void _OnR1Pressed() {

		if(CurShock == "nuc_reintro") {
			// Make sure that nuclear powerplants get turned back on
			EmitSignal(SignalName.ReintroduceNuclear);
			return;
		}

		// Signal that the first reaction was picked
		EmitSignal(SignalName.SelectReaction, 0);
	}

	// Reaction to the second button being pressed
	// This will trigger the effects set by the second reaction of the shock
	public void _OnR2Pressed() {
		// Signal that the second reaction was picked
		EmitSignal(SignalName.SelectReaction, 1);
	}

	// Reaction to the third button being pressed
	// This will trigger the effects set by the third reaction of the shock
	public void _OnR3Pressed() {
		// Signal that the third reaction was picked
		EmitSignal(SignalName.SelectReaction, 2);
	}
}
