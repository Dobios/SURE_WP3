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
using System.Diagnostics;
using System.Linq;


// ============================================================
// ==================== RESOURCE DATATYPES ====================
// ============================================================

// ==================== Energy DataType ====================

// Models the resource managed by the EnergyManager
public struct Energy {
	public float SupplySummer; // Total Supply for the next turn for the summer months
	public float SupplyWinter; // Total Supply for the next turn for the winter months
	public float DemandSummer; // Total Demand for the next turn for the summer months
	public float DemandWinter; // Total Demand for the next turn for the winter months
	public float SurplusSummer; // Amount of excess energy produced in the summer months (represents an underproduction if negative)
	public float SurplusWinter; // Amount of excess energy produced in the winter months (represents an underproduction if negative)


	// Basic constructor for the Energy Ressource
	public Energy(float SS=0, float SW=0, float DS=0, float DW=0, float SurS=0, float SurW=0) {
		SupplySummer = SS;
		SupplyWinter = SW;
		DemandSummer = DS;
		DemandWinter = DW;
		SurplusSummer = SurS;
		SurplusWinter = SurW;
	}
}

// ==================== Support DataType ====================

// Models the public support resource
public struct Support {
	public int Value; // Basic support type for now.

	public Support(int v=60) {
		Value = v;
	}
}

// ==================== Environment DataType ====================

// Models the environment resource
public struct Environment {
	public int Pollution; // Aggregate amount of pollution generated by the power plants
	public float LandUse; // Aggregate space taken up by all of the power plants
	public float Biodiversity; // Level of biodiversity remaining on the map
	public int ImportedPollution; // The amount of pollution caused by energy imports
	public float Shock;

	// Computes the value used to update the environment bar
	public double EnvBarValue() {
		// Use a basic signmoid to compute the mixture
		double x = Math.Exp(6.0 * (Biodiversity - 1.1 * LandUse)) - Shock;
		return x / (x + 1.0);
	}

	public int PollutionBarValue() => Pollution + ImportedPollution;

	// Basic constructor for the environment struct
	public Environment(int p=0, float lu=0.0f, float bd=0.0f, int ip=0, float s=0.0f) {
		Pollution = p;
		ImportedPollution = ip;

		// Make sure that the following two metrics are percentages
		LandUse = Math.Max(0.0f, Math.Min(lu, 1.0f)); 
		Biodiversity = Math.Max(0.0f, Math.Min(bd, 1.0f));
		
		Shock = s;
	}
}

// ==================== Money DataType ====================

// Contains all of the fields related to money
public struct MoneyData {
	public int Money; // Total amount of available money
	public int Budget; // Total budget for this round
	public int Production; // Money spent on production costs this round
	public int Build; // Money spent on build costs this round
	public int Imports; // Money spent on imports this turn
	public int Debt; // The amount of debt acquired so far

	// Default constructor for the MoneyData
	public MoneyData(int start_money) {
		Money = start_money;
		Budget = start_money;
		Production = 0;
		Build = 0;
		Imports = 0;
		Debt = 0;
	}

	// Resets the spending statistics at the end of each round
	public void NextTurn(int new_budget, int production, int ImportCost) {
		Budget = Money + new_budget;
		// Each turn the player needs to pay for:
		// Production costs: the cost of running the power plants
		// Import costs: the cost of importing the requested amount of power
		// Debt: pay back the acquired debt
		Money += new_budget - production - ImportCost - Debt;
		Production = production;
		Build = 0;
		Imports = ImportCost;
		Debt = 0;
	}

	// Spends money by updating the data correctly
	public void SpendMoney(int amountBuild) {
		Build += amountBuild;
		Money -= amountBuild;
	}

	// Acquire debt
	// @param debt: the amount debt acquired
	// @param borrowed: the amount of money that was borrowed
	public void AcquireDebt(int debt, int borrowed) {
		// Record debt
		Debt += debt;

		// Add the money
		Money += borrowed;
	}

	// Simply updates the cost of imports
	// @param importCost: the cost of the current import
	public void UpdateImportCost(int importCost) {
		Imports = importCost;
	}

	// Updates the production cost to the given amount
	// @param prodCost: The amount of money that production currently costs
	public void UpdateProductionCost(int prodCost) {
		Production = prodCost;
	}
}

// Keeps track of the current active multiplier overloads
// -1 means that no overload is active
// These are created by enacting policies
public struct MultiplierOverloads {
	public int WindMax;
	public int WindBuildTime;
	public int SolarMax;
	public int SolarBuildTime;

	public MultiplierOverloads() {
		WindMax = -1;
		WindBuildTime = -1;
		SolarMax = -1;
		SolarBuildTime = -1;
	}
}

// ============================================================
// ======================= FANCY ENUMS ========================
// ============================================================

// ==================== Building Type Enum ====================

// Represents the different types of power plants
public readonly struct Building {

	public readonly Type type;

	// The various types of power plants
	public enum Type { HYDRO, GAS, SOLAR, TREE, NUCLEAR, WIND, WASTE, BIOMASS, RIVER, PUMP, NONE };

	// Labels used as string representations of the types
	public const string GAS_LABEL = "gas";
	public const string HYDRO_LABEL = "hydro";
	public const string SOLAR_LABEL = "solar";
	public const string TREE_LABEL = "tree";
	public const string NUCLEAR_LABEL = "nuclear";
	public const string WIND_LABEL = "wind";
	public const string WASTE_LABEL = "waste";
	public const string BIOMASS_LABEL = "biomass";
	public const string RIVER_LABEL = "river";
	public const string PUMP_LABEL = "pump";

	// Base values for the model's building types
	private const int GAS_ID_BASE = 1;
	private const int NUC_ID_BASE = 2;
	private const int RIV_ID_BASE = 3;
	private const int SOL_ID_BASE = 6;
	private const int WND_ID_BASE = 7;
	// Could you fill this ?

	// Basic constructor for the Building type
	public Building(Type bt)  {
		type = bt;
	}

	// Override equality and inequality operators
	public static bool operator ==(Building b, Building other) => b.type == other.type;
	public static bool operator !=(Building b, Building other) => b.type != other.type;

	// Override the incrementation and decrementation operators
	public static Building operator ++(Building b) => new Building((Type)((int)(b.type + 1) % (int)(Type.NONE)));
	public static Building operator --(Building b) => new Building((Type)((int)(b.type - 1) % (int)(Type.NONE)));

	// Implicit conversion from the enum to the struct
	public static implicit operator Building(Type bt) => new Building(bt);

	// Implicit conversion from the struct to the enum
	public static implicit operator Type(Building b) => b.type;

	// Implicit conversion from a string to a config type
	public static implicit operator Building(string s) {
		// Make it as easy to parse as possible
		string s_ = s.ToLower().StripEdges();

		// Check the given string against the labels
		if(s_ == GAS_LABEL) {
			return new Building(Type.GAS);
		}
		if(s_ == HYDRO_LABEL) {
			return new Building(Type.HYDRO);
		} 
		if(s_ == SOLAR_LABEL) {
			return new Building(Type.SOLAR);
		} 
		if(s_ == TREE_LABEL) {
			return new Building(Type.TREE);
		}
		if(s_ == WIND_LABEL) {
			return new Building(Type.WIND);
		}
		if(s_ == NUCLEAR_LABEL) {
			return new Building(Type.NUCLEAR);
		}
		if(s_ == WASTE_LABEL) {
			return new Building(Type.WASTE);
		}
		if(s_ == BIOMASS_LABEL) {
			return new Building(Type.BIOMASS);
		}
		if(s_ == RIVER_LABEL) {
			return new Building(Type.RIVER);
		}
		if(s_ == PUMP_LABEL) {
			return new Building(Type.PUMP);
		}

		// The given string was invalid so we give it the impossible type
		return new Building(Type.NONE);
	}
	
	// Implicit conversion to a string
	public override string ToString() => 
		type == Type.GAS ? GAS_LABEL :
		type == Type.HYDRO ? HYDRO_LABEL :
		type == Type.SOLAR ? SOLAR_LABEL :
		type == Type.TREE ? TREE_LABEL :
		type == Type.WIND ? WIND_LABEL :
		type == Type.NUCLEAR ? NUCLEAR_LABEL : 
		type == Type.WASTE ? WASTE_LABEL : 
		type == Type.BIOMASS ? BIOMASS_LABEL : 
		type == Type.RIVER ? RIVER_LABEL : 
		type == Type.PUMP ? PUMP_LABEL : 
		"";

	// Explicit conversion to an int
	public int ToInt() =>
		type == Type.GAS ? GAS_ID_BASE :
		type == Type.HYDRO ? RIV_ID_BASE :
		type == Type.SOLAR ? SOL_ID_BASE :
		type == Type.NUCLEAR ? NUC_ID_BASE : 
		-1;
		// Could you fill this ?

	// Performs the same check as the == operator, but with a run-time check on the type
	public override bool Equals(object obj) {
		// Check for null and compare run-time types.
		if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
			return false;
		}
		// Perform actual equality check
		return type == ((Building)obj).type;
	}

	// Override of the get hashcode method (needed to overload == and !=)
	public override int GetHashCode() => HashCode.Combine(type);
}

// Models a Build Slot
public struct BuildLocation {
	// Contains the Position at which the build wild take place
	public Vector2 Position;

	// Contains the types we are allowed to build at this location
	public List<Building> AvailableTypes; 

	// Struct constructor using var-args for the available types
	public BuildLocation(Vector2 _P, params Building[] _BT) {
		Position = _P;
		AvailableTypes = new List<Building>();

		// Fill in the available types
		foreach(var bt in _BT) {
			AvailableTypes.Add(bt);
		}
	}
}

// ==================== Config Enum ====================

// Models a config type
public readonly struct Config {
	// Represents the different types of configs
	public enum Type { POWER_PLANT, NONE };

	// Internal storage of a config
	public readonly Type type;

	// Basic constructor for the Config type
	public Config(Type ct)  {
		type = ct;
	}

	// Override equality and inequality operators
	public static bool operator ==(Config l, Config other) => l.type == other.type;
	public static bool operator !=(Config l, Config other) => l.type != other.type;

	// Override the incrementation and decrementation operators
	public static Config operator ++(Config l) => new Config((Type)((int)(l.type + 1) % (int)(Type.NONE + 1)));
	public static Config operator --(Config l) => new Config((Type)((int)(l.type - 1) % (int)(Type.NONE + 1)));

	// Implicit conversion from the enum to the struct
	public static implicit operator Config(Type lt) => new Config(lt);

	// Implicit conversion from the struct to the enum
	public static implicit operator Type(Config l) => l.type;

	// Implicit conversion from a string to a config type
	public static implicit operator Config(string s) {
		// Make it as easy to parse as possible
		string s_ = s.ToLower().StripEdges();
		if(s_ == "powerplants") {
			return new Config(Type.POWER_PLANT);
		} 
		return new Config(Type.NONE);
	}
	
	// Implicit conversion to a string
	public override string ToString() => type == Type.POWER_PLANT ? "powerplants.xml" : "";

	// Performs the same check as the == operator, but with a run-time check on the type
	public override bool Equals(object obj) {
		// Check for null and compare run-time types.
		if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
			return false;
		}
		// Perform actual equality check
		return type == ((Config)obj).type;
	}

	// Override of the get hashcode method (needed to overload == and !=)
	public override int GetHashCode() => HashCode.Combine(type);
}

// Represents the multiplier config of a powerplant
public readonly struct Multiplier {

	public readonly int MaxElements; // The maximum number of elements allowed for a given plant
	public readonly int Cost; // The cost of adding an element to the given power plant
	public readonly float Pollution; // The factor by which the pollution is increased when an element is added
	public readonly float LandUse; // The factor by which the land use is increased when an element is added
	public readonly float Biodiversity; // The factor by which the biodiversity is increased when an element is added
	public readonly float ProductionCost; // The factor by which the production cost is increased when an element is added
	public readonly int Capacity; // The amount by which the capacity is increased when an element is added

	// Basic constructor
	public Multiplier(int me, int c, float p, float lu, float bd, float pc, int cap) {
		MaxElements = me;
		Cost = c;
		Pollution = p;
		LandUse = lu;
		Biodiversity = bd;
		ProductionCost = pc;
		Capacity = cap;
	}
}

// ==================== Language Enum ====================

// Models a language
public readonly struct Language {
	// Represents the different types of languages
	public enum Type { EN, FR, DE, IT };

	// Internal storage of a language
	public readonly Type lang;

	// Basic constructor for the language
	public Language(Type l)  {
		lang = l;
	}

	// Override equality and inequality operators
	public static bool operator ==(Language l, Language other) => l.lang == other.lang;
	public static bool operator !=(Language l, Language other) => l.lang != other.lang;

	// Override the incrementation and decrementation operators
	public static Language operator ++(Language l) => new Language((Type)((int)(l.lang + 1) % (int)(Type.IT + 1)));
	public static Language operator --(Language l) => new Language((Type)((int)(l.lang - 1) % (int)(Type.IT + 1)));

	// Implicit conversion from the enum to the struct
	public static implicit operator Language(Type lt) => new Language(lt);

	// Implicit conversion from the struct to the enum
	public static implicit operator Type(Language l) => l.lang;

	// Implicit conversion from a string to a language
	public static implicit operator Language(string s) {
		// Make it as easy to parse as possible
		string s_ = s.ToLower().StripEdges();
		if(s == "en" || s == "english") {
			return new Language(Type.EN);
		} 
		if (s == "fr" || s == "french" || s == "français") {
			return new Language(Type.FR);
		} 
		if(s == "de" || s == "german" || s == "deutsch") {
			return new Language(Type.DE);
		} 
		return new Language(Type.IT);
	}
	
	// Implicit conversion to a string
	public override string ToString() => lang == Type.EN ? "en" : 
										lang == Type.FR ? "fr" :
										lang == Type.DE ? "de" :
										"it";

	// Converts the language to a human-readable format
	public string ToName() => lang == Type.EN ? "Language: English" : 
							lang == Type.FR ? "Langue: Français" :
							lang == Type.DE ? "Sprache: Deutsch" :
							"Lingua: Italiano";

	// Performs the same check as the == operator, but with a run-time check on the type
	public override bool Equals(object obj) {
		// Check for null and compare run-time types.
		if ((obj == null) || ! this.GetType().Equals(obj.GetType())) {
			return false;
		}
		// Perform actual equality check
		return lang == ((Language)obj).lang;
	}

	// Override of the get hashcode method (needed to overload == and !=)
	public override int GetHashCode() => HashCode.Combine(lang);
}

// ==========================================================
// ==================== UTILITY DATATYPES ===================
// ==========================================================

// ==================== Config Datatype ===================

// Abstract notion of config data
public interface ConfigData {
	// Copies the data into a powerplant
	public abstract void _CopyTo(ref PowerPlant nd);
}

// Represents the data retrieved from a powerplant config file
public readonly struct PowerPlantConfigData : ConfigData {
	
	// Metadata fields
	public readonly int BuildCost;
	public readonly int BuildTime;
	public readonly int EndTurn;

	// Energy fields
	public readonly int ProductionCost;
	public readonly int Capacity;
	public readonly float Availability_W;
	public readonly float Availability_S;

	// Environment fields
	public readonly int Pollution;
	public readonly float LandUse;
	public readonly float Biodiversity;

	// Basic constructor for the datatype
	public PowerPlantConfigData(
		int bc=0, int bt=0, int lc=0,
		int pc=0, int cap=0, float avw=0, float avs=0,
		int pol=0, float lu=0, float bd=0
	) {
		// Simply fill in the fields
		BuildCost = bc;
		BuildTime = bt;
		EndTurn = lc;
		ProductionCost = pc;
		Capacity = cap;
		Availability_W = Math.Max(0.0f, Math.Min(avw, 1.0f));
		Availability_S = Math.Max(0.0f, Math.Min(avs, 1.0f));
		Pollution = pol;
		LandUse = lu;
		Biodiversity = bd;
	}

	// Copies the datatype fields into a PowerPlant Object
	public void _CopyTo(ref PowerPlant PP) {
		// Sanity check 
		if(PP == null) {
			throw new ArgumentException("Invalid PowerPlant was given!");
		}

		// Copy in the public fields
		PP.BuildCost = BuildCost;
		PP.BuildTime = BuildTime;
		PP.EndTurn = EndTurn;
		PP.LandUse = LandUse;
		PP.BiodiversityImpact = Biodiversity;

		// Copy in the private fields
		PP._UdpatePowerPlantFields(
			true, 
			Pollution,
			ProductionCost,
			Capacity
		);
	}
}

// The different types of resources the player has access to
public enum ResourceType { 
	ENERGY_W, ENERGY_S, DEMAND_W,
	DEMAND_S, ENVIRONMENT, SUPPORT,
	MONEY, TAG, WIND_MULT_MAX,
	WIND_BUILD_TIME, SOLAR_MULT_MAX, SOLAR_BUILD_TIME
};

// Struct simply containing the couple of methods useful for the resource type enum
// RTM = Resource Type Methods
public readonly struct RTM {
	// Converts a given string into a resource type enum field
	public static ResourceType ResourceTypeFromString(string s) => s switch {
		"energyW" => ResourceType.ENERGY_W,
		"energyS" => ResourceType.ENERGY_S,
		"energyDemandW" => ResourceType.DEMAND_W,
		"energyDemandS" => ResourceType.DEMAND_S,
		"wind_mult_max" => ResourceType.WIND_MULT_MAX,
		"wind_buildtime" => ResourceType.WIND_BUILD_TIME,
		"solar_mult_max" => ResourceType.SOLAR_MULT_MAX,
		"solar_buildtime" => ResourceType.SOLAR_BUILD_TIME,
		"env" => ResourceType.ENVIRONMENT,
		"support" => ResourceType.SUPPORT,
		"money" => ResourceType.MONEY,
		"tag" => ResourceType.TAG,
		_ => throw new ArgumentException("The given string can't be converted to a resource type")
	};
}

// Represents the requirements of a given shock
public struct Requirement {
	// The resource impacted by this requirement
	public ResourceType RT;

	// The value required by the requirement
	public float Value; 

	// Basic constructors
	public Requirement(string s, float v) {
		RT = RTM.ResourceTypeFromString(s);
		Value = v;
	}
	public Requirement(ResourceType rt, float v) {
		RT = rt;
		Value = v;
	}
}

// Represents an effect on a resource
public struct Effect {
	// The type of resource that's targeted by this effect
	public ResourceType RT;
	// The amount by which the resource will be impacted (can be negative)
	public float Value;

	// Basic constructor
	public Effect(ResourceType _RT, float _Val) {
		RT = _RT;
		Value = _Val;
	}
}

// Represents the rewards of surviving a given shock
public struct Reward {
	// The text show for the given reward
	public string Text;

	// The effects of the reward
	public List<Effect> Effects;

	// Basic Constructor
	public Reward(string t, List<Effect> es) {
		Text = t;
		Effects = es;
	}

	// Converts an effect into a list of requirements
	public List<Requirement> ToRequirements() => 
		// Only negative effects have requirements, all others are clamped to 0
		Effects.Select(se => new Requirement(se.RT, Math.Abs(Math.Min(se.Value, 0)))).ToList();
	
}

// ==================== UI Info Datatype ===================

// Data structure for the information displayed in the info boxes
public struct InfoData {
	// === Field Numbers for each type ===
	public const int N_W_ENERGY_FIELDS = 2;
	public const int N_S_ENERGY_FIELDS = 2;
	public const int N_ENV_FIELDS = 5;
	public const int N_SUPPORT_FIELDS = 1;
	public const int N_MONEY_FIELDS = 5;

	// === Energy metrics ===
	public int W_EnergyDemand; // Energy demand for the winter season
	public int W_EnergySupply; // Energy supply for the winter season
	public int S_EnergyDemand; // Energy demand for the summer season
	public int S_EnergySupply; // Energy supply for the summer season

	// === Support metrics ===
	public int SupportVal; // The amount of support the player has (0 to 100)

	// === Environment metrics ===
	public int LandUse; // Used in the environment bar
	public int Pollution; // Also for the environment bar
	public int Biodiversity; // For the environment bar
	public int ImportPollution; // The pollution caused by energy imports

	// === Money Metrics ===
	public int Budget; // The amount of money you are generating this turn
	public int Production; // The amount of money used for production this turn
	public int Building; // The amount of money spent on building this turn
	public int Money; // The total amount of money you have
	public int Imports; // The total amount spent on imports last turn

	// Constructor for the Data
	public InfoData() {
		W_EnergyDemand = 0; 
		W_EnergySupply = 0; 
		S_EnergyDemand = 0; 
		S_EnergySupply = 0; 

		SupportVal = 0; 

		LandUse = 0;
		Pollution = 0;
		Biodiversity = 0; 
		ImportPollution = 0;

		Budget = 0;
		Production = 0;
		Building = 0;
		Money = 0; 
		Imports = 0;
	}
}

