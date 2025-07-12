#version 460

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 mousePos;
uniform float radius;
uniform float power;
uniform vec2 resolution;
uniform float time;
#define PI 3.1415926535897932384626433832795

// Noise functions
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    
    for (int i = 0; i < 5; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Star field generation
float stars(vec2 uv, float size) {
    vec2 grid = floor(uv * size);
    vec2 fract_uv = fract(uv * size);
    
    float star = noise(grid);
    if (star > 0.95) {
        float brightness = (star - 0.95) * 20.0;
        float dist = length(fract_uv - 0.5);
        return brightness * exp(-dist * 20.0);
    }
    return 0.0;
}

// Nebula generation
vec3 nebula(vec2 uv, float time) {
    float n1 = fbm(uv * 3.0 + time * 0.1);
    float n2 = fbm(uv * 5.0 + time * 0.05);
    float n3 = fbm(uv * 8.0 + time * 0.08);
    
    vec3 color1 = vec3(0.8, 0.2, 0.9); // Purple
    vec3 color2 = vec3(0.2, 0.6, 1.0); // Blue
    vec3 color3 = vec3(1.0, 0.3, 0.1); // Orange
    
    vec3 nebula_color = mix(color1, color2, n1);
    nebula_color = mix(nebula_color, color3, n2 * 0.5);
    
    float density = (n1 + n2 + n3) / 3.0;
    return nebula_color * density * 0.6;
}

// Galaxy spiral arms
float spiral(vec2 uv, vec2 center, float time) {
    vec2 dir = uv - center;
    float angle = atan(dir.y, dir.x);
    float radius = length(dir);
    
    // Spiral equation
    float spiral_angle = angle + log(radius * 10.0 + 0.1) * 2.0 + time * 0.2;
    float spiral_val = sin(spiral_angle * 3.0) * 0.5 + 0.5;
    
    // Fade with distance
    float fade = exp(-radius * 3.0);
    
    return spiral_val * fade;
}

// Galaxy core
float galactic_core(vec2 uv, vec2 center) {
    float dist = distance(uv, center);
    return exp(-dist * 8.0) * 2.0;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 center = vec2(0.5, 0.5);
    
    // Mouse interaction
    vec2 mouse = mousePos / resolution;
    if (length(mousePos) > 0.0) {
        center = mix(center, mouse, 0.3);
    }
    
    vec3 color = vec3(0.0);
    
    // Deep space background
    color += vec3(0.01, 0.02, 0.05);
    
    // Multiple star layers
    color += stars(uv, 100.0) * vec3(1.0, 0.9, 0.8);
    color += stars(uv + vec2(0.3), 150.0) * vec3(0.8, 0.9, 1.0);
    color += stars(uv + vec2(0.7), 200.0) * vec3(1.0, 0.8, 0.9);
    
    // Nebula clouds
    vec3 nebula_color = nebula(uv, time);
    color += nebula_color;
    
    // Galaxy structure
    float spiral_arms = spiral(uv, center, time);
    color += spiral_arms * vec3(0.8, 0.6, 1.0) * 0.5;
    
    // Galactic core
    float core = galactic_core(uv, center);
    color += core * vec3(1.0, 0.8, 0.3);
    
    // Mouse interaction - supernova effect
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float explosion = exp(-mouseDist * radius * 0.5) * power;
        
        // Bright explosion colors
        vec3 explosion_color = vec3(1.0, 0.8, 0.3) + vec3(0.5, 0.2, 0.8) * sin(time * 10.0);
        color += explosion * explosion_color;
        
        // Shockwave rings
        float shockwave = sin(mouseDist * 30.0 - time * 5.0) * exp(-mouseDist * 3.0);
        color += shockwave * explosion * vec3(0.3, 0.6, 1.0);
    }
    
    // Add some cosmic dust
    float dust = fbm(uv * 20.0 + time * 0.02);
    color += dust * vec3(0.1, 0.05, 0.03) * 0.3;
    
    // Subtle color variation over time
    color *= 1.0 + sin(time * 0.5) * 0.1;
    
    // Add atmospheric glow
    float glow = 1.0 - distance(uv, center);
    glow = pow(glow, 3.0) * 0.2;
    color += glow * vec3(0.2, 0.1, 0.4);
    
    // Contrast and brightness adjustment
    color = pow(color, vec3(0.8));
    
    fragColor = vec4(color, 1.0);
}
