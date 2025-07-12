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

// Noise functions for aurora generation
float hash(float n) {
    return fract(sin(n) * 43758.5453);
}

float noise(vec2 p) {
    vec2 ip = floor(p);
    vec2 u = fract(p);
    u = u * u * (3.0 - 2.0 * u);

    float res = mix(
        mix(hash(ip.x + ip.y * 57.0), hash(ip.x + 1.0 + ip.y * 57.0), u.x),
        mix(hash(ip.x + (ip.y + 1.0) * 57.0), hash(ip.x + 1.0 + (ip.y + 1.0) * 57.0), u.x), u.y);
    return res * res;
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

// Aurora wave patterns
float auroraWaves(vec2 uv, float time) {
    // Create flowing wave patterns
    float wave1 = sin(uv.x * 3.0 + time * 0.8) * 0.1;
    float wave2 = sin(uv.x * 5.0 + time * 1.2) * 0.05;
    float wave3 = sin(uv.x * 7.0 + time * 0.6) * 0.03;
    
    float y = uv.y + wave1 + wave2 + wave3;
    
    // Multiple aurora bands
    float aurora = 0.0;
    
    // Main aurora band
    aurora += exp(-abs(y - 0.6) * 8.0) * (sin(time * 2.0 + uv.x * 10.0) * 0.5 + 0.5);
    
    // Secondary bands
    aurora += exp(-abs(y - 0.4) * 12.0) * (sin(time * 1.5 + uv.x * 8.0) * 0.3 + 0.3);
    aurora += exp(-abs(y - 0.8) * 10.0) * (sin(time * 2.5 + uv.x * 6.0) * 0.2 + 0.2);
    
    return aurora;
}

// Particle system for aurora sparkles
float particles(vec2 uv, float time) {
    float sparkles = 0.0;
    
    for (int i = 0; i < 20; i++) {
        float seed = float(i) * 12.9898;
        
        // Particle position
        vec2 pos = vec2(
            fract(sin(seed) * 43758.5453 + time * 0.1),
            fract(sin(seed * 2.0) * 43758.5453 + time * 0.05) * 0.6 + 0.2
        );
        
        float dist = distance(uv, pos);
        float size = 0.002 + sin(time * 3.0 + seed) * 0.001;
        
        // Twinkling effect
        float twinkle = sin(time * 5.0 + seed * 10.0) * 0.5 + 0.5;
        sparkles += exp(-dist * 200.0 / size) * twinkle;
    }
    
    return sparkles;
}

// Aurora color mapping
vec3 auroraColors(float intensity, vec2 uv, float time) {
    // Create color variations based on position and time
    float colorShift = sin(uv.x * 5.0 + time) * 0.5 + 0.5;
    
    vec3 green = vec3(0.2, 1.0, 0.4);
    vec3 blue = vec3(0.3, 0.6, 1.0);
    vec3 purple = vec3(0.8, 0.4, 1.0);
    vec3 pink = vec3(1.0, 0.5, 0.8);
    
    vec3 color = mix(green, blue, colorShift);
    color = mix(color, purple, sin(time * 0.5 + uv.y * 3.0) * 0.5 + 0.5);
    color = mix(color, pink, sin(time * 0.3 + uv.x * 2.0) * 0.3 + 0.3);
    
    return color * intensity;
}

// Atmospheric glow
float atmosphere(vec2 uv) {
    float dist = distance(uv, vec2(0.5, 0.5));
    return exp(-dist * 2.0) * 0.3;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Dark space background
    vec3 color = vec3(0.05, 0.05, 0.15);
    
    // Add stars
    float stars = noise(uv * 100.0);
    if (stars > 0.98) {
        color += vec3(1.0) * (stars - 0.98) * 50.0;
    }
    
    // Generate aurora intensity
    float auroraIntensity = auroraWaves(uv, time);
    
    // Add noise for organic movement
    auroraIntensity += fbm(uv * 4.0 + time * 0.1) * 0.3;
    
    // Aurora colors
    vec3 auroraColor = auroraColors(auroraIntensity, uv, time);
    color += auroraColor;
    
    // Add particles/sparkles
    float sparkles = particles(uv, time);
    color += sparkles * vec3(0.8, 1.0, 1.0);
    
    // Atmospheric glow
    color += atmosphere(uv) * vec3(0.2, 0.3, 0.6);
    
    // Mouse interaction - create aurora disturbance
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseEffect = exp(-mouseDist * radius) * power;
        
        // Energy ripples
        float ripple = sin(mouseDist * 20.0 - time * 8.0) * exp(-mouseDist * 3.0);
        color += ripple * mouseEffect * vec3(1.0, 0.8, 1.0);
        
        // Magnetic field distortion
        vec2 distortion = normalize(uv - mouse) * mouseEffect * 0.1;
        vec2 distortedUV = uv + distortion;
        float distortedAurora = auroraWaves(distortedUV, time);
        color += distortedAurora * vec3(1.0, 0.6, 1.0) * mouseEffect;
    }
    
    // Add subtle vertical gradient
    color *= 1.0 - uv.y * 0.3;
    
    // Color enhancement
    color = pow(color, vec3(0.8));
    
    fragColor = vec4(color, 1.0);
}
