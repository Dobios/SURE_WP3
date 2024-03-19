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
	[Export]
	public bool HasMultSprite = false;

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
	
	// Signal the position of the selected plant for a zoom effect
	[Signal]
	public delegate void ZoomSignalEventHandler(Vector2 PlantPos);


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
	public float InitialPollution = 10f;

	// Percentage of the total land used up by this power plant
	public float LandUse = 0.1f;

	// Percentage by which this plant reduces the biodiversity in the country
	// If negative, this will increase the total biodiversity
	public float BiodiversityImpact = 0.1f;

	// Internal metrics
	private int ProductionCost = 0;
	private int EnergyCapacity = 100;
	private (float, float) EnergyAvailability = (1.0f, 1.0f); // (Winter, Summer)
	private float Pollution = 10f;

	// Life flag: Whether or not the plant is on
	private bool IsAlive = true;
	
	// Power off modulate color
	private Color GRAY = new Color(0.7f, 0.7f, 0.7f);
	private Color HOVER_COLOR = new Color(0.9f, 0.9f, 0.7f);
	private Color DEFAULT_COLOR = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	private Color RED = new Color(1f, 0.1f, 0.2f, 1.0f);
	private Color GREEN = new Color(0.3f, 1f, 0.1f, 1.0f);
	
	private string AnimName;

	// Children Nodes
	private Sprite2D Sprite;
	private Sprite2D Sprite2;
	private Sprite2D Sprite3;
	private ColorRect NameR;
	private Label NameL;
	private Label NameBI;
	private Label PollN;
	private Label PollL;
	private Label EnergyS;
	private Label EnergyW;
	private Label EnergySL;
	private Label EnergyWL;
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
	private Label LandN;
	private Label BioN;
	private Label LandL;
	private Label BioL;
	private Label LifeSpan;
	private Label LifeSpanWarning;
	private Label MultProd;
	private Label MultPoll;
	private Label MultLand;
	private Label MultBio;
	private Label MultPrice;
	private Label MultWinterE;
	private Label MultSummerE;
	private Sprite2D NoMoneySprite;
	
	public AnimationPlayer AP;
	private Label AnimMoney;
	
	// The Area used to detect hovering
	private Area2D HoverArea;
	private CollisionShape2D CollShape;

	// Configuration controller
	private ConfigController CC;

	// Context
	private Context C;
	
	// Text controller for the dynamic text
	private TextController TC;

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
	private TextureButton MultInc; // Increases the multiplier
	private TextureButton MultDec; // Decreases the multiplier
	private int MultiplierMax;


	// ==================== GODOT Method Overrides ====================
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch all children nodes
		Sprite = GetNode<Sprite2D>("Sprite");
		if(HasMultSprite) {
			Sprite2 = GetNode<Sprite2D>("Sprite2");
			Sprite3 = GetNode<Sprite2D>("Sprite3");
		}
		NameBI = GetNode<Label>("BuildInfo/Name");
		NameL = GetNode<Label>("NameRect/Name");
		NameR = GetNode<ColorRect>("NameRect");
		PollN = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Poll");
		PollL = GetNode<Label>("BuildInfo/ColorRect/ContainerL/Poll");
		EnergyS = GetNode<Label>("ResRect/EnergyS");
		EnergyW = GetNode<Label>("ResRect/EnergyW");
		EnergySL = GetNode<Label>("BuildInfo/EnergyContainer/Summer/BuildMenuNumHole/SummerE");
		EnergyWL = GetNode<Label>("BuildInfo/EnergyContainer/Winter/BuildMenuNumHole2/WinterE");
		MoneyL = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Prod");
		Switch = GetNode<CheckButton>("BuildInfo/Switch");
		CC = GetNode<ConfigController>("ConfigController");
		PreviewInfo = GetNode<Control>("PreviewInfo");
		Price = GetNode<Label>("PreviewInfo/Price");
		HoverArea = GetNode<Area2D>("HoverArea");
		CollShape = GetNode<CollisionShape2D>("HoverArea/CollisionShape2D");
		Info = GetNode<Control>("BuildInfo");
		BTime = GetNode<Label>("PreviewInfo/Time");
		C = GetNode<Context>("/root/Context");
		Delete = GetNode<Button>("Delete");
		Multiplier = GetNode<ColorRect>("BuildInfo/EnergyContainer/Multiplier");
		MultiplierL = GetNode<Label>("BuildInfo/EnergyContainer/Multiplier/MultAmount");
		MultInc = GetNode<TextureButton>("BuildInfo/EnergyContainer/Multiplier/Inc");
		MultDec = GetNode<TextureButton>("BuildInfo/EnergyContainer/Multiplier/Dec");
		InfoButton = GetNode<Button>("InfoButton");
		ResRect = GetNode<ColorRect>("ResRect");
		LandN = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Land");
		LandL = GetNode<Label>("BuildInfo/ColorRect/ContainerL/Land");
		BioN = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Bio");
		BioL = GetNode<Label>("BuildInfo/ColorRect/ContainerL/Bio");
		LifeSpan = GetNode<Label>("BuildInfo/ColorRect/ContainerN/LifeSpan");
		LifeSpanWarning = GetNode<Label>("LifeSpanWarning");
		AP = GetNode<AnimationPlayer>("AP");
		AnimMoney = GetNode<Label>("Money");
		MultProd = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Prod/MultProd");
		MultPoll = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Poll/MultPoll");
		MultLand = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Land/MultLand");
		MultBio = GetNode<Label>("BuildInfo/ColorRect/ContainerN/Bio/MultBio");
		MultPrice = GetNode<Label>("BuildInfo/EnergyContainer/Multiplier/MultPrice");
		MultWinterE = GetNode<Label>("BuildInfo/MultWinterE");
		MultSummerE = GetNode<Label>("BuildInfo/MultSummerE");
		NoMoneySprite = GetNode<Sprite2D>("NoMoneySprite");
		
		// Fetch the text controller
		TC = GetNode<TextController>("/root/TextController");

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
		NameBI.Text = PlantName;
		NameL.Text = PlantName;
		EnergyS.Text = EnergyCapacity.ToString();
		EnergyW.Text = EnergyCapacity.ToString();
		EnergySL.Text = EnergyCapacity.ToString();
		EnergyWL.Text = EnergyCapacity.ToString();
		MoneyL.Text = "💰/⌛ " +  ProductionCost.ToString();
		Price.Text = BuildCost.ToString() + "$";
		PollN.Text = "🏭 " + Pollution.ToString();
		BTime.Text = BuildTime.ToString();
		LandN.Text = (LandUse * 100).ToString();
		BioN.Text = (-BiodiversityImpact * 100).ToString();
		
		// Set text labels coorectly
		PollL.Text = TC._GetText("labels.xml", "infobar", "label_pollution");
		LandL.Text = TC._GetText("labels.xml", "infobar", "label_land");
		BioL.Text = TC._GetText("labels.xml", "infobar", "label_biodiversity");
		

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
		MultInc.MouseEntered += OnMultIncMouseEntered;
		MultInc.MouseExited += OnMultIncMouseExited;
		MultDec.MouseEntered += OnMultDecMouseEntered;
		MultDec.MouseExited += OnMultDecMouseExited;

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
			Pollution *= mult.Pollution;
			LandUse *= mult.LandUse;
			BiodiversityImpact *= mult.Biodiversity;
			ProductionCost = (int)(ProductionCost * mult.ProductionCost);
			EnergyCapacity += mult.Capacity;

			// Propagate update to the ui
			_UpdatePlantData();

			// Update the label and make sure that it is shown
			MultiplierL.Text = MultiplierValue.ToString();
			Multiplier.Show();
			
			if(HasMultSprite) {
				if(MultiplierValue == 2) {
					Sprite.Hide();
					Sprite2.Show();
					Sprite3.Hide();
				}
				if(MultiplierValue == 3) {
					Sprite.Hide();
					Sprite2.Hide();
					Sprite3.Show();
				}
			}
		}

		// Check if we can still increase
		if(MultiplierValue >= MultiplierMax) {
			MultInc.Hide();
			HideMultInfo();
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
			Pollution /= mult.Pollution;
			LandUse /= Math.Max(1.0f, mult.LandUse);
			BiodiversityImpact /= Math.Max(1.0f, mult.Biodiversity);
			ProductionCost = (int)(ProductionCost / Math.Max(1.0f, mult.ProductionCost));
			EnergyCapacity -= mult.Capacity;

			// Propagate update to the ui
			_UpdatePlantData();

			// Update the label and make sure that it is shown
			MultiplierL.Text = MultiplierValue.ToString();
			Multiplier.Show();
			
			if(HasMultSprite){
				if(Sprite2 != null && MultiplierValue == 2) {
					Sprite.Hide();
					Sprite2.Show();
					Sprite3.Hide();
				}
				if(Sprite2 != null && MultiplierValue == 1) {
					Sprite.Show();
					Sprite2.Hide();
					Sprite3.Hide();
				}
			}
			
		}

		// Check if we can still increase
		if(MultiplierValue < MultiplierMax) {
			MultInc.Show();
		} 

		// Check if we can decrement now 
		if(MultiplierValue <= 1) {
			MultDec.Hide();
			HideMultInfo();
		}
	}

	// Makes the sprite transparent
	public void _MakeTransparent() {
		Sprite.Modulate = new (1, 0.75f, 0.75f, 0.5f);
		CollShape.Disabled = true;
		NoMoneySprite.Show();
	}

	// Makes the sprite opaque
	public void _MakeOpaque() {
		Sprite.Modulate = new(1, 1, 1, 1);
		CollShape.Disabled = false;
		NoMoneySprite.Hide();
  }
  
	// Resets the plant
	public void _Reset() {
		// Disable the switch
		Switch.ButtonPressed = true;
		Switch.Disabled = false;
		Switch.Show();
		MultInc.Disabled = false;
		MultDec.Disabled = false;
		
		if(HasMultSprite) {
			Sprite.Show();
			Sprite2.Hide();
			Sprite3.Hide();
		}

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
		HideMultInfo();
		
		// Workaround to allow for an immediate update
		IsAlive = false;
		_OnSwitchToggled(false);
	}

	// Getter for the powerplant's current capacity
	public int _GetCapacity() => EnergyCapacity;

	// Getter for the Pollution amount
	public float _GetPollution() => Pollution;

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
		float pol=-1, // pollution amount
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
				HideMultInfo();
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
		NameBI.Text = name;
		PlantName = name;
		
		// Update buildInfo text labels correctly
		PollL.Text = TC._GetText("labels.xml", "infobar", "label_pollution");
		LandL.Text = TC._GetText("labels.xml", "infobar", "label_land");
		BioL.Text = TC._GetText("labels.xml", "infobar", "label_biodiversity");
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
			ResRect.Hide();
		}

		// Set the labels correctly
		NameL.Text = PlantName;
		NameBI.Text = PlantName;
		EnergyS.Text = (EnergyCapacity * EnergyAvailability.Item2).ToString();
		EnergySL.Text = (EnergyCapacity * EnergyAvailability.Item2).ToString();
		EnergyW.Text = (EnergyCapacity * EnergyAvailability.Item1).ToString();
		EnergyWL.Text = (EnergyCapacity * EnergyAvailability.Item1).ToString();
		MoneyL.Text = ProductionCost.ToString();
		Price.Text = BuildCost.ToString() + "$";
		PollN.Text = Pollution.ToString("0.0");
		BTime.Text = BuildTime.ToString();
		LandN.Text = (LandUse * 100).ToString("0.0");
		BioN.Text = (-BiodiversityImpact * 100).ToString("0.0");
		
		// Set text labels coorectly
		PollL.Text = TC._GetText("labels.xml", "infobar", "label_pollution");
		LandL.Text = TC._GetText("labels.xml", "infobar", "label_land");
		BioL.Text = TC._GetText("labels.xml", "infobar", "label_biodiversity");
		
		// Update label colors to represent levels
		if (BiodiversityImpact < 0) {
			BioN.Set("theme_override_colors/font_color", GREEN);
		}
		if (LandUse < 0) {
			LandN.Set("theme_override_colors/font_color", GREEN);
		}
		if (Pollution <= 0) {
			PollN.Set("theme_override_colors/font_color", GREEN);

		} else {
			PollN.Set("theme_override_colors/font_color", RED);
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
			AnimationPlayer APlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			AnimName = APlayer.CurrentAnimation;
			APlayer.Play("RESET");
			APlayer.Stop();
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
			AnimationPlayer APlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			APlayer.Play(AnimName);
		}
		
		// Propagate the new values to the UI
		_UpdatePlantData();
	}
	
	public void Disable() {
		MultInc.Disabled = true;
		MultDec.Disabled = true;
	}
	
	public void PlayAnimation() {
		// TODO set text in lang
		//NoMoney.Text =
		AP.Play("noMoney");
	}
	
	public void ShowMultInfo() {
		MultProd.Show();
		MultPoll.Show();
		MultLand.Show();
		MultBio.Show();
		MultPrice.Show();
		MultWinterE.Show();
		MultSummerE.Show();
	}
	
	public void HideMultInfo() {
		MultProd.Hide();
		MultPoll.Hide();
		MultLand.Hide();
		MultBio.Hide();
		MultPrice.Hide();
		MultWinterE.Hide();
		MultSummerE.Hide();
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
			Sprite.SelfModulate = HOVER_COLOR;
			
		} else {
			Info.Show();
		}
	}

	// Display the plant information when the mouse is hovering over the plant
	private void OnArea2DMouseExited() {
		// Make sure that the plant isn't in the build menu
		if(!IsPreview) {
			Sprite.SelfModulate = DEFAULT_COLOR;
		} else {
			Info.Hide();
		}
	}
	
	// Press on the powerplant to get more info about it
	private void OnInfoButtonPressed() {
		// only show the multiplier if  the plant can be upgraded
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());
	
		if(Info.Visible) {
			Info.Visible = false;
			ResRect.Visible = false;
			Multiplier.Visible = false;
		} else {
			// Hide other powerplants info before showing the selected plant's info
			foreach (PowerPlant Plant in GetTree().GetNodesInGroup("PP")) {
				Plant.Info.Visible = false;
				Plant.ResRect.Visible = false;
				Plant.Multiplier.Visible = false;
			}
			Info.Visible = true;
			//ResRect.Visible = true;
			// Toggle multiplier state if several elements are available
			if(mult.MaxElements > 1) {
				Multiplier.Visible = true;
			}
		}
		
		EmitSignal(SignalName.ZoomSignal, Position);
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
			if(C._GetGL()._CheckBuildReq(mult.Cost)) {
				AnimMoney.Text = "-" + mult.Cost.ToString() + "$";
				AP.Play("Money-");
			}
			
			// Signal the request to the game loop
			EmitSignal(SignalName.UpgradePlant, true, mult.Cost, this);
		}
		// Recalculate multiplier info numbers
		GetMultIncInfo();
	}

	// Reacts to the decrease request by requesting it to the game loop
	// which will enact it if we have enough money
	private void OnMultDecPressed() {
		Debug.Print("DOWNGRADE PLANT");
		// Retrieve the multiplier
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());
		
		// Check that the min hasn't been reached
		if(MultiplierValue > 1) {
			if(mult.Cost > 0) {
				AnimMoney.Text = "+" + mult.Cost.ToString() + "$";
				AP.Play("Money+");
			}
			// Signal the request to the game loop
			EmitSignal(SignalName.UpgradePlant, false, -mult.Cost, this);
		}
		
		// Recalculate multiplier info numbers
		GetMultDecInfo();
	}
	
	private void GetMultIncInfo() {
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());
		MultPrice.Text = "-" + mult.Cost.ToString() + "$";
		MultPrice.Set("theme_override_colors/font_color", RED);
		MultBio.Text = BioN.Text;
		BioN.Text = (-BiodiversityImpact * 100 * mult.Biodiversity).ToString("0.0");
		MultProd.Text = MoneyL.Text;
		MoneyL.Text = (ProductionCost * mult.ProductionCost).ToString("0.0");
		MultPoll.Text = PollN.Text; 
		PollN.Text = (Pollution * mult.Pollution).ToString("0.0");
		MultLand.Text = LandN.Text;
		LandN.Text = (LandUse * 100 * mult.LandUse).ToString("0.0");
		MultWinterE.Text = "+" + (mult.Capacity * EnergyAvailability.Item1).ToString();
		MultSummerE.Text = "+" + (mult.Capacity * EnergyAvailability.Item2).ToString();
	}
	
	private void GetMultDecInfo() {
		Multiplier mult = CC._ReadMultiplier(Config.Type.POWER_PLANT, PlantType.ToString());
		MultPrice.Text = "+" + mult.Cost.ToString() + "$";
		MultPrice.Set("theme_override_colors/font_color", GREEN);
		MultBio.Text = BioN.Text; 
		BioN.Text = (-BiodiversityImpact * 100 / mult.Biodiversity).ToString("0.0");
		MultProd.Text = MoneyL.Text;
		MoneyL.Text = (ProductionCost / mult.ProductionCost).ToString("0.0");
		MultPoll.Text = PollN.Text;
		PollN.Text = (Pollution / mult.Pollution).ToString("0.0");
		MultLand.Text = LandN.Text;
		LandN.Text = (LandUse * 100 / mult.LandUse).ToString("0.0");
		MultWinterE.Text = "-" + (mult.Capacity * EnergyAvailability.Item1).ToString();
		MultSummerE.Text = "-" + (mult.Capacity * EnergyAvailability.Item2).ToString();
	}
	
	private void OnMultIncMouseEntered() {
		GetMultIncInfo();
		ShowMultInfo();
	}
	
	private void OnMultIncMouseExited() {
		HideMultInfo();
		_UpdatePlantData();
	}
	
	private void OnMultDecMouseEntered() {
		GetMultDecInfo();
		ShowMultInfo();
	}
	
	private void OnMultDecMouseExited() {
		HideMultInfo();
		_UpdatePlantData();
	}
}
