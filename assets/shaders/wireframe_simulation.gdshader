// ==============================================================================
// Axiom Force - Wireframe Simulation Shader (Protocol Zero)
// ==============================================================================
// Purpose: Renders the training-level backdrop -- the "wireframe grid simulation
//          of Los Angeles skyline" from english.json levels["0"] -- as a
//          holographic wireframe in Godot 4.7.2 .NET.
// Intended use: apply to cyber_city_skyline.glb (or any static mesh) inside the
//          training level via a ShaderMaterial; pair with the cyber_glow
//          WorldEnvironment so emission feeds bloom.
// Features:
//   - mesh wireframe rendering (render_mode wireframe) with additive blending
//   - world-space Y sweep: a bright scanning band that travels up the mesh
//   - subtle UV scanline shimmer for projected-data read
//   - double-faced (cull_disabled) so thin geometry still reads
// ==============================================================================
shader_type spatial;
render_mode wireframe, blend_add, cull_disabled, depth_draw_opaque;

uniform vec4 base_color : source_color = vec4(0.0, 0.94, 1.0, 0.85);
uniform float sweep_speed : hint_range(0.0, 10.0) = 1.2;
uniform float sweep_height : hint_range(1.0, 200.0) = 40.0;
uniform float sweep_bandwidth : hint_range(0.5, 20.0) = 6.0;
uniform float sweep_boost : hint_range(0.0, 10.0) = 3.0;
uniform float scanline_speed : hint_range(0.0, 10.0) = 2.0;
uniform float scanline_frequency : hint_range(1.0, 200.0) = 80.0;
uniform float emission_strength : hint_range(0.0, 10.0) = 2.5;

varying vec3 v_world_pos;

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec4 color = base_color;

	// UV shimmer -- fine scanline flicker
	float scanline = sin(UV.y * scanline_frequency + TIME * scanline_speed);
	color.rgb *= (0.75 + 0.25 * scanline);

	// World-height sweep band -- a bright band climbing the simulation
	float phase = mod(TIME * sweep_speed, sweep_height);
	float band = 1.0 - smoothstep(0.0, sweep_bandwidth, abs(v_world_pos.y - phase));
	color.rgb *= (1.0 + band * sweep_boost);

	ALBEDO = color.rgb;
	EMISSION = color.rgb * emission_strength;
	ALPHA = color.a;
}
