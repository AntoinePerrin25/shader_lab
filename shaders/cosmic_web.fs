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

// Advanced noise functions for cosmic structure
float hash(vec3 p) {
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3D(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    return mix(mix(mix(hash(i + vec3(0,0,0)), hash(i + vec3(1,0,0)), f.x),
                   mix(hash(i + vec3(0,1,0)), hash(i + vec3(1,1,0)), f.x), f.y),
               mix(mix(hash(i + vec3(0,0,1)), hash(i + vec3(1,0,1)), f.x),
                   mix(hash(i + vec3(0,1,1)), hash(i + vec3(1,1,1)), f.x), f.y), f.z);
}

float fbm3D(vec3 p) {
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise3D(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Dark matter filament structure
float darkMatterFilaments(vec3 pos, float time) {
    // Large scale structure simulation
    float density = 0.0;
    
    // Primary cosmic web nodes
    for (int i = 0; i < 8; i++) {
        float phase = float(i) * PI / 4.0;
        vec3 node = vec3(
            sin(phase + time * 0.1) * 2.0,
            cos(phase * 1.3 + time * 0.08) * 2.0,
            sin(phase * 0.7 + time * 0.12) * 2.0
        );
        
        float dist = distance(pos, node);
        
        // Gravitational influence creates filamentary structure
        float influence = 1.0 / (1.0 + dist * dist * 0.5);
        density += influence;
    }
    
    // Add turbulence for realistic cosmic web structure
    density += fbm3D(pos * 0.3 + time * 0.05) * 0.3;
    
    return density;
}

// Galaxy formation sites
float galaxyFormation(vec3 pos, float time) {
    float galaxies = 0.0;
    
    // Multiple galaxy formation regions
    for (int i = 0; i < 12; i++) {
        float seed = float(i) * 7.239;
        vec3 center = vec3(
            sin(seed + time * 0.05) * 3.0,
            cos(seed * 1.4 + time * 0.06) * 3.0,
            sin(seed * 0.8 + time * 0.04) * 3.0
        );
        
        float dist = distance(pos, center);
        
        // Spiral galaxy structure
        float angle = atan(pos.y - center.y, pos.x - center.x) + time * 0.1;
        float spiral = sin(angle * 3.0 + dist * 2.0 - time) * 0.5 + 0.5;
        
        // Galaxy formation probability
        float formation = exp(-dist * 0.8) * spiral;
        galaxies += formation;
    }
    
    return galaxies;
}

// Cosmic void detection
float cosmicVoids(vec3 pos, float time) {
    // Voids are regions of low dark matter density
    float voidField = darkMatterFilaments(pos, time);
    
    // Invert to create void regions
    float voids = 1.0 - smoothstep(0.2, 0.8, voidField);
    
    // Add void expansion effect
    float expansion = sin(time * 0.3) * 0.1 + 0.9;
    voids *= expansion;
    
    return voids;
}

// Gravitational lensing effect
vec2 gravitationalLensing(vec2 uv, float time) {
    vec2 lensed = uv;
    
    // Multiple massive objects causing lensing
    for (int i = 0; i < 4; i++) {
        float phase = float(i) * PI * 0.5;
        vec2 mass = vec2(
            0.5 + sin(time * 0.1 + phase) * 0.3,
            0.5 + cos(time * 0.15 + phase) * 0.3
        );
        
        vec2 dir = uv - mass;
        float dist = length(dir);
        
        // Einstein ring formation
        float lensStrength = 0.1 / (dist * dist + 0.01);
        lensed += normalize(dir) * lensStrength * sin(time * 2.0 + phase);
    }
    
    return lensed;
}

// Cosmic microwave background radiation
float cmb(vec2 uv, float time) {
    // Temperature fluctuations in early universe
    vec3 pos3D = vec3(uv * 10.0, time * 0.1);
    
    float temperature = noise3D(pos3D);
    
    // Add characteristic angular scale
    temperature += noise3D(pos3D * 0.3) * 0.5;
    temperature += noise3D(pos3D * 5.0) * 0.1;
    
    // Dipole anisotropy
    float dipole = dot(uv - 0.5, vec2(cos(time * 0.05), sin(time * 0.05))) * 0.1;
    temperature += dipole;
    
    return temperature;
}

// Redshift visualization
vec3 redshiftColors(float distance, float time) {
    // Color shift based on cosmic distance
    float redshift = distance * 0.5 + sin(time * 0.1) * 0.1;
    
    // Blue (nearby) to red (distant) galaxies
    vec3 nearColor = vec3(0.4, 0.6, 1.0);
    vec3 farColor = vec3(1.0, 0.3, 0.2);
    
    return mix(nearColor, farColor, redshift);
}

// Dark energy expansion
float darkEnergyExpansion(vec2 uv, float time) {
    // Accelerating expansion of space
    float expansion = 1.0 + sin(time * 0.2) * 0.1;
    
    // Scale factor affects large-scale structure
    vec2 expandedUV = (uv - 0.5) * expansion + 0.5;
    
    // Measure expansion effect
    float expansionField = length(expandedUV - uv) * 10.0;
    
    return expansionField;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Apply gravitational lensing
    vec2 lensedUV = gravitationalLensing(uv, time);
    
    // 3D position in cosmic web
    vec3 cosmicPos = vec3(lensedUV * 5.0, time * 0.1);
    
    // Dark cosmic background
    vec3 color = vec3(0.01, 0.02, 0.05);
    
    // Add cosmic microwave background
    float cmbRadiation = cmb(uv, time);
    color += cmbRadiation * 0.02 * vec3(1.0, 0.8, 0.6);
    
    // Dark matter filament structure
    float filaments = darkMatterFilaments(cosmicPos, time);
    
    // Visualize dark matter as purple wisps
    color += filaments * 0.3 * vec3(0.4, 0.2, 0.8);
    
    // Galaxy formation sites
    float galaxies = galaxyFormation(cosmicPos, time);
    
    // Add galaxy colors with redshift
    vec3 galaxyColors = redshiftColors(length(cosmicPos), time);
    color += galaxies * 0.5 * galaxyColors;
    
    // Cosmic voids (dark regions)
    float voids = cosmicVoids(cosmicPos, time);
    color *= (1.0 - voids * 0.7); // Darken void regions
    
    // Add bright quasars
    float quasars = noise3D(cosmicPos * 2.0 + time * 0.3);
    if (quasars > 0.95) {
        color += (quasars - 0.95) * 20.0 * vec3(1.0, 0.9, 0.8);
    }
    
    // Dark energy visualization
    float darkEnergy = darkEnergyExpansion(uv, time);
    color += darkEnergy * 0.05 * vec3(0.2, 0.8, 0.4);
    
    // Add cosmic web connections
    float webConnections = 0.0;
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * PI / 3.0 + time * 0.1;
        vec2 direction = vec2(cos(angle), sin(angle));
        
        // Create filamentary connections
        float connection = abs(dot(uv - 0.5, direction));
        connection = exp(-connection * 10.0) * filaments;
        webConnections += connection;
    }
    color += webConnections * 0.2 * vec3(0.6, 0.4, 1.0);
    
    // Mouse interaction - cosmic collision
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(lensedUV, mouse);
        float collision = exp(-mouseDist * radius * 2.0) * power;
        
        // Galaxy merger event
        float merger = sin(mouseDist * 20.0 - time * 8.0) * collision;
        color += merger * vec3(1.0, 0.6, 0.2);
        
        // Gravitational waves
        for (int i = 0; i < 3; i++) {
            float waveRadius = float(i) * 0.1 + time * 0.5;
            float wave = abs(mouseDist - waveRadius);
            wave = exp(-wave * 50.0) * collision;
            color += wave * vec3(0.2, 1.0, 0.8);
        }
        
        // Black hole formation
        if (collision > 0.5) {
            float blackHole = exp(-mouseDist * 20.0) * collision;
            color *= (1.0 - blackHole * 0.9); // Create dark region
            
            // Accretion disk
            float accretion = exp(-abs(mouseDist - 0.05) * 100.0) * collision;
            color += accretion * vec3(1.0, 0.4, 0.1);
        }
    }
    
    // Add cosmic dust and gas
    float dust = fbm3D(cosmicPos * 3.0 + time * 0.2);
    color += dust * 0.1 * vec3(0.8, 0.6, 0.4);
    
    // Enhance cosmic glow
    color = pow(color, vec3(0.8));
    color *= 1.3;
    
    // Add subtle chromatic aberration from spacetime curvature
    float curvature = filaments * 0.01;
    color.r += galaxyFormation(cosmicPos + vec3(curvature, 0, 0), time) * 0.1;
    color.b += galaxyFormation(cosmicPos - vec3(curvature, 0, 0), time) * 0.1;
    
    fragColor = vec4(color, 1.0);
}
