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

// Multi-layered plasma effect
float plasma(vec2 uv, float time) {
    float x = uv.x;
    float y = uv.y;
    float v = 0.0;
    
    // Base sine waves
    v += sin((x * 8.0 + time * 1.2));
    v += sin((y * 6.0 + time * 0.8) / 2.0);
    v += sin((x * 8.0 + y * 6.0 + time * 0.5) / 2.0);
    
    // Moving center point
    float cx = x + 0.5 * sin(time / 3.0);
    float cy = y + 0.5 * cos(time / 4.0);
    v += sin(sqrt(50.0 * (cx * cx + cy * cy) + 1.0) + time * 2.0);
    
    return v / 4.0;
}

// Additional wave layers
float waves(vec2 uv, float time) {
    float w = 0.0;
    
    // Circular waves
    float dist = length(uv - 0.5);
    w += sin(dist * 20.0 - time * 3.0) * 0.5;
    
    // Diagonal waves
    w += sin((uv.x + uv.y) * 15.0 - time * 2.0) * 0.3;
    w += sin((uv.x - uv.y) * 12.0 - time * 1.5) * 0.3;
    
    return w;
}

// HSV to RGB conversion
vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Calculate plasma values
    float plasma1 = plasma(uv * 2.0, time);
    float plasma2 = plasma(uv * 3.0 + vec2(0.5), time * 0.7);
    float waveVal = waves(uv, time);
    
    // Mouse interaction
    vec2 mouse = mousePos / resolution;
    float mouseDist = distance(uv, mouse);
    float mouseInfluence = 0.0;
    
    if (length(mousePos) > 0.0) {
        mouseInfluence = exp(-mouseDist * radius * 0.3) * power;
        // Add ripples from mouse
        float ripple = sin(mouseDist * 30.0 - time * 8.0) * exp(-mouseDist * 5.0);
        plasma1 += ripple * mouseInfluence * 0.5;
    }
    
    // Combine plasma effects
    float combined = (plasma1 + plasma2 * 0.7 + waveVal * 0.4) * 0.5 + 0.5;
    combined += mouseInfluence * 0.3;
    
    // Create rainbow effect using HSV
    float hue = combined + time * 0.1;
    float saturation = 0.8 + sin(combined * 10.0) * 0.2;
    float value = 0.7 + combined * 0.3;
    
    vec3 color = hsv2rgb(vec3(hue, saturation, value));
    
    // Add some bright highlights
    float highlight = pow(max(0.0, combined - 0.7), 3.0) * 2.0;
    color += vec3(highlight);
    
    // Subtle vignette
    float vignette = 1.0 - smoothstep(0.5, 1.2, distance(uv, vec2(0.5)));
    color *= vignette;
    
    fragColor = vec4(color, 1.0);
}
