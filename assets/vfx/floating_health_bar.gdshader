shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;

uniform float progress : hint_range(0.0, 1.0) = 1.0;
uniform float trail_progress : hint_range(0.0, 1.0) = 1.0;
uniform vec4 health_color : source_color = vec4(0.02, 0.85, 1.0, 1.0);

void fragment() {
	vec2 p = abs(UV - vec2(0.5));
	float panel = step(p.x, 0.5) * step(p.y, 0.5);
	float interior = step(p.x, 0.477) * step(p.y, 0.31);
	float border = step(p.x, 0.492) * step(p.y, 0.455) - step(p.x, 0.483) * step(p.y, 0.365);
	float x = clamp((UV.x - 0.023) / 0.954, 0.0, 1.0);
	float fill = (1.0 - step(progress, x)) * interior;
	float trail = (1.0 - step(trail_progress, x)) * interior;
	float gaps = smoothstep(0.02, 0.09, fract(x * 20.0));
	vec3 lit = health_color.rgb * fill * gaps + vec3(1.0, 0.27, 0.04) * max(0.0, trail - fill) * gaps;
	ALBEDO = vec3(0.015, 0.026, 0.04) + lit + health_color.rgb * border * 0.45;
	EMISSION = lit * 2.2 + health_color.rgb * border * 0.8;
	ALPHA = panel * 0.96;
}
