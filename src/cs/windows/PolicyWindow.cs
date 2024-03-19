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
using System.Linq;
using System.Reflection.Metadata;

// Represents the policy window containing all policy choices
// TODO: This requires implementing policies, which is a tricky task that will require a ton of work.
public partial class PolicyWindow : CanvasLayer {

	// ==================== Constants ====================

	private const string POLICY_GROUP = "policies";
	private const string POLICY_SUCCESS = "policy_success";
	private const string POLICY_FAILURE = "policy_failure";

	// ==================== Children Nodes ====================

	private ColorRect P;
	private AnimationPlayer AP;
	public TextureButton Vote;
	private TextureButton WindButton;
	private ButtonGroup PolicyGroup;
	private TextureButton PressedPolicy;
	private List<TextureButton> ImplementedPolicy;
	private Label VoteResult;
	private List<TextureButton> PolicyButtons;
	private string SelectedPolicy;
	private Label Implemented;

	// ==================== UI fields ====================

	private Label PN; // Policy Name
	private Label PT; // Policy Text
	private Label ET; // Effects Text
	private ProgressBar Pop; // Vote probability

	// ==================== Singletons ====================	

	private Context C;
	private PolicyController PC;
	private TextController TC;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		C = GetNode<Context>("/root/Context");
		PC = GetNode<PolicyController>("/root/PolicyController");
		TC = GetNode<TextController>("/root/TextController");
		P = GetNode<ColorRect>("ColorRect");
		AP = GetNode<AnimationPlayer>("AnimationPlayer");
		VoteResult = GetNode<Label>("Control/Policies-base-2/Vote/VoteResult");
		Vote = GetNode<TextureButton>("Control/Policies-base-2/Vote");
		WindButton = GetNode<TextureButton>("Control/PoliciesBase-1/Wind_buildtime");
		Implemented = GetNode<Label>("Control/Policies-base-2/Implemented");

		// Fetch UI Elements
		PN = GetNode<Label>("Control/Policies-base-2/PolicyName");
		PT = GetNode<Label>("Control/Policies-base-2/Text");
		ET = GetNode<Label>("Control/Policies-base-2/EffectTitle/Text");
		Pop = GetNode<ProgressBar>("Control/Policies-base-2/Vote/Popularity");
		
		PolicyGroup = WindButton.ButtonGroup;
		PressedPolicy = PolicyGroup.GetPressedButton() as TextureButton;
		
		PolicyButtons = new()
		{
			// Fetch policy buttons
			GetNode<TextureButton>("Control/PoliciesBase-1/Wind_buildtime"),
			GetNode<TextureButton>("Control/PoliciesBase-1/Upgrade_wind"),
			GetNode<TextureButton>("Control/PoliciesBase-2/home_regulation"),
			GetNode<TextureButton>("Control/PoliciesBase-2/industry_subsidy"),
			GetNode<TextureButton>("Control/PoliciesBase-1/Upgrade_PV")
		};
		
		ImplementedPolicy = new(){};

		// Connect the policy button callbacks
		PolicyButtons.ForEach(pb => pb.Pressed += _OnPolicyButtonPressed);
		
		P.GuiInput += _OnPanelGuiInput;
		Vote.Pressed += _OnVotePressed;
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {}

	// ==================== Public API ====================

	// Activates the given animation either forwards or in reverse.
	public void _PlayAnim(string Anim, bool forward = true) {
		if (forward) {
			AP.Play(Anim);
		} else {
			AP.PlayBackwards(Anim);
		}
	}
	
	// Reset vote button at the end of each turn (player can vote once/turn)
	public void _ResetVote() {
		Vote.Disabled = false;
		VoteResult.Hide();
	}
			
	// ==================== Interaction Callbacks ====================

	// Hides the window if the panel is pressed
	public void _OnPanelGuiInput(InputEvent input) {
		if(input.GetType() == new InputEventMouseButton().GetType()) {
			Hide();
			PressedPolicy = PolicyGroup.GetPressedButton() as TextureButton;
			if (PressedPolicy != null) {
				Vote.Hide();
				PressedPolicy.ButtonPressed = false;
			}
		}
	} 

	// When a policy button is pressed, we simply show the vote button 
	// We also need to update the window to display all of the specific data
	public void _OnPolicyButtonPressed() {
		// Retrieve the policy information to use it to update the UI
		PressedPolicy = PolicyGroup.GetPressedButton() as TextureButton;
		if(PressedPolicy != null) {
			// Allow for the user to trigger a vote
			Vote.Show();
			Implemented.Hide();
			if (!Vote.Disabled) {VoteResult.Hide();}
			// Retrieve the UI infor such as name, text and effects
			// and update the UI with them
			PN.Text = PC._GetPolicyName(PressedPolicy.Name);
			PT.Text = PC._GetPolicyText(PressedPolicy.Name);
			ET.Text = PC._GetEffects("policy", PressedPolicy.Name)
				.Aggregate("", (acc, e) =>
					 e.Text == "" ? acc : acc + "- " + e.Text + "\n"
				);

			// Update the probability preview
			Pop.Value = C._GetGL()._GetPM()._GetRealProb(PressedPolicy.Name) * 100.0f;
		} if(ImplementedPolicy.Contains(PressedPolicy)) {
			Vote.Hide();
			Implemented.Show();
		} if (Vote.Disabled) {
			VoteResult.Text = "You can only implement one policy per turn.";
		}
	}

	// Attempts a vote and shows the result
	public void _OnVotePressed() {
		// Check the vote result based on the selected policy
		PressedPolicy = PolicyGroup.GetPressedButton() as TextureButton;
		if (PressedPolicy != null) {
			// Attempt the vote
			bool success = C._GetGL()._GetPM()._RequestPolicy(PressedPolicy.Name);
			
			if(success) {
				ImplementedPolicy.Add(PressedPolicy);
			}
			// Disable the vote
			Vote.Disabled = true;
			VoteResult.Show();

			// Show the result
			VoteResult.Text = TC._GetText(
				"labels.xml", POLICY_GROUP, 
				success ? POLICY_SUCCESS : POLICY_FAILURE
			);
		} else {
			VoteResult.Text = "Please select a policy.";
			VoteResult.Show();
		}
	}
}
