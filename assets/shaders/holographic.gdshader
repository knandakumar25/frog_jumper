shader_type spatial;

uniform vec4 base_color : source_color = vec4(0.0, 0.9, 1.0, 1.0);
uniform float scanline_speed = 2.0;
uniform float scanline_frequency = 100.0;
uniform float glitch_intensity = 0.02;
uniform float glitch_speed = 10.0;

varying vec3 v_world_pos;

void vertex() {
    v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

float random(vec2 seed) {
    return fract(sin(dot(seed, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    // Basic holographic base
    vec4 color = base_color;

    // Scanlines
    float scanline = sin(UV.y * scanline_frequency + TIME * scanline_speed);
    color.rgb *= (0.8 + 0.2 * scanline);

    // Glitch effect (disabled due to shader compatibility)
    // float glitch = step(0.98, random(vec2(TIME * 0.1, 0.0)));
    // if (glitch > 0.5) {
    //     float offset = (random(vec2(TIME, 0.0)) - 0.5) * glitch_intensity;
    //     color.rgb = texture(SCREEN_TEXTURE, UV + vec2(offset, 0.0)).rgb * base_color.rgb;
    // }

    // Fade edges for a more "projected" look
    float edge_fade = smoothstep(0.0, 0.2, UV.x) * smoothstep(1.0, 0.8, UV.x) *
                      smoothstep(0.0, 0.2, UV.y) * smoothstep(1.0, 0.8, UV.y);
    color.a *= edge_fade;

    ALBEDO = color.rgb;
    EMISSION = color.rgb * 2.0; // High emission for glow
    ALPHA = color.a;
}