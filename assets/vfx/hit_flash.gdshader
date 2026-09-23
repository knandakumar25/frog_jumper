shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, shadows_disabled;

uniform float strength : hint_range(0.0, 1.0) = 1.0;
uniform float energy : hint_range(0.0, 16.0) = 8.0;

void fragment() {
	ALBEDO = vec3(1.0);
	EMISSION = vec3(energy);
	ALPHA = strength;
}
