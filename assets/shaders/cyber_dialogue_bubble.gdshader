// ==============================================================================
// Axiom Force - Cyber Dialogue Bubble Holographic CanvasItem Shader
// ==============================================================================
// Purpose: Renders high-fidelity holographic speech terminal panels for AXIOM-7.
// Features:
//   - Animated horizontal scanlines with customizable frequency and velocity.
//   - Dynamic chromatic aberration / optical fringe on transmission pulse.
//   - Sinusoidal perimeter neon pulse synchronized with voice transmission.
//   - Diagonal holographic light-sweep beam across the glass visor.
//   - High-tech digital grid overlay for authentic sci-fi datapad aesthetic.
// ==============================================================================
shader_type canvas_item;

uniform vec4 base_tint : source_color = vec4(0.015, 0.025, 0.05, 0.92);
uniform vec4 border_glow_color : source_color = vec4(0.0, 0.94, 1.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.0, 1.0, 0.55, 1.0);

uniform float scanline_frequency : hint_range(50.0, 600.0) = 220.0;
uniform float scanline_speed : hint_range(0.0, 10.0) = 2.0;
uniform float scanline_opacity : hint_range(0.0, 0.3) = 0.07;

uniform float transmission_pulse : hint_range(0.0, 2.0) = 0.0;
uniform float glitch_intensity : hint_range(0.0, 1.0) = 0.0;
uniform float aberration_amount : hint_range(0.0, 0.02) = 0.003;
uniform float sweep_speed : hint_range(0.0, 5.0) = 0.6;

void fragment() {
	vec2 uv = UV;

	// Subtle holographic glitch jitter
	if (glitch_intensity > 0.01) {
		float glitch_noise = sin(uv.y * 80.0 + TIME * 45.0) * glitch_intensity * 0.012;
		if (fract(sin(dot(vec2(TIME, uv.y), vec2(12.9898, 78.233))) * 43758.5453) > 0.85) {
			uv.x += glitch_noise;
		}
	}

	// Chromatic aberration sampling of texture
	float ab_offset = aberration_amount * (1.0 + transmission_pulse * 0.5);
	vec4 tex_r = texture(TEXTURE, vec2(uv.x - ab_offset, uv.y));
	vec4 tex_g = texture(TEXTURE, uv);
	vec4 tex_b = texture(TEXTURE, vec2(uv.x + ab_offset, uv.y));

	vec4 tex_color = vec4(tex_r.r, tex_g.g, tex_b.b, tex_g.a);

	// Animated vertical scanlines
	float scanline = sin(uv.y * scanline_frequency + TIME * scanline_speed) * 0.5 + 0.5;
	vec4 scanline_effect = border_glow_color * scanline * scanline_opacity;

	// Diagonal holographic light-sweep beam
	float sweep_pos = fract(TIME * sweep_speed);
	float diagonal = uv.x * 0.7 + uv.y * 0.3;
	float sweep_dist = abs(diagonal - sweep_pos * 1.4);
	float sweep = smoothstep(0.12, 0.0, sweep_dist) * (0.15 + transmission_pulse * 0.12);

	// Dynamic edge pulse
	float pulse = (sin(TIME * 4.0) * 0.5 + 0.5) * transmission_pulse * 0.35;
	vec4 edge_boost = border_glow_color * pulse * tex_color.a;

	// Composite holographic panel
	vec4 final_color = tex_color;
	final_color.rgb += scanline_effect.rgb;
	final_color.rgb += (border_glow_color.rgb * sweep);
	final_color.rgb += edge_boost.rgb;

	// Subtle audio transmission glow on green/cyan channels
	if (transmission_pulse > 0.05) {
		final_color.rgb += accent_color.rgb * transmission_pulse * 0.08 * scanline;
	}

	COLOR = final_color * COLOR;
}
