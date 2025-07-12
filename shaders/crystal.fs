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

// Crystal structure generation
float crystalStructure(vec2 uv, vec2 center, float size) {
    vec2 dir = uv - center;
    float angle = atan(dir.y, dir.x);
    float radius = length(dir);
    
    // Create hexagonal pattern
    float hexagon = cos(angle * 6.0) * 0.5 + 0.5;
    
    // Add faceted surface
    float facets = cos(angle * 12.0) * 0.2 + 0.8;
    
    // Distance field for crystal shape
    float crystal = 1.0 - smoothstep(size * 0.5, size, radius);
    crystal *= hexagon * facets;
    
    return crystal;
}

// Crystal refraction simulation
vec3 refraction(vec2 uv, float intensity) {
    vec2 offset = vec2(
        sin(uv.x * 10.0 + time) * intensity,
        cos(uv.y * 8.0 + time * 0.8) * intensity
    );
    
    vec2 refractedUV = uv + offset;
    
    // Create rainbow effect
    float r = sin(refractedUV.x * 20.0 + time) * 0.5 + 0.5;
    float g = sin(refractedUV.x * 20.0 + time + 2.0) * 0.5 + 0.5;
    float b = sin(refractedUV.x * 20.0 + time + 4.0) * 0.5 + 0.5;
    
    return vec3(r, g, b);
}

// Crystal growth pattern
float crystalGrowth(vec2 uv, float time) {
    float growth = 0.0;
    
    // Multiple growth centers
    for (int i = 0; i < 5; i++) {
        vec2 center = vec2(
            sin(float(i) * 2.0 + time * 0.3) * 0.3 + 0.5,
            cos(float(i) * 1.5 + time * 0.2) * 0.3 + 0.5
        );
        
        float dist = distance(uv, center);
        float size = 0.1 + sin(time * 0.5 + float(i)) * 0.05;
        
        float crystal = crystalStructure(uv, center, size);
        growth += crystal;
    }
    
    return growth;
}

// Reflection and highlights
float reflection(vec2 uv, float crystalMask) {
    // Simulate light reflection
    float refl = 0.0;
    
    // Multiple light sources
    vec2 light1 = vec2(0.3, 0.8);
    vec2 light2 = vec2(0.7, 0.2);
    
    float dist1 = distance(uv, light1);
    float dist2 = distance(uv, light2);
    
    refl += exp(-dist1 * 5.0) * crystalMask;
    refl += exp(-dist2 * 4.0) * crystalMask;
    
    return refl;
}

// Caustic patterns
float caustics(vec2 uv, float time) {
    vec2 causticsUV = uv * 15.0 + time * 0.5;
    
    float caustic = 0.0;
    caustic += sin(causticsUV.x + sin(causticsUV.y)) * 0.5 + 0.5;
    caustic += sin(causticsUV.y + cos(causticsUV.x)) * 0.3 + 0.3;
    
    caustic = pow(caustic, 3.0);
    return caustic;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Base crystal color
    vec3 color = vec3(0.1, 0.2, 0.4);
    
    // Generate crystal growth
    float crystalMask = crystalGrowth(uv, time);
    
    // Add noise to crystal surface
    float surface = fbm(uv * 20.0 + time * 0.2);
    crystalMask += surface * 0.1;
    
    // Crystal base color with depth
    vec3 crystalColor = vec3(0.6, 0.8, 1.0) * crystalMask;
    color += crystalColor;
    
    // Add refraction effects
    if (crystalMask > 0.1) {
        vec3 refracted = refraction(uv, crystalMask * 0.1);
        color += refracted * crystalMask * 0.5;
    }
    
    // Add reflections and highlights
    float refl = reflection(uv, crystalMask);
    color += refl * vec3(1.0, 1.0, 0.8);
    
    // Add caustic patterns
    float causticsPattern = caustics(uv, time);
    color += causticsPattern * crystalMask * vec3(0.3, 0.5, 0.8);
    
    // Mouse interaction - create new crystal formation
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseEffect = exp(-mouseDist * radius * 0.3) * power;
        
        // New crystal growth at mouse position
        float newCrystal = crystalStructure(uv, mouse, 0.2 * power);
        color += newCrystal * vec3(1.0, 0.8, 1.0) * mouseEffect;
        
        // Energy waves
        float wave = sin(mouseDist * 15.0 - time * 8.0) * exp(-mouseDist * 2.0);
        color += wave * mouseEffect * vec3(0.8, 0.9, 1.0);
        
        // Sparkling effect
        float sparkle = noise(uv * 50.0 + time * 5.0);
        if (sparkle > 0.95 && mouseEffect > 0.1) {
            color += vec3(1.0) * (sparkle - 0.95) * 20.0;
        }
    }
    
    // Add inner glow
    float glow = crystalMask * 0.3;
    color += glow * vec3(0.4, 0.6, 1.0);
    
    // Add edge lighting
    float edge = 1.0 - smoothstep(0.1, 0.3, crystalMask);
    edge *= crystalMask;
    color += edge * vec3(0.8, 1.0, 1.0);
    
    // Add prismatic dispersion
    float dispersion = sin(uv.x * 30.0 + time * 2.0) * crystalMask;
    color.r += dispersion * 0.1;
    color.g += sin(dispersion + 2.0) * 0.1;
    color.b += sin(dispersion + 4.0) * 0.1;
    
    // Enhance brightness and contrast
    color *= 1.2;
    color = pow(color, vec3(0.9));
    
    fragColor = vec4(color, 1.0);
}
