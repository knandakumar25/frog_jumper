shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform vec4 pulse_color : source_color = vec4(0.02, 0.85, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 1.0;
uniform float energy : hint_range(0.0, 12.0) = 4.0;
varying vec3 local_position;

void vertex() {
	local_position = VERTEX;
}

void fragment() {
	float rim = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.8);
	float latitude = pow(max(0.0, 1.0 - abs(local_position.y) * 18.0), 2.0);
	float bands = pow(max(0.0, cos(local_position.y * 22.0)), 28.0) * 0.14;
	float shell = 0.025 + rim * 0.85 + latitude * 0.25 + bands;
	ALBEDO = pulse_color.rgb;
	EMISSION = pulse_color.rgb * energy;
	ALPHA = shell * opacity * pulse_color.a;
}
