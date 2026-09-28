// ==============================================================================
// Axiom Force - Holographic Spatial Shader
// ==============================================================================
// Purpose: Renders high-fidelity holographic projections (pedestal holo-banners,
//          tactical readouts, shield nodes) in Godot 4.7.2 .NET.
// Features:
//   - Additive blending (blend_add) & double-sided rendering (cull_disabled)
//   - Dynamic sine-based scanline sweep with configurable speed & frequency
//   - Projected edge fade (smoothstep UV falloff) for ethereal light-cone look
//   - High emission multiplier for rich bloom in the cyber_glow WorldEnvironment
// ==============================================================================
shader_type spatial;
render_mode blend_add, cull_disabled, depth_draw_opaque;

uniform vec4 base_color : source_color = vec4(0.0, 0.94, 1.0, 0.85);
uniform float scanline_speed : hint_range(0.0, 10.0) = 2.0;
uniform float scanline_frequency : hint_range(1.0, 200.0) = 80.0;
uniform float emission_strength : hint_range(0.0, 10.0) = 2.5;
uniform float edge_fade_margin : hint_range(0.01, 0.5) = 0.15;

varying vec3 v_world_pos;

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec4 color = base_color;

	// Animated horizontal scanlines
	float scanline = sin(UV.y * scanline_frequency + TIME * scanline_speed);
	color.rgb *= (0.75 + 0.25 * scanline);

	// Edge fade for projected beam falloff
	float edge_fade = smoothstep(0.0, edge_fade_margin, UV.x) * smoothstep(1.0, 1.0 - edge_fade_margin, UV.x) *
	                  smoothstep(0.0, edge_fade_margin, UV.y) * smoothstep(1.0, 1.0 - edge_fade_margin, UV.y);
	color.a *= edge_fade;

	ALBEDO = color.rgb;
	EMISSION = color.rgb * emission_strength;
	ALPHA = color.a;
}