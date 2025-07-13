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

// Quantum noise functions
float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

vec2 hash22(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

// Quantum field fluctuations
float quantumField(vec2 p, float time) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    
    // Quantum uncertainty - creates random field fluctuations
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    
    // Add temporal quantum fluctuations
    a += sin(time * 5.0 + a * 20.0) * 0.5;
    b += sin(time * 4.8 + b * 18.0) * 0.5;
    c += sin(time * 5.2 + c * 22.0) * 0.5;
    d += sin(time * 4.6 + d * 16.0) * 0.5;
    
    vec2 u = f * f * (3.0 - 2.0 * f);
    
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Particle wave function
float waveFunction(vec2 uv, vec2 center, float time, float phase) {
    float dist = distance(uv, center);
    
    // Wave equation with quantum interference
    float wave = sin(dist * 15.0 - time * 8.0 + phase) * exp(-dist * 2.0);
    
    // Add quantum tunneling effect
    float tunnel = exp(-pow(dist * 3.0, 2.0)) * sin(time * 10.0 + phase);
    
    return wave + tunnel * 0.5;
}

// Probability density visualization
float probabilityDensity(vec2 uv, float time) {
    float density = 0.0;
    
    // Multiple quantum states superposition
    for (int i = 0; i < 6; i++) {
        float phase = float(i) * PI / 3.0;
        vec2 center = vec2(
            0.5 + sin(time * 0.3 + phase) * 0.3,
            0.5 + cos(time * 0.4 + phase * 1.5) * 0.3
        );
        
        float wave = waveFunction(uv, center, time, phase);
        density += wave * wave; // |ψ|²
    }
    
    return density;
}

// Quantum entanglement visualization
vec3 entanglement(vec2 uv, float time) {
    vec2 particle1 = vec2(0.3 + sin(time) * 0.1, 0.5);
    vec2 particle2 = vec2(0.7 - sin(time) * 0.1, 0.5);
    
    float dist1 = distance(uv, particle1);
    float dist2 = distance(uv, particle2);
    
    // Entangled state visualization
    float entangled = sin(dist1 * 20.0 - time * 5.0) * sin(dist2 * 20.0 + time * 5.0);
    entangled *= exp(-dist1 * 5.0) * exp(-dist2 * 5.0);
    
    // Connection line with quantum correlation
    vec2 line = particle2 - particle1;
    vec2 proj = particle1 + dot(uv - particle1, line) / dot(line, line) * line;
    float lineDist = distance(uv, proj);
    float connection = exp(-lineDist * 50.0) * sin(time * 8.0);
    
    vec3 color = vec3(0.0);
    color += entangled * vec3(1.0, 0.3, 0.8);
    color += connection * vec3(0.5, 1.0, 0.5);
    
    return color;
}

// Heisenberg uncertainty principle visualization
float uncertainty(vec2 uv, float time) {
    // Position uncertainty
    float posUncertainty = quantumField(uv * 8.0, time * 2.0);
    
    // Momentum uncertainty (inversely related)
    float momUncertainty = quantumField(uv * 2.0, time * 0.5);
    
    // Heisenberg relation: Δx * Δp ≥ ℏ/2
    return abs(posUncertainty * momUncertainty) * 0.5;
}

// Quantum vacuum fluctuations
vec3 vacuumFluctuations(vec2 uv, float time) {
    vec3 fluctuations = vec3(0.0);
    
    // Virtual particle pairs
    for (int i = 0; i < 10; i++) {
        float seed = float(i) * 12.9898;
        vec2 pos = vec2(
            fract(sin(seed) * 43758.5453 + time * 0.5),
            fract(sin(seed * 2.0) * 43758.5453 + time * 0.3)
        );
        
        float dist = distance(uv, pos);
        float lifetime = sin(time * 10.0 + seed) * 0.5 + 0.5;
        float intensity = exp(-dist * 30.0) * lifetime;
        
        // Pair creation/annihilation
        if (lifetime > 0.7) {
            fluctuations += intensity * vec3(0.8, 0.9, 1.0);
        } else {
            fluctuations += intensity * vec3(1.0, 0.4, 0.6);
        }
    }
    
    return fluctuations;
}

// Quantum interference patterns
float interference(vec2 uv, float time) {
    float pattern = 0.0;
    
    // Double-slit interference
    vec2 slit1 = vec2(0.45, 0.3);
    vec2 slit2 = vec2(0.55, 0.3);
    
    float dist1 = distance(uv, slit1);
    float dist2 = distance(uv, slit2);
    
    float wave1 = sin(dist1 * 25.0 - time * 6.0);
    float wave2 = sin(dist2 * 25.0 - time * 6.0);
    
    // Interference pattern
    pattern = (wave1 + wave2) * exp(-uv.y * 2.0);
    
    return pattern * pattern; // Intensity
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Dark quantum vacuum background
    vec3 color = vec3(0.02, 0.03, 0.08);
    
    // Add quantum field fluctuations
    float field = quantumField(uv * 6.0, time);
    color += field * 0.1 * vec3(0.5, 0.8, 1.0);
    
    // Probability density visualization
    float probability = probabilityDensity(uv, time);
    color += probability * 0.3 * vec3(0.8, 0.4, 1.0);
    
    // Quantum entanglement
    color += entanglement(uv, time) * 0.5;
    
    // Heisenberg uncertainty
    float heisenberg = uncertainty(uv, time);
    color += heisenberg * vec3(1.0, 0.8, 0.3);
    
    // Vacuum fluctuations
    color += vacuumFluctuations(uv, time) * 0.2;
    
    // Interference patterns
    float interf = interference(uv, time);
    color += interf * 0.4 * vec3(0.6, 1.0, 0.8);
    
    // Quantum foam at small scales
    float foam = quantumField(uv * 50.0, time * 5.0);
    color += foam * 0.05 * vec3(1.0, 1.0, 0.8);
    
    // Mouse interaction - quantum measurement collapse
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float measurement = exp(-mouseDist * radius * 2.0) * power;
        
        // Wave function collapse
        float collapse = sin(mouseDist * 30.0 - time * 15.0) * measurement;
        color += collapse * vec3(1.0, 0.5, 0.0);
        
        // Observer effect
        float observer = measurement * sin(time * 8.0);
        color += observer * vec3(1.0, 1.0, 1.0);
        
        // Quantum decoherence
        float decoherence = measurement * exp(-time * 0.5);
        color *= 1.0 + decoherence;
    }
    
    // Add energy levels visualization
    float levels = step(0.1, fract(uv.y * 10.0)) * 0.1;
    color += levels * vec3(0.3, 0.6, 1.0);
    
    // Enhance quantum glow
    color = pow(color, vec3(0.8));
    color *= 1.2;
    
    fragColor = vec4(color, 1.0);
}
