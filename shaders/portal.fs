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
    
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Portal geometry
float portalRing(vec2 uv, vec2 center, float innerRadius, float outerRadius) {
    float dist = distance(uv, center);
    return smoothstep(outerRadius, innerRadius, dist) - smoothstep(innerRadius, innerRadius - 0.02, dist);
}

// Energy flow patterns
float energyFlow(vec2 uv, vec2 center, float time) {
    vec2 dir = uv - center;
    float angle = atan(dir.y, dir.x);
    float radius = length(dir);
    
    // Spiraling energy
    float spiral = sin(angle * 8.0 + radius * 20.0 - time * 5.0) * 0.5 + 0.5;
    
    // Pulsing waves
    float pulse = sin(radius * 15.0 - time * 8.0) * 0.5 + 0.5;
    
    return spiral * pulse;
}

// Portal distortion field
vec2 portalDistortion(vec2 uv, vec2 center, float strength) {
    vec2 dir = uv - center;
    float dist = length(dir);
    float angle = atan(dir.y, dir.x);
    
    // Swirling distortion
    float swirl = sin(time * 2.0 + dist * 10.0) * strength;
    angle += swirl;
    
    // Radial compression
    float compression = 1.0 - exp(-dist * 5.0) * strength;
    dist *= compression;
    
    return center + vec2(cos(angle), sin(angle)) * dist;
}

// Interdimensional static
float dimensionalStatic(vec2 uv, float time) {
    float static_noise = fbm(uv * 50.0 + time * 10.0);
    static_noise = pow(static_noise, 3.0);
    return static_noise;
}

// Portal core energy
float portalCore(vec2 uv, vec2 center, float time) {
    float dist = distance(uv, center);
    
    // Pulsating core
    float pulse = sin(time * 4.0) * 0.3 + 0.7;
    float core = exp(-dist * 8.0) * pulse;
    
    // Energy turbulence
    float turbulence = fbm(uv * 10.0 + time * 2.0);
    core += turbulence * 0.2;
    
    return core;
}

// Reality tears
float realityTear(vec2 uv, float time) {
    vec2 tearUV = uv * 20.0 + time * 0.5;
    
    float tear = 0.0;
    for (int i = 0; i < 3; i++) {
        vec2 offset = vec2(sin(float(i) * 2.0 + time), cos(float(i) * 1.5 + time)) * 0.3;
        tear += abs(sin(tearUV.x + offset.x)) * abs(cos(tearUV.y + offset.y));
    }
    
    return pow(tear, 4.0);
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 center = vec2(0.5, 0.5);
    vec2 mouse = mousePos / resolution;
    
    // Mouse affects portal position
    if (length(mousePos) > 0.0) {
        center = mix(center, mouse, 0.3);
    }
    
    // Apply portal distortion to UV coordinates
    vec2 distortedUV = portalDistortion(uv, center, 0.1);
    
    // Dark space background
    vec3 color = vec3(0.02, 0.01, 0.05);
    
    // Portal rings
    float ring1 = portalRing(uv, center, 0.15, 0.25);
    float ring2 = portalRing(uv, center, 0.25, 0.35);
    float ring3 = portalRing(uv, center, 0.35, 0.45);
    
    // Ring colors with energy flow
    float flow1 = energyFlow(uv, center, time);
    float flow2 = energyFlow(uv, center, time * 1.3);
    float flow3 = energyFlow(uv, center, time * 0.7);
    
    color += ring1 * flow1 * vec3(0.0, 0.8, 1.0);
    color += ring2 * flow2 * vec3(0.8, 0.0, 1.0);
    color += ring3 * flow3 * vec3(1.0, 0.3, 0.0);
    
    // Portal core
    float core = portalCore(distortedUV, center, time);
    color += core * vec3(0.5, 1.0, 1.5);
    
    // Add interdimensional static around portal
    float dist = distance(uv, center);
    if (dist > 0.1 && dist < 0.5) {
        float static_intensity = (0.5 - dist) * 2.0;
        float static_noise = dimensionalStatic(uv, time);
        color += static_noise * static_intensity * vec3(0.3, 0.2, 0.8);
    }
    
    // Reality tears
    float tears = realityTear(uv, time);
    color += tears * vec3(1.0, 0.5, 0.8) * 0.2;
    
    // Mouse interaction - portal energy surge
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, center);
        float mouseEffect = exp(-mouseDist * radius * 0.2) * power;
        
        // Energy surge
        vec3 surgeColor = vec3(1.0, 0.8, 1.5);
        color += mouseEffect * surgeColor;
        
        // Lightning bolts
        float lightning = sin(mouseDist * 30.0 - time * 10.0) * exp(-mouseDist * 5.0);
        color += lightning * mouseEffect * vec3(1.0, 1.0, 0.5);
        
        // Portal size fluctuation
        float sizeFluctuation = 1.0 + mouseEffect * 0.5;
        // This affects the visual intensity rather than actual size
        color *= sizeFluctuation;
    }
    
    // Add swirling particle effects
    vec2 particleUV = uv;
    particleUV = portalDistortion(particleUV, center, sin(time) * 0.05);
    
    float particles = fbm(particleUV * 15.0 + time * 2.0);
    particles = smoothstep(0.6, 0.8, particles);
    color += particles * vec3(0.4, 0.8, 1.0) * 0.3;
    
    // Add temporal glitches
    if (sin(time * 8.0) > 0.9) {
        float glitch = noise(uv * 100.0);
        color += glitch * vec3(1.0, 0.2, 0.8) * 0.5;
    }
    
    // Edge glow effect
    float edgeGlow = 1.0 - smoothstep(0.45, 0.55, dist);
    color += edgeGlow * vec3(0.2, 0.4, 1.0) * 0.3;
    
    // Add dimensional flux
    float flux = sin(time * 3.0 + dist * 10.0) * 0.1 + 0.9;
    color *= flux;
    
    // Final intensity boost
    color *= 1.5;
    
    fragColor = vec4(color, 1.0);
}
