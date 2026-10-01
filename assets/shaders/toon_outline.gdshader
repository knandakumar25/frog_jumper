shader_type spatial;
render_mode cull_front, unshaded;

// Inverted Hull Ink Outline Shader
uniform float outline_width : hint_range(0.0, 10.0, 0.1) = 2.5;
uniform vec4 outline_color : source_color = vec4(0.08, 0.09, 0.14, 1.0);

void vertex() {
	// In Godot 4 spatial shaders, VERTEX and NORMAL are already in View/Eye space.
	vec4 clip_pos = PROJECTION_MATRIX * vec4(VERTEX, 1.0);
	vec3 clip_normal = mat3(PROJECTION_MATRIX) * NORMAL;
	
	float norm_len = length(clip_normal.xy);
	vec2 norm = norm_len > 0.0001 ? (clip_normal.xy / norm_len) : vec2(0.0);
	float vp_y = max(VIEWPORT_SIZE.y, 1.0);
	
	// Uniform screen-pixel outline extrusion
	vec2 offset = norm * (outline_width / vp_y) * clip_pos.w * 2.0;
	clip_pos.xy += offset;
	
	// Small Z-bias push to avoid z-fighting on shallow angles
	clip_pos.z += 0.0005 * clip_pos.w;
	
	POSITION = clip_pos;
}

void fragment() {
	ALBEDO = outline_color.rgb;
}
