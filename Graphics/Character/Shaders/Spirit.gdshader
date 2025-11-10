shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

uniform vec4 albedo_color = vec4(0.5, 0.9, 1.0, 0.9);
uniform sampler2D albedo_texture;
uniform float emission_energy = 1.2;

void fragment() {
    vec4 tex = texture(albedo_texture, UV);
    vec3 col = mix(albedo_color.rgb, tex.rgb, tex.a);
    float alpha = max(albedo_color.a, tex.a);
    ALBEDO = col;
    ALPHA = alpha;
    EMISSION = col * emission_energy * alpha;
}