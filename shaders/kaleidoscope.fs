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

// Kaleidoscope effect with multiple reflections
vec2 kaleidoscope(vec2 uv, int segments) {
    float angle = atan(uv.y, uv.x);
    float r = length(uv);
    
    // Create symmetrical segments
    float segmentAngle = 2.0 * PI / float(segments);
    angle = mod(angle, segmentAngle);
    
    // Mirror every other segment for kaleidoscope effect
    float segmentIndex = floor(angle / (segmentAngle * 0.5));
    if (mod(segmentIndex, 2.0) > 0.5) {
        angle = segmentAngle * 0.5 - mod(angle, segmentAngle * 0.5);
    } else {
        angle = mod(angle, segmentAngle * 0.5);
    }
    
    return vec2(cos(angle), sin(angle)) * r;
}

// Fractal noise function
float noise(vec2 p) {
    return sin(p.x * 10.0 + time) * sin(p.y * 10.0 + time * 1.1) * 0.5 + 0.5;
}

// Fbm noise for more complex patterns
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 6; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
        p = mat2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5)) * p;
    }
    return value;
}

// Psychedelic color palette
vec3 psychedelicColor(float t, vec2 uv) {
    float r = sin(t * 2.0 + length(uv) * 5.0 + time * 0.8) * 0.5 + 0.5;
    float g = sin(t * 3.0 + uv.x * 10.0 + time * 1.2) * 0.5 + 0.5;
    float b = sin(t * 4.0 + uv.y * 8.0 + time * 0.6) * 0.5 + 0.5;
    
    // Add rainbow effect
    vec3 rainbow = vec3(
        sin(t * 6.0 + time) * 0.5 + 0.5,
        sin(t * 6.0 + time + 2.094) * 0.5 + 0.5,
        sin(t * 6.0 + time + 4.189) * 0.5 + 0.5
    );
    
    return mix(vec3(r, g, b), rainbow, 0.3);
}

void main() {
    vec2 uv = (fragTexCoord - 0.5) * 2.0;
    
    // Mouse interaction affects rotation and zoom
    vec2 mouse = (mousePos / resolution - 0.5) * 2.0;
    float mouseInfluence = 0.0;
    if (length(mousePos) > 0.0) {
        mouseInfluence = power * 0.1;
        
        // Mouse affects rotation
        float rotation = length(mouse) * mouseInfluence * 10.0 + time * 0.5;
        mat2 rot = mat2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
        uv = rot * uv;
    } else {
        // Default rotation
        float rotation = time * 0.3;
        mat2 rot = mat2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
        uv = rot * uv;
    }
    
    // Dynamic number of segments based on time and mouse
    int segments = 6 + int(sin(time * 0.5) * 2.0 + mouseInfluence * 8.0);
    
    // Apply kaleidoscope effect
    vec2 kaleido = kaleidoscope(uv, segments);
    
    // Zoom and movement
    float zoom = 1.0 + sin(time * 0.4) * 0.3 + mouseInfluence * 2.0;
    kaleido *= zoom;
    kaleido += vec2(sin(time * 0.2), cos(time * 0.3)) * 0.1;
    
    // Generate multiple pattern layers
    float pattern1 = fbm(kaleido * 3.0 + time * 0.1);
    float pattern2 = fbm(kaleido * 6.0 - time * 0.15);
    float pattern3 = noise(kaleido * 12.0 + time * 0.2);
    
    // Combine patterns
    float combined = pattern1 * 0.5 + pattern2 * 0.3 + pattern3 * 0.2;
    
    // Add radial waves
    float radial = length(kaleido);
    float waves = sin(radial * 20.0 - time * 5.0) * 0.5 + 0.5;
    combined = mix(combined, waves, 0.3);
    
    // Add spiral pattern
    float spiral = sin(atan(kaleido.y, kaleido.x) * 8.0 + radial * 10.0 - time * 3.0) * 0.5 + 0.5;
    combined = mix(combined, spiral, 0.2);
    
    // Generate psychedelic colors
    vec3 color = psychedelicColor(combined, kaleido);
    
    // Add metallic highlights
    float highlight = pow(combined, 4.0);
    color += highlight * vec3(1.0, 0.8, 0.2) * 0.5;
    
    // Mouse glow effect
    if (mouseInfluence > 0.0) {
        float mouseGlow = exp(-length(uv - mouse * 0.5) * 3.0) * mouseInfluence;
        color += mouseGlow * vec3(1.0, 1.0, 1.0) * 2.0;
    }
    
    // Vignette effect for focus
    float vignette = 1.0 - length(uv * 0.7);
    vignette = smoothstep(0.3, 0.8, vignette);
    color *= vignette;
    
    // Final color adjustments
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.2; // Brightness boost
    
    fragColor = vec4(color, 1.0);
}
