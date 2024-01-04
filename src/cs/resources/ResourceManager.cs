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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

// Encapsulates all of the resource management used throughout the game
// A resource is any mesurable field that is actively used by the player to make decisions
// There are 3 groups of resources, which represent the 3 pillars of our simulation
// - Support: How aligned the population with the decisions the player is making
// - Energy: The core resource of the game, are the people getting enough energy to meet their demand
// - Environment: How are the energy management decisions impacting the environment
public partial class ResourceManager : Node {

	[Signal]
	public delegate void UpdateNextTurnStateEventHandler(bool state);

	[Export]
	/* Whether or not we import in the summer */
	public bool ImportInSummer = false;

	[Export]
	/* The base cost of a kWh imported from abroad */
	public float ImportCost = 5.0f;

	[Export]
	/* The base pollution of a kWh imported from abroad */
	public float ImportPollution = 0.1f;

	private float InitImportCost, InitImportPollution;

	// Children resource managers
	private SupportManager SM;
	private EnergyManager EngM;
	private EnvironmentManager EnvM;

	// Reference to the UI object
	private UI _UI;

	// Contains all of the PowerPlants and BuildButtons in the scene
	private List<PowerPlant> PowerPlants;
	private List<BuildButton> BBs;

	// Context
	private Context C;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch nodes
		SM = GetNode<SupportManager>("SupportManager");
		EngM = GetNode<EnergyManager>("EnergyManager");
		EnvM = GetNode<EnvironmentManager>("EnvironmentManager");
		_UI = GetNode<UI>("../UI");
		C = GetNode<Context>("/root/Context");
		
		// Initialize the powerplant and Buildbutton lists
		PowerPlants = new List<PowerPlant>();
		BBs = new List<BuildButton>();
	}

	// ==================== Public API ====================

	// Starts the game for the resource manager
	public void _StartGame(ref MoneyData money) {
		// Check the current state of the next turn button
		Energy E = EngM._GetEnergyValues(_UI._GetImportSliderPercentage(), ImportInSummer);
		UpdateEnergyUI(E);

		// Set initial fields for future reset
		InitImportCost = ImportCost;
		InitImportPollution = ImportPollution;

		// This is done in order to guarantee that all resource ui elements are up to date
		// at the game's start
		_UpdateResourcesUI(false, ref money);

		// Check if the demand has been reached
		EmitSignal(
			SignalName.UpdateNextTurnState,
			E.DemandSummer > E.SupplySummer || E.DemandWinter > E.SupplyWinter
		);
	}

	// Progresses to the next turn
	// The game loop must pass in the amount of money as a ref
	public void _NextTurn(ref MoneyData Money) {

		// Make the plants distinct
		BBs = BBs.Distinct().ToList();
		PowerPlants = PowerPlants.Distinct().ToList();

		// Create a copy of the buildbutton and powerplant lists
		BuildButton[] tmp_bb = new BuildButton[BBs.Count];
		PowerPlant[] tmp_pp = new PowerPlant[PowerPlants.Count];

		BBs.CopyTo(tmp_bb);
		PowerPlants.CopyTo(tmp_pp);

		// Update all build buttons
		foreach(BuildButton bb in tmp_bb) {
			bb._NextTurn();
		}

		// Update all plants
		foreach(PowerPlant pp in tmp_pp) {
			pp._NextTurn();
		}

		// Update the energy managers
		Energy E = EngM._GetEnergyValues(_UI._GetImportSliderPercentage(), ImportInSummer);

		// Compute the total import cost
		int imported = EngM._ComputeTotalImportAmount(_UI._GetImportSliderPercentage(), ImportInSummer);

		// Update the amount of pollution caused by imports
		EnvM._UpdateImportPollution(imported, ImportPollution);

		// Update the environment manager
		Environment Env = EnvM._NextTurn();

		// Compute the production cost for this turn and update the money
		Money.NextTurn(
			GameLoop.BUDGET_PER_TURN, 
			AggregateProductionCost(),
			_GetTotalImportCost(_UI._GetImportSliderPercentage())
		);

		// Update the energy UI
		UpdateEnergyUI(E);
		UpdateEnvironmentUI(Env);
		
		// Update the support bar 
		UpdateSupportUI(SM._GetSupport());

		EmitSignal(
			SignalName.UpdateNextTurnState,
			E.DemandSummer > E.SupplySummer || E.DemandWinter > E.SupplyWinter
		);
	}

	// Allows for resources to be updated without having access to the moneydata field
	public void _UpdateResourcesUI(bool predict) {
		// Get the energy manager data
		if(!predict) {
			Energy E = EngM._GetEnergyValues(_UI._GetImportSliderPercentage(), ImportInSummer);
			UpdateEnergyUI(E);

			// Check if the demand has been reached
			EmitSignal(
				SignalName.UpdateNextTurnState,
				E.DemandSummer > E.SupplySummer || E.DemandWinter > E.SupplyWinter
			);
		}

		// Compute the total import cost
		int imported = EngM._ComputeTotalImportAmount(_UI._GetImportSliderPercentage(), ImportInSummer);

		// Update the amount of pollution caused by imports
		EnvM._UpdateImportPollution(imported, ImportPollution);

		// Get the environment manager data
		Environment Env = EnvM._GetEnvValues();

		// Update the UI
		UpdateEnvironmentUI(Env);

		// Update the support bar 
		UpdateSupportUI(SM._GetSupport());
	}

	// Updates all of the resource managers
	public void _UpdateResourcesUI(bool predict, ref MoneyData money) {

		// Get the energy manager data
		if(!predict) {
			Energy E = EngM._GetEnergyValues(_UI._GetImportSliderPercentage(), ImportInSummer);
			UpdateEnergyUI(E);

			// Check if the demand has been reached
			EmitSignal(
				SignalName.UpdateNextTurnState,
				E.DemandSummer > E.SupplySummer || E.DemandWinter > E.SupplyWinter
			);
		}

		// Compute the total import cost
		int imported = EngM._ComputeTotalImportAmount(_UI._GetImportSliderPercentage(), ImportInSummer);

		// Update the import and production cost in the moneydata
		money.UpdateImportCost(_GetTotalImportCost(_UI._GetImportSliderPercentage()));
		money.UpdateProductionCost(AggregateProductionCost());

		// Update the amount of pollution caused by imports
		EnvM._UpdateImportPollution(imported, ImportPollution);

		// Get the environment manager data
		Environment Env = EnvM._GetEnvValues();

		// Update the UI
		UpdateEnvironmentUI(Env);

		// Update the support bar 
		UpdateSupportUI(SM._GetSupport());

	}

	// Wrapper used for signal compatibility
	public void _UpdateResourcesUI() {
		_UpdateResourcesUI(true);
	}

	// Updates the current list of power plants via a deep copy
	public void _UpdatePowerPlants(List<PowerPlant> lPP) {
		// Clear the current list to be safe
		PowerPlants.Clear();

		// Fill in the contents of the list with those of the given one
		foreach(PowerPlant pp in lPP) {
			PowerPlants.Add(pp);
		}

		// Remove all duplicates
		PowerPlants = PowerPlants.Distinct().ToList();

		// Propagate the update to the energy manager
		EngM._UpdatePowerPlants(PowerPlants);
		EnvM._UpdatePowerPlants(PowerPlants);

		// Connect the powerplants signals to propagate changes to the UI
		foreach(PowerPlant pp in PowerPlants) {
			// Check that the signal isn't already connected
			if(!pp._GetEnergyConnectFlag()) {
				pp._SetEnergyConnectFlag();
				pp.UpdatePlant += _OnBuildDone;
			}
		}
	}

	// Updates the current list of build buttons
	public void _UpdateBuildButtons(List<BuildButton> lBB) {
		// Clear the current list to be safe
		BBs.Clear();

		// Fill in the contents of the list with those of the given one
		foreach(BuildButton bb in lBB) {
			BBs.Add(bb);
		}
		foreach(BuildButton bb in BBs) {
			bb.BuildDone += _OnBuildDone;
		}
	} 

	// Returns the current resource values for all resources
	public (Energy, Environment, Support) _GetResources() => (
		EngM._GetEnergyValues(_UI._GetImportSliderPercentage(), ImportInSummer),
		EnvM._GetEnvValues(),
		SM._GetSupport()
	);

	// Applies a given shock effect
	public void _ApplyEffect(Effect e) {
		// Figure out which resource to affect
		switch(e.RT) {
			case ResourceType.ENERGY_S:
				// Update the summer model's demand
				C._UpdateModelDemand(e.Value, winter: false);
				break;
			case ResourceType.ENERGY_W:
				// Update the winter model's demand
				C._UpdateModelDemand(e.Value);
				break;
			case ResourceType.ENVIRONMENT:
				EnvM._ApplyShockEffect(e.Value);
				break;
			case ResourceType.SUPPORT:
				// Naive update for support for now
				SM._UpdateSupport((int)e.Value);
				Debug.Print("Support: " + SM._GetSupportValue().ToString());
				break;
			default:
			 return;
		}
	}

	// Computes the cost of the current import amount
	// Given the percentage selected by the player
	public int _GetTotalImportCost(float import_perc) {
		// Retrieve the import amounts
		var (import_amount_w, import_amount_s) = EngM._ComputeImportAmount(C._GetDemand(), import_perc, ImportInSummer);

		// Compute the final cost
		return (int)(import_amount_w + import_amount_s * ImportCost);
	}

	// ==================== Helper Methods ====================  
	
	// Updates the UI fields related to the energy resource
	private void UpdateEnergyUI(Energy E) {
		// Update the UI
		_UI._UpdateData(
			UI.InfoType.W_ENGERGY,
			(int)Math.Floor(E.DemandWinter), 
			(int)Math.Floor(E.SupplyWinter)
		);
		_UI._UpdateData(
			UI.InfoType.S_ENGERGY, 
			(int)Math.Floor(E.DemandSummer), 
			(int)Math.Floor(E.SupplySummer)
		);
	}

	// Updates the UI fields related to the environment resource
	private void UpdateEnvironmentUI(Environment Env) {
		// Update the UI
		_UI._UpdateData(
			UI.InfoType.ENVIRONMENT,
			(int)(Env.LandUse * 100), // Convert floating point to integer percentage
			Env.Pollution,
			(int)(Env.Biodiversity * 100),
			(int)(Env.EnvBarValue() * 100),
			Env.ImportedPollution
		);
	}

	// Updates the UI fields related to the support resource
	private void UpdateSupportUI(Support Sup) {
		_UI._UpdateData(
			UI.InfoType.SUPPORT,
			Sup.Value
		);
	}

	// Gets the production cost accumulated over every building
	private int AggregateProductionCost() =>	
		PowerPlants.Where(pp => pp._GetLiveness()).Aggregate(0, (acc, pp) => acc + pp._GetProductionCost());

	// ==================== Callbacks ====================  

	// Simply reacts to a power plant toggle by updating the UI
	// The parameter is only used for signal interface compatibility
	private void _OnPowerPlantSwitchToggle(bool b) { 
		_UpdateResourcesUI(true);
	}

	// Reacts to a new power plant being built
	public void _OnBuildDone() {
		_UpdateResourcesUI(false);
	}

	// Resets the game to its initial state
	public void _Reset() {
		ImportCost = InitImportCost;
		ImportPollution = InitImportPollution;

		EngM._Reset();
		EnvM._Reset();
	}
	
}
