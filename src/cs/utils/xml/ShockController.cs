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
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

// XML Controller specifically tailored for reading the shock config files
// These are particular, as they contain both translatable text and config data.
public partial class ShockController : XMLController {
	// The currently loaded xml document
	private XDocument LoadedXML;
	private string LoadedFileName;
	private Language LoadedLanguage;

	// The current language
	private Language Lang = Language.Type.EN;

	// Context
	private Context C;

	// Shock xml file name (always the same)
	private const string SHOCK_FILENAME = "shocks.xml";

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Fetch Context
		C = GetNode<Context>("/root/Context");

		// Connect to the context's update language signal
		C.UpdateLanguage += _UpdateLanguage;
	}

	// ==================== Public API ====================

	// Updates the language the textcontroller is set to  
	public void _UpdateLanguage() {
		// Check that the given language is new
		if(C._GetLanguage() != Lang) {
			Lang = C._GetLanguage();
			
			// Update the loaded xml
			ParseXML(ref LoadedXML, Path.Combine("text/", Lang.ToString() + "/" + LoadedFileName));
		}
		// Don't do anything if the languages are the same
	}

	// Retrieves the shock's name from the shocks xml file given the shock's id
	public string _GetShockName(string id) => GetField(id, "name");

	// Retrieves the shock's description from the shocks xml given the shock's id
	public string _GetShockText(string id) => GetField(id, "text");
	
	// Retrieves the shock's image name from the shocks xml file given the shock's id
	public string _GetShockImg(string id) => GetField(id, "img");

	// Retrieves the text from a requirement given the id of the shock and that of the requirement
	public List<Requirement> _GetRequirements(string shock_id) {
		// Start by checking if the file is loaded in or not
		CheckXML();

		// Retrieve the shock from the currently parsed xml file
		IEnumerable<XElement> shock = 
			from s in LoadedXML.Root.Descendants("shock")
			where s.Attribute("id").Value == shock_id
			select s;

		// Find the correct requirement from the shock
		IEnumerable<XElement> requirements = shock.Descendants("requirement");

		// Extract the requirement values and return them in a struct list
		return shock.Descendants("requirement").Select(r => new Requirement(
			r.Attribute("field").Value,
			r.Attribute("value").Value.ToFloat()
		)).ToList();
	}

	// Retrieves the reward from surviving a shock, given the shock id
	public Reward _GetReward(string id) {
		// Start by checking if the file is loaded in or not
		CheckXML();

		// Retrieve the shock from the currently parsed xml file
		IEnumerable<XElement> shock = 
			from s in LoadedXML.Root.Descendants("shock")
			where s.Attribute("id").Value == id
			select s;

		// Extract the different fields
		string t = shock.Descendants("reward").ElementAt(0)
						.Descendants("text").ElementAt(0).Value;

		// Extract all of the effects
		IEnumerable<XElement> effects_xml = 
			shock.Descendants("reward").ElementAt(0)
				 .Descendants("effect");
		
		// Build out the effects list
		List<Effect> effects = effects_xml.Select(e => new Effect(
			RTM.ResourceTypeFromString(e.Attribute("field").Value),
			e.Attribute("value").Value.ToFloat()
		)).ToList();

		// Return the reward as a struct
		return new (t, effects);
	}

	// Retrieves all of the reactions associated to the given shock
	public List<Reward> _GetReactions(string id) {
		// Start by checking if the file is loaded in or not
		CheckXML();

		// Retrieve the shock from the currently parsed xml file
		IEnumerable<XElement> shock = 
			from s in LoadedXML.Root.Descendants("shock")
			where s.Attribute("id").Value == id
			select s;

		// Extract all of the rewards
		IEnumerable<XElement> reacts_xml = shock.Descendants("reaction");

		// Build out the shock effect list and return it
		return reacts_xml.Select(r => new Reward(
			r.Descendants("text").ElementAt(0).Value,
			r.Descendants("effect").Select(e => new Effect( // Build out the effects list
				RTM.ResourceTypeFromString(e.Attribute("field").Value),
				e.Attribute("value").Value.ToFloat()
			)).ToList()
		)).ToList();
	}


	// ==================== Internal Helpers ====================

	// Retrives a first-level field's content given an id and the field string
	private string GetField(string id, string field) {
		// Start by checking if the file is loaded in or not
		CheckXML();

		// Retrieve the shock from the currently parsed xml file
		return (
			from s in LoadedXML.Root.Descendants("shock")
			where s.Attribute("id").Value == id
			select s.Descendants(field).ElementAt(0).Value // Only the first field in the xml is considered
		).ElementAt(0);
	}

	// Checks if the currently loaded xml is up to date
	private void CheckXML() {
		// Check if the file is loaded in or not
		if(LoadedFileName != SHOCK_FILENAME || LoadedLanguage != Lang) {
			ParseXML(ref LoadedXML, Path.Combine("text/", Lang.ToString() + "/" + SHOCK_FILENAME));
			LoadedFileName = SHOCK_FILENAME;
			LoadedLanguage = Lang;
		}
	}
}
