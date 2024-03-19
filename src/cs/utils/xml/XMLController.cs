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

// Base class for all controllers that work with xml objects
public abstract partial class XMLController : Node {

	// The path to the base of the db 
	protected const string DB_PATH = "res://db/";

	// ==================== XML Generic methods ====================

	// Parses a given xml file and stores in in a target XDocument object
	// The filename should include the relative path from db/
	protected void ParseXML(ref XDocument targetXML, string filename) {
		if(filename == null) {
			throw new Exception("No xml file was input for the scene!");
		}
		
		//Load XML file into a XDocument for querying
		string loadedXML;
		XDocument xml;
		string path = DB_PATH + filename;
		try {
			using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
			loadedXML = file.GetAsText();
			xml = XDocument.Parse(loadedXML);
		} catch(Exception) {
			// Control what error is displayed for better debugging
			throw new Exception("File not found: " + path);
		}
		
		//Sanity check
		if(xml != null) {
			targetXML = xml;
		} else {
			throw new Exception("Unable to load xml file: " + filename);
		}
	}
}
