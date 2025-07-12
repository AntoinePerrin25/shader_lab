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

// Noise function
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Black hole event horizon
float eventHorizon(vec2 uv, vec2 center, float time) {
    float dist = distance(uv, center);
    
    // Event horizon radius with slight pulsation
    float horizonRadius = 0.1 + sin(time * 2.0) * 0.01;
    
    return smoothstep(horizonRadius - 0.02, horizonRadius, dist);
}

// Gravitational lensing effect
vec2 gravitationalLensing(vec2 uv, vec2 center, float strength) {
    vec2 dir = uv - center;
    float dist = length(dir);
    
    // Schwarzschild radius effect
    float lensing = strength / (dist + 0.01);
    
    // Bend space-time
    float angle = atan(dir.y, dir.x);
    angle += lensing * 0.5;
    
    // Compress space near black hole
    dist = dist + lensing * 0.1;
    
    return center + vec2(cos(angle), sin(angle)) * dist;
}

// Accretion disk
float accretionDisk(vec2 uv, vec2 center, float time) {
    vec2 dir = uv - center;
    float dist = length(dir);
    float angle = atan(dir.y, dir.x);
    
    // Disk shape
    float diskInner = 0.15;
    float diskOuter = 0.4;
    float diskMask = smoothstep(diskInner, diskInner + 0.02, dist) * 
                     (1.0 - smoothstep(diskOuter - 0.05, diskOuter, dist));
    
    // Spiraling material
    float spiral = sin(angle * 4.0 + dist * 20.0 - time * 3.0);
    spiral = spiral * 0.5 + 0.5;
    
    // Turbulence
    float turbulence = noise(vec2(angle * 10.0, dist * 30.0) + time * 2.0);
    
    return diskMask * spiral * turbulence;
}

// Hawking radiation
float hawkingRadiation(vec2 uv, vec2 center, float time) {
    float dist = distance(uv, center);
    
    // Radiation near event horizon
    float radiation = exp(-abs(dist - 0.11) * 50.0);
    radiation *= noise(uv * 100.0 + time * 5.0);
    
    return radiation;
}

// Light bending visualization
float photonSphere(vec2 uv, vec2 center, float time) {
    float dist = distance(uv, center);
    
    // Photon sphere at 1.5 * Schwarzschild radius
    float sphereRadius = 0.15;
    float sphere = abs(dist - sphereRadius);
    sphere = 1.0 - smoothstep(0.0, 0.01, sphere);
    
    // Orbiting light
    float angle = atan((uv - center).y, (uv - center).x);
    float orbit = sin(angle * 6.0 + time * 4.0) * 0.5 + 0.5;
    
    return sphere * orbit;
}

// Plasma jets
float plasmaJets(vec2 uv, vec2 center, float time) {
    vec2 dir = uv - center;
    float dist = length(dir);
    
    // Vertical jets
    float jetWidth = 0.05;
    float jetLength = 0.6;
    
    float jet = 0.0;
    
    // Top jet
    if (dir.y > 0.0 && abs(dir.x) < jetWidth) {
        float jetDist = dir.y / jetLength;
        jet += exp(-jetDist * 3.0) * (1.0 - smoothstep(0.0, jetWidth, abs(dir.x)));
    }
    
    // Bottom jet
    if (dir.y < 0.0 && abs(dir.x) < jetWidth) {
        float jetDist = -dir.y / jetLength;
        jet += exp(-jetDist * 3.0) * (1.0 - smoothstep(0.0, jetWidth, abs(dir.x)));
    }
    
    // Add turbulence
    jet *= noise(uv * 20.0 + time * 3.0);
    
    return jet;
}

// Space-time distortion grid
float spacetimeGrid(vec2 uv, vec2 center, float time) {
    vec2 lensedUV = gravitationalLensing(uv, center, 0.3);
    
    vec2 grid = fract(lensedUV * 20.0);
    float lines = min(smoothstep(0.0, 0.02, grid.x), smoothstep(0.0, 0.02, grid.y));
    lines += min(smoothstep(0.98, 1.0, grid.x), smoothstep(0.98, 1.0, grid.y));
    
    return lines * 0.3;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 center = vec2(0.5, 0.5);
    vec2 mouse = mousePos / resolution;
    
    // Mouse affects black hole position
    if (length(mousePos) > 0.0) {
        center = mix(center, mouse, 0.2);
    }
    
    // Apply gravitational lensing to background
    vec2 lensedUV = gravitationalLensing(uv, center, 0.2);
    
    // Deep space background
    vec3 color = vec3(0.01, 0.02, 0.05);
    
    // Add distorted star field
    float stars = noise(lensedUV * 200.0);
    if (stars > 0.95) {
        color += (stars - 0.95) * 20.0 * vec3(1.0, 0.9, 0.8);
    }
    
    // Space-time grid visualization
    float grid = spacetimeGrid(uv, center, time);
    color += grid * vec3(0.2, 0.3, 0.8);
    
    // Accretion disk
    float disk = accretionDisk(lensedUV, center, time);
    vec3 diskColor = mix(vec3(1.0, 0.3, 0.1), vec3(1.0, 1.0, 0.5), disk);
    color += disk * diskColor * 2.0;
    
    // Plasma jets
    float jets = plasmaJets(uv, center, time);
    color += jets * vec3(0.5, 0.8, 1.0) * 3.0;
    
    // Photon sphere
    float photons = photonSphere(lensedUV, center, time);
    color += photons * vec3(1.0, 1.0, 0.8) * 2.0;
    
    // Hawking radiation
    float radiation = hawkingRadiation(uv, center, time);
    color += radiation * vec3(0.8, 0.4, 1.0);
    
    // Event horizon (pure black)
    float horizon = eventHorizon(uv, center, time);
    color *= horizon;
    
    // Mouse interaction - gravitational waves
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, center);
        float mouseEffect = power * 0.5;
        
        // Gravitational wave ripples
        float wave = sin(mouseDist * 20.0 - time * 10.0) * exp(-mouseDist * 2.0);
        color += wave * mouseEffect * vec3(0.5, 0.7, 1.0);
        
        // Enhanced accretion disk activity
        float enhanced = exp(-mouseDist * 3.0) * mouseEffect;
        color += enhanced * vec3(1.0, 0.5, 0.2);
    }
    
    // Add gravitational redshift effect
    float dist = distance(uv, center);
    float redshift = 1.0 - exp(-dist * 5.0);
    color.r *= redshift;
    color.g *= mix(redshift, 1.0, 0.5);
    
    // Enhance the dramatic effect
    color *= 1.5;
    
    // Add subtle time distortion
    float timeDistortion = 1.0 + sin(time * 0.5) * 0.1;
    color *= timeDistortion;
    
    fragColor = vec4(color, 1.0);
}
