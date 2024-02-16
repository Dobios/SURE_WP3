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
using System.Linq;

// Models the overarching game loop, which controls every aspect of the game
// and makes sure that things are synchronized across game objects
public partial class GameLoop : Node2D {

	// Represents the various states that the game can be in
	public enum GameState { NOT_STARTED, PLAYING, ENDED };

	// Start year const
	private const int START_YEAR = 2020;

	// Context of the game
	private Context C;

	// The total number of turns in the game
	[Export]
	public int N_TURNS = 10;

	// The amount of money the player starts with (in millions of CHF)
	[Export]
	public int START_MONEY = 420;

	[Export]
	public static int BUDGET_PER_TURN = 280;

	// Internal game state
	private GameState GS;

	// Contains all of the PowerPlants in the scene
	private List<PowerPlant> PowerPlants;
	
	// Contains all of the BuildButtons in the scene
	private List<BuildButton> BBs;

	// Reference to the UI
	private UI _UI;

	// Reference to the buildmenu
	private BuildMenu BM;

	private PolicyManager PM;

	public MoneyData Money; // The current money the player has
	public MultiplierOverloads MO; // The current active overloads

	private int RemainingTurns; // The number of turns remaining until the end of the game

	// Updates and maintains various resources in the simulation
	private ResourceManager RM;

	// Model controller
	private ModelController MC;

	// Shock Window
	private Shock ShockWindow;
	
	private End EndScreen;

	// Other references used for reset
	private MainMenu MM;
	private Camera Cam;
	private Tutorial Tuto;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch context and model controller
		C = GetNode<Context>("/root/Context");
		// Set the number of turns in the context
		C._SetNTurns(N_TURNS);

		// Set the game loop reference
		C._SetGLRef(this);

		
		MC = GetNode<ModelController>("ModelController");
		ShockWindow = GetNode<Shock>("Shock");
		EndScreen = GetNode<End>("End");
		MM = GetNode<MainMenu>("Menu");
		Cam = GetNode<Camera>("World/Camera2D");
		Tuto = GetNode<Tutorial>("Tutorial");

		// Init Data
		GS = GameState.NOT_STARTED;
		RemainingTurns = N_TURNS;
		Money = new MoneyData(START_MONEY);
		PowerPlants = new List<PowerPlant>();
		BBs = new List<BuildButton>();
		MO = new();

		// Fetch initial nodes
		// Start with PowerPlants, in the begining there are only 2 PowerPlants Nuclear and Coal
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear2"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear3"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Hydro"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Pump"));
		PowerPlants.Add(GetNode<PowerPlant>("World/River"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Waste"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Biomass"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Solar"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Wind"));
		

		// Fill in build buttons
		BBs.Add(GetNode<BuildButton>("World/BuildButton"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton2"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton3"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton4"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton5"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton6"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton7"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton8"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton9"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton10"));

		// Fetch UI and BuildMenu
		_UI = GetNode<UI>("UI");
		BM = GetNode<BuildMenu>("BuildMenu");
		
		// Fetch resource manager
		RM = GetNode<ResourceManager>("ResourceManager");
		PM = GetNode<PolicyManager>("ResourceManager/PolicyManager");


		// Initially set all plants form their configs
		foreach(PowerPlant pp in PowerPlants) {
			pp._SetPlantFromConfig(pp.PlantType);

			// Add the power plants to the stats
			C._UpdatePPStats(pp.PlantType);

			// Connect the upgrade signal if missing
			if(!pp._GetUpgradeConnectFlag()) {
				pp.UpgradePlant += _OnUpgradePlant;
				pp._SetUpgradeConnectFlag(true);
			}

			// Make sure that nuclear plants can be reactivated
			ShockWindow.ReintroduceNuclear += pp._OnReintroduceNuclear;
		}

		// Connect Callback to each build button and give them a reference to the loop
		foreach(BuildButton bb in BBs) {
			bb.UpdateBuildSlot += _OnUpdateBuildSlot;

			// Record a reference to the game loop
			bb._RecordGameLoopRef(this);
		}
		
		// Temp for test pls change
		BM._RecordGameLoopRef(this);

		// Connect to the UI's signals
		_UI.NextTurn += _OnNextTurn;
		_UI.DebtRequest += _OnDebtRequest;
		C.UpdateContext += _OnContextUpdate;

		// Connect the shock related callbacks
		ShockWindow.Continue.Pressed += _OnShockWindowContinuePressed; // If the continue button is pressed at the end of a shock it triggers a new turn
		ShockWindow.SelectReaction += _OnShockSelectReaction;
		ShockWindow.ApplyReward += _OnShockApplyReward;
		RM.UpdateNextTurnState += _UI._OnNextTurnStateUpdate;
		_UI.ResetGame += _OnResetGame;

		// Finally make sure that the resource ui is up to date
		RM._UpdateResourcesUI(false, ref Money);
	}

	// ==================== Resource access API ====================
	
	// Checks if a current build is legal and if so updates the amount of money
	public bool _RequestBuild(int cost) {
		// You can always buy something that's free
		if(cost == 0) return true;

		// Check that we have enough money
		if(Money.Money >= cost) {
			// Spend some money
			Money.SpendMoney(cost);

			Debug.Print("Requested Build");

			// Notify the UI of the resource update
			UpdateResources();
			return true;
		}
		// Return false if we couldn't afford the build
		return false;
	}
	
	// Checks that we can afford a certain build
	public bool _CheckBuildReq(int cost) => Money.Money >= cost || cost == 0;

	// Getter for the internal list of built powerplants
	public List<PowerPlant> _GetPowerPlants() => PowerPlants;

	// Retrieves the current resource estimates from the resource manager
	public (Energy, Environment, Support) _GetResources() => RM._GetResources();

	// Returns the resource manager itself
	public ResourceManager _GetRM() => RM;

	public PolicyManager _GetPM() => PM;

	// Enable public access to a resource update request
	public void _UpdateResourcesUI() {
		// Request a non-new turn update of the UI
		UpdateResources();
	}

	// Apply the current overloads to all plants
	public void _ApplyOverloads() {
		PowerPlants.ForEach(pp => _ApplyOverload(ref pp));
	}

	// Checks for overloads and applies then to the given plant if necessary
	public void _ApplyOverload(ref PowerPlant PP) {
		// Figure out what kind of overload the plant needs and apply it
		if(PP.PlantType.type == Building.Type.WIND) {
			if(MO.WindMax != -1) {
				PP._OverloadMultiplier(MO.WindMax);
			}
			if(MO.WindBuildTime != -1) {
				PP._OverloadBuildTime(MO.WindBuildTime);
			}
		}
		if(PP.PlantType.type == Building.Type.SOLAR) {
			if(MO.SolarMax != -1) {
				PP._OverloadMultiplier(MO.SolarMax);
			}
			if(MO.SolarBuildTime != -1) {
				PP._OverloadBuildTime(MO.SolarBuildTime);
			}
		}
	}

	// Applies a given effect
	public void _ApplyEffect(Effect e) {
		// Handle each resource type differently
		switch(e.RT) {
			// Some resources are managed by the resource manager
			case ResourceType.ENERGY_W:
			case ResourceType.ENERGY_S:
			case ResourceType.ENVIRONMENT:
			case ResourceType.SUPPORT:
				RM._ApplyEffect(e);
			break;

			// The other types must be handled separately
			case ResourceType.MONEY:
				Money.Money += (int)e.Value;
			break;
			case ResourceType.WIND_MULT_MAX:
				MO.WindMax = (int)e.Value;
			break;
			case ResourceType.WIND_BUILD_TIME:
				MO.WindBuildTime = (int)e.Value;
			break;
			case ResourceType.SOLAR_MULT_MAX:
				MO.SolarMax = (int)e.Value;
			break;
			case ResourceType.SOLAR_BUILD_TIME:
				MO.SolarBuildTime = (int)e.Value;
			break;

			default:
			break;
		}

		// Apply the new overloads
		_ApplyOverloads();
	}

	// ==================== Internal Helpers ====================
	
	// Propagates resource updates to the UI
	private void UpdateResources(bool newturn=false) {
		// Update the ressource manager
		if(newturn) {
			RM._NextTurn(ref Money);
		} else {
			RM._UpdateResourcesUI(false, ref Money);
		}

		// Update Money UI
		_UI._UpdateData(
			UI.InfoType.MONEY,
			Money.Budget, 
			Money.Production,
			Money.Build,
			Money.Money,
			Money.Imports
		);

		// Udpate debt
		_UI._UpdateDebtResource(Money.Debt);

		// Propagate the update to the UI
		_UI._UpdateUI();
	}

	// Computes which turn we are at
	private int GetTurn() => N_TURNS - RemainingTurns;

	// Triggers the selection and display of a new shock
	private bool DisplayShock() {
		// Retrieve the resources
		(Energy E, Environment Env, Support Sup) = RM._GetResources();

		// Select a new shock
		if(!ShockWindow._SelectNewShock(Money, E, Env, Sup)) {
			return false;
		}

		// Show the shock
		ShockWindow._Show(Money, E, Env, Sup);
		return true;
	}

	// ==================== Main Game Loop Methods ====================  

	// Initializes all of the data that is propagated across the game
	// This only happens once at the begining of the playthrough
	// This is done in a way that does not interact with the model
	private void StartGameOffline() {
		// Update the game state
		GS = GameState.PLAYING;

		// Initialize the context stats
		C._InitializePPStats(PowerPlants);

		// Initialize the demand
		C._InitDemand();

		// Perform initial Resouce update
		UpdateResources(true);

		// Set the initial power plants and build buttons
		RM._UpdatePowerPlants(PowerPlants);
		RM._UpdateBuildButtons(BBs);

		// Update the UI
		_UI._UpdateUI();

		// Initialize resources
		_UI._OnUpdatePrediction();
		RM._UpdateResourcesUI(false, ref Money);

		RM._StartGame(ref Money);
	}

	// Initializes all of the data that is propagated across the game
	// This only happens once at the begining of the playthrough
	private void StartGame() {
		// Update the game state
		GS = GameState.PLAYING;

		// Initialize the context stats
		C._InitializePPStats(PowerPlants);

		// Initialize the model
		MC._InitModel();

		// Perform initial Resouce update
		UpdateResources(true);

		// Update the model to include all of the initial plants
		foreach(PowerPlant pp in PowerPlants) {
			C._UpdateModelFromClient(pp);
		}

		// Update model with our current data
		foreach((ModelCol mc, Building b, float val) in C._GetModel(ModelSeason.WINTER).ModifiedCols) {
			// Create a new request for each modified filed in our model
			MC._UpsertModelColumnDataAsync(mc, b);
		}

		// Create a fetch request to get the data
		MC._FetchModelData();

		// Clear the model's modified columns
		C._ClearModified();

		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		// All of the plants must be in the model for the availability to be set
		// This is why we require two separate loops
		// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		// Now that we have the initial model data, update the availability
		foreach(PowerPlant pp in PowerPlants) {
			pp._SetAvailabilityFromContext();			
		}

		// Set the initial power plants and build buttons
		RM._UpdatePowerPlants(PowerPlants);
		RM._UpdateBuildButtons(BBs);

		// Update the UI
		_UI._UpdateUI();

		// Initialize resources
		_UI._OnUpdatePrediction();
		RM._UpdateResourcesUI();
	}

	// Triggers all of the updates across the whole game at the beginnig of the turn
	// A new turn is triggered when the player presses the next turn button in the UI.
	// This is done in a way that does not interact with the model
	private void NewTurnOffline() {
		// Hide the shock window
		ShockWindow.Hide();

		// Decerement the remaining turns and check for game end
		if((GS == GameState.PLAYING) && (RemainingTurns-- > 1)) {

			// Update the Context's turn count
			C._UpdateTurn(GetTurn());

			// Update the demand
			C._IncDemand();

			// Update Resources 
			UpdateResources(true);
			RM._UpdateResourcesUI();

		} else if(RemainingTurns <= 1) {
			// Update the Context's turn count
			C._UpdateTurn(GetTurn());

			// End the game if all turns have been spent
			EndGame();
		}
	}

	// Triggers all of the updates across the whole game at the beginnig of the turn
	// A new turn is triggered when the player presses the next turn button in the UI.
	private void NewTurn() {
		// Hide the shock window
		ShockWindow.Hide();

		// Decerement the remaining turns and check for game end
		if((GS == GameState.PLAYING) && (RemainingTurns-- > 1)) {

			// Update the Context's turn count
			C._UpdateTurn(GetTurn());

			// Update model with our current data
			foreach((ModelCol mc, Building b, float val) in C._GetModel(ModelSeason.WINTER).ModifiedCols) {
				// Create a new request for each modified filed in our model
				MC._UpsertModelColumnDataAsync(mc, b);
			}

			// Create a fetch request to get the summer data
			MC._FetchModelDataAsync();

			// Clear the model's modified columns
			C._ClearModified();

			// Update Resources 
			UpdateResources(true);
			RM._UpdateResourcesUI();

		} else if(RemainingTurns <= 1) {
			// Update the Context's turn count
			C._UpdateTurn(GetTurn());

			// End the game if all turns have been spent
			EndGame();
		}
	}

	// Triggers all of the actions that happen at the end of the game
	// The end of the game is triggered when the number of remaining turns reaches 0
	private void EndGame() {
		// Update the game state 
		GS = GameState.ENDED;

		// Deactivate all buttons
		foreach(var bb in BBs) {
			bb._Disable();
		}

		// Retrieve the current resources
		(Energy Eng, Environment Env, Support Sup) = RM._GetResources();
		
		// Prepare and show the final end screen
		EndScreen._SetEndStats(
			C._GetShocksSurvived(),
			Eng.SupplyWinter,
			Eng.SupplySummer,
			Money.Money < 0, // Are we currently in debt?
			Sup.Value,
			Env.PollutionBarValue() <= 0, // Check that the pollution is below 0 (netzero)
			Env.EnvBarValue()
		);
		EndScreen.Show();
	}

	// Applies a given shock effect
	private void ApplyShockEffect(Reward SE) {
		// Apply each individual effect
		foreach(Effect e in SE.Effects) {
			// Figure out which resource to affect
			switch(e.RT) {
				// The game loop only handles money (for some reason lol)
				case ResourceType.MONEY:
					Money.Money += (int)e.Value;
					break;
				// All other resources are handled by the resource manager
				default:
					RM._ApplyEffect(e);
					break;
			}
		}
	}

	// ==================== Interaction Callbacks ====================
	
	// Updates the internal lists on every build slot update
	public void _OnUpdateBuildSlot(BuildButton bb, PowerPlant pp, bool remove) {
		// Check if the update was a power plant addition or removal
		if(remove) {
			//Sanity Check
			Debug.Assert(PowerPlants.Contains(pp));

			// Disconnect the upgrade signal if connected
			if(pp._GetUpgradeConnectFlag()) {
				pp.UpgradePlant -= _OnUpgradePlant;
				pp._SetUpgradeConnectFlag(false);
			}

			// Destroy the power plant
			PowerPlants.Remove(pp);

			// Update the context stats
			C._UpdatePPStats(pp.PlantType, false);

			// Connect the new build button to our signal
			if(!bb.IsConnected(BuildButton.SignalName.UpdateBuildSlot, 
					Callable.From<BuildButton, PowerPlant, bool>(_OnUpdateBuildSlot))) {
				bb.UpdateBuildSlot += _OnUpdateBuildSlot;
			}

			// Make sure that it has access to the game loop
			bb._RecordGameLoopRef(this);

			// Replace it with the new build button
			BBs.Add(bb);

			// Reset the build button
			bb._Reset();

			// Reimburse the price 
			Money.SpendMoney(-pp._GetRefund());

			// Notify the UI of the resource update
			UpdateResources();
		} else {
			// Sanity Check
			if(BBs.Contains(bb)) {
				// Destroy the Build Button
				BBs.Remove(bb);
			}

			// Replace it with the new power plant
			PowerPlants.Add(pp);
			PowerPlants = PowerPlants.Distinct().ToList();

			// Connect the upgrade signal if missing
			if(!pp._GetUpgradeConnectFlag()) {
				pp.UpgradePlant += _OnUpgradePlant;
				pp._SetUpgradeConnectFlag(true);
			}

			// Connect to the delete signal
			if(!pp._GetDeleteConnectFlag()) {
				pp.DeletePlant += _OnUpdateBuildSlot;
				pp._SetDeleteConnectFlag();
			}

			// Show the delete button
			pp._ShowDelete();

			// Update the context stats
			C._UpdatePPStats(pp.PlantType);
		}

		// Propagate the updates to the resource manager
		RM._UpdatePowerPlants(PowerPlants);
		RM._UpdateBuildButtons(BBs);
		RM._UpdateResourcesUI(true);
	}

	// Triggers a new turn if the game is currently acitve
	public void _OnNextTurn() {
		// Display a shock
		// If no shock was selected go to the next turn
		if(!DisplayShock()) {
			// Check wether or not offline mode is active
			if(C._GetOffline()) {
				NewTurnOffline();
			} else {
				NewTurn();
			}
		}
	}

	// Reacts to a context update
	public void _OnContextUpdate() {
		// Propate update to the UI
		UpdateResources();
		RM._UpdateResourcesUI();
	}

	// Updates the resources after a reaction to a shock has been selected
	public void _OnShockSelectReaction(int id) {
		// Fetch the reaction effect
		List<Reward> reactions = ShockWindow._GetReactions();

		// Sanity check: make sure that the id is valid
		if(reactions.Count <= id) {
			throw new ArgumentException("Invalid ID was given to select a shock reaction: " + id + " >= " + reactions.Count);
		}

		// Apply the reaction
		ApplyShockEffect(reactions[id]);

		// Hide all reaction buttons and show the continue one
		ShockWindow._HideReactions();
	}

	// Updates the resources to apply a given shock reward
	public void _OnShockApplyReward() {
		// Extract the reward
		Reward reward = ShockWindow._GetReward();

		// Apply the reward
		ApplyShockEffect(reward);
	}

	// Reacts to the play button being pressed
	public void _OnPlayPressed() {
		if(C._GetOffline()) {
			StartGameOffline();
		} else {
			StartGame();
		}
	}

	// Reacts to the shock window's continue button being pressed
	public void _OnShockWindowContinuePressed() {
		// Reset the shock window animation (if you find a better place, go for it)
		ShockWindow.AP.Play("RESET");
		
		// Check wether or not offline mode is active
		if(C._GetOffline()) {
			NewTurnOffline();
		} else {
			NewTurn();
		}
	}

	// Resets the game's state
	public void _OnResetGame() {
		// Create a copy of the power plants array
		PowerPlant[] tmp = new PowerPlant[PowerPlants.Count];
		PowerPlants.Distinct().ToList().CopyTo(tmp);
		MO = new();
		PM._Reset();
		

		// Delete all plants
		foreach(var pp in tmp) {
			// Disconnect upgrade signal if missing
			if(pp._GetUpgradeConnectFlag()) {
				pp.UpgradePlant -= _OnUpgradePlant;
				pp._SetUpgradeConnectFlag(false);
			}
			pp.OnDeletePressed();
		}

		// reinit Data
		GS = GameState.NOT_STARTED;
		RemainingTurns = N_TURNS;
		PowerPlants.Clear();
		BBs.Clear();

		// Reset the context
		C._Reset();

		// Reset the resource manager
		RM._Reset();

		// Start with PowerPlants, in the begining there are only 2 PowerPlants Nuclear and Coal
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear2"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Nuclear3"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Hydro"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Pump"));
		PowerPlants.Add(GetNode<PowerPlant>("World/River"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Waste"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Biomass"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Solar"));
		PowerPlants.Add(GetNode<PowerPlant>("World/Wind"));

		// Fill in build buttons
		BBs.Add(GetNode<BuildButton>("World/BuildButton"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton2"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton3"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton4"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton5"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton6"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton7"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton8"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton9"));
		BBs.Add(GetNode<BuildButton>("World/BuildButton10"));

		// Set the number of turns in the context
		C._SetNTurns(N_TURNS);

		// Set the game loop reference
		C._SetGLRef(this);

		// Initially set all plants form their configs
		foreach(PowerPlant pp in PowerPlants) {
			pp._SetPlantFromConfig(pp.PlantType);

			// Connect the upgrade signal if missing
			if(!pp._GetUpgradeConnectFlag()) {
				pp.UpgradePlant += _OnUpgradePlant;
				pp._SetUpgradeConnectFlag(true);
			}

			// Reset the plant
			pp._Reset();

			// Show the plant
			pp.Show();
		}

		// Connect Callback to each build button and give them a reference to the loop
		foreach(BuildButton bb in BBs) {
			// Rest the build button
			bb._Reset();
			
			bb.UpdateBuildSlot += _OnUpdateBuildSlot;

			// Record a reference to the game loop
			bb._RecordGameLoopRef(this);

			// Show the button
			bb.Show();
		}

		// Reset the arrays
		RM._UpdateBuildButtons(BBs);
		RM._UpdatePowerPlants(PowerPlants);
		C._ClearModified();
		C._InitializePPStats(PowerPlants);

		// Show the main menu
		MM.Show();

		// Reset the year
		_UI.SetNextYearsNoAnim(START_YEAR);

		// Update the UI
		RM._UpdateResourcesUI();
		_UI._UpdateUI();

		// Reset money and propagate resource update to UI
		Money = new MoneyData(START_MONEY);
		_UpdateResourcesUI();

		// Reset the camera's position
		Cam._ResetPos();
		
		// Reset the tutorial and main menu
		Tuto._Reset();
		MM._Reset();
	}

	// Reacts to the reception of a debt request
	public void _OnDebtRequest(int debt, int borrowed) {
		// Acquire more debt
		Money.AcquireDebt(debt, borrowed);

		// Update Money UI
		_UI._UpdateData(
			UI.InfoType.MONEY,
			Money.Budget, 
			Money.Production,
			Money.Build,
			Money.Money,
			Money.Imports
		);

		// Update the UI to reflect the new debt
		_UI._UpdateDebtResource(Money.Debt);
	}

	// Reacts to a plant upgrade request by checking if we can afford it or not
	public void _OnUpgradePlant(bool inc, int cost, PowerPlant pp) {
		// Check if the upgrade can be afforded
		if(_RequestBuild(cost)) {
			// Enact the upgrade or downgrade
			if(inc) {
				Debug.Print("Upgrading Plant");
				pp._IncMutliplier();
			} else {
				Debug.Print("Downgrading Plant");
				pp._DecMultiplier();
			}
			_UpdateResourcesUI();
		}
	}
	
	public override void _UnhandledInput(InputEvent E) {
		if (E is InputEventKey eventKey) {
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape) {
				GetTree().Quit();
			}
		} 
	}
}
