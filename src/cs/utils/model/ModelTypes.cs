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

// ==================== Model Datatypes ===================
// This file contains all of the datastructures pertaining 
// to the internal representation of the data retrieved 
// from the remote energy grid model
// ========================================================

// Internal datastructure containing the fields that are obtained through Toby's Model
public struct Model {
    // Different categories of data retrieved from the model
    public Availability _Availability;
    public Capacity _Capacity;
    public Demand _Demand;

    // Base Constructor for the model
    public Model(
        Availability A=new Availability(), 
        Capacity C=new Capacity(), 
        Demand D=new Demand()
    ) {
        _Availability = A;
        _Capacity = C;
        _Demand = D;
    }
}

// Represents the data retrieved from the availability columns of the model
public struct Availability {
    // Current availability of various type-based energies
    public float Gas; // Refers to avl_gas
    public float Nuclear; // Refers to avl_nuc
    public float River; // Refers to avl_riv (Hydro-electic)
    public float Solar; // Refers to avl_sol
    public float Wind; // Refers to avl_win

    //TODO: Add entries for avl_pet, avl_res, avl_pmp, avl_bio, avl_wst, and avl_geo

    // Basic constructor for the Availability struct
    public Availability(float g=0.0f, float n=0.0f, float r=0.0f, float s=0.0f, float w=0.0f) {
        Gas = g;
        Nuclear = n;
        River = r;
        Solar = s;
        Wind = w;
    }
}

// Represents the data retrieved from the Capacity columns of the model
public struct Capacity {
    // Current availability of various type-based energies
    public int Gas; // Refers to cap_ele_gas
    public int Nuclear; // Refers to cap_ele_nuc
    public int River; // Refers to cap_ele_riv (Hydro-electic)
    public int Solar; // Refers to cap_ele_sol
    public int Wind; // Refers to cap_ele_win

    //TODO: Add entries for cap_pet, cap_res, cap_pmp, cap_bio, cap_wst, and cap_geo

    // Basic constructor for the Capacity struct
    public Capacity(int g=0, int n=0, int r=0, int s=0, int w=0) {
        Gas = g;
        Nuclear = n;
        River = r;
        Solar = s;
        Wind = w;
    }
}

// Represents the data retrived from the Demand columns of the model
public struct Demand {
    // Current demand for certain types of resources
    public int Base; // Base energy demand

    //TODO: Add entries for dem_cool, dem_hind, dem_hres, dem_road, dem_rail

    // Basic constructor for the Demand struct
    public Demand(int b=0) {
        Base = 0;
    }
} 
