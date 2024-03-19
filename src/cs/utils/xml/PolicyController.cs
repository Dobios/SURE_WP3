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
using System.Diagnostics;
using System.Runtime.CompilerServices;

// XML Controller specifically tailored for reading the policy config files
// These are particular, as they contain both translatable text and config data.
// These also contain both campaigns and policies which are handled differently
public partial class PolicyController : XMLController {
	// The currently loaded xml document
	private XDocument LoadedXML;
	private string LoadedFileName;
	private Language LoadedLanguage;

	// The current language
	private Language Lang = Language.Type.EN;

	// Context
	private Context C;

	// Policy xml file name (always the same)
	private const string POLICY_FILENAME = "policies.xml";

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
			
			CheckXML();
			
			// Update the loaded xml
			ParseXML(ref LoadedXML, Path.Combine("text/", Lang.ToString() + "/" + LoadedFileName));
		}
		// Don't do anything if the languages are the same
	}

	// Retrieves the policy's name from the policies xml file given the id
	public string _GetPolicyName(string id) => GetField("policy", id, "name");

	// Retrieve the tag associated with a given policy
	public string _GetPolicyTag(string id) => _GetTag("policy", id);

	// Retrieves the policy's description from the policies xml given the id
	public string _GetPolicyText(string id) => GetField("policy", id, "text");
	
	// Retrieves the policy's probability from the policies xml file given the id
	public float _GetPolicyProba(string id) => float.Parse(GetField("policy", id, "probability"));

    // Retrieves the campaign's name from the policies xml file given the id
	public string _GetCampaigName(string id) => GetField("campaign", id, "name");

	// Retrieve the tag associated with a given campaign
	public string _GetCampaignTag(string id) => _GetTag("campaign", id);

	// Retrieves the campaign's description from the policies xml given the id
	public string _GetCampaignText(string id) => GetField("campaign", id, "text");
	
	// Retrieves the campaign's probability from the policies xml file given the id
	public float _GetCampaigProba(string id) => float.Parse(GetField("campaign", id, "probability"));

    // Retrieves the campaign's length from the policies xml file give the id
    public int _GetCampaignLength(string id) => int.Parse(GetField("campaign", id, "length"));

	// Retrieves the text from a requirement given the id of the policy 
	public List<Requirement> _GetRequirements(string policyId) {
		// Start by checking if the file is loaded in or not
		CheckXML();

		// Retrieve the policy from the currently parsed xml file
		IEnumerable<XElement> policy = 
			from s in LoadedXML.Root.Descendants("policy")
			where s.Attribute("id").Value == policyId
			select s;

		// Find the correct requirement from the policy
		IEnumerable<XElement> requirements = policy.Descendants("requirement");

		// Extract the requirement values and return them in a struct list
		return policy.Descendants("requirement").Select(r => new Requirement(
			r.Attribute("field").Value,
			r.Attribute("value").Value.ToFloat()
		)).ToList();
	}

	// Retrieves all of the effects associated to the given policy
	public List<Effect> _GetEffects(string type, string id) {
		// Start by checking if the file is loaded in or not
		CheckXML();

        // Sanity check: Make sure the type is valid
        Debug.Assert(type == "policy" || type == "campaign");

		// Retrieve the policy from the currently parsed xml file
		IEnumerable<XElement> policy = 
			from s in LoadedXML.Root.Descendants(type)
			where s.Attribute("id").Value == id
			select s;

		// Build out the policy effect list and return it
		return policy.Descendants("effect").Select(e => new Effect(
			RTM.ResourceTypeFromString(e.Attribute("field").Value),
			e.Attribute("value").Value.ToFloat(),
			e.Value ?? ""
        )).ToList();
	}


	// ==================== Internal Helpers ====================

	// Retrives a first-level field's content given an id and the field string
    // A type (policy or campaign) is also required
	private string GetField(string type, string id, string field) {
		// Start by checking if the file is loaded in or not
		CheckXML();

        // Sanity check: make sure that type is valid
        Debug.Assert(type == "policy" || type == "campaign");

		// Retrieve the policy from the currently parsed xml file
		return (
			from s in LoadedXML.Root.Descendants(type)
			where s.Attribute("id").Value == id
			select s.Descendants(field).ElementAt(0).Value // Only the first field in the xml is considered
		).ElementAt(0);
	}

    // Retrieves the policy's tag given its id
    private string _GetTag(string type, string id) {
        // Start by checking if the file is loaded in or not
        CheckXML();

        // Sanity check: make sure that type is valid
        Debug.Assert(type == "policy" || type == "campaign");

        return (from s in LoadedXML.Root.Descendants(type)
            where s.Attribute("id").Value == id
            select s.Attribute("tag").Value
        ).ElementAt(0);
    }

	// Checks if the currently loaded xml is up to date
	private void CheckXML() {
		// Check if the file is loaded in or not
		if(LoadedFileName != POLICY_FILENAME || LoadedLanguage != Lang) {
			ParseXML(ref LoadedXML, Path.Combine("text/", Lang.ToString() + "/" + POLICY_FILENAME));
			LoadedFileName = POLICY_FILENAME;
			LoadedLanguage = Lang;
		}
	}
}
