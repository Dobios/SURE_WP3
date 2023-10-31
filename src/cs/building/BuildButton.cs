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
using System.Collections.Generic;
using System.Net.Http.Headers;

// Models a button that can be used to create power plants
public partial class BuildButton : TextureButton {

	// The possible states the button can be in
	public enum BuildState { IDLE, BUILDING, DONE };

	// Names for the various power plants
	public const string GAS_NAME = "Gas";
	public const string SOLAR_NAME = "Solar";
	public const string HYDRO_NAME = "Hydro";
	public const string TREE_NAME = "Tree";
	public const string WIND_NAME = "Wind";
	public const string WASTE_NAME = "Waste";
	public const string BIOMASS_NAME = "Biomass";
	public const string RIVER_NAME = "River";
	public const string PUMP_NAME = "Pump";

	// Signal used to trigger the showing of the build menu
	[Signal]
	public delegate void ShowBuildMenuEventHandler(BuildButton bb);

	[Signal]
	public delegate void UpdateBuildSlotEventHandler(BuildButton bb, PowerPlant pp, bool remove);

	[Signal]
	public delegate void BuildDoneEventHandler();

	// The only special plant for the time being is Hydro
	// This flag tells us whether or not it is permitted to build a hydro plant at this location.
	[Export]
	public bool AllowHydro = false;
	
	public BuildLocation BL;
	private BuildMenu BM;

	// Power Plants
	private PowerPlant GasPlant;
	private PowerPlant SolarPlant;
	private PowerPlant HydroPlant;
	private PowerPlant TreePlant;
	private PowerPlant WindPlant;
	private PowerPlant WastePlant;
	private PowerPlant BiomassPlant;
	private PowerPlant RiverPlant;
	private PowerPlant PumpPlant;
	
	// Building sprite
	private Sprite2D BuildSprite;
	private Label TL;

	// Build cancellation button
	private Button Cancel;
	private int RefundAmount = -1;
	
	// Money animation
	public AnimationPlayer AP;
	public Label AnimMoney;

	
	private AudioStreamPlayer2D BuildingSound;

	// Reference to the game loop
	private GameLoop GL;

	// The internal state of the button (used for multy turn builds)
	private BuildState BS;

	// Plant we are currently building (only valid is state is BUILDING)
	private PowerPlant PPInProgress;

	// Number of Turns remaining in current build
	private int TurnsToBuild;

	// Reference to the context 
	private Context C;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Initialize state
		BS = BuildState.IDLE;

		// Fetch Node  
		BM = GetNode<BuildMenu>("../../BuildMenu");

		// Fetch Power plants
		GasPlant = GetNode<PowerPlant>(GAS_NAME);
		SolarPlant = GetNode<PowerPlant>(SOLAR_NAME);
		HydroPlant = GetNode<PowerPlant>(HYDRO_NAME);
		TreePlant = GetNode<PowerPlant>(TREE_NAME);
		WindPlant = GetNode<PowerPlant>(WIND_NAME);
		WastePlant = GetNode<PowerPlant>(WASTE_NAME);
		BiomassPlant = GetNode<PowerPlant>(BIOMASS_NAME);
		RiverPlant = GetNode<PowerPlant>(RIVER_NAME);
		PumpPlant = GetNode<PowerPlant>(PUMP_NAME);

		// Set their bb reference
		GasPlant._SetBuildButton(this);
		SolarPlant._SetBuildButton(this);
		HydroPlant._SetBuildButton(this);
		TreePlant._SetBuildButton(this);
		WindPlant._SetBuildButton(this);
		WastePlant._SetBuildButton(this);
		BiomassPlant._SetBuildButton(this);
		RiverPlant._SetBuildButton(this);
		PumpPlant._SetBuildButton(this);
		
		BuildSprite = GetNode<Sprite2D>("Building");
		TL = GetNode<Label>("Building/ColorRect/TurnsLeft");
		Cancel = GetNode<Button>("Cancel");
		AP = GetNode<AnimationPlayer>("AnimationPlayer");
		AnimMoney = GetNode<Label>("Money");
		BuildingSound = GetNode<AudioStreamPlayer2D>("BuildingSound");

		// Fetch the context
		C = GetNode<Context>("/root/Context");

		// Initially hide all of the plants
		HideAllPlants();

		// Connect the onShowBuildMenu callback to our signal
		ShowBuildMenu += BM._OnShowBuildMenu;
		Cancel.Pressed += _OnCancelPressed;

		// Make sure that the location is set correctly
		if(AllowHydro) {
			BL = new BuildLocation(Position, Building.Type.HYDRO, Building.Type.RIVER, Building.Type.PUMP);
		} else {
			BL = new BuildLocation(Position, Building.Type.GAS, Building.Type.SOLAR, Building.Type.TREE, Building.Type.WIND, Building.Type.WASTE, Building.Type.BIOMASS);
		}

		// Connect the button press callback
		Pressed += _OnPressed;
	}

	// ==================== Public API ====================
	// Records a reference to the game loop (triggered by the game loop itself)
	public void _RecordGameLoopRef(GameLoop _GL) {
		GL = _GL;
	}

	// Signals that a turn has passed
	public void _NextTurn() {

		// Hide the cancel button
		Cancel.Hide();

		// Check if we are currently building something
		if(BS == BuildState.BUILDING) {
			// Decrement the number of remaining turns and check for build completion
			if(--TurnsToBuild <= 0) {
				// Finish the current build
				FinishBuild();
			} else {
				// Update the button to show the number of remaining turns
				SetToBuild();
			}
		}
	}	

	// Public accessor which disables the current build button
	public void _Disable() {
		Disabled = true;
	}

	// Resets the build button
	public void _Reset() {
		// Cancel all current builds
		if(BuildSprite.Visible) {
			_OnCancelPressed();
		}
		// Hide all associated plants
		HideAllPlants();

		// Show the button
		ShowOnlyButton();

		// Reset state
		BS = BuildState.IDLE;
	}

	// ==================== Internal Helpers ====================

	// Begins the mutli-turn build of the given power plant
	private void BeginBuild(PowerPlant PP) {
		// Update build state
		BS = BuildState.BUILDING;

		// Show the cancel button
		Cancel.Show();
		RefundAmount = PP.BuildCost;

		// Update requested build fields
		PPInProgress = PP;
		TurnsToBuild = PP.BuildTime;

		// Update the button
		SetToBuild();
	}

	// Finalizes the current Build
	private void FinishBuild() {
		// Sanity Check
		Debug.Assert(PPInProgress != null);

		// Reset number of turns
		TurnsToBuild = 0;

		// Propagate the newly built plant to the context
		C._UpdatePPStats(PPInProgress.PlantType);

		// Update the data stored in the model struct if online
		if(C._GetOffline()) {
			C._UpdateModelFromClient(PPInProgress);
		} 
		
		// Update the current power plant placed at this slot
		UpdateGenericPlant(PPInProgress);

		// Hide the button
		HideOnlyButton();

		BuildingSound.Stop();

		// Update the requested build fields
		PPInProgress = null;
		BS = BuildState.DONE;

		// Signal that the build is complete
		EmitSignal(SignalName.BuildDone);
	}

	// Hides the button but not its children
	private void HideOnlyButton() {
		Disabled = true;
		SelfModulate = new Color(1,1,1,0);
	}

	// Show only the button
	private void ShowOnlyButton() {
		Disabled = false;
		SelfModulate = new Color(1,1,1,1);
	}

	// Resets the button to it's initial state
	private void Reset() {
		Disabled = false;
	}

	// Sets the button to the build state
	private void SetToBuild() {
		BuildingSound.Play();
		BuildSprite.Show();
		TL.Text = "⌛ " + TurnsToBuild.ToString();
		Disabled = true;
	}

	// Hides all of the plants related to this button
	private void HideAllPlants() {
		// Hide all plants
		GasPlant.Hide();
		HydroPlant.Hide();
		SolarPlant.Hide();
		TreePlant.Hide();
		WindPlant.Hide();
		BiomassPlant.Hide();
		WastePlant.Hide();
		RiverPlant.Hide();
		PumpPlant.Hide();
		
		BuildSprite.Hide();
	}

	// Wrapper for a more specific call depending on the selected plant type
	private void UpdateGenericPlant(PowerPlant PP) {
		// Make sure that the selected plant is setup correctly
		PP._SetPlantFromConfig(PP.PlantType);

		// Update the availability if online mode is active
		if(!C._GetOffline()) {
			PP._SetAvailabilityFromContext();
		}

		// Pick which plant to show and update
		switch(PP.PlantType.type) {
			case Building.Type.GAS:
				UpdatePowerPlant(ref GasPlant, PP);
				break;
			
			case Building.Type.HYDRO:
				UpdatePowerPlant(ref HydroPlant, PP);
				break;

			case Building.Type.SOLAR:
				UpdatePowerPlant(ref SolarPlant, PP);
				break;
			
			case Building.Type.TREE:
				UpdatePowerPlant(ref TreePlant, PP);
				break;
				
			case Building.Type.WIND:
				UpdatePowerPlant(ref WindPlant, PP);
				break;
				
			case Building.Type.WASTE:
				UpdatePowerPlant(ref WastePlant, PP);
				break;
				
			case Building.Type.BIOMASS:
				UpdatePowerPlant(ref BiomassPlant, PP);
				break;
				
			case Building.Type.RIVER:
				UpdatePowerPlant(ref RiverPlant, PP);
				break;
				
			case Building.Type.PUMP:
				UpdatePowerPlant(ref PumpPlant, PP);
				break;
				
			default:
				break;
		}		
	}

	// Updates a given power plant to match the received power plant
	private void UpdatePowerPlant(ref PowerPlant PP, PowerPlant PPRec) {
		// Set it up to display at our button's location
		PP.Position = new Vector2(112,88);
		PP.Scale = new Vector2(1, 1);
		PP.IsPreview = false;
		PP.BuildCost = PPRec.BuildCost;
		PP.PlantType = PPRec.PlantType;
		PP.BiodiversityImpact = PPRec.BiodiversityImpact;
		PP.LandUse = PPRec.LandUse;

		// Update the plant's internal private fields
		PP._UdpatePowerPlantFields(
			true, // Update the initial values as well
			PPRec._GetPollution(),
			PPRec._GetProductionCost(), 
			PPRec._GetCapacity()
		);

		// Force name to be consistent with type
		PP.PlantName = PPRec.PlantName;

		// Make sure that the data is propagated to the UI
		PP._UpdatePlantData();
		PP.Show();
		BuildSprite.Hide();

		// Add the building to the power plant list
		EmitSignal(SignalName.UpdateBuildSlot, this, PP, false);
	}

	// ==================== Interaction Callbacks ====================

	// Show the build menu containing the correct buildings to build
	public void _OnPressed() {
		// Only do stuff if the button is idle
		if(BS == BuildState.IDLE) {
			BM.Show();
			EmitSignal(SignalName.ShowBuildMenu, this);
		}
	}

	// Receives the power plant selected by the user and now we need to place it
	public void _OnSelectBuilding(PowerPlant PP) {
		// Sanity check: Check explicitly for hydro builds and dissallow illegal ones
		if(!AllowHydro && (
			PP.PlantType.type == Building.Type.HYDRO || 
			PP.PlantType.type == Building.Type.RIVER || 
			PP.PlantType.type == Building.Type.PUMP)
		) {
			return;
		}

		// Hide the build menu UI
		BM.Hide();
		
		// play money animation
		if(PP.BuildCost > 0) {
			AnimMoney.Text = "-" + PP.BuildCost.ToString() + "$";
			AP.Play("Money-");
		}

		// Check if the requested build was legal
		if(GL._RequestBuild(PP.BuildCost)) {
			// Hide all plants
			HideAllPlants();
			
			// Check for the requested plant's build time
			if(PP.BuildTime >= 1) {
				BeginBuild(PP);
			} else {
				// Propagate the newly built plant to the context
				C._UpdatePPStats(PP.PlantType);

				// Update the data stored in the model struct if online
				if(!C._GetOffline()) {
					C._UpdateModelFromClient(PP);
				}

				// Select which plant to show and update it's fields to match those that were given
				UpdateGenericPlant(PP);

				// Hide our button
				HideOnlyButton();

				// Update Build State
				BS = BuildState.DONE;

				Debug.Print("BUILT PP: CAP = " + PP._GetCapacity() + ", NAME = " + PP.PlantName);

				// Signal that the build is complete
				EmitSignal(SignalName.BuildDone);
			}
		}		
	}

	// Reacts to a cancelation request
	private void _OnCancelPressed() {
		// play money anim
		if(RefundAmount > 0) {
			AnimMoney.Text = "+" + RefundAmount.ToString() + "$";
			AP.Play("Money+");
		}
		
		// Hide all plants
		HideAllPlants();

		//Stop building sound
		BuildingSound.Stop();

		// Hide the cancel button
		Cancel.Hide();

		// Show the buildbutton
		ShowOnlyButton();

		// Refund
		GL._RequestBuild(-RefundAmount);
		
		// Reset the refund amount
		RefundAmount = -1;

		// Reset the build state
		BS = BuildState.IDLE;
	}
}
