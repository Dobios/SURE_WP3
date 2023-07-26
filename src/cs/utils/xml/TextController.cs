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

// Utility class used to access XML db files.
// This is usually done to display text in a way that is linguistically dynamic .
public partial class TextController : XMLController {

	// The currently loaded xml document
	private XDocument LoadedXML;
	private string LoadedFileName;
	private Language LoadedLanguage;

	// The current language
	private Language Lang = Language.Type.EN;

	// ==================== Public API ====================
	
	// Updates the language the textcontroller is set to  
	public void _UpdateLanguage(Language l) {
		// Check that the given language is new
		if(l != Lang) {
			Lang = l;
			
			// Update the loaded xml
			ParseXML(ref LoadedXML, Path.Combine("text", Lang.ToString() + "/" + LoadedFileName));
		}
		// Don't do anything if the languages are the same
	}

	// Increments the language
	public void _NextLanguage() {
		Lang = ++Lang;
	}

	// Retrieve the language name
	public string _GetLanguageName() => Lang.ToName();

	// Queries the given xml file to retrieve the wanted text
	public string _GetText(string filename, string groupid, string id) {
		// Start by checking if the file is loaded in or not
		if(LoadedFileName != filename || LoadedLanguage != Lang) {
			ParseXML(ref LoadedXML, Path.Combine("text", Lang.ToString() + "/" + filename));
			LoadedFileName = filename;
			LoadedLanguage = Lang;
		}

		// Query the file
		var query = from g in LoadedXML.Root.Descendants("group")
					where g.Attribute("id").Value == groupid // Find the correct group
					select (
						from t in g.Descendants("text")
						where t.Attribute("id").Value == id // Find the correct text in the group
						select t.Value
					);

		// Extract query result
		foreach(var g in query) {
			foreach(var t in g) {
				return t;
			}
		}

		// If we reach this point in the method, then we failed somewhere
		throw new Exception("No valid string matches the given query!!");
	}
}