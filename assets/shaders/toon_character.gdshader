shader_type spatial;
render_mode diffuse_toon, specular_toon;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable, hint_default_white;
uniform vec4 albedo_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// Cel Shading thresholds
uniform float shadow_threshold : hint_range(-0.5, 0.8, 0.01) = 0.10;
uniform float shadow_softness : hint_range(0.001, 0.2, 0.005) = 0.04;
uniform vec4 shadow_tint : source_color = vec4(0.62, 0.64, 0.74, 1.0); // Anime cool blue-violet shade

// Stylized Specular Band
uniform float specular_threshold : hint_range(0.8, 0.999, 0.005) = 0.96;
uniform float specular_strength : hint_range(0.0, 2.0, 0.05) = 0.45;

// Fresnel / Anime Rim Light
uniform float rim_threshold : hint_range(0.0, 1.0, 0.01) = 0.58;
uniform float rim_softness : hint_range(0.01, 0.4, 0.01) = 0.14;
uniform vec4 rim_color : source_color = vec4(0.75, 0.88, 1.0, 1.0);

// Glowing Sci-Fi Emissive detection
uniform float emission_boost : hint_range(1.0, 8.0, 0.1) = 2.2;
uniform float emission_threshold : hint_range(0.4, 1.0, 0.01) = 0.75;

// Surface properties
uniform float metallic : hint_range(0.0, 1.0, 0.01) = 0.02;
uniform float roughness : hint_range(0.0, 1.0, 0.01) = 0.45;

void fragment() {
	vec4 tex_color = texture(albedo_texture, UV) * albedo_tint;
	ALBEDO = tex_color.rgb;
	
	// Stylized anime shading maintains rich diffuse colors without PBR metalness washout
	METALLIC = metallic;
	ROUGHNESS = roughness;
	
	// Auto-detect glowing neon stripes & energy cores (vibrant saturation + high brightness)
	float max_c = max(tex_color.r, max(tex_color.g, tex_color.b));
	float min_c = min(tex_color.r, min(tex_color.g, tex_color.b));
	float saturation = max_c - min_c;
	
	// Glowing neon elements: cyan stripes, plasma runes, glowing solar cores
	if (max_c > emission_threshold && (saturation > 0.35 || max_c > 0.97)) {
		EMISSION = tex_color.rgb * emission_boost;
	}
}

void light() {
	float NdotL = dot(NORMAL, LIGHT);
	float shadow_atten = ATTENUATION;
	float light_factor = NdotL * shadow_atten;
	
	// Discrete 2-tone Anime Cel Shading
	float step1 = smoothstep(shadow_threshold - shadow_softness, shadow_threshold + shadow_softness, light_factor);
	vec3 cel_light = mix(shadow_tint.rgb, vec3(1.0), step1);
	
	// Clamp light brightness to 1.15 to preserve rich anime albedo colors even under high-energy directional lights
	vec3 light_intensity = min(LIGHT_COLOR, vec3(1.15));
	
	// In Godot 4, DIFFUSE_LIGHT is multiplied by ALBEDO automatically by the engine.
	// Providing cel_light directly gives ALBEDO * cel_light * light_intensity.
	DIFFUSE_LIGHT += light_intensity * cel_light;
	
	// Stylized Sharp Anime Specular Glint (only on lit surfaces)
	vec3 half_vec = normalize(VIEW + LIGHT);
	float NdotH = dot(NORMAL, half_vec);
	float spec_glint = smoothstep(specular_threshold - 0.01, specular_threshold + 0.01, NdotH) * step1;
	vec3 spec_light = light_intensity * spec_glint * specular_strength;
	
	// Stylized Rim Light (backlight / edge accent)
	float NdotV = 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0);
	float rim = smoothstep(rim_threshold - rim_softness, rim_threshold + rim_softness, NdotV) * clamp(NdotL + 0.30, 0.0, 1.0);
	vec3 rim_light = rim_color.rgb * rim * 0.40;
	
	SPECULAR_LIGHT += spec_light + rim_light;
}
