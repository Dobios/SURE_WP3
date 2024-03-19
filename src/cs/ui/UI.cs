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

// General controller for the UI
public partial class UI : CanvasLayer {

	// Describes the type of bar that contains information about certain metrics
	public enum InfoType { W_ENGERGY, S_ENGERGY, SUPPORT, ENVIRONMENT, POLLUTION, MONEY };

	// XML querying strings
	private const string LABEL_FILENAME = "labels.xml";
	private const string INFOBAR_GROUP = "infobar";
	private const string RES_GROUP = "resources";
	private const string POWERPLANT_GROUP = "powerplants";	
	private const string UI_GROUP = "ui";
	private const string MENU_GROUP = "menu";

	// Signals to the game loop that the turn must be passed
	[Signal]
	public delegate void NextTurnEventHandler();

	[Signal]
	/* Signals that a game reset should take place */
	public delegate void ResetGameEventHandler();

	[Signal]
	/* Signals that a debt has been requested */
	public delegate void DebtRequestEventHandler(int debt, int borrowed);

	// Timeline update values
	[ExportGroup("Timeline Parameters")]
	[Export]
	public int TIMELINE_STEP_SIZE = 3;
	[Export]
	public int TIMELINE_MAX_VALUE = 2050;

	[ExportGroup("Money Borrowing Parameters")]
	[Export]
	// The amount of debt applied
	public float InterestRate = 0.2f;

	// Contains the data displayed in the UI
	private InfoData Data;

	// TextController reference set by the game loop
	private TextController TC;

	// Button that triggers the passage to a next turn
	public TextureButton NextTurnButton;
	private Label NextTurnL;
	private AnimationPlayer NextTurnAP;
	private Label Warning;

	// The two energy bars, showing the availability and demand
	private InfoBar WinterEnergy;
	private InfoBar WinterEnergyPredict;
	private InfoBar SummerEnergy;
	private InfoBar SummerEnergyPredict;
	private Button WinterButton;
	private Button SummerButton;
	
	// The two information bars
	private InfoBar EnvironmentBar;
	private InfoBar SupportBar;
	private InfoBar PollutionBar;
	private Button EnvButton;
	private Button SuppButton;
	private Button PollButton;

	// Date progression
	private HSlider Timeline;
	private AnimationPlayer TimelineAP;

	// Imports
	private ImportSlider Imports;
	

	// Money related nodes
	private Label MoneyL;
	private Label MoneyNameL;
	private Label BudgetNameL;
	private Label BuildNameL;
	private Label ProdNameL;
	private Label ImportCostNameL;
	private Button MoneyButton;
	private TextureButton MoneyInfoB;
	private Control MoneyInfo;
	private Label BudgetL;
	private Label BuildL;
	private Label ProdL;
	private Label ImportCostL;
	private Label BudgetNext;
	private Label TotalNow;
	private Label TotalNext;
	private Label Debt;
	private Label DebtAmount;
	private Label BorrowL;
	
	// Money info nodes
	private Label BudgetInfo;
	private Label ProdInfo;
	private Label BuildInfo;
	private Label ImportInfo;
	private Label DebtInfo;

	// Borrow related fields
	private Label BorrowTitle;
	private Label BorrowText;
	private Label PayBackText;
	private Label BorrowAmount;
	private Label PayBackAmount;
	private Button DebtApplyButton;
	private Button DebtCancelButton;
	private ColorRect BorrowMoneyWindow;
	private HSlider DebtSlider;
	private ColorRect DebtResource;
	private Label DebtResLabel;
	private TextureButton BorrowMoneyButton;
	private Button BorrowContainer;
	private int DebtN = 0;
	private int BorrowN = 0;


	// Window buttons
	private TextureButton PolicyButton;
	private TextureButton StatsButton;
	public Label PolicyNotif;

	// Windows
	private PolicyWindow PW;

	// Build Menu
	private BuildMenu BM;

	// Settings
	private Button SettingsButton;
	private ColorRect SettingsBox;
	private Button LanguageButton;
	private Button SettingsClose;
	private Button ScreenOption;

	// Reset button and confirmation
	private TextureButton ResetButton;
	private Label ResetButtonL;
	private ColorRect ResetPrompt;
	private Label ResetConfirmL;
	private Button ResetYes;
	private Button ResetNo;

	// Game Loop
	private GameLoop GL;

	// Context
	private Context C;

	// Year label
	private Label Year;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch Nodes
		NextTurnButton = GetNode<TextureButton>("NextTurn");
		NextTurnL = GetNode<Label>("NextTurn/Label");
		NextTurnAP = GetNode<AnimationPlayer>("NextTurn/NextTurnAP");
		Warning = GetNode<Label>("NextTurn/Warning");
		TC = GetNode<TextController>("/root/TextController");
		BM = GetNode<BuildMenu>("../BuildMenu");
		GL = GetOwner<GameLoop>();
		C = GetNode<Context>("/root/Context");

		// Settings
		SettingsButton = GetNode<Button>("SettingsButton");
		SettingsBox = GetNode<ColorRect>("SettingsButton/ColorRect");
		LanguageButton = GetNode<Button>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Language");
		SettingsClose = GetNode<Button>("SettingsButton/ColorRect/SettingsBox/Close");
		ScreenOption = GetNode<Button>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/ScreenOption");

		// Reset nodes
		ResetButton = GetNode<TextureButton>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset");
		ResetButtonL = GetNode<Label>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset/Label");
		ResetPrompt = GetNode<ColorRect>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset/ResetConfirm");
		ResetConfirmL = GetNode<Label>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset/ResetConfirm/Label");
		ResetYes = GetNode<Button>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset/ResetConfirm/Yes");
		ResetNo = GetNode<Button>("SettingsButton/ColorRect/SettingsBox/VBoxContainer/Reset/ResetConfirm/No");

		// Info Bars
		WinterEnergy = GetNode<InfoBar>("EnergyBarWinter");
		WinterEnergyPredict = GetNode<InfoBar>("EnergyBarWinterPredict");
		SummerEnergy = GetNode<InfoBar>("EnergyBarSummer");
		SummerEnergyPredict = GetNode<InfoBar>("EnergyBarSummerPredict");
		EnvironmentBar = GetNode<InfoBar>("Env");
		SupportBar = GetNode<InfoBar>("Trust");
		PollutionBar = GetNode<InfoBar>("Poll");
		WinterButton = GetNode<Button>("EnergyBarWinter/Button");
		SummerButton = GetNode<Button>("EnergyBarSummer/Button");
		EnvButton = GetNode<Button>("Env/Button");
		SuppButton = GetNode<Button>("Trust/Button");
		PollButton = GetNode<Button>("Poll/Button");

		// Sliders
		Timeline = GetNode<HSlider>("Top/Timeline");
		Imports = GetNode<ImportSlider>("Import");
		Year = GetNode<Label>("TimePanelBlank/Year");
		
		TimelineAP = GetNode<AnimationPlayer>("TimePanelBlank/TimelineAnimation");

		// Money Nodes
		MoneyL = GetNode<Label>("Money/money");
		MoneyButton = GetNode<Button>("MoneyUI");
		MoneyInfo = GetNode<Control>("MoneyInfo");
		BudgetL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BudgetName/budgetNow");
		BuildL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BuildName/build");
		ProdL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ProdName/prod");
		ImportCostL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ImportName/importamounts");
		BudgetNext = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BudgetName/budgetNext");
		TotalNow = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/TotalName/TotalNow");
		TotalNext = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/TotalName/TotalNext");
		MoneyInfoB = GetNode<TextureButton>("MoneyInfo/MoneyInfoButton");
		BudgetInfo = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BudgetInfo");
		ProdInfo = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ProdInfo");
		BuildInfo = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BuildInfo");
		ImportInfo = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ImportInfo");
		DebtInfo = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/DebtInfo");
		Debt = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/Debt");
		DebtAmount = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/Debt/debtamount");
		BorrowL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/Debt/borrowamount");

		// Borrow Nodes
		BorrowTitle = GetNode<Label>("BorrowContainer/BorrowMoneyWindow/Title");
		BorrowText = GetNode<Label>("BorrowContainer/BorrowMoneyWindow/Money");
		BorrowAmount = GetNode<Label>("BorrowContainer/BorrowMoneyWindow/Money/BorrowAmount");
		PayBackText = GetNode<Label>("BorrowContainer/BorrowMoneyWindow/Interest");
		PayBackAmount = GetNode<Label>("BorrowContainer/BorrowMoneyWindow/Interest/InterestAmount");
		DebtApplyButton = GetNode<Button>("BorrowContainer/BorrowMoneyWindow/Apply");
		DebtCancelButton = GetNode<Button>("BorrowContainer/BorrowMoneyWindow/Cancel");
		BorrowMoneyWindow = GetNode<ColorRect>("BorrowContainer/BorrowMoneyWindow");
		DebtSlider = GetNode<HSlider>("BorrowContainer/BorrowMoneyWindow/BorrowSlider");
		DebtResource = GetNode<ColorRect>("BorrowMoney/Debt");
		DebtResLabel = GetNode<Label>("BorrowMoney/Debt/DebtAmount");
		BorrowMoneyButton = GetNode<TextureButton>("BorrowMoney");
		BorrowContainer = GetNode<Button>("BorrowContainer");

		// Name labels
		MoneyNameL = GetNode<Label>("Money/Label");
		BudgetNameL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BudgetName");
		BuildNameL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/BuildName");
		ProdNameL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ProdName");
		ImportCostNameL = GetNode<Label>("MoneyInfo/MarginContainer/MarginContainer/VBoxContainer/ImportName");

		// Window buttons
		PolicyButton = GetNode<TextureButton>("PolicyButton");
		StatsButton = GetNode<TextureButton>("Stats");
		PolicyNotif = GetNode<Label>("PolicyButton/Notif");

		// Windows
		PW = GetNode<PolicyWindow>("Window");

		// Connect Various signals
		MoneyButton.Pressed += _OnMoneyButtonPressed;
		MoneyInfoB.Pressed += _OnMoneyInfoPressed;
		NextTurnButton.Pressed += _OnNextTurnPressed;
		NextTurnButton.GuiInput += _OnNextTurnGuiInput;
		SettingsButton.Pressed += _OnSettingsButtonPressed;
		LanguageButton.Pressed += _OnLanguageButtonPressed;
		SettingsClose.Pressed += _OnSettingsClosePressed;
		PolicyButton.Pressed += _OnPolicyButtonPressed;
		WinterButton.Pressed += _OnWinterEnergyPressed;
		SummerButton.Pressed += _OnSummerEnergyPressed;
		EnvButton.Pressed += _OnEnvButtonPressed;
		SuppButton.Pressed += _OnSuppButtonPressed;
		PollButton.Pressed += _OnPollButtonPressed;
		Imports.ImportUpdate += _OnImportUpdate;
		DebtApplyButton.Pressed += _OnDebtApplyPressed;
		DebtCancelButton.Pressed += _OnDebtCancelPressed;
		DebtSlider.ValueChanged += _OnDebtSliderValueChanged;
		BorrowContainer.Pressed += _OnDebtCancelPressed;
		BorrowMoneyButton.Pressed += BorrowContainer.Show;
		ScreenOption.Toggled += _OnScreenOption;

		// Initially hide the borrow container
		BorrowContainer.Hide();

		// Reset signals
		ResetButton.Pressed += _OnResetPressed;
		ResetYes.Pressed += _OnResetYesPressed;
		ResetNo.Pressed += _OnResetNoPressed;

		// For predictive updates
		C.UpdatePrediction += _OnUpdatePrediction;
		TC.UpdateUI += _UpdateUI;

		// Initialize data
		Data = new InfoData();

		// Set the language
		C._UpdateLanguage(Language.Type.EN);
	}

	// ==================== UI Update API ====================

	// Updates the various labels across the UI
	public void _UpdateUI() {
		// Updates the displayed language to match the selected one
		LanguageButton.Text = C._GetLanguageName();

		// Fetch the build menu names
		string gas_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_gas");
		string hydro_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_hydro");
		string solar_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_solar");
		string wind_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_wind");
		string tree_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_tree");
		string nuclear_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_nuclear");
		string waste_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_waste");
		string biomass_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_biomass");
		string river_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_river");
		string pump_name = TC._GetText(LABEL_FILENAME, POWERPLANT_GROUP, "label_pump");

		// Fetch the energy bar names
		string WinterEnergy_name = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_energy_w");
		string SummerEnergy_name = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_energy_s");
		string EnvironmentBar_name = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_environment");
		string SupportBar_name = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_support");
		string PollutionBar_name = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_pollution");

		// UI buttons
		string next_turn_name = TC._GetText(LABEL_FILENAME, UI_GROUP, "label_next_turn");
		string import_name = TC._GetText(LABEL_FILENAME, UI_GROUP, "label_import");

		// Reset texts
		string reset_name = TC._GetText(LABEL_FILENAME, MENU_GROUP, "label_reset");
		string reset_prompt = TC._GetText(LABEL_FILENAME, MENU_GROUP, "reset_prompt");
		string reset_yes = TC._GetText(LABEL_FILENAME, MENU_GROUP, "reset_yes");
		string reset_no = TC._GetText(LABEL_FILENAME, MENU_GROUP, "reset_no");

		// Debt texts
		string debt_title = TC._GetText(LABEL_FILENAME, UI_GROUP, "debt_title");
		string borrow_text = TC._GetText(LABEL_FILENAME, UI_GROUP, "label_borrow");
		string debt_text = TC._GetText(LABEL_FILENAME, UI_GROUP, "label_debt");
		string debt_apply = TC._GetText(LABEL_FILENAME, UI_GROUP, "apply_button");
		string debt_cancel = TC._GetText(LABEL_FILENAME, UI_GROUP, "cancel_button");

		// Update debt texts
		BorrowTitle.Text = debt_title;
		BorrowText.Text = borrow_text;
		PayBackText.Text = debt_text;
		DebtApplyButton.Text = debt_apply;
		DebtCancelButton.Text = debt_cancel;

		// Update the reset texet
		ResetButtonL.Text = reset_name;
		ResetConfirmL.Text = reset_prompt;
		ResetYes.Text = reset_yes;
		ResetNo.Text = reset_no;

		// Update the various plants
		BM._UpdatePlantName(Building.Type.GAS, gas_name);
		BM._UpdatePlantName(Building.Type.HYDRO, hydro_name);
		BM._UpdatePlantName(Building.Type.SOLAR, solar_name);
		BM._UpdatePlantName(Building.Type.TREE, tree_name);
		BM._UpdatePlantName(Building.Type.WIND, wind_name);
		BM._UpdatePlantName(Building.Type.WASTE, waste_name);
		BM._UpdatePlantName(Building.Type.BIOMASS, biomass_name);
		BM._UpdatePlantName(Building.Type.RIVER, river_name);
		BM._UpdatePlantName(Building.Type.PUMP, pump_name);

		// Update the placed plants
		foreach(PowerPlant pp in GL._GetPowerPlants()) {
			switch(pp.PlantType.type) {
				case Building.Type.GAS:
					pp._UpdatePlantName(gas_name);
					break;
				case Building.Type.HYDRO:
					pp._UpdatePlantName(hydro_name);
					break;
				case Building.Type.SOLAR:
					pp._UpdatePlantName(solar_name);
					break;
				case Building.Type.TREE:
					pp._UpdatePlantName(tree_name);
					break;
				case Building.Type.NUCLEAR:
					pp._UpdatePlantName(nuclear_name);
					break;
				case Building.Type.WIND:
					pp._UpdatePlantName(wind_name);
					break;
				case Building.Type.WASTE:
					pp._UpdatePlantName(waste_name);
					break;
				case Building.Type.BIOMASS:
					pp._UpdatePlantName(biomass_name);
					break;
				case Building.Type.RIVER:
					pp._UpdatePlantName(river_name);
					break;
				case Building.Type.PUMP:
					pp._UpdatePlantName(pump_name);
					break;	
				default:
					break;
			}
		}

		// Update the resource bar names
		WinterEnergy._UpdateBarName(WinterEnergy_name);
		SummerEnergy._UpdateBarName(SummerEnergy_name);
		EnvironmentBar._UpdateBarName(EnvironmentBar_name);
		SupportBar._UpdateBarName(SupportBar_name);
		PollutionBar._UpdateBarName(PollutionBar_name);

		// Update UI buttons
		NextTurnL.Text = next_turn_name;

		// Update the import slider
		Imports._UpdateLabel(import_name);

		// Upate the money labels
		SetMoneyInfo();
	}

	// Updates the value of the a given bar
	public void _UpdateBarValue(InfoType t, int val) {
		switch (t) {
			case InfoType.W_ENGERGY:
				WinterEnergy._UpdateProgress(val);
				break;
			case InfoType.S_ENGERGY:
				SummerEnergy._UpdateProgress(val);
				break;
			case InfoType.SUPPORT:
				SupportBar._UpdateProgress(val);
				break;
			case InfoType.ENVIRONMENT:
				EnvironmentBar._UpdateProgress(val);
				break;
			case InfoType.POLLUTION:
				PollutionBar._UpdateProgress(val);
				break;
			default:
				break;
		}
	}

	// Updates the slider in the middle of the a given bar
	public void _UpdateBarSlider(InfoType t, float val) {
		switch (t) {
			// For the energy sliders, the value represents the percentage of the total allowed capacity
			// that is allowed on the grid that the demand takes up
			case InfoType.W_ENGERGY:
				WinterEnergy._UpdateSlider(val);
				WinterEnergyPredict._UpdateSlider(val);
				break;
			case InfoType.S_ENGERGY:
				SummerEnergy._UpdateSlider(val);
				SummerEnergyPredict._UpdateSlider(val);
				break;
			case InfoType.SUPPORT:
				SupportBar._UpdateSlider(val);
				break;
			case InfoType.ENVIRONMENT:
				EnvironmentBar._UpdateSlider(val);
				break;
			case InfoType.POLLUTION:
				PollutionBar._UpdateSlider(val);
				break;
			default:
				break;
		}
	}

	// Update the internal UI data
	// This is done following the ordering of the fields in the InfoData struct
	// Energy: demand, supply
	// Support: energy_affordability, env_aesthetic
	// Environment: land_use, pollution, biodiversity, envbarval, import pollution
	// Money: budget, production, building, money, import cost
	public void _UpdateData(InfoType t, params int[] d) {
		switch (t) {
			case InfoType.W_ENGERGY:
				// Sanity check, make sure that you were given enough fields
				Debug.Assert(d.Length >= InfoData.N_W_ENERGY_FIELDS);

				// Set the fields in order, for energy it's demand, supply
				Data.W_EnergyDemand = d[0];
				Data.W_EnergySupply = d[1];

				// Set the text fields
				SetEnergyInfo(ref WinterEnergy, InfoType.W_ENGERGY);

				// Update the bar value
				_UpdateBarValue(InfoType.W_ENGERGY, Data.W_EnergySupply);

				// Update the bar slider
				_UpdateBarSlider(
					InfoType.W_ENGERGY, 
					(float)Data.W_EnergyDemand / EnergyManager.MAX_ENERGY_BAR_VAL
				);
				
				WinterEnergy._UpdateColor(Data.W_EnergySupply < Data.W_EnergyDemand);

				// Update the required import target (only in winter due to conservative estimates)
				SetTargetImport();
				break;
			case InfoType.S_ENGERGY:
				// Sanity check, make sure that you were given enough fields
				Debug.Assert(d.Length >= InfoData.N_S_ENERGY_FIELDS);

				// Set the fields in order, for energy it's demand, supply
				Data.S_EnergyDemand = d[0];
				Data.S_EnergySupply = d[1];

				// Update the UI
				SetEnergyInfo(ref SummerEnergy, InfoType.S_ENGERGY);

				// Update the bar value
				_UpdateBarValue(InfoType.S_ENGERGY, Data.S_EnergySupply);

				// Update the bar slider
				_UpdateBarSlider(
					InfoType.S_ENGERGY, 
					(float)Data.S_EnergyDemand / (float)EnergyManager.MAX_ENERGY_BAR_VAL
				);
				
				SummerEnergy._UpdateColor(Data.S_EnergySupply < Data.S_EnergyDemand);
				break;
			case InfoType.SUPPORT:
				// Sanity check, make sure that you were given enough fields
				Debug.Assert(d.Length >= InfoData.N_SUPPORT_FIELDS);

				// Set the fields in order, for support it's affordability, aesthetic
				Data.SupportVal = d[0];

				_UpdateBarValue(InfoType.SUPPORT, Data.SupportVal);

				// Update the UI
				SetSupportInfo();
				break;
			case InfoType.ENVIRONMENT:
				// Sanity check, make sure that you were given enough fields
				Debug.Assert(d.Length >= InfoData.N_ENV_FIELDS);

				// Set the fields in order, for environment it's landuse, pollution, biodiversity
				Data.LandUse = d[0];
				Data.Pollution = d[1];
				Data.Biodiversity = d[2];
				Data.ImportPollution = d[4];

				// Update the UI
				SetEnvironmentInfo();

				// Update the bar value
				_UpdateBarValue(InfoType.ENVIRONMENT, d[3]);
				_UpdateBarValue(InfoType.POLLUTION, Data.Pollution + Data.ImportPollution);

				// Update the bar slider
				_UpdateBarSlider(
					InfoType.ENVIRONMENT, 
					0.75f
				);
				_UpdateBarSlider(
					InfoType.POLLUTION,
					0.0f
				);
				break;
			case InfoType.MONEY:
				// Sanity check, make sure that there are enough fields
				Debug.Assert(d.Length >= InfoData.N_MONEY_FIELDS);

				// Set the fields in order, for money it's budget, production, building
				Data.Budget = d[0];
				Data.Production = d[1];
				Data.Building = d[2];
				Data.Money = d[3];
				Data.Imports = d[4];

				// Update the UI 
				SetMoneyInfo();
				break;
			default:
				break;
		}
	}

	// Hides the debt label in the case where no debt is acquired this turn
	// Shows the debt label when debt was acquired
	public void _UpdateDebtResource(int debt) {
		// Propagate the new amount to the label
		DebtN = debt;
		DebtResLabel.Text = "-" + debt.ToString();
		DebtAmount.Text = DebtResLabel.Text;

		// Check if there is any debt to show
		if(debt > 0) {
			DebtResource.Show();
		} else {
			DebtResource.Hide();
		}
	}

	// Retrieves the import percentage selected by the user
	public float _GetImportSliderPercentage() => (float)Imports._GetImportValue() / 250.0f;

	// Getter for the green energy toggle state
	public bool _GetGreenImportState() => Imports._GetGreenImports();

	// ==================== Internal Helpers ====================

	// Sets the required imports based on the demand
	private void SetTargetImport() {
		// Fetch the demand and supply
		float demand  = C._GetDemand().Item1;
		float supply = C._GetGL()._GetResources().Item1.SupplyWinter;

		// Compute the different, clamped to 0 as no imports are required
		// when the supply meets the demand
		float imported = C._GetDemand().Item1 * Imports._GetImportValue() / 250;
		float diff = Math.Max(0.0f, demand - (supply - imported)); 

		// Compute the percentage of the total demand tha the diff represents
		float diff_perc = diff / demand;

		// Set the import target to that percentage
		Imports._UpdateTargetImport(diff_perc);
	}

	// Sets the energy in
	private void SetEnergyInfo(ref InfoBar eng, InfoType t) {
		// Sanity check
		Debug.Assert(t == InfoType.W_ENGERGY || t == InfoType.S_ENGERGY);

		// Extract data based on energy type
		int demand = t == InfoType.W_ENGERGY ? Data.W_EnergyDemand : Data.S_EnergyDemand;
		int supply = t == InfoType.W_ENGERGY ? Data.W_EnergySupply : Data.S_EnergySupply;

		// Get the labels from the XML file
		string demand_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_demand");
		string supply_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_supply");

		// Set the info
		eng._UpdateInfo(
			"The energy supply needs to reach the energy demand.", // N/Max TODO: Figure out what to use here
			demand_label, demand.ToString(), // T0, N0
			supply_label, supply.ToString() // T1, N1
		);
	}

	// Sets the information fields for the support bar
	private void SetSupportInfo() {
		// Get the labels from the XML file
		string support_label = TC._GetText(LABEL_FILENAME, RES_GROUP, "label_support");

		SupportBar._UpdateInfo(
			" ", // N/Max TODO: Figure out what to use here
			support_label, Data.SupportVal.ToString() // T0, N0
		);
	}

	// Sets the information fields for the environment bar
	private void SetEnvironmentInfo() {
		// Get the labels from the XML file
		string land_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_land");
		string buidiv_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_biodiversity");

		EnvironmentBar._UpdateInfo(
			"", // N/Max TODO: Figure out what to use here
			land_label, Data.LandUse.ToString() + "%", // T0, N0
			buidiv_label, Data.Biodiversity.ToString() + "%" // T2, N2
		);
	}

	// Sets the information fields for the pollution bar
	private void SetPollutionInfo() {
		string poll_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_pollution");
		string import_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_import_pol");

		PollutionBar._UpdateInfo(
			// N/Max TODO: Figure out what to use here
			"Goal: Reach net zero by 2050.",
			poll_label, Data.Pollution.ToString(), // T0, N0
			import_label, Data.ImportPollution.ToString() // T2, N2
		);
	}

	// Sets the information related to the money metric
	private void SetMoneyInfo() {
		// Query the label xml to get the names
		string money_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_money");
		string budget_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_budget");
		string prod_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_production");
		string build_label = TC._GetText(LABEL_FILENAME, INFOBAR_GROUP, "label_building");
		string import_name = TC._GetText(LABEL_FILENAME, UI_GROUP, "label_import");


		// Set Names
		MoneyNameL.Text = money_label;
		BudgetNameL.Text = budget_label;
		ProdNameL.Text = prod_label;
		BuildNameL.Text = build_label;
		ImportCostNameL.Text = import_name;
		
		// Set Values
		BudgetL.Text = Data.Budget.ToString();
		BuildL.Text = "-" + Data.Building.ToString();
		ProdL.Text = "-" + Data.Production.ToString();
		MoneyL.Text = Data.Money.ToString();
		ImportCostL.Text = "-" + Data.Imports.ToString();
		GD.Print(Data.Money, GameLoop.BUDGET_PER_TURN);
		int BudgetNextN = Data.Money + GameLoop.BUDGET_PER_TURN;
		BudgetNext.Text = BudgetNextN.ToString();
		TotalNow.Text = Data.Money.ToString();
		int TotalNextN = BudgetNextN - Data.Imports - Data.Production - DebtN;
		TotalNext.Text = TotalNextN.ToString();
	}

	// Sets the correct years without the Next Turn Animation
	public void SetNextYearsNoAnim(int val) {
		Year.Text = val.ToString();
		Timeline.Value = val;
	}
	
	// Sets the correct years on the Next Turn Animation
	public void SetNextYears() {
		// Retrieve the current year
		double Year = Timeline.Value;
		
		string LastDecade = Year.ToString();

		// Set up the animation for the year progression
		Animation Anim = TimelineAP.GetAnimation("NextTurnAnim");
		// Set up decade change animation player
		AnimationPlayer DecadeAP = GetNode<AnimationPlayer>("TimePanelBlank/NextDecadeAP");
		Animation DecadeAnim = DecadeAP.GetAnimation("NextDecade");

		// Retrieve the animation track related to our current turn progression
		int Track = Anim.FindTrack("Year:text", Animation.TrackType.Value);
		// Retrieve the animation track related to the arrow's rotation
		int ArrowTrack = Anim.FindTrack("Arrow:rotation", Animation.TrackType.Value);
		int ShadowTrack = Anim.FindTrack("Shadow:rotation", Animation.TrackType.Value);
		// Retrieve the animation track related to year's numbers
		int YearTrack = Anim.FindTrack("Year4:text", Animation.TrackType.Value);
		int Year2Track = Anim.FindTrack("Year5:text", Animation.TrackType.Value);

		// Retrieve the number of animation keypoints in the current track
		int NKeys = Anim.TrackGetKeyCount(Track);

		// Update the year incrementally during the animation and move the clock's arrow
		for (int i = 0; i < NKeys; i++) {
			Anim.TrackSetKeyValue(Track, i, (Year + i).ToString());
			Anim.TrackSetKeyValue(ArrowTrack, i, (Mathf.Remap(Year+i, 2022, 2052, -2.18, 2.18)));
			Anim.TrackSetKeyValue(ShadowTrack, i, (Mathf.Remap(Year+i, 2022, 2052, -2.22, 2.22)));
			string YearNum = (Year + i).ToString();
			Anim.TrackSetKeyValue(YearTrack, i, YearNum[3].ToString());
			if (i < NKeys-1) {
				string YearNum2 = (Year + i + 1).ToString();
				Anim.TrackSetKeyValue(Year2Track, i, YearNum2[3].ToString());
				if(YearNum2[3].ToString() == "0") {
					// Retrieve the animation track related to decade's year numbers
					int DecadeTrack = DecadeAnim.FindTrack("Year3:text", Animation.TrackType.Value);
					int Decade2Track = DecadeAnim.FindTrack("Year6:text", Animation.TrackType.Value);
					// Set the decade track numbers to the right tracks
					DecadeAnim.TrackSetKeyValue(DecadeTrack, 0, LastDecade[2].ToString());
					DecadeAnim.TrackSetKeyValue(Decade2Track, 0, YearNum2[2].ToString());
					DecadeAP.Play("NextDecade");
				}
			}
		}
		
			
			
	}

	// ==================== Interaction Callbacks ====================

	// Displays additional details about the money usage
	public void _OnMoneyButtonPressed() {
		// Simply toggle the money info
		if(MoneyInfo.Visible) {
			MoneyInfo.Hide();
			if(DebtN > 0) {DebtResource.Show();}
		} else {
			// Set the info first
			SetMoneyInfo();
			
			DebtResource.Hide();

			// Finally display it
			MoneyInfo.Show();
		}
	}
	
	public void _OnMoneyInfoPressed() {
		BudgetInfo.Visible = !BudgetInfo.Visible;
		ProdInfo.Visible = !ProdInfo.Visible;
		BuildInfo.Visible = !BuildInfo.Visible;
		ImportInfo.Visible = !ImportInfo.Visible;
		if(Debt.Visible) {DebtInfo.Visible = !DebtInfo.Visible;}
	}

	// Updates the timelines and propagates the request up to the game loop
	public async void _OnNextTurnPressed() {
		
		// Sets the correct years and plays the next turn animation
		if(C._GetRemainingTurns() > 0) {
			SetNextYears();
			TimelineAP.Play("NextTurnAnim");
		}
		
		// Waits for the next turn animation before sending the next turn signal
		await ToSignal(TimelineAP, "animation_finished");
		
		// reset policy vote button
		PW._ResetVote();
		PolicyNotif.Show();
		
		// Hides debt in money UI
		Debt.Hide();
		
		// Trigger the next turn
		EmitSignal(SignalName.NextTurn);

		// Update the required import target (only in winter due to conservative estimates)
		SetTargetImport();
		
		// Update the Timeline
		Timeline.Value = Math.Min(Timeline.Value + TIMELINE_STEP_SIZE, TIMELINE_MAX_VALUE); 
	}
	
	// If the player tries to press on the disabled next turn button they will get a warning
	public void _OnNextTurnGuiInput(InputEvent E) {
		if(E is InputEventMouseButton MouseButton) {
			if(MouseButton.ButtonMask == MouseButtonMask.Left)
				if(NextTurnButton.Disabled)
					NextTurnAP.Play("warning");
		}
	}
	
	public void _OnWinterEnergyPressed() {
		if(WinterEnergy.Box.Visible) {
			WinterEnergy._HideInfo();
		} else {
			// Hide other infos first
			WinterEnergy._HideInfo();
			SummerEnergy._HideInfo();
			EnvironmentBar._HideInfo();
			PollutionBar._HideInfo();
			SupportBar._HideInfo();
			
			SetEnergyInfo(ref WinterEnergy, InfoType.W_ENGERGY);

		 	// Display the energy info
		 	WinterEnergy._DisplayInfo();
		}
	}
	
	public void _OnSummerEnergyPressed() {
		if(SummerEnergy.Box.Visible) {
			SummerEnergy._HideInfo();
		} else {
			// Hide other infos first
			WinterEnergy._HideInfo();
			SummerEnergy._HideInfo();
			EnvironmentBar._HideInfo();
			PollutionBar._HideInfo();
			SupportBar._HideInfo();
			
			SetEnergyInfo(ref SummerEnergy, InfoType.S_ENGERGY);

		 	// Display the energy info
		 	SummerEnergy._DisplayInfo();
		}
	}
	
	public void _OnEnvButtonPressed() {
		if(EnvironmentBar.Box.Visible) {
			EnvironmentBar._HideInfo();
		} else {
			// Hide other infos first
			WinterEnergy._HideInfo();
			SummerEnergy._HideInfo();
			EnvironmentBar._HideInfo();
			PollutionBar._HideInfo();
			SupportBar._HideInfo();
			// Set the information first
			SetEnvironmentInfo();

			// Then show the info
			EnvironmentBar._DisplayInfo();
		}
	}
	
	public void _OnSuppButtonPressed() {
		if(SupportBar.Box.Visible) {
			SupportBar._HideInfo();
		} else  {
			// Hide other infos first
			WinterEnergy._HideInfo();
			SummerEnergy._HideInfo();
			EnvironmentBar._HideInfo();
			PollutionBar._HideInfo();
			SupportBar._HideInfo();
			// Set the information first
			SetSupportInfo();

			// Show the new info
			SupportBar._DisplayInfo();
		}
	}
	
	public void _OnPollButtonPressed() {
		if(PollutionBar.Box.Visible) {
			PollutionBar._HideInfo();
		} else {
			// Hide other infos first
			WinterEnergy._HideInfo();
			SummerEnergy._HideInfo();
			EnvironmentBar._HideInfo();
			PollutionBar._HideInfo();
			SupportBar._HideInfo();
			
			// Make sure that the pollution info is up to date
			SetPollutionInfo();

			// Show the new info
			PollutionBar._DisplayInfo();
		}
	}

	// Shows the policy window
	public void _OnPolicyButtonPressed() {
		// Toggle the window visibility  
		if(PW.Visible) {
			PW._PlayAnim("popup", false);
			PW.Hide();
		} else {
			PW.Show();
			PW._PlayAnim("popup");
		}
		if(PW.Vote.Disabled) {
				GD.Print("vote disabled");
				PolicyNotif.Hide();
			}
	}

	// Toggles the settings box
	public void _OnSettingsButtonPressed() {
		// Check the current visibility of the box and act accordingly
		if(SettingsBox.Visible) {
			SettingsBox.Hide();
		} else {
			SettingsBox.Show();
		}
	}

	// Propagates the language update to the game loop
	public void _OnLanguageButtonPressed() {
		// Move to the next language
		C._NextLanguage();

		// Update the ui
		_UpdateUI();
	}
	
	// Closes the language settings by clicking outside
	public void _OnSettingsClosePressed() {
		SettingsBox.Hide();
	}

	// Propagates an import update to the rest of the system
	public void _OnImportUpdate() {
		// Propagate the request of a resource update to the game loop
		GL._UpdateResourcesUI();
	}

	// Propagates a predictive update to the UI
	public void _OnUpdatePrediction() {
		// Get predictive values
		var (MW, MS) = C._GetModels();

		// Update prediction bars
		WinterEnergyPredict._UpdateProgress((int)MW._GetTotalSupply());
		SummerEnergyPredict._UpdateProgress((int)MS._GetTotalSupply());

	}

	// Updates the state of the next turn button
	public void _OnNextTurnStateUpdate(bool state) {
		NextTurnButton.Disabled = state;
	}

	// Reacts to the reset button being pressed
	public void _OnResetPressed() {
		// Simply show the confirmation prompt
		ResetPrompt.Show();
	}

	// Reacts to a reset confirmation
	public void _OnResetYesPressed() {
		// Singal that a reset will happen
		ResetPrompt.Hide();
		SettingsBox.Hide();

		// Reset the imports
		Imports.Value = 0;
		Imports._OnApplySelectionPressed(true);

		EmitSignal(SignalName.ResetGame);
	}

	// Reacts to a reset cancelation
	public void _OnResetNoPressed() {
		// Simply hide the reset confirmation
		ResetPrompt.Hide();
	}

	// Reacts to the money borrowing apply button being pressed
	public void _OnDebtApplyPressed() {
		// Send the amount of debt to the game loop to be applied and update the debt resource
		DebtResLabel.Text = "-" + PayBackAmount.Text;
		BorrowContainer.Hide();
		Debt.Show();
		BorrowL.Text = BorrowN.ToString();
		EmitSignal(SignalName.DebtRequest, (int)(DebtSlider.Value + (InterestRate * DebtSlider.Value)), (int)DebtSlider.Value);
	}

	// Reacts to the money borrowing cancel button being pressed
	public void _OnDebtCancelPressed() {
		// Reset the slider and close the window
		DebtSlider.Value = 0;
		BorrowContainer.Hide();
	}

	// Reacts the the selected debt amount being changed
	public void _OnDebtSliderValueChanged(double value) {
		// Update the borrow and pay back amounts to reflect the new values
		BorrowAmount.Text = ((int)value).ToString();
		BorrowN += (int)value;
		PayBackAmount.Text = ((int)(value + (value * InterestRate))).ToString(); // Add the interest rate
	}
	
	public void _OnScreenOption(bool Toggle) {
		if(Toggle) {
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		} else {
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
	}
	
}
