shader_type canvas_item;

uniform float scanline_count : hint_range(100.0, 1000.0) = 400.0;
uniform float scanline_opacity : hint_range(0.0, 0.5) = 0.06;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.4;
uniform vec4 scanline_color : source_color = vec4(0.0, 0.94, 1.0, 1.0);

void fragment() {
	vec2 uv = SCREEN_UV;
	
	// Scanlines
	float scanline = sin(uv.y * scanline_count * 3.14159) * 0.5 + 0.5;
	vec4 line_color = scanline_color * scanline * scanline_opacity;
	
	// Vignette
	vec2 uv_center = uv - vec2(0.5);
	float dist = length(uv_center);
	float vignette = smoothstep(0.3, 0.75, dist) * vignette_intensity;
	
	COLOR = line_color + vec4(0.0, 0.0, 0.0, vignette);
}
