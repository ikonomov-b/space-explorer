shader_type spatial;

uniform float bump_height : hint_range(0.0, 1.0) = 0.18;
uniform float bump_scale : hint_range(1.0, 20.0) = 6.0;
uniform vec3 albedo : source_color = vec3(0.5, 0.48, 0.45);
uniform float roughness_value : hint_range(0.0, 1.0) = 0.95;

varying float height;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453123);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void vertex() {
	vec2 p = VERTEX.xz * bump_scale;
	float eps = 0.35;
	float h = noise(p);
	float hx = noise(p + vec2(eps, 0.0));
	float hz = noise(p + vec2(0.0, eps));
	VERTEX.y += h * bump_height;
	vec3 tangent_x = vec3(eps, (hx - h) * bump_height, 0.0);
	vec3 tangent_z = vec3(0.0, (hz - h) * bump_height, eps);
	NORMAL = normalize(cross(tangent_z, tangent_x));
	height = h;
}

void fragment() {
	ALBEDO = albedo * (0.85 + 0.3 * height);
	ROUGHNESS = roughness_value;
	METALLIC = 0.0;
}
