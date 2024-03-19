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

// @brief: Manages all of the resources related to the environment bar.
// This inlcudes land use, pollution, and biodiversity.
// These metrics are all encoded directly in each power plant.
// Some plants reduce them, some increase them, this class aggregates those
// metrics and propagates the information across the game to the UI and GameLoop.
public partial class EnvironmentManager : Node {

	// Maximum value allowed by the UI
	public const int MAX_ENV_BAR_VAL = 100;

	// List of all of the power plants currently in the game
	private List<PowerPlant> PowerPlants;

	// Keep track of all of the environmental resources
	private Environment Env;

	// Keep track of the pollution caused by imports
	private int ImportPollution;

	// Models the impact a shock can have on the environment
	private float ShockImpact;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {

		// Initialize Power Plants and import pollution
		PowerPlants = new List<PowerPlant>();
		ImportPollution = 0;
		ShockImpact = 0;
	}

	// ==================== Public API ====================

	// Updates the current internal power plant list
	public void _UpdatePowerPlants(List<PowerPlant> lP) {
		// Clear the current list to be safe
		PowerPlants.Clear();

		// Fill in the contents of the list with those of the given one
		foreach(PowerPlant pp in lP) {
			PowerPlants.Add(pp);
		}
	}

	// Progresses the environment resources to the next turn
	// The total imported energy amount is given to be able to compute 
	// the pollution caused by imports
	public Environment _NextTurn() {
		// Reset the shock impact 
		ShockImpact = 0;

		// Estimate the values for the next turn
		return _GetEnvValues();
	}

	// Estimates and retrieves the environment values
	public Environment _GetEnvValues() {
		Env = EstimateEnvironment();
		return Env;
	}

	// Applies a shock's impact
	public void _ApplyShockEffect(float v) {
		ShockImpact = v;
	} 


	// The amount of energy covered by inputs, and the pollution/kWh are given
	// This allows us to compute the amount of pollution caused by imports
	// and include it in our pollution bar
	public void _UpdateImportPollution(int import_amount, float pol_per_kwh) {
		// Compute the total imported pollution
		int pol = (int)Math.Ceiling(import_amount * pol_per_kwh);

		// Update the internally stored value
		ImportPollution = pol;
	}

	// Reset the environment fields
	public void _Reset() {
		// Initialize Power Plants and import pollution
		PowerPlants.Clear();
		ImportPollution = 0;
		ShockImpact = 0;
	}

	// ==================== Helper Methods ====================  

	// Aggregate the biodiverstiy contributions
	private float AggregateBiodiversity() => Math.Max(0.0f, Math.Min(
		PowerPlants.Where(pp => pp._GetLiveness()).Select(pp => pp.BiodiversityImpact)
			.Aggregate(1.0f, (acc, bd) => acc - bd),
		1.0f)
	);

	// Aggregate the land use contributions
	private float AggregateLandUse() => Math.Max(0.0f, Math.Min(
		PowerPlants.Where(pp => pp._GetLiveness()).Select(pp => pp.LandUse).Sum(),
		1.0f)
	);

	// Aggregate the pollution contributions from all power plants
	private float AggregatePollution() =>
		PowerPlants.Where(pp => pp._GetLiveness()).Select(pp => pp._GetPollution()).Sum();

	// Estimates the environmental impact the various power plants have
	private Environment EstimateEnvironment() {
		// Compute the various contributions
		float biodiv = AggregateBiodiversity();
		float lu = AggregateLandUse();
		float pol = AggregatePollution();

		return new Environment(pol, lu, biodiv, ImportPollution, s: ShockImpact);
	}
}
