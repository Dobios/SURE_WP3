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

// Represents a Power Plant object in the game
public partial class PowerPlant : Node2D {


	[Signal]
	public delegate void UpdatePlantEventHandler();
	
	// Check if the powerplant has an animation
	[Export]
	public bool HasAnimation = false;

	[Signal]
	/* Signals that the plant should be deleted and replaced by a buildbutton */
	public delegate void DeletePlantEventHandler(BuildButton bb, PowerPlant pp, bool remove);

	[Signal]
	/** Signals that a plant was up/downgraded with a multiplier 
	 * @param inc, wether the multiplier was increased or decreased
	 * @param cost, the cost of the upgrade (can be negative)
	 * @param pp, the plant that emitted the signal (in order to potentially revert the request) 
	 */
	public delegate void UpgradePlantEventHandler(bool inc, int cost, PowerPlant pp);


	[ExportGroup("Meta Parameters")]
	[Export] 
	// The name of the power plant that will be displayed in the game
	// This should align with the plant's type
	public string PlantName = "Power Plant";

	[Export] 
	// The type of the power plant, this is for internal use, other fields have to be 
	// updated to match the type of the building
	private Building.Type _PlantType = Building.Type.GAS;
	public Building PlantType;

	[Export]
	// Life cycle of a nuclear power plant
	public int NUCLEAR_LIFE_SPAN = 2; 
	public int DEFAULT_LIFE_SPAN = 11;

	[Export]
	// Defines whether or not the building is a preview
	// This is true when the building is being shown in the build menu
	// and is used to know when to hide certain fields
	public bool IsPreview = false; 

	[Export]
	// Defines the maximum number of elements this plant can contain
	public int MAX_ELEMENTS = 3;

	// The number of turns it takes to build this plant
	public int BuildTime = 0;

	// The initial cost of creating the power plant
	// This is what will be displayed in the build menu
	public int BuildCost = 0;

	// The turn at which the power plant stops working
	public int EndTurn = 10;

	// The cost that the power plant will require each turn to function
	public int InitialProductionCost = 0;

	// This is the amount of energy that the plant can produce per turn
	public int InitialEnergyCapacity = 100;

	// This is the amount of energy that the plant is able to produce given environmental factors
	public (float, float) InitialEnergyAvailability = (1.0f, 1.0f); // This is a percentage

	// Amount of pollution caused by the power plant (can be negative in the tree case)
	public int InitialPollution = 10;

	// Percentage of the total land used up by this power plant
	public float LandUse = 0.1f;

	// Percentage by which this plant reduces the biodiversity in the country
	// If negative, this will increase the total biodiversity
	public float BiodiversityImpact = 0.1f;

	// Internal metrics
	private int ProductionCost = 0;
	private int EnergyCapacity = 100;
	private (float, float) EnergyAvailability = (1.0f, 1.0f); // (Winter, Summer)
	private int Pollution = 10;

	// Life flag: Whether or not the plant is on
	private bool IsAlive = true;
	
	// Power off modulate color
	private Color GRAY = new Color(0.7f, 0.7f, 0.7f);
	private Color HOVER_COLOR = new Color(0.9f, 0.9f, 0.7f);
	private Color DEFAULT_COLOR = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	private Color RED = new Color(0.65f, 0f, 0.05f, 1.0f);
	private Color GREEN = new Color(0, 0.55f, 0.27f, 1.0f);
	
	private string AnimName;

	// Children Nodes
	private Sprite2D Sprite;
	private ColorRect NameR;
	private Label NameL;
	private Label PollL;
	private Label EnergyS;
	private Label EnergyW;
	private Label MoneyL;
	public CheckButton Switch;
	private Control PreviewInfo;
	private Label Price;
	private Label BTime;
	private Control Info;
	private Button Delete;
	private Control InfoBubble;
	private Button InfoButton;
	private ColorRect ResRect;
	private Label LandL;
	private Label BioL;
	private Label LifeSpan;
	private Label LifeSpanWarning;
	
	// The Area used to detect hovering
	private Area2D HoverArea;

	// Configuration controller
	private ConfigController CC;

	// Context
	private Context C;

	// Build Button associated to this plant
	private BuildButton BB;

	// Refund from a build deletion
	private int RefundAmount = -1;

	private bool DeleteSignalConnected = false;
	private bool EnergySignalConnected = false;
	private bool UpgradeSignalConnected = false;
	
	// Fields related to multipliers for wind and solar
	private int MultiplierValue = 1; // The number of elements the plant contains
	private ColorRect Multiplier; // The visual Multiplier amount display
	private Label MultiplierL; // The label containing the multiplier amount
	private Button MultInc; // Increases the multiplier
	private Button MultDec; // Decreases the multiplier
	private int MultiplierMax;


	// ==================== GODOT Method Overrides ====================
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch all children nodes
		Sprite = GetNode<Sprite2D>("Sprite");
		NameL = GetNode<Label>("NameRect/Name");
		NameR = GetNode<ColorRect>("NameRect");
		PollL = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Poll");
		EnergyS = GetNode<Label>("ResRect/EnergyS");
		EnergyW = GetNode<Label>("ResRect/EnergyW");
		MoneyL = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Prod");
		Switch = GetNode<CheckButton>("BuildInfo/Switch");
		CC = GetNode<ConfigController>("ConfigController");
		PreviewInfo = GetNode<Control>("PreviewInfo");
		Price = GetNode<Label>("PreviewInfo/Price");
		HoverArea = GetNode<Area2D>("HoverArea");
		Info = GetNode<Control>("BuildInfo");
		BTime = GetNode<Label>("PreviewInfo/Time");
		C = GetNode<Context>("/root/Context");
		Delete = GetNode<Button>("Delete");
		Multiplier = GetNode<ColorRect>("Multiplier");
		MultiplierL = GetNode<Label>("Multiplier/MultAmount");
		MultInc = GetNode<Button>("Multiplier/Inc");
		MultDec = GetNode<Button>("Multiplier/Dec");
		InfoButton = GetNode<Button>("InfoButton");
		ResRect = GetNode<ColorRect>("ResRect");
		LandL = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Land");
		BioL = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Bio");
		LifeSpan = GetNode<Label>("BuildInfo/ColorRect/ContainerN/LifeSpan");
		LifeSpanWarning = GetNode<Label>("LifeSpanWarning");

		// the delete button should only be shown on new constructions
		Delete.Hide();
		
		// Initialize plant type
		PlantType = _PlantType;

		// Hide unnecessary fields if we are in preview mode
		if(IsPreview) {
			Switch.Hide();
			PreviewInfo.Show();
			ResRect.Show();
		} else {
			PreviewInfo.Hide();
			Switch.Show();
			ResRect.Hide();
		}

		// Set the labels correctly
		NameL.Text = PlantName;
		EnergyS.Text = EnergyCapacity.ToString();
		EnergyW.Text = EnergyCapacity.ToString();
		MoneyL.Text = "💰/⌛ " +  ProductionCost.ToString();
		Price.Text = BuildCost.ToString() + "$";
		PollL.Text = "🏭 " + Pollution.ToString();
		BTime.Text = BuildTime.ToString();
		LandL.Text = (LandUse * 100).ToString();
		BioL.Text = (-BiodiversityImpact * 100).ToString();

		// Set plant life cycle
		EndTurn = (PlantType == Building.Type.NUCLEAR) ? NUCLEAR_LIFE_SPAN : DEFAULT_LIFE_SPAN;

		// Activate the plant
		ActivatePowerPlant();

		// Propagate to UI
		_UpdatePlantData();

		// Initially show the name rectangle
		NameR.Show();

		// Connect the various signals
		Switch.Toggled += _OnSwitchToggled;
		HoverArea.MouseEntered += OnArea2DMouseEntered;
		HoverArea.MouseExited += OnArea2DMouseExited;
		InfoButton.Pressed += OnInfoButtonPressed;
		Delete.Pressed += OnDeletePressed;
		MultInc.Pressed += OnMultIncPressed;
		MultDec.Pressed += OnMultDecPressed;

		// Reset multiplier
		MultiplierValue = 1;

		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());
		MultiplierMax = mult.MaxElements;

		// Check if the multiplier window should be shown
		if(MultiplierMax <= 1) {
			Multiplier.Hide();
		} else {
			//Multiplier.Show();
			MultInc.Show();
			MultDec.Hide();
		}
	}

	// Hides the powerplant info if the player clicks somewhere else on the map
	public override void _UnhandledInput(InputEvent E) {
		if(E is InputEventMouseButton MouseButton) {
			if(MouseButton.ButtonMask == MouseButtonMask.Left) {
				Info.Hide();
				ResRect.Hide();
				Multiplier.Hide();
			}
		}
	}

	// ==================== Power Plant Update API ====================

	// Shows the delete button
	public void _ShowDelete() {
		if(BuildTime < 1 || PlantType.type == Building.Type.TREE) {
			Delete.Show();
		}
	}

	// Applies a multiplier overload to the current value
	public void _OverloadMultiplier(int mo) {
		MultiplierMax = mo;
	}

	// Applies a build time overload to the powerplant
	public void _OverloadBuildTime(int mo) {
		BuildTime = mo;
	}

	// Increases the multiplier amount for the current plant 
	public void _IncMutliplier() {
		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Check that the max hasn't been reached
		if(MultiplierValue < MultiplierMax) {
			MultiplierValue++;

			// Apply the multiplier
			Pollution = (int)(Pollution * mult.Pollution);
			LandUse *= mult.LandUse;
			BiodiversityImpact *= mult.Biodiversity;
			ProductionCost = (int)(ProductionCost * mult.ProductionCost);
			EnergyCapacity += mult.Capacity;

			// Propagate update to the ui
			_UpdatePlantData();

			// Update the label and make sure that it is shown
			MultiplierL.Text = MultiplierValue.ToString();
			Multiplier.Show();
		}

		// Check if we can still increase
		if(MultiplierValue >= MultiplierMax) {
			MultInc.Hide();
		} 

		// Check if we can decrement now 
		if(MultiplierValue > 1) {
			MultDec.Show();
		}
	}

	// Decreases the multiplier amount for the current plant
	public void _DecMultiplier() {
		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Check that the max hasn't been reached
		if(MultiplierValue > 1) {
			MultiplierValue--;

			// Apply the multiplier
			Pollution = (int)(Pollution / Math.Max(1.0f, mult.Pollution));
			LandUse /= Math.Max(1.0f, mult.LandUse);
			BiodiversityImpact /= Math.Max(1.0f, mult.Biodiversity);
			ProductionCost = (int)(ProductionCost / Math.Max(1.0f, mult.ProductionCost));
			EnergyCapacity -= mult.Capacity;

			// Propagate update to the ui
			_UpdatePlantData();

			// Update the label and make sure that it is shown
			MultiplierL.Text = MultiplierValue.ToString();
			Multiplier.Show();
		}

		// Check if we can still increase
		if(MultiplierValue < MultiplierMax) {
			MultInc.Show();
		} 

		// Check if we can decrement now 
		if(MultiplierValue <= 1) {
			MultDec.Hide();
		}
	}

	// Makes the sprite transparent
	public void _MakeTransparent() {
		Sprite.Modulate = new (1, 0.75f, 0.75f, 0.5f);
	}

	// Makes the sprite opaque
	public void _MakeOpaque() {
		Sprite.Modulate = new(1, 1, 1, 1);
  }
  
	// Resets the plant
	public void _Reset() {
		// Disable the switch
		Switch.ButtonPressed = true;
		Switch.Disabled = false;
		Switch.Show();

		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Reset multiplier
		MultiplierValue = 1;
		MultiplierL.Text = MultiplierValue.ToString();

		// Check if the multiplier window should be shown
		if(MultiplierValue < MultiplierMax) {
			MultInc.Show();
		}
		MultDec.Hide();
		
		// Workaround to allow for an immediate update
		IsAlive = false;
		_OnSwitchToggled(false);
	}

	// Getter for the powerplant's current capacity
	public int _GetCapacity() => EnergyCapacity;

	// Getter for the Pollution amount
	public int _GetPollution() => Pollution;

	// Getter for the plant's production cost
	public int _GetProductionCost() => ProductionCost;

	// Getter for the current availability EA in [0.0, 1.0]
	public (float, float) _GetAvailability() => EnergyAvailability;

	// Getter for the powerplant's liveness status
	public bool _GetLiveness() => IsAlive;

	// Getter for the refund amount
	public int _GetRefund() => RefundAmount;

	// Getter for the delete signal connection flag
	public bool _GetDeleteConnectFlag() => DeleteSignalConnected;

	// Getter for the energy signal connection flag
	public bool _GetEnergyConnectFlag() => EnergySignalConnected;

	// Getter for the upgrade signal connection flag
	public bool _GetUpgradeConnectFlag() => UpgradeSignalConnected;

	// Sets the upgrade signal connection flag
	public void _SetUpgradeConnectFlag(bool v) {
		UpgradeSignalConnected = v;
	}

	// Sets the delete signal connection flag
	public void _SetDeleteConnectFlag() {
		DeleteSignalConnected = true;
	}

	// Sets the energy signal connection flag
	public void _SetEnergyConnectFlag() {
		EnergySignalConnected = true;
	}

	// Sets the reference to the buildbutton that created this plant
	public void _SetBuildButton(BuildButton bb) {
		// Only set the reference if no reference was already present
		BB ??= bb;
	}

	// Sets the values of the plant from a given config
	public void _SetPlantFromConfig(Building bt) {
		// Sanity check: only reset the plant if it's alive
		if(IsAlive) {
			// Read out the given plant's config
			PowerPlantConfigData PPCD = (PowerPlantConfigData)CC._ReadConfig(Config.Type.POWER_PLANT, bt.ToString());

			// Copy over the data to our plant
			CopyFrom(PPCD);

			// Propagate change to the UI
			_UpdatePlantData();
		}
	}

	// The availability of a plant is set from the data retrieved by the model
	// This method does that set.
	public void _SetAvailabilityFromContext() {
		// Get the model from the context
		(Model MW, Model MS)  = C._GetModels();
		
		// Extract the availability
		float avw = MW._Availability._GetField(PlantType);
		float avs = MS._Availability._GetField(PlantType);

		// Based on the number of built plants of this type, divide the availability
		EnergyAvailability = (avw / C._GetPPStat(PlantType), avs / C._GetPPStat(PlantType));
	}

	// Reacts to a new turn taking place
	public void _NextTurn() {

		// Only allow the delete button to be pressed during the turn the plant was built in
		// Allow for trees to be deleted at any time
		if(Delete.Visible) {
			Delete.Hide();
		}

		// Trees always have a delete button
		if(PlantType == Building.Type.TREE) {
			Delete.Show();
		}

		// Check if the plant should be deactivated
		if(EndTurn <= C._GetTurn()) {
			// Deactivate the plant
			KillPowerPlant();

			// Disable the switch
			Switch.ButtonPressed = false;
			Switch.Disabled = true;
			
			// Workaround to allow for an immediate update
			IsAlive = true;
			_OnSwitchToggled(false);
		} 
		
		// Update plants after every turn
		_UpdatePlantData();
	}

	// Update API for the private fields of the plant
	public void _UdpatePowerPlantFields(
		bool updateInit=false, // Whether or not to update the initial values as well
		int pol=-1, // pollution amount
		int PC=-1, // Production cost
		int EC=-1, // Energy capacity
		float AV_W=-1, // Winter availability
		float AV_S=-1 // Summer availability
	) {
		// Only update internal fields that where given a proper value
		Pollution = pol == -1 ? Pollution : pol;
		ProductionCost = PC == -1 ? ProductionCost : PC;
		EnergyCapacity = EC == -1 ? EnergyCapacity : EC;
		EnergyAvailability = (
			AV_W == -1 ? EnergyAvailability.Item1 : AV_W,
			AV_S == -1 ? EnergyAvailability.Item2 : AV_S
		);

		// Check for initial value updates
		if(updateInit) {
			InitialPollution = pol == -1 ? InitialPollution : pol;
			InitialProductionCost = PC == -1 ? InitialProductionCost : PC;
			InitialEnergyCapacity = EC == -1 ? InitialEnergyCapacity : EC;
			InitialEnergyAvailability = (
				AV_W == -1 ? InitialEnergyAvailability.Item1 : AV_W,
				AV_S == -1 ? InitialEnergyAvailability.Item2 : AV_S
			);
		}
	}

	// Forces the update of the isPreview state of the plant
	public void _UpdateIsPreview(bool n) {
		IsPreview = n;
		// If the plant is in preview mode, then it's being shown in the build menu
		// and thus should not have any visible interactive elements.
		if(IsPreview) {
			Switch.Hide();
			NameR.Show();
			PreviewInfo.Show();
			Multiplier.Hide();
			ResRect.Show();
		} 
		// When not in preview mode, the interactive elements should be visible
		else {
			// Retrieve the multiplier
			Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

			// Check if the multiplier window should be shown
			if(mult.MaxElements <= 1) {
				Multiplier.Hide();
			} else {
				Multiplier.Show();
				MultInc.Show();
				MultDec.Hide();
			}
			
			Switch.Show();
			PreviewInfo.Hide();
			NameR.Hide();
			ResRect.Hide();
		}
	}

	// Updates the UI label for the plant to the given name
	public void _UpdatePlantName(string name) {
		NameL.Text = name;
		PlantName = name;
	}

	// Updates the UI to match the internal state of the plant
	public void _UpdatePlantData() {
		// Update the preview state of the plant (in case this happens during a build menu selection)
		if(IsPreview) {
			Switch.Hide();
			NameR.Show();
			ResRect.Show();
		} else {
			NameR.Hide();
		}

		// Set the labels correctly
		NameL.Text = PlantName;
		EnergyS.Text = (EnergyCapacity * EnergyAvailability.Item2).ToString();
		EnergyW.Text = (EnergyCapacity * EnergyAvailability.Item1).ToString();
		MoneyL.Text = ProductionCost.ToString();
		Price.Text = BuildCost.ToString() + "$";
		PollL.Text = Convert.ToInt32(Pollution).ToString();
		BTime.Text = BuildTime.ToString();
		LandL.Text = Convert.ToInt32(LandUse * 100).ToString();
		BioL.Text = Convert.ToInt32(-BiodiversityImpact * 100).ToString();
		
		// Update label colors to represent levels
		if (BiodiversityImpact < 0) {
			BioL.Set("theme_override_colors/font_color", GREEN);
		}
		if (LandUse < 0) {
			LandL.Set("theme_override_colors/font_color", GREEN);
		}
		if (Pollution <= 0) {
			PollL.Set("theme_override_colors/font_color", GREEN);

		} else {
			PollL.Set("theme_override_colors/font_color", RED);
		}
		if (ProductionCost <= 0) {
			MoneyL.Set("theme_override_colors/font_color", GREEN);
		} else {
			MoneyL.Set("theme_override_colors/font_color", RED);
		}
		
		// Set the end turn based on the building type
		EndTurn = (PlantType == Building.Type.NUCLEAR) ? NUCLEAR_LIFE_SPAN : DEFAULT_LIFE_SPAN;
		
		LifeSpan.Text = (EndTurn - C._GetTurn()).ToString() + "⌛";
		
		if (EndTurn - C._GetTurn() == 1) {
			LifeSpanWarning.Show();
		} else {
			LifeSpanWarning.Hide();
		}
	}

	// ==================== Helper Methods ====================    

	// Sets the internal fields of a powerplant from a given config data
	private void CopyFrom(PowerPlantConfigData PPCD) {
		// Copy in the public fields
		BuildCost = PPCD.BuildCost;
		BuildTime = PPCD.BuildTime;
		EndTurn = PPCD.EndTurn;
		LandUse = PPCD.LandUse;
		BiodiversityImpact = PPCD.Biodiversity;

		// Copy in the private fields
		_UdpatePowerPlantFields(
			true, 
			PPCD.Pollution,
			PPCD.ProductionCost,
			PPCD.Capacity,
			PPCD.Availability_W,
			PPCD.Availability_S
		);
	}

	// Deactivates the current power plant
	private void KillPowerPlant() {
		// Save initial values
		InitialEnergyAvailability = EnergyAvailability;
		InitialEnergyCapacity = EnergyCapacity;
		InitialProductionCost = ProductionCost;
		InitialPollution = Pollution;

		IsAlive = false;
		EnergyCapacity = 0;
		EnergyAvailability = (0.0f, 0.0f);
		ProductionCost = 0;

		// Plant no longer pollutes when it's powered off
		Pollution = 0;
		
		// Changes the plant's color
		Modulate = GRAY;
		
		// Turns animation off
		if(HasAnimation) {
			AnimationPlayer AP = GetNode<AnimationPlayer>("AnimationPlayer");
			AnimName = AP.CurrentAnimation;
			AP.Play("RESET");
			AP.Stop();
		}
		
		// Propagate the new values to the UI
		_UpdatePlantData();
	}

	// Activates the power plant
	private void ActivatePowerPlant() {
		IsAlive = true;

		// Reset the internal metrics to their initial values
		EnergyCapacity = InitialEnergyCapacity;
		EnergyAvailability = InitialEnergyAvailability;
		ProductionCost = InitialProductionCost;
		Pollution = InitialPollution;

		_SetPlantFromConfig(PlantType);
		
		// Resets the plant's original color
		Modulate = DEFAULT_COLOR;
		
		if(HasAnimation) {
			AnimationPlayer AP = GetNode<AnimationPlayer>("AnimationPlayer");
			AP.Play(AnimName);
		}
		
		// Propagate the new values to the UI
		_UpdatePlantData();
	}

	// ==================== Button Callbacks ====================  
	
	// Reacts to the power switch being toggled
	// We chose to ignore the state of the toggle as it should be identical to the IsAlive field
	public void _OnSwitchToggled(bool pressed) {
		// Check the liveness of the current plant
		if(IsAlive) {
			// If the plant is currently alive, then kill it
			KillPowerPlant();
		} else {
			// If the plant is currently dead, then activate it
			ActivatePowerPlant();
		}

		// Update the UI
		_UpdatePlantData();

		// Singal that the plant was updated
		EmitSignal(SignalName.UpdatePlant);
	}
	
	// Hide the plant information when the mouse no longer hovers over the plant
	private void OnArea2DMouseEntered() {
		// Make sure that the plant isn't in the build menu
		if(!IsPreview) {
			NameR.Show();
			Sprite.SelfModulate = HOVER_COLOR;
			
		} else {
			Info.Show();
		}
	}

	// Display the plant information when the mouse is hovering over the plant
	private void OnArea2DMouseExited() {
		// Make sure that the plant isn't in the build menu
		if(!IsPreview) {
			NameR.Hide();
			Sprite.SelfModulate = DEFAULT_COLOR;
		} else {
			Info.Hide();
		}
	}
	
	// Press on the powerplant to get more info about it
	private void OnInfoButtonPressed(){
		Info.Visible = !Info.Visible;
		ResRect.Visible = !ResRect.Visible;
		
		// Toggle multiplier state if several elements are available
		if(MultiplierMax > 1) {
			Multiplier.Visible = !Multiplier.Visible;
		}
	}

	// Requests a deletion of the powerplant
	public void OnDeletePressed() {
		// Hide the current plant
		Hide();

		// Reset the button
		BB?._Reset();

		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Set the refund amount
		RefundAmount = BuildCost + (mult.Cost * (MultiplierValue - 1));
		
		// Reset the multiplier
		MultiplierValue = 1;

		// Reset the label
		MultiplierL.Text = MultiplierValue.ToString();

		// Reset the plant from config
		_SetPlantFromConfig(PlantType);
		C._GetGL()._ApplyOverloads();
		
		if(BB != null && RefundAmount > 0) {
			BB.AnimMoney.Text = "+" + RefundAmount.ToString() + "$";
			BB.AP.Play("Money+");
		}

		// Kill the deleted power plant
		KillPowerPlant();

		// Update the UI
		_UpdatePlantData();

		// Singal that the plant was updated
		EmitSignal(SignalName.UpdatePlant);

		// Signal that the plant was deleted
		EmitSignal(SignalName.DeletePlant, BB, this, true);

		// Reactivate the plant for future construction
		ActivatePowerPlant();

		_Reset();
	}

	// Reacts to the increase request by requesting it to the game loop
	// which will enact it if we have enough money
	private void OnMultIncPressed() {
		Debug.Print("UPGRADE PLANT");
		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Check that the max hasn't been reached
		if(MultiplierValue < MultiplierMax) {
			// check if the cost is more than 0 before playing the money anim
			if(C._GetGL()._CheckBuildReq(mult.Cost) && BB != null) {
				BB.AnimMoney.Text = "-" + mult.Cost.ToString() + "$";
				BB.AP.Play("Money-");
			}
			// Signal the request to the game loop
			EmitSignal(SignalName.UpgradePlant, true, mult.Cost, this);
		}
	}

	// Reacts to the decrease request by requesting it to the game loop
	// which will enact it if we have enough money
	private void OnMultDecPressed() {
		Debug.Print("DOWNGRADE PLANT");
		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());

		// Check that the min hasn't been reached
		if(MultiplierValue > 1) {
			if(mult.Cost > 0 && BB != null) {
				BB.AnimMoney.Text = "+" + mult.Cost.ToString() + "$";
				BB.AP.Play("Money+");
			}
			// Signal the request to the game loop
			EmitSignal(SignalName.UpgradePlant, false, -mult.Cost, this);
		}
	}

	// Reactivates dead nuclear plants
	public void _OnReintroduceNuclear() {
		if(PlantType.type == Building.Type.NUCLEAR) {
			Debug.Print("REACTIVATING PLANT");
			EndTurn = DEFAULT_LIFE_SPAN;

			// Reactivate the plant
			ActivatePowerPlant();

			// Disable the switch
			Switch.ButtonPressed = true;
			Switch.Disabled = false;
			
			// Workaround to allow for an immediate update
			IsAlive = true;
			_OnSwitchToggled(true);
		}
	}
}
