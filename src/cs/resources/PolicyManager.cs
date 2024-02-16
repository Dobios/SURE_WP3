/**
	Sustainable Energy Development game modeling the Swiss energy Grid.
	Copyright (C) 2024 Università della Svizzera Italiana

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

// Manages the various fields maintained for the policies
public partial class PolicyManager : Node {

	// ==================== Children Nodes ====================

	// XML Controller for the policies config files
	public PolicyController PC;
	
	// Context of the game
	private Context C;

	// ==================== Internal Fields ====================

	// Stores the current active bonus for each tag
	public Dictionary<string, float> Bonuses;
	public const string ENV_TAG = "env";
	public const string DEM_TAG = "demand";

	// Keeps track of the number of turns remaining for each ongoing campaign
	// Does so by mapping affected tags with the value of the campaign 
	// and the number of turns left
	public List<(string, float, int)> OngoingCampaings;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Initialize the bonuses
		Bonuses = new() {
			{ENV_TAG, 0.0f},
			{DEM_TAG, 0.0f}
		};
		OngoingCampaings = new();

		// Fetch Children Nodes
		PC = GetNode<PolicyController>("/root/PolicyController");
		C = GetNode<Context>("/root/Context");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {}

	// ==================== Public API ====================

	// Resets the PolicyManager
	public void _Reset() {
		Bonuses = new() {
			{ENV_TAG, 0.0f},
			{DEM_TAG, 0.0f}
		};
		OngoingCampaings = new();
	}

	// Checks that the requirements are met for a given policy
	public bool _CheckRequirements(string policyId) {
		// Extract the requirements from the config
		List<Requirement> reqs = PC._GetRequirements(policyId);

		// Fetch the current state of resources
		(Energy E, Environment Env, Support S) = C._GetResources();

		// Check that our current requirements don't surpass our available resources
		return reqs.Select(req => CheckReq(req, E, Env, S))
					.Aggregate(true, (acc, pass) => acc && pass);
	}

	// Retrieves the current real probability of passing a policy
	// The real probability is computed as 
	// (baseProbability + bonus) - ((baseProbability + bonus) * (requirement - support))
	public float _GetRealProb(string policyId) {
		// Compute the augmented base probability (clamped to [0, 1])
		float baseWBonus = Math.Max(0.0f, 
			Math.Min(
				PC._GetPolicyProba(policyId) + Bonuses[PC._GetPolicyTag(policyId)],
				1.0f
			));

		// Retrieve the aggregated support requirements
		float req = PC._GetRequirements(policyId)
			.Aggregate(1.0f, (acc, r) => acc * (r.RT == ResourceType.SUPPORT ? r.Value : 1.0f));

		// Retrieve current support
		float support = C._GetResources().Item3.Value;

		// return the final result
		return  Math.Max(0.0f, Math.Min(
			baseWBonus - (baseWBonus * (req - support)),
		1.0f));
	}

	// Request the enaction of a particular policy
	// @returns {bool} whether or not the enaction was successful
	public bool _RequestPolicy(string policyId) {
		// Start by checking the requirements
		if(!_CheckRequirements(policyId)) {
			Debug.Print("Requirements were not met for " + policyId);
			return false;
		}

		// Recover the tag and probability for the policy
		string tag = PC._GetPolicyTag(policyId);

		// Sanity check, make sure that the tag is valid
		if(!CheckTag(tag)) {
			Debug.Print("Tag was invalid for " + policyId);
			return false;
		}

		// Perform the vote and apply the effects if the vote passed
		bool pass = PerformVote(policyId);
		if(pass) {
			ApplyEffects(PC._GetEffects("policy", policyId));

			Debug.Print("VOTE WAS SUCCESSFULL FOR: " + PC._GetPolicyName(policyId));
		} else {
			Debug.Print("VOTE FAILED FOR " + policyId);
		}
		return pass;
	}

	// Schedule a campaign if it takes longer than a turn
	// We assume that all campaigns take at least 1 turn
	// We also assume that it only contains a single effect
	public void _ScheduleCampaign(string campaignId) {
		// Extract the campaign data and schedule it
		OngoingCampaings.Add((
			PC._GetCampaignTag(campaignId),
			PC._GetEffects("campaign", campaignId)[0].Value,
			PC._GetCampaignLength(campaignId)
		));
	}	

	// On a turn tick, update the state of the ongoing campaigns
	public void _NextTurn() {
		OngoingCampaings = OngoingCampaings.Select(c => {
			// Extract arguments
			(string tag, float amount, int turns) = c;

			// Update the number of turns
			int remainingTurns =  turns - 1;
			if(remainingTurns <= 0) {
				// Apply the campaign effect
				Bonuses[tag] += amount;

				// Set a sentinal to mark this for deletion
				return ("", -1.0f, -1);
			}
			return (tag, amount, remainingTurns);
		}).ToList();

		// Delete all sentinal nodes
		OngoingCampaings = OngoingCampaings.Where(c => c.Item3 != -1).ToList();
	} 

	// ==================== Internal Helpers ====================

	// Checks that the given tag is valid
	private bool CheckTag(string tag) => tag == ENV_TAG || tag == DEM_TAG;

	// Checks that a given requirement is met
	private bool CheckReq(Requirement req, Energy E, Environment Env, Support S) => 
		req.RT switch {
			// We only handle support requirements for policies
			ResourceType.SUPPORT => req.Value <= S.Value,
			ResourceType.ENERGY_S => req.Value <=  E.SupplySummer, 
			ResourceType.ENERGY_W => req.Value <= E.SupplyWinter,
			ResourceType.ENVIRONMENT => req.Value <= Env.EnvBarValue(),
			_ => true
		};

	// Dice roll to see if the vote passes or not given the probability and bonus
	// The method is simply Rand(0, 100) <= (prob + bonus) * 100
	private bool PerformVote(string policyId) =>
		new Random().Next(0, 100) <= (_GetRealProb(policyId) * 100);

	// Applies the given effects to the current resources
	private void ApplyEffects(List<Effect> effects) {
		effects.ForEach(e => C._GetGL()._ApplyEffect(e));
	}
}
