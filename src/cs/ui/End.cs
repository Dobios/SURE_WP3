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

// Simply used for localization purposes
// Gathers all text in the final screen and translates it
public partial class End : CanvasLayer {
	// Text label ids
	private const string END_FILE = "labels.xml";
	private const string END_GROUP = "end";
	private const string BRAVO_ID = "title";
	private const string ENDTEXT_ID = "subtitle";
	private const string SHOCK_ID = "shock";
	private const string ENERGY_ID = "energy";
	private const string MONEY_NO_DEBT_ID = "money_good";
	private const string MONEY_DEBT_ID = "money_bad";
	private const string SUPPORT_ID = "support";
	private const string ENV_NETZ_ID = "pollution_good";
	private const string ENV_NO_NETZ_ID = "pollution_bad";
	private const string ENV_TEXT_ID = "env";

	// End screen stats value labels
	private Label Shocks;
	private Label EnergyW;
	private Label EnergyS;
	private Label Support;
	private Label EnvScore;

	// End Screen text labels
	private ColorRect EndWindow;
	private Label Bravo;
	private Label EndText;

	// End screen stats text labels
	private Label ShockT;
	private Label EnergyT;
	private Label MoneyT;
	private Label SupportT;
	private Label PollT;
	private Label EnvT;
	
	private TextureButton ScoreToggle;

	// Text controller for the dynamic text
	private TextController TC;

	// ==================== GODOT Method Overrides ====================
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {

		// Fetch Value label nodess
		Shocks = GetNode<Label>("EndWindow/Stats/Shocks");
		EnergyW = GetNode<Label>("EndWindow/Stats/EnergyW");
		EnergyS = GetNode<Label>("EndWindow/Stats/EnergyS");
		Support = GetNode<Label>("EndWindow/Stats/Support");
		EnvScore = GetNode<Label>("EndWindow/Stats/Env");

		// Fetch Text Label Nodes
		EndWindow = GetNode<ColorRect>("EndWindow");
		Bravo = GetNode<Label>("EndWindow/Bravo");
		EndText = GetNode<Label>("EndWindow/EndText");
		ShockT = GetNode<Label>("EndWindow/HBoxContainer/Shock");
		EnergyT = GetNode<Label>("EndWindow/HBoxContainer/Energy");
		MoneyT = GetNode<Label>("EndWindow/HBoxContainer/Money");
		SupportT = GetNode<Label>("EndWindow/HBoxContainer/Support");
		PollT = GetNode<Label>("EndWindow/HBoxContainer/Poll");
		EnvT = GetNode<Label>("EndWindow/HBoxContainer/Env");
		
		ScoreToggle = GetNode<TextureButton>("Score");

		// Fetch the text controller
		TC = GetNode<TextController>("/root/TextController");
		
		ScoreToggle.Pressed += _OnScorePressed;
	}

	// ==================== Public API ====================
	
	// Sets the stats that are shown in the final screen
	// @param nShocks, the number of shocks the player was able to wistand without needing a solution
	// @param {enw, ens}, the amount of {winter, summer} energy the player was producing in the last turn
	// @param debt, wether or not the player was ever in debt
	// @param sup, the amount of support the player had during the last turn
	// @param netz, wether or not the player reached net zero pollution
	// @param env_, the percentage at which the environment score was at at the end of the last turn
	public void _SetEndStats(int nShocks, float enw, float ens, bool debt, float sup, bool netz, double env_) {
		// Set the numerical stats
		Shocks.Text = nShocks.ToString();
		EnergyW.Text = enw.ToString();
		EnergyS.Text = ens.ToString();
		Support.Text = sup.ToString() + "%";
		EnvScore.Text = ((int)(env_ * 100)).ToString()+ "%";

		// Set textual stats
		MoneyT.Text = TC._GetText(END_FILE, END_GROUP, debt ? MONEY_DEBT_ID : MONEY_NO_DEBT_ID);
		PollT.Text = TC._GetText(END_FILE, END_GROUP, netz ? ENV_NETZ_ID : ENV_NO_NETZ_ID);

		// Set all other texts
		Bravo.Text = TC._GetText(END_FILE, END_GROUP, BRAVO_ID);
		EndText.Text = TC._GetText(END_FILE, END_GROUP, ENDTEXT_ID);
		ShockT.Text = TC._GetText(END_FILE, END_GROUP, SHOCK_ID);
		EnergyT.Text = TC._GetText(END_FILE, END_GROUP, ENERGY_ID);
		SupportT.Text = TC._GetText(END_FILE, END_GROUP, SUPPORT_ID);
		EnvT.Text = TC._GetText(END_FILE, END_GROUP, ENV_TEXT_ID);
	}
	
	private void _OnScorePressed() {
		EndWindow.Visible = !EndWindow.Visible;
	}
}
