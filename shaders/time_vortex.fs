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

// Temporal noise functions
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    float f = 0.0;
    float w = 0.5;
    for (int i = 0; i < 5; i++) {
        f += w * noise(p);
        w *= 0.5;
        p *= 2.0;
    }
    return f;
}

// Time distortion field
float timeDistortion(vec2 uv, float globalTime) {
    // Create multiple time vortices
    float distortion = 0.0;
    
    // Primary time vortex
    vec2 center1 = vec2(0.3 + sin(globalTime * 0.2) * 0.1, 0.4);
    float dist1 = distance(uv, center1);
    float vortex1 = 1.0 / (1.0 + dist1 * 8.0);
    distortion += vortex1 * sin(atan(uv.y - center1.y, uv.x - center1.x) * 5.0 + globalTime * 3.0);
    
    // Secondary time vortex (counter-rotating)
    vec2 center2 = vec2(0.7, 0.6 + cos(globalTime * 0.3) * 0.1);
    float dist2 = distance(uv, center2);
    float vortex2 = 1.0 / (1.0 + dist2 * 6.0);
    distortion -= vortex2 * sin(atan(uv.y - center2.y, uv.x - center2.x) * 4.0 - globalTime * 2.5);
    
    return distortion * 0.5;
}

// Temporal wave propagation
float temporalWaves(vec2 uv, float baseTime) {
    float waves = 0.0;
    
    // Multiple time wave sources
    for (int i = 0; i < 6; i++) {
        float phase = float(i) * PI / 3.0;
        vec2 source = vec2(
            0.5 + sin(baseTime * 0.3 + phase) * 0.3,
            0.5 + cos(baseTime * 0.4 + phase * 1.2) * 0.3
        );
        
        float dist = distance(uv, source);
        float waveTime = baseTime + timeDistortion(uv, baseTime) * 2.0;
        
        // Time waves with varying speeds
        float speed = 2.0 + sin(phase) * 1.0;
        waves += sin(dist * 15.0 - waveTime * speed) * exp(-dist * 2.0);
    }
    
    return waves;
}

// Time paradox visualization
float timeParadox(vec2 uv, float time) {
    // Create self-intersecting temporal loops
    float paradox = 0.0;
    
    // Causal loop
    vec2 loopCenter = vec2(0.5, 0.5);
    float loopDist = distance(uv, loopCenter);
    float loopAngle = atan(uv.y - loopCenter.y, uv.x - loopCenter.x);
    
    // Möbius-like temporal structure
    float loopTime = time + loopAngle * 2.0;
    float loop = sin(loopDist * 20.0 + loopTime * 4.0) * exp(-pow(loopDist - 0.2, 2.0) * 10.0);
    paradox += loop;
    
    // Bootstrap paradox visualization
    float bootstrap = sin(uv.x * 10.0 + time) * sin(uv.y * 8.0 - time * 1.5);
    bootstrap *= exp(-pow(distance(uv, vec2(0.8, 0.2)) - 0.1, 2.0) * 20.0);
    paradox += bootstrap * 0.5;
    
    return paradox;
}

// Chronon particles (quantum units of time)
float chronons(vec2 uv, float time) {
    float particles = 0.0;
    
    for (int i = 0; i < 25; i++) {
        float seed = float(i) * 17.5432;
        
        // Particle position with temporal drift
        vec2 pos = vec2(
            fract(sin(seed) * 43758.5453 + time * 0.1 + timeDistortion(uv, time) * 0.05),
            fract(sin(seed * 2.0) * 43758.5453 + time * 0.08)
        );
        
        float dist = distance(uv, pos);
        
        // Chronon properties
        float phase = time * 8.0 + seed * 10.0;
        float intensity = sin(phase) * 0.5 + 0.5;
        float size = 0.003 + sin(phase * 1.3) * 0.002;
        
        // Quantum uncertainty in time
        float uncertainty = noise(pos * 100.0 + time * 5.0) * 0.5 + 0.5;
        
        particles += exp(-dist * 150.0 / size) * intensity * uncertainty;
    }
    
    return particles;
}

// Temporal echoes - showing past and future states
vec3 temporalEchoes(vec2 uv, float currentTime) {
    vec3 echoes = vec3(0.0);
    
    // Past echoes (red-shifted)
    for (int i = 1; i <= 3; i++) {
        float pastTime = currentTime - float(i) * 0.5;
        float pastIntensity = temporalWaves(uv, pastTime) * (1.0 / float(i + 1));
        echoes.r += pastIntensity * 0.3;
    }
    
    // Present (green)
    echoes.g += temporalWaves(uv, currentTime) * 0.5;
    
    // Future echoes (blue-shifted)
    for (int i = 1; i <= 3; i++) {
        float futureTime = currentTime + float(i) * 0.5;
        float futureIntensity = temporalWaves(uv, futureTime) * (1.0 / float(i + 1));
        echoes.b += futureIntensity * 0.3;
    }
    
    return echoes;
}

// Spacetime curvature visualization
float spacetimeCurvature(vec2 uv, float time) {
    // Gravitational time dilation effects
    vec2 massCenter = vec2(0.5 + sin(time * 0.1) * 0.2, 0.5);
    float dist = distance(uv, massCenter);
    
    // Einstein's field equations approximation
    float mass = 1.0;
    float curvature = mass / (1.0 + dist * dist * 4.0);
    
    // Geodesic distortion
    float angle = atan(uv.y - massCenter.y, uv.x - massCenter.x);
    float geodesic = sin(angle * 8.0 + time * 2.0 + curvature * 10.0) * curvature;
    
    return geodesic;
}

// Time crystal formation
float timeCrystals(vec2 uv, float time) {
    float crystals = 0.0;
    
    // Periodic time structure
    vec2 lattice = floor(uv * 8.0);
    vec2 cell = fract(uv * 8.0);
    
    // Crystal oscillation in time dimension
    float oscillation = sin(time * 4.0 + dot(lattice, vec2(1.0, 7.0))) * 0.5 + 0.5;
    
    // Crystal shape within cell
    float dist = distance(cell, vec2(0.5));
    float crystal = smoothstep(0.3, 0.2, dist) * oscillation;
    
    crystals += crystal;
    
    return crystals;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Calculate local time with distortions
    float localTime = time + timeDistortion(uv, time) * 3.0;
    
    // Dark spacetime background
    vec3 color = vec3(0.02, 0.03, 0.05);
    
    // Add temporal echoes
    vec3 echoes = temporalEchoes(uv, localTime);
    color += echoes * 0.4;
    
    // Time paradox effects
    float paradox = timeParadox(uv, localTime);
    color += paradox * vec3(1.0, 0.5, 0.8) * 0.3;
    
    // Chronon particles
    float particles = chronons(uv, localTime);
    color += particles * vec3(0.8, 1.0, 0.6);
    
    // Spacetime curvature
    float curvature = spacetimeCurvature(uv, time);
    color += curvature * vec3(0.4, 0.8, 1.0) * 0.5;
    
    // Time crystals
    float crystals = timeCrystals(uv, time);
    color += crystals * vec3(0.9, 0.7, 1.0) * 0.3;
    
    // Add temporal noise
    float temporalNoise = fbm(uv * 6.0 + localTime * 0.2);
    color += temporalNoise * 0.1 * vec3(0.6, 0.8, 1.0);
    
    // Mouse interaction - temporal manipulation
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float manipulation = exp(-mouseDist * radius) * power;
        
        // Time dilation field
        float dilation = manipulation * sin(localTime * 6.0);
        color += dilation * vec3(1.0, 1.0, 0.5);
        
        // Temporal ripples
        float ripple = sin(mouseDist * 25.0 - localTime * 8.0) * manipulation;
        color += ripple * vec3(0.5, 1.0, 1.0);
        
        // Causality violation
        float violation = manipulation * noise(uv * 20.0 + localTime * 3.0);
        color += violation * vec3(1.0, 0.3, 0.3);
        
        // Bootstrap paradox at mouse position
        float bootstrap = manipulation * sin(mouseDist * 40.0 + localTime * 15.0);
        color += bootstrap * vec3(1.0, 0.8, 0.0);
    }
    
    // Add time flow visualization
    float flow = sin(uv.x * 5.0 + localTime * 2.0) * sin(uv.y * 3.0 + localTime * 1.5);
    color += flow * 0.05 * vec3(0.7, 0.9, 1.0);
    
    // Enhance temporal glow
    color = pow(color, vec3(0.9));
    color *= 1.3;
    
    // Add chromatic aberration for time distortion
    float aberration = timeDistortion(uv, time) * 0.01;
    color.r += temporalWaves(uv + vec2(aberration, 0.0), localTime) * 0.1;
    color.b += temporalWaves(uv - vec2(aberration, 0.0), localTime) * 0.1;
    
    fragColor = vec4(color, 1.0);
}
