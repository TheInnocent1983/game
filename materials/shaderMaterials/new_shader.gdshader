shader_type spatial;

// Uniforms are variables you can edit in the Inspector
uniform vec3 grid_color : source_color = vec3(0.1); // Dark Gray
uniform vec3 line_color : source_color = vec3(1.0); // White
uniform float line_thickness : hint_range(0.0, 0.5) = 0.02;
uniform float grid_size = 1.0;

void fragment() {
	// NODE_POSITION_WORLD + VERTEX gives us the exact coordinate in the game world
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	
	// We use the X and Z coordinates (the horizontal plane)
	vec2 grid_uv = fract(world_pos.xz / grid_size);
	
	float line_x = step(grid_uv.x, line_thickness) + step(1.0 - line_thickness, grid_uv.x);
	float line_y = step(grid_uv.y, line_thickness) + step(1.0 - line_thickness, grid_uv.y);
	
	float mask = clamp(line_x + line_y, 0.0, 1.0);
	ALBEDO = mix(grid_color, line_color, mask);
}