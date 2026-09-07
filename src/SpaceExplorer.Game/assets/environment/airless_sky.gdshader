shader_type sky;

uniform float star_density : hint_range(50.0, 1000.0) = 400.0;
uniform float star_threshold : hint_range(0.9, 0.999) = 0.9925;
uniform float sun_angular_radius : hint_range(0.001, 0.1) = 0.03;

float hash(vec3 p) {
	p = fract(p * 0.3183099 + vec3(0.71, 0.113, 0.419));
	p *= 17.0;
	return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float disc(vec3 dir, vec3 center, float angular_radius) {
	return step(cos(angular_radius), dot(dir, center));
}

void sky() {
	vec3 cell = floor(EYEDIR * star_density);
	float h = hash(cell);
	float brightness = smoothstep(star_threshold, 1.0, h);
	vec3 color = vec3(brightness);

	// Hard-edged disc, no scattering/halo: correct for an airless sky.
	if (LIGHT0_ENABLED) {
		float sun = disc(EYEDIR, LIGHT0_DIRECTION, sun_angular_radius);
		color = mix(color, LIGHT0_COLOR, sun);
	}

	// The system's other two worlds, seen from the rocky one: icy and the life-bearing one
	// required by the Worlds row (development-plan.md). Fixed, decorative, not simulated orbits.
	vec3 nearest_icy_planet = normalize(vec3(0.55, 0.3, -0.75));
	vec3 nearest_living_planet = normalize(vec3(-0.6, 0.45, 0.6));
	color = mix(color, vec3(0.6, 0.72, 0.85), disc(EYEDIR, nearest_icy_planet, 0.02));
	color = mix(color, vec3(0.3, 0.55, 0.3), disc(EYEDIR, nearest_living_planet, 0.016));

	COLOR = color;
}
