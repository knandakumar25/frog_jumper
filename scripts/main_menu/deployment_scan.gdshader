shader_type canvas_item;
// Timeline-driven scan texture; a uniform avoids wall-clock drift during seeks.
uniform float seconds = 0.0;
uniform float strength : hint_range(0.0, 1.0) = 0.0;
varying vec4 vertex_tint;

// Preserve inherited modulation so the communication portrait fades with its rig.
void vertex() {
    vertex_tint = COLOR;
}

// Restrained scan lines retain the original artwork's color and transparency.
void fragment() {
    vec4 source = texture(TEXTURE, UV);
    float line = sin(UV.y * 920.0 + seconds * 8.0) * 0.5 + 0.5;
    COLOR = source * vertex_tint * vec4(vec3(1.0 - line * strength * 0.14), 1.0);
}
