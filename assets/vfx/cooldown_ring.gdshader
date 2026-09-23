shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform float progress : hint_range(0.0, 1.0) = 1.0;
uniform vec4 ring_color : source_color = vec4(0.02, 0.85, 1.0, 1.0);
uniform float energy : hint_range(0.0, 12.0) = 3.0;

void fragment() {
	vec2 p = (UV - vec2(0.5)) * 2.0;
	float radius = length(p);
	float aa = max(fwidth(radius), 0.004);
	float band = smoothstep(0.76 - aa, 0.76 + aa, radius) * (1.0 - smoothstep(0.91 - aa, 0.91 + aa, radius));
	float angle = mod(atan(p.x, -p.y) + TAU, TAU) / TAU;
	float filled = (1.0 - step(progress, angle)) * step(0.0001, progress);
	float ticks = smoothstep(0.045, 0.15, fract(angle * 32.0));
	float rail = (1.0 - smoothstep(0.007, 0.007 + aa, abs(radius - 0.95))) * 0.32;
	float charge = band * ticks * mix(0.10, 1.0, filled);
	float ready = step(0.999, progress) * (0.08 + sin(TIME * 4.0) * 0.05);
	float intensity = charge + rail + band * ready;
	ALBEDO = ring_color.rgb;
	EMISSION = ring_color.rgb * energy;
	ALPHA = intensity * ring_color.a;
}
