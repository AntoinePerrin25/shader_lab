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

// Enhanced noise functions
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
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}

// Ocean wave function
float oceanWaves(vec2 uv, float time) {
    float wave = 0.0;
    
    // Large waves
    wave += sin(uv.x * 3.0 + time * 0.8) * 0.3;
    wave += sin(uv.x * 2.0 + uv.y * 1.5 + time * 1.2) * 0.2;
    
    // Medium waves
    wave += sin(uv.x * 8.0 + time * 2.0) * 0.1;
    wave += sin(uv.y * 6.0 + time * 1.5) * 0.15;
    
    // Small ripples
    wave += fbm(uv * 20.0 + time * 0.5) * 0.05;
    
    return wave;
}

// Foam generation
float foam(vec2 uv, float waveHeight, float time) {
    float f = fbm(uv * 15.0 + time * 2.0);
    float threshold = 0.3 + waveHeight * 0.5;
    return smoothstep(threshold - 0.1, threshold + 0.1, f);
}

// Water surface normal calculation
vec3 calculateNormal(vec2 uv, float time) {
    float eps = 0.001;
    float h0 = oceanWaves(uv, time);
    float hx = oceanWaves(uv + vec2(eps, 0.0), time);
    float hy = oceanWaves(uv + vec2(0.0, eps), time);
    
    vec3 normal = normalize(vec3(-(hx - h0) / eps, -(hy - h0) / eps, 1.0));
    return normal;
}

// Sky reflection
vec3 skyColor(vec3 direction) {
    float t = 0.5 * (direction.y + 1.0);
    vec3 horizon = vec3(0.7, 0.9, 1.0);
    vec3 zenith = vec3(0.3, 0.6, 1.0);
    return mix(horizon, zenith, t);
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Create wave displacement
    float waveHeight = oceanWaves(uv, time);
    vec2 waveUV = uv + vec2(sin(time * 0.5), cos(time * 0.3)) * 0.02;
    
    // Calculate surface normal for reflections
    vec3 normal = calculateNormal(waveUV, time);
    
    // Base ocean color
    vec3 deepWater = vec3(0.0, 0.2, 0.4);
    vec3 shallowWater = vec3(0.0, 0.4, 0.6);
    vec3 oceanColor = mix(deepWater, shallowWater, (waveHeight + 1.0) * 0.5);
    
    // Reflection calculation
    vec3 viewDir = normalize(vec3(uv - 0.5, 0.5));
    vec3 reflectDir = reflect(viewDir, normal);
    vec3 reflection = skyColor(reflectDir) * 0.6;
    
    // Fresnel effect
    float fresnel = pow(1.0 - dot(-viewDir, normal), 5.0);
    fresnel = mix(0.2, 1.0, fresnel);
    
    // Mix ocean color with reflection
    vec3 color = mix(oceanColor, reflection, fresnel);
    
    // Add foam
    float foamAmount = foam(waveUV, waveHeight, time);
    vec3 foamColor = vec3(1.0, 1.0, 0.95);
    color = mix(color, foamColor, foamAmount * 0.7);
    
    // Sun reflection (bright spot)
    vec2 sunPos = vec2(0.7, 0.8);
    float sunReflection = pow(max(0.0, dot(normal.xy, normalize(sunPos - uv))), 20.0);
    color += sunReflection * vec3(1.0, 0.9, 0.7) * 2.0;
    
    // Mouse interaction - create ripples
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float ripple = sin(mouseDist * radius - time * 5.0) * exp(-mouseDist * 5.0);
        ripple *= power * 0.5;
        
        // Disturb the water surface
        color += ripple * vec3(0.2, 0.3, 0.4);
        
        // Add splash effect
        float splash = exp(-mouseDist * 10.0) * power;
        color += splash * vec3(1.0, 1.0, 0.9) * 0.5;
    }
    
    // Depth darkening
    float depth = smoothstep(0.0, 1.0, uv.y);
    color *= mix(1.0, 0.3, depth);
    
    // Add underwater caustics
    float caustics = fbm(uv * 30.0 + time * 0.8);
    caustics = pow(caustics, 3.0);
    color += caustics * vec3(0.1, 0.2, 0.3) * (1.0 - depth);
    
    // Atmospheric perspective
    float fog = smoothstep(0.5, 1.0, uv.y);
    vec3 fogColor = vec3(0.8, 0.9, 1.0);
    color = mix(color, fogColor, fog * 0.3);
    
    fragColor = vec4(color, 1.0);
}
