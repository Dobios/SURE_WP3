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

// Models the interactive camera system which allows the player 
// to drag the view around and zoom in and out.
public partial class Camera : Camera2D {

	// Camera control parameters
	private Vector2 ZOOM_MIN = new (0.5f,0.5f);
	private Vector2 ZOOM_MAX = new (0.8f,0.8f);
	private Vector2 ZOOM_SPEED = new (0.2f,0.2f);
	private Vector2 ZoomVal = new (0.8f,0.8f);
	private Vector2 SCALE_LIMIT = new (1f, 1f);

	// Record initial position and zoom for reset
	private Vector2 InitPos;
	private Vector2 InitZoom;

	// ==================== GODOT Method Overrides ====================

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// Record initial position and zoom
		InitPos = Position;
		InitZoom = Zoom;
	}
	
	// Updates the size of the plants to follow the zoom amount
	public void ScalePlants(Vector2 ZoomVal) {
		// Check for valid zoom
		if (ZoomVal < SCALE_LIMIT) {
			// Update all powerplants
			foreach (Node2D Plant in GetTree().GetNodesInGroup("PP")) {
				// Create an animation for the zoom
				Tween TweenScale = CreateTween();
				TweenScale.TweenProperty(Plant, "scale", new Vector2(1,1) / (ZoomVal*2f), 0.3f);
			}

			// Do the same for all buildbuttons
			foreach (TextureButton BuildButton in GetTree().GetNodesInGroup("BB")) {
				Tween TweenScale = CreateTween();
				TweenScale.TweenProperty(BuildButton, "scale", new Vector2(1,1) / (ZoomVal*2f), 0.3f);
			}
		}
	}

	// Adds control to inputs that otherwise would not have triggered any events  
	// In our case, this includes 2 events:  
	//     1) When the player clicks the screen, in which case we want to drag the camera around
	//     2) When the player uses the mouse wheel, in which case we want to zoom in or out
	public override void _UnhandledInput(InputEvent E) {
		// Camera can be moved by holding left click and dragging the mouse
		if(E is InputEventMouseMotion MouseMotion) {
			if(MouseMotion.ButtonMask == MouseButtonMask.Left) {
				Position -= MouseMotion.Relative / Zoom;
			}
		}

		// Can zoom the camera using the mouse wheel, smoothed with a tween animation
		if(E is InputEventMouseButton MouseBtn) {
			// Check what type of scroll was done
			// If we are scrolling down, then we want the view to zoom out
			if (MouseBtn.ButtonIndex == MouseButton.WheelDown) {
					// Make sure that we clamp the zoom to avoid seeing out of the scene
					if(Zoom > ZOOM_MIN) {
						// Udpate the zoom using a fancy animation to smoothen the transition
						ZoomVal = Zoom - ZOOM_SPEED;
						Tween TweenZoomIn = CreateTween();
						TweenZoomIn.TweenProperty(this, "zoom", ZoomVal, 0.3f);
						ScalePlants(ZoomVal);
					}
			}
			// If we are scrolling up, the we want the view to zoom in
			if (MouseBtn.ButtonIndex == MouseButton.WheelUp) {
					// Make sure we can't over zoom
					if(Zoom < ZOOM_MAX) {
						// Update the zoom using a fancy animation
						ZoomVal = Zoom + ZOOM_SPEED;
						Tween TweenZoomOut = CreateTween();
						TweenZoomOut.TweenProperty(this, "zoom", ZoomVal, 0.3f);
						ScalePlants(ZoomVal);
					}
			}
		}
	}

	// ==================== Public API ====================

	// Reset the camera position and zoom
	public void _ResetPos() {
		Offset = Vector2.Zero;
		Position = InitPos;
		ZoomVal = new (0.8f, 0.8f);
		Zoom = InitZoom;
		
		// reset scale
		foreach (Node2D Plant in GetTree().GetNodesInGroup("PP")) {
			Plant.Scale = new Vector2(1.0f, 1.0f);
		}
		foreach (TextureButton BuildButton in GetTree().GetNodesInGroup("BB")) {
			BuildButton.Scale = new Vector2(1.0f, 1.0f);
		}
	}
}
