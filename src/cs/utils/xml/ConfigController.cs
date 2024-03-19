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

// XMLController that can read config files
public partial class ConfigController : XMLController {

	// The currently loaded xml document and filename
	private XDocument LoadedXML;
	private string LoadedFileName;

	// ==================== Public API ====================

	// Reads out a config file given a config type and a config id
	public ConfigData _ReadConfig(Config config, string id) => config.type switch {
		Config.Type.POWER_PLANT => ReadPPConfig(config.ToString(), id),	
		_ => throw new ArgumentException("Invalid Configuration Type was given!!")
	};

	// Reads out a multiplier for a given plant
	public Multiplier _ReadMultiplier(Config config, string id) => config.type switch {
		Config.Type.POWER_PLANT => ReadPPMultiplier(config.ToString(), id),
		_ => throw new ArgumentException("Invalid Configuration Type was given!!")
	};
	

	// ==================== Internal Helper Methods ====================
	
	// Queries a given config for a specific parameter group
	private IEnumerable<XElement> GetParamGroup(IEnumerable<XElement> config, string groupid) =>
		from p in config.Descendants("param")
		where p.Attribute("groupid").Value == groupid
		select p;

	// Retrieves an integer parameter from a given group
	private int GetIntParam(IEnumerable<XElement> group, string id) =>
		int.Parse(group
			.Where(g => (g.Attribute("id").Value == id) && (g.Attribute("type").Value == "int"))
			.Select(p => p.Value).ElementAt(0));

	// Retrieves a float parameter from a given group
	private float GetFloatParam(IEnumerable<XElement> group, string id) =>
		float.Parse(group
			.Where(g => (g.Attribute("id").Value == id) && (g.Attribute("type").Value == "float"))
			.Select(p => p.Value).ElementAt(0));

	// Reads out a multiplier from a powerplant config
	private Multiplier ReadPPMultiplier(string filename, string id) {
		// Start by checking if the file is loaded in or not
		if(LoadedFileName != filename) {
			ParseXML(ref LoadedXML, Path.Combine("configs/", filename));
			LoadedFileName = filename;
		}

		// Query the file
		IEnumerable<XElement> query = 
			from g in LoadedXML.Root.Descendants("config")
			where g.Attribute("id").Value == id // Find the correct group
			select g;
		
		// Retrive parameter group
		IEnumerable<XElement> multparams = GetParamGroup(query, "multiplier");

		if(multparams == null) throw new Exception("HELP");

		// Build out the mutliplier struct
		return new (
			GetIntParam(multparams, "max_elements"), 
			GetIntParam(multparams, "cost"),
			GetFloatParam(multparams, "pollution"),
			GetFloatParam(multparams, "land_use"),
			GetFloatParam(multparams, "biodiversity"),
			GetFloatParam(multparams, "production_cost"),
			GetIntParam(multparams, "capacity")
		);
	}

	// Reads out a power plant configuation file
	private PowerPlantConfigData ReadPPConfig(string filename, string id) {
		// Start by checking if the file is loaded in or not
		if(LoadedFileName != filename) {
			ParseXML(ref LoadedXML, Path.Combine("configs/", filename));
			LoadedFileName = filename;
		}

		// Query the file
		IEnumerable<XElement> query = 
			from g in LoadedXML.Root.Descendants("config")
			where g.Attribute("id").Value == id // Find the correct group
			select g;
		
		// Retrive parameter groups
		IEnumerable<XElement> metaParams = GetParamGroup(query, "meta");
		IEnumerable<XElement> energyParams = GetParamGroup(query, "energy");
		IEnumerable<XElement> environmentParams = GetParamGroup(query, "environment");
		
		// Build out the config data and return it
		return new PowerPlantConfigData(
			GetIntParam(metaParams, "build_cost"),
			GetIntParam(metaParams, "build_time"),
			GetIntParam(metaParams, "life_cycle"),
			GetIntParam(energyParams, "production_cost"),
			GetIntParam(energyParams, "capacity"),
			GetFloatParam(energyParams, "availability_w"),
			GetFloatParam(energyParams, "availability_s"),
			GetIntParam(environmentParams, "pollution"),
			GetFloatParam(environmentParams, "land_use"),
			GetFloatParam(environmentParams, "biodiversity")
		);
	}					
}
