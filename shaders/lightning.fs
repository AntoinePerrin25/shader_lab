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

// Fractal noise
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

// Lightning bolt generation
float lightning(vec2 uv, vec2 start, vec2 end, float time) {
    vec2 dir = end - start;
    float len = length(dir);
    dir = normalize(dir);
    
    // Project point onto line
    vec2 toPoint = uv - start;
    float proj = dot(toPoint, dir);
    proj = clamp(proj, 0.0, len);
    
    vec2 closestPoint = start + dir * proj;
    float dist = distance(uv, closestPoint);
    
    // Add noise to make it look like lightning
    float noiseVal = fbm(uv * 20.0 + time * 5.0) - 0.5;
    dist += noiseVal * 0.02;
    
    // Create multiple branches
    float branch1 = 1.0 / (1.0 + dist * 100.0);
    float branch2 = 1.0 / (1.0 + dist * 200.0);
    float branch3 = 1.0 / (1.0 + dist * 50.0);
    
    return pow(branch1 + branch2 * 0.5 + branch3 * 0.3, 2.0);
}

// Electric field visualization
float electricField(vec2 uv, float time) {
    float field = 0.0;
    
    // Multiple electric sources
    vec2 source1 = vec2(0.2, 0.3) + 0.1 * vec2(sin(time), cos(time * 1.3));
    vec2 source2 = vec2(0.8, 0.7) + 0.1 * vec2(sin(time * 0.7), cos(time));
    vec2 source3 = vec2(0.5, 0.1) + 0.1 * vec2(sin(time * 1.5), cos(time * 0.8));
    
    // Distance-based field strength
    field += 1.0 / (distance(uv, source1) * 10.0 + 1.0);
    field += 1.0 / (distance(uv, source2) * 10.0 + 1.0);
    field += 1.0 / (distance(uv, source3) * 8.0 + 1.0);
    
    return field;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse position
    vec2 mouse = mousePos / resolution;
    
    vec3 color = vec3(0.0);
    
    // Electric field background
    float field = electricField(uv, time);
    color += field * vec3(0.1, 0.2, 0.8) * 0.3;
    
    // Lightning bolts
    vec2 bolt1Start = vec2(0.1, 0.2);
    vec2 bolt1End = vec2(0.9, 0.8) + 0.2 * vec2(sin(time * 2.0), cos(time * 1.5));
    
    vec2 bolt2Start = vec2(0.9, 0.1);
    vec2 bolt2End = vec2(0.1, 0.9) + 0.1 * vec2(sin(time * 1.7), cos(time * 2.3));
    
    float lightning1 = lightning(uv, bolt1Start, bolt1End, time);
    float lightning2 = lightning(uv, bolt2Start, bolt2End, time + 1.0);
    
    // Add lightning colors
    color += lightning1 * vec3(0.8, 0.9, 1.0) * 2.0;
    color += lightning2 * vec3(1.0, 0.8, 0.9) * 1.5;
    
    // Mouse interaction - create lightning from mouse
    if (length(mousePos) > 0.0) {
        // Lightning from mouse to multiple points
        vec2 target1 = vec2(0.2, 0.8);
        vec2 target2 = vec2(0.8, 0.2);
        
        float mouseLightning1 = lightning(uv, mouse, target1, time * 3.0);
        float mouseLightning2 = lightning(uv, mouse, target2, time * 2.5);
        
        color += mouseLightning1 * vec3(1.0, 1.0, 0.5) * power * 2.0;
        color += mouseLightning2 * vec3(0.5, 1.0, 1.0) * power * 1.5;
        
        // Glow around mouse
        float mouseDist = distance(uv, mouse);
        float mouseGlow = exp(-mouseDist * radius * 2.0) * power;
        color += mouseGlow * vec3(1.0, 0.8, 0.2);
    }
    
    // Add atmospheric sparks
    float sparkNoise = fbm(uv * 50.0 + time * 10.0);
    if (sparkNoise > 0.85) {
        color += vec3(1.0) * (sparkNoise - 0.85) * 10.0;
    }
    
    // Electric hum visualization
    float hum = sin(time * 60.0) * 0.02 + 1.0;
    color *= hum;
    
    // Add some blue tint to the background
    color += vec3(0.02, 0.05, 0.15);
    
    // Contrast enhancement
    color = pow(color, vec3(0.7));
    
    fragColor = vec4(color, 1.0);
}
