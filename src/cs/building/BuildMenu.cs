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
using System.Linq;
using System.Collections.Generic;

// Represents the menu that pops up when a build is requested
public partial class BuildMenu : CanvasLayer {
	// Returns the result of the building selection to the build button
	[Signal]
	public delegate void SelectBuildingEventHandler(PowerPlant PP);

	[Export] 
	public Vector2 BuildingSpriteBase = new Vector2(150, 100);
	[Export]
	public int BuildingSpriteOffset = 250;
	[Export]
	public string BuildingAssetPathBase = "res://assets";

	public bool IsOpen = false;
	
	// Reference to the currently registered button (only one is allowed)
	private BuildButton RegisteredBuildButton;

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

	// Buttons to select the power plants
	private Button GasButton;
	private Button SolarButton;
	private Button HydroButton;
	private Button TreeButton;
	private Button WindButton;
	private Button WasteButton;
	private Button BiomassButton;
	private Button RiverButton;
	private Button PumpButton;

	// Close button
	private Button CloseButton;
	
	// Animation Player
	private AnimationPlayer BuildMenuAP;
	
	// Tab Container
	private ColorRect MenuC;
	
	private GameLoop GL;

	// Context
	private Context C;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Initially hide this menu
		Hide();

		// Fetch Power plants
		GasPlant = GetNode<PowerPlant>("Container/Gas");
		SolarPlant = GetNode<PowerPlant>("Container/Solar");
		HydroPlant = GetNode<PowerPlant>("Container/Hydro");
		TreePlant = GetNode<PowerPlant>("Container/Tree");
		WindPlant = GetNode<PowerPlant>("Container/Wind");
		WastePlant = GetNode<PowerPlant>("Container/Waste");
		BiomassPlant = GetNode<PowerPlant>("Container/Biomass");
		RiverPlant = GetNode<PowerPlant>("Container/River");
		PumpPlant = GetNode<PowerPlant>("Container/Pump");

		// Fetch associated buttons
		GasButton = GetNode<Button>("Container/Gas/GasButton");
		SolarButton = GetNode<Button>("Container/Solar/SolarButton");
		HydroButton = GetNode<Button>("Container/Hydro/HydroButton");
		TreeButton = GetNode<Button>("Container/Tree/TreeButton");
		WindButton = GetNode<Button>("Container/Wind/WindButton");
		WasteButton = GetNode<Button>("Container/Waste/WasteButton");
		BiomassButton = GetNode<Button>("Container/Biomass/BiomassButton");
		RiverButton = GetNode<Button>("Container/River/RiverButton");
		PumpButton = GetNode<Button>("Container/Pump/PumpButton");

		// Fetch Close button
		CloseButton = GetNode<Button>("CloseButton");
		
		// Fetch Animation Player
		BuildMenuAP = GetNode<AnimationPlayer>("AnimationPlayer");

		// Fetch Context
		C = GetNode<Context>("/root/Context");
		
		// Fetch TabContainer and sets tab titles
		MenuC = GetNode<ColorRect>("Container");

		// Connect the associated button callbacks
		GasButton.Pressed += _OnGasButtonPressed;
		SolarButton.Pressed += _OnSolarButtonPressed;
		HydroButton.Pressed += _OnHydroButtonPressed;
		TreeButton.Pressed += _OnTreeButtonPressed;
		WindButton.Pressed += _OnWindButtonPressed;
		WasteButton.Pressed += _OnWasteButtonPressed; 
		BiomassButton.Pressed += _OnBiomassButtonPressed;
		RiverButton.Pressed += _OnRiverButtonPressed;
		PumpButton.Pressed += _OnPumpButtonPressed;
		CloseButton.Pressed += _OnCloseButtonPressed;

		HideAllPlants();
	}

	// ==================== Public API ====================

	// Updates the name of the associate building type (for localization)
	public void _UpdatePlantName(Building Bt, string newName) {
		// Find which building to update
		switch (Bt.type) {
			case Building.Type.GAS:
				// Position the plant correctly
				SetPlantName(ref GasPlant, newName);
				break;

			case Building.Type.HYDRO:
				SetPlantName(ref HydroPlant, newName);
				break;

			case Building.Type.SOLAR:
				SetPlantName(ref SolarPlant, newName);
				break;

			case Building.Type.TREE:
				SetPlantName(ref TreePlant, newName);
				break;
				
			case Building.Type.WIND:
				SetPlantName(ref WindPlant, newName);
				break;

			case Building.Type.WASTE:
				SetPlantName(ref WastePlant, newName);
				break;

			case Building.Type.BIOMASS:
				SetPlantName(ref BiomassPlant, newName);
				break;

			case Building.Type.RIVER:
				SetPlantName(ref RiverPlant, newName);
				break;

			case Building.Type.PUMP:
				SetPlantName(ref PumpPlant, newName);
				break;
			
			default:
				break;
		}
	}

	// ==================== Internal Helpers ====================
	
	// If you know a better way please change
	public void _RecordGameLoopRef(GameLoop _GL) {
		GL = _GL;
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
	}

	// Sets the plants name and propagates info to ui
	private void SetPlantName(ref PowerPlant PP, string name) {
		PP.PlantName = name;
		PP._UpdatePlantData();
	}
	
	// Sets the position of the given plant according to its position in the list
	private void SetPlantPosition(ref PowerPlant pp, int idx) {
		
		// Make sure that these plants are in preview mode
		pp._UpdateIsPreview(true);

		// Show the plant once the position is set
		pp.Show();
	}
	
	private void SetPlantColor(ref PowerPlant pp) {
		if (GL.Money.Money >= pp.BuildCost || pp.BuildCost == 0) {
			pp.Modulate = new Color(1,1,1,1);
		} else {
			pp.Modulate = new Color(0.5f,0.5f,0.5f,1f);
		}
	}

	// ==================== Interaction Callbacks ====================

	// When the "ShowBuildMenu" signal is triggered, show the given powerplants 
	// in the given order using the predefined base and offset.
	public void _OnShowBuildMenu(BuildButton bb) {
		BuildMenuAP.Play("SlideUp");
		// What happens when we press another button when the menu is open
		if(IsOpen) {
			// Clear the Menu
			HideAllPlants();

			// Deregister the previous Build Button from the selection signal
			// This is done to avoid double building spawns
			SelectBuilding -= RegisteredBuildButton._OnSelectBuilding;
		}

		// Make sure that the previous registered button is deregistered
		if(RegisteredBuildButton != null) {
			SelectBuilding -= RegisteredBuildButton._OnSelectBuilding;
		}

		// Update open flag
		IsOpen = true;

		// Connect the build button to our return signal and register it
		RegisteredBuildButton = bb;
		SelectBuilding += bb._OnSelectBuilding;

		// Sanity check: List must not contain duplicates
		List<Building> BTs = bb.BL.AvailableTypes.Distinct().ToList();

		// Clear the Menu
		HideAllPlants();

		// Display the buildings sent with the signal  
		int idx = 0;
		foreach(var _bt in BTs) {
			switch (_bt.type) {
				case Building.Type.GAS:
					// Position the plant correctly
					SetPlantPosition(ref GasPlant, idx++);
					SetPlantColor(ref GasPlant);
					// Make sure that its fields are set correctly before displaying anything
					GasPlant._SetPlantFromConfig(Building.Type.GAS);
					GasPlant.PlantType = Building.Type.GAS;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(GasPlant.BuildCost)) {
						GasPlant._MakeTransparent();
						GasButton.Disabled = true;
					} else {
						GasPlant._MakeOpaque();
						GasButton.Disabled = false;
					}
					break;

				case Building.Type.HYDRO:
					SetPlantPosition(ref HydroPlant, idx++);
					SetPlantColor(ref HydroPlant);
					HydroPlant._SetPlantFromConfig(Building.Type.HYDRO);
					HydroPlant.PlantType = Building.Type.HYDRO;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(HydroPlant.BuildCost)) {
						HydroPlant._MakeTransparent();
						HydroButton.Disabled = true;
					} else {
						HydroPlant._MakeOpaque();
						HydroButton.Disabled = false;
					}
					break;

				case Building.Type.SOLAR:
					SetPlantPosition(ref SolarPlant, idx++);
					SetPlantColor(ref SolarPlant);
					SolarPlant._SetPlantFromConfig(Building.Type.SOLAR);
					SolarPlant.PlantType = Building.Type.SOLAR;
					C._GetGL()._ApplyOverload(ref SolarPlant);

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(SolarPlant.BuildCost)) {
						SolarPlant._MakeTransparent();
						SolarButton.Disabled = true;
					} else {
						SolarPlant._MakeOpaque();
						SolarButton.Disabled = false;
					}
					break;

				case Building.Type.TREE:
					SetPlantPosition(ref TreePlant, idx++);
					SetPlantColor(ref TreePlant);
					TreePlant._SetPlantFromConfig(Building.Type.TREE);
					TreePlant.PlantType = Building.Type.TREE;

					// You can always afford trees
					TreePlant._MakeOpaque();
					TreeButton.Disabled = false;
					break;
					
				case Building.Type.WIND:
					SetPlantPosition(ref WindPlant, idx++);
					SetPlantColor(ref WindPlant);
					WindPlant._SetPlantFromConfig(Building.Type.WIND);
					WindPlant.PlantType = Building.Type.WIND;
					C._GetGL()._ApplyOverload(ref WindPlant);

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(WindPlant.BuildCost)) {
						WindPlant._MakeTransparent();
						WindButton.Disabled = true;
					} else {
						WindPlant._MakeOpaque();
						WindButton.Disabled = false;
					}
					break;

				case Building.Type.WASTE:
					SetPlantPosition(ref WastePlant, idx++);
					SetPlantColor(ref WastePlant);
					WastePlant._SetPlantFromConfig(Building.Type.WASTE);
					WastePlant.PlantType = Building.Type.WASTE;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(WastePlant.BuildCost)) {
						WastePlant._MakeTransparent();
						WasteButton.Disabled = true;
					} else {
						WastePlant._MakeOpaque();
						WasteButton.Disabled = false;
					}
					break;

				case Building.Type.BIOMASS:
					SetPlantPosition(ref BiomassPlant, idx++);
					SetPlantColor(ref BiomassPlant);
					BiomassPlant._SetPlantFromConfig(Building.Type.BIOMASS);
					BiomassPlant.PlantType = Building.Type.BIOMASS;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(BiomassPlant.BuildCost)) {
						BiomassPlant._MakeTransparent();
						BiomassButton.Disabled = true;
					} else {
						BiomassPlant._MakeOpaque();
						BiomassButton.Disabled = false;
					}
					break;

				case Building.Type.RIVER:
					SetPlantPosition(ref RiverPlant, idx++);
					SetPlantColor(ref RiverPlant);
					RiverPlant._SetPlantFromConfig(Building.Type.RIVER);
					RiverPlant.PlantType = Building.Type.RIVER;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(RiverPlant.BuildCost)) {
						RiverPlant._MakeTransparent();
						RiverButton.Disabled = true;
					} else {
						RiverPlant._MakeOpaque();
						RiverButton.Disabled = false;
					}
					break;

				case Building.Type.PUMP:
					SetPlantPosition(ref PumpPlant, idx++);
					SetPlantColor(ref PumpPlant);
					PumpPlant._SetPlantFromConfig(Building.Type.PUMP);
					PumpPlant.PlantType = Building.Type.PUMP;

					// Check if we can afford the build
					if(!C._GetGL()._CheckBuildReq(PumpPlant.BuildCost)) {
						PumpPlant._MakeTransparent();
						PumpButton.Disabled = true;
					} else {
						PumpPlant._MakeOpaque();
						PumpButton.Disabled = false;
					}
					break;
			}
		}
	} 

	// Set of stupid button callbacks
	public void _OnGasButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, GasPlant);

		// Close the menu
		IsOpen = false;
	}
	public void _OnHydroButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, HydroPlant);

		// Close the menu
		IsOpen = false;
	}
	public void _OnSolarButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, SolarPlant);

		// Close the menu
		IsOpen = false;
	}
	public void _OnTreeButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, TreePlant);

		// Close the menu
		IsOpen = false;
	}
	public void _OnWindButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, WindPlant);

		// Close the menu
		IsOpen = false;
	}

	public void _OnWasteButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, WastePlant);

		// Close the menu
		IsOpen = false;
	}

	public void _OnBiomassButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, BiomassPlant);

		// Close the menu
		IsOpen = false;
	}

	public void _OnRiverButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, RiverPlant);

		// Close the menu
		IsOpen = false;
	}
	
	public void _OnPumpButtonPressed() {
		// Send out the selection
		EmitSignal(SignalName.SelectBuilding, PumpPlant);

		// Close the menu
		IsOpen = false;
	}
	// Closes the menu
	public void _OnCloseButtonPressed() {
		// Hide and close the menu
		IsOpen = false;
		Hide();
	}
}
