shader_type spatial;
render_mode diffuse_toon, specular_disabled;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable, hint_default_white;
uniform vec4 albedo_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// Cel Shading thresholds
uniform float shadow_threshold : hint_range(-0.5, 0.8, 0.01) = 0.08;
uniform float shadow_softness : hint_range(0.001, 0.25, 0.005) = 0.05;
uniform vec4 shadow_tint : source_color = vec4(0.55, 0.58, 0.70, 1.0); // Anime cool shade

// Stylized Specular Glint (soft & clamped to prevent harsh white glare)
uniform float specular_threshold : hint_range(0.70, 0.999, 0.005) = 0.92;
uniform float specular_softness : hint_range(0.01, 0.20, 0.005) = 0.06;
uniform float specular_strength : hint_range(0.0, 1.0, 0.05) = 0.18;

// Fresnel / Anime Rim Light (silhouette accentuation)
uniform float rim_power : hint_range(1.0, 6.0, 0.1) = 3.2;
uniform float rim_strength : hint_range(0.0, 1.0, 0.02) = 0.35;
uniform vec4 rim_color : source_color = vec4(0.0, 0.90, 1.0, 1.0); // Operative accent color

// Glowing Sci-Fi Emissive detection
uniform float emission_boost : hint_range(0.0, 8.0, 0.1) = 2.4;
uniform float emission_threshold : hint_range(0.4, 1.0, 0.01) = 0.75;
uniform float emission_saturation : hint_range(0.05, 1.0, 0.01) = 0.30;

void fragment() {
	vec4 tex_color = texture(albedo_texture, UV) * albedo_tint;
	ALBEDO = tex_color.rgb;
	
	// Zero out PBR metallic & maximize roughness to eliminate harsh polygonal chrome reflections
	METALLIC = 0.0;
	ROUGHNESS = 1.0;
	
	// Auto-detect glowing neon stripes & energy cores (vibrant saturation + high brightness)
	float max_c = max(tex_color.r, max(tex_color.g, tex_color.b));
	float min_c = min(tex_color.r, min(tex_color.g, tex_color.b));
	float saturation = max_c - min_c;
	
	// Glowing neon elements: cyan stripes, plasma runes, glowing solar cores, emerald daggers
	// Only truly saturated, bright neon elements emit light — white armor/surfaces never blow out
	if (emission_boost > 0.0 && max_c > emission_threshold && saturation > emission_saturation) {
		EMISSION = tex_color.rgb * emission_boost;
	}
}

void light() {
	float NdotL = dot(NORMAL, LIGHT);
	float shadow_atten = ATTENUATION;
	float light_factor = NdotL * shadow_atten;
	
	// Clean 2-tone Anime Cel Shading
	float step1 = smoothstep(shadow_threshold - shadow_softness, shadow_threshold + shadow_softness, light_factor);
	vec3 cel_light = mix(shadow_tint.rgb, vec3(1.0), step1);
	
	// Clamp light brightness to 1.15 to preserve rich anime albedo colors even under high-energy directional lights
	vec3 light_intensity = min(LIGHT_COLOR, vec3(1.15));
	
	// Base cel-shaded diffuse lighting (ALBEDO is automatically multiplied by DIFFUSE_LIGHT in Godot 4)
	DIFFUSE_LIGHT += light_intensity * cel_light;
	
	// Stylized Soft Anime Glint (tinted and clamped, never blown out to white mirror blobs, NaN-safe)
	vec3 vl = VIEW + LIGHT;
	float len_vl = length(vl);
	vec3 half_vec = len_vl > 0.0001 ? vl / len_vl : NORMAL;
	float NdotH = clamp(dot(NORMAL, half_vec), 0.0, 1.0);
	float spec_glint = smoothstep(specular_threshold - specular_softness, specular_threshold + specular_softness, NdotH) * step1;
	DIFFUSE_LIGHT += light_intensity * spec_glint * specular_strength;
	
	// Smooth Anime Fresnel Rim Light (gently kisses the silhouette, tinted with operative signature color)
	float NdotV = 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0);
	float rim = pow(clamp(NdotV, 0.0, 1.0), rim_power) * clamp(NdotL * 0.5 + 0.5, 0.0, 1.0);
	DIFFUSE_LIGHT += rim_color.rgb * rim * rim_strength;
}
