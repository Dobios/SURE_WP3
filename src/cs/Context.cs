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

// Global state of the game
// This will record all persistent data such as:
// - Game instance id
// - Player actions
// - Aggregated model data
// - Player choice statistics
// ...
public partial class Context : Node {

	private const float DEMAND_INC_S = 9;
	private const float DEMAND_INC_W = 22;
	private const float DEMAND_INIT_S = 240;
	private const float DEMAND_INIT_W = 260;

    [Signal]
    // Signals that the context has been updated by an external actor
    public delegate void UpdateContextEventHandler();

    [Signal]
    // Signals that the context has been updated by the player
    public delegate void UpdatePredictionEventHandler();

    [Signal] 
    // Signals that the language has been updated
    public delegate void UpdateLanguageEventHandler();

    // Whether or not the current instance is offline
    private bool Offline;

    // Current internal storage of the game instance's id
    private int ResId = -1;

    // Current internal storage of the game instance's name
    private string ResName = "";

    // The Curent Turn
    private int Turn = 0;

    // The total number of turns
    private int N_TURNS = 10;

    // Internal representation of the most recent data retrieved from the model
    private Model MSummer;
    private Model MWinter;

    // To estimate the demand
    private (float, float) DemandEstimate; // (DemandWinter, DemandSummer)

    // Dictionary to link power plant type to the number of plants
    private Dictionary<Building.Type, int> PPStats; 

    // Current language
    private Language Lang;

    // Reference to the game loop
    private GameLoop GL;

    // Stat for the number of shocks that were survived
    private int ShocksSurvived = 0;

    // ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
        // Initialize models
        MSummer = new Model(ModelSeason.SUMMER); 
        MWinter = new Model(ModelSeason.WINTER);

        // Initialize the language
        Lang = Language.Type.EN;

        // Initialize the internal stats
        ResetPPStats();
	}    

    // ==================== Public API ====================

    // Reset the context
    public void _Reset() {
         // Initialize models
        MSummer = new Model(ModelSeason.SUMMER); 
        MWinter = new Model(ModelSeason.WINTER);

        // Initialize the language
        _UpdateLanguage(Language.Type.EN);

        Turn = 0;

        // Initialize the internal stats
        ResetPPStats();
    }

    // Initialize the internal stats dictionary given a list of powerplants
    public void _InitializePPStats(List<PowerPlant> PPs) {
        // Reset the stats
        ResetPPStats();

        // Loop over all power plants and aggregate the data
        foreach(PowerPlant pp in PPs) {
            // Increment the number of plants of that type
            PPStats[pp.PlantType]++;
        }
    }

    // Sets the reference to the game loop
    public void _SetGLRef(GameLoop gl) {
        // Only assign if not null
        GL ??= gl;
    }

    // Sets the number of turns the game has
    // This should only be called by the game loop
    public void _SetNTurns(int nt) {
        N_TURNS = nt;
    }

    // Increases the number of shocks survived
    public void _IncShocksSurvived() {
        ShocksSurvived++;
    }

    // Updates the PPStats to modifiy the number of plants of a certain type
    public void _UpdatePPStats(Building b, bool inc=true) {
        // Sanity check
        if(!inc && PPStats[b] == 0) {
            // You can't decrement a type of building that wasn't build
            throw new Exception(b + " can't be decremented as it is at 0 in PPStats!");
        }
        // Increment or decrement the given statistic
        PPStats[b] = inc ? PPStats[b] + 1 : PPStats[b] - 1;
    }

    // Updates the internal model using data retrieved from the server
    // This method should only be called from the ModelController directly
    public void _UdpateModelFromServer(ModelSeason S, Availability A, Capacity C, Demand D) {
        // Check which model needs to be updated using the given season
        if(S == ModelSeason.WINTER) {
            MWinter._UpdateFields(A, C, D, ModelCoherencyState.SHARED);
        } else {
            MSummer._UpdateFields(A, C, D, ModelCoherencyState.SHARED);
        }

        // Signal that the context has been updated 
        EmitSignal(SignalName.UpdateContext);
    }

    // Wrapper for _UdpateModelFromServer that simply unfolds the model struct before calling the update method
    public void _UdpateModelFromServer(Model new_M) {
        _UdpateModelFromServer(new_M._Season, new_M._Availability, new_M._Capacity, new_M._Demand);
    }

    // Sets a new value in the internal model struct and sets a modification to be sent to server
    // This can only be done by adding or removing power plants
    public void _UpdateModelFromClient(PowerPlant pp, bool inc=true) {
        // Fetch the old value from the model
        float cur_cap_w = MWinter._Capacity._GetField(pp.PlantType);
        float cur_cap_s = MSummer._Capacity._GetField(pp.PlantType);

        // Compute the new capacity by suming the current one with the new addition
        float added_cap = (inc ? 1.0f : -1.0f) * pp._GetCapacity();
        float new_cap_w = cur_cap_w + added_cap;
        float new_cap_s = cur_cap_s + added_cap;

        // Only the capacity can be updated by player action
        MWinter._ModifyField(ModelCol.Type.CAP, pp.PlantType, new_cap_w);
        MSummer._ModifyField(ModelCol.Type.CAP, pp.PlantType, new_cap_w);

        // Signal that the predictions need to be updated
        EmitSignal(SignalName.UpdatePrediction);
    }

    // Updates the demand in the model manually
    // Given a value, we either add or subtract said value to the current demand
    public void _UpdateModelDemand(float v, bool inc=true, bool winter=true) {
        // Check if it's winter or summer
        if(winter) {
            // Compute the new demand
            float dem = MWinter._Demand.Base + ((inc ? -1 : 1) * v);
            MWinter._ModifyField(ModelCol.Type.DEM, Building.Type.NONE, dem);
        } else {
            // Compute the new demand
            float dem = MSummer._Demand.Base + ((inc ? -1 : 1) * v);
            MSummer._ModifyField(ModelCol.Type.DEM, Building.Type.NONE, dem);
        }

        // Signal that the context has been updated 
        EmitSignal(SignalName.UpdateContext);
    }

    // Updates the current ID (should only be done once per game)
    // Returns the internal value of the game id:
    // - newId if the id wasn't set 
    // - ResID if it already had a value (in this case the given id is ignored)
    public int _UpdateGameID(int newId) {
        // Check if the ID has been set yet
        if(ResId == -1) {
            // Set the game id only if it was not previously set
            ResId = newId;
        }
        return ResId;
    }

    // Updates the current name (can be done multiple times per game)
    public void _UpdateGameName(string name) {
        ResName = name;
    }

    // Updates the turn (should only be called by the GameLoop)
    public void _UpdateTurn(int newTurn) {
        Turn = newTurn;
    }

    // Updates the language the textcontroller is set to  
	public void _UpdateLanguage(Language l) {
		// Check that the given language is new
		if(l != Lang) {
			Lang = l;

            // Signal to controllers that the language has changed
            EmitSignal(SignalName.UpdateLanguage);
		}
		// Don't do anything if the languages are the same
	}

    // Clear the modified columns in each model
    public void _ClearModified() {
        MWinter._ClearModified();
        MSummer._ClearModified();
    }

	// Increments the language
	public void _NextLanguage() {
		Lang = ++Lang;

        // Signal to controllers that the language has changed
        EmitSignal(SignalName.UpdateLanguage);
	}

    // Get the number of survived shocks
    public int _GetShocksSurvived() => ShocksSurvived;

	// Retrieve the language name
	public string _GetLanguageName() => Lang.ToName();

    // Retrieve the language
    public Language _GetLanguage() => Lang;

    // Retrieves the building statistics for a given building type
    public int _GetPPStat(Building b) => PPStats[b];

    // Fetches the game ID and thorws an exception if it's not set
    // Exception: NullReferenceException -> ID has not been set yet
    public int _GetGameID() {
        // Check if the ID has been set yet or not
        if(ResId != -1) {
            return ResId;
        }
        // Otherwise throw an exception
        throw new NullReferenceException("Game ID has not been set yet!");
    }

    // Fetches the game name and returns an empty string if not set
    public string _GetGameName() => ResName;

    // Returns whether or not the model is valid
    public bool _GetModelValidity(ModelSeason S) => 
        S == ModelSeason.WINTER ? MWinter._IsValid() : MSummer._IsValid();

    // Returns the current turn
    public int _GetTurn() => Turn;

    // Returns the number of remaining turns
    public int _GetRemainingTurns() => N_TURNS - Turn;

    // Returns a reference to the game loop
    public GameLoop _GetGL() => GL;

    // Returns the requested model
    public Model _GetModel(ModelSeason S) => 
        S == ModelSeason.WINTER ? MWinter : MSummer;

    // Returns both models together (first winter then summer)
    public (Model, Model) _GetModels() => (MWinter, MSummer);

    // Toggles the online/offline modes
    public bool _ToggleOffline() {
        Offline = !Offline;
        return Offline;
    }

    // Retrieves the offline status of the game
    public bool _GetOffline() => Offline;

    // Increments the demand at the end of each turn
    public (float, float) _IncDemand() {
        DemandEstimate = (
            DemandEstimate.Item1 + DEMAND_INC_W, 
            DemandEstimate.Item2 + DEMAND_INC_S
        );
        return DemandEstimate;
    }

    // Initializes the demand
    public (float, float) _InitDemand() {
        DemandEstimate = (
            DEMAND_INIT_W, 
            DEMAND_INIT_S
        );
        return DemandEstimate;
    }

    // Getter for the demand
    public (float, float) _GetDemand() => DemandEstimate;
    

    // ==================== Internal Helpers ====================

    // Resets the internal PPStats to set all types to 0
    private void ResetPPStats() {
        // Make sure that the stats are clear
        if(PPStats != null) {
            PPStats.Clear();
        }

		// Initialize the internal stats
		PPStats = new() {
			{Building.Type.GAS, 0},
			{Building.Type.HYDRO, 0},
			{Building.Type.NUCLEAR, 0},
			{Building.Type.SOLAR, 0},
			{Building.Type.TREE, 0},
			{Building.Type.WIND, 0},
			{Building.Type.WASTE, 0},
			{Building.Type.BIOMASS, 0},
			{Building.Type.RIVER, 0},
			{Building.Type.PUMP, 0}
		};
	}
}
