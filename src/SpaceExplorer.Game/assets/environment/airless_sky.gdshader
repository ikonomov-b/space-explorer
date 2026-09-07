shader_type sky;

uniform float star_density : hint_range(50.0, 1000.0) = 400.0;
uniform float star_threshold : hint_range(0.9, 0.999) = 0.9925;

float hash(vec3 p) {
	p = fract(p * 0.3183099 + vec3(0.71, 0.113, 0.419));
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

void sky() {
	vec3 cell = floor(EYEDIR * star_density);
	float h = hash(cell);
	float brightness = smoothstep(star_threshold, 1.0, h);
	COLOR = vec3(brightness);
}
