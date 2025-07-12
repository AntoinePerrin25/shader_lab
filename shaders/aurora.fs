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
    
    for (int i = 0; i < 4; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Aurora wave function
float auroraWave(vec2 uv, float time, float frequency, float amplitude) {
    float wave = sin(uv.x * frequency + time * 2.0) * amplitude;
    wave += sin(uv.x * frequency * 1.5 + time * 1.5) * amplitude * 0.5;
    wave += fbm(uv * 5.0 + time * 0.3) * 0.1;
    return wave;
}

// Aurora curtain generation
float auroraCurtain(vec2 uv, float time, float height, float thickness) {
    float wave = auroraWave(uv, time, 3.0, 0.3);
    float curtain = abs(uv.y - height - wave);
    
    // Create flowing effect
    float flow = sin(uv.x * 2.0 + time * 1.5) * 0.1;
    curtain += flow;
    
    return 1.0 - smoothstep(0.0, thickness, curtain);
}

// Aurora color palette
vec3 auroraColor(float intensity, float variation) {
    vec3 green = vec3(0.0, 1.0, 0.3);
    vec3 blue = vec3(0.0, 0.5, 1.0);
    vec3 purple = vec3(0.8, 0.0, 1.0);
    vec3 pink = vec3(1.0, 0.2, 0.8);
    
    // Mix colors based on variation
    vec3 color = mix(green, blue, sin(variation * PI) * 0.5 + 0.5);
    color = mix(color, purple, sin(variation * PI * 2.0) * 0.3 + 0.3);
    color = mix(color, pink, sin(variation * PI * 3.0) * 0.2 + 0.2);
    
    return color * intensity;
}

// Star field
float stars(vec2 uv) {
    vec2 grid = floor(uv * 200.0);
    float star = noise(grid);
    
    if (star > 0.95) {
        vec2 fract_uv = fract(uv * 200.0);
        float dist = length(fract_uv - 0.5);
        return (star - 0.95) * 20.0 * exp(-dist * 30.0);
    }
    return 0.0;
}

// Atmospheric glow
float atmosphere(vec2 uv) {
    float horizon = smoothstep(0.0, 0.3, uv.y);
    float zenith = 1.0 - smoothstep(0.7, 1.0, uv.y);
    return horizon * zenith;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Dark sky background
    vec3 color = vec3(0.02, 0.05, 0.15);
    
    // Add stars
    color += stars(uv) * vec3(1.0, 0.9, 0.8);
    
    // Create multiple aurora curtains
    float aurora1 = auroraCurtain(uv, time, 0.6, 0.15);
    float aurora2 = auroraCurtain(uv + vec2(0.3, 0.0), time * 0.8, 0.7, 0.12);
    float aurora3 = auroraCurtain(uv + vec2(0.6, 0.0), time * 1.2, 0.5, 0.1);
    
    // Color variations for different curtains
    vec3 color1 = auroraColor(aurora1, uv.x + time * 0.5);
    vec3 color2 = auroraColor(aurora2, uv.x + time * 0.3 + 1.0);
    vec3 color3 = auroraColor(aurora3, uv.x + time * 0.7 + 2.0);
    
    // Combine aurora colors
    color += color1 * 0.8;
    color += color2 * 0.6;
    color += color3 * 0.4;
    
    // Add flowing particles
    float particles = fbm(uv * 10.0 + vec2(time * 0.5, time * 0.8));
    particles = smoothstep(0.6, 0.8, particles);
    color += particles * vec3(0.3, 0.8, 0.5) * 0.3;
    
    // Mouse interaction - aurora brightening
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseEffect = exp(-mouseDist * radius * 0.2) * power;
        
        // Brighten aurora at mouse position
        vec3 brightAurora = auroraColor(mouseEffect, mouse.x * 5.0 + time);
        color += brightAurora;
        
        // Add ripple effect
        float ripple = sin(mouseDist * 20.0 - time * 5.0) * exp(-mouseDist * 3.0);
        color += ripple * mouseEffect * vec3(0.5, 1.0, 0.8);
    }
    
    // Add atmospheric glow at horizon
    float atmo = atmosphere(uv);
    color += atmo * vec3(0.1, 0.2, 0.4) * 0.3;
    
    // Subtle shimmer effect
    float shimmer = sin(time * 3.0 + uv.x * 10.0) * 0.1 + 0.9;
    color *= shimmer;
    
    // Add gentle movement to entire aurora
    float globalWave = sin(time * 0.5) * 0.1 + 1.0;
    color *= globalWave;
    
    // Enhance contrast
    color = pow(color, vec3(0.9));
    
    // Add subtle color temperature variation
    float temp = sin(time * 0.2) * 0.1;
    color += temp * vec3(0.1, -0.05, -0.1);
    
    fragColor = vec4(color, 1.0);
}
