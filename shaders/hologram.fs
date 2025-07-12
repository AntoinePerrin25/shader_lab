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

// Holographic interference patterns
float interference(vec2 uv, vec2 source1, vec2 source2, float wavelength, float phase) {
    float dist1 = distance(uv, source1);
    float dist2 = distance(uv, source2);
    
    float wave1 = sin(dist1 * 2.0 * PI / wavelength + phase);
    float wave2 = sin(dist2 * 2.0 * PI / wavelength + phase);
    
    return wave1 + wave2;
}

// Multiple interference sources
float multipleInterference(vec2 uv, float time) {
    float result = 0.0;
    
    // Array of interference sources
    vec2 sources[6];
    sources[0] = vec2(0.2, 0.3) + vec2(sin(time * 0.3), cos(time * 0.4)) * 0.1;
    sources[1] = vec2(0.8, 0.3) + vec2(sin(time * 0.5), cos(time * 0.3)) * 0.1;
    sources[2] = vec2(0.5, 0.7) + vec2(sin(time * 0.4), cos(time * 0.5)) * 0.1;
    sources[3] = vec2(0.3, 0.8) + vec2(sin(time * 0.6), cos(time * 0.2)) * 0.1;
    sources[4] = vec2(0.7, 0.2) + vec2(sin(time * 0.2), cos(time * 0.6)) * 0.1;
    sources[5] = vec2(0.5, 0.5) + vec2(sin(time * 0.1), cos(time * 0.1)) * 0.05;
    
    // Generate interference between all source pairs
    for (int i = 0; i < 6; i++) {
        for (int j = i + 1; j < 6; j++) {
            float wavelength = 0.05 + float(i + j) * 0.01;
            float phase = time * (2.0 + float(i) * 0.3 + float(j) * 0.2);
            result += interference(uv, sources[i], sources[j], wavelength, phase) * 0.1;
        }
    }
    
    return result;
}

// Hologram scan lines
float scanLines(vec2 uv, float time) {
    float lines = sin(uv.y * 300.0 + time * 50.0) * 0.5 + 0.5;
    lines *= sin(uv.y * 150.0 - time * 30.0) * 0.5 + 0.5;
    return lines;
}

// Digital noise for holographic effect
float digitalNoise(vec2 uv, float time) {
    vec2 grid = floor(uv * 100.0);
    float noise = fract(sin(dot(grid, vec2(127.1, 311.7))) * 43758.5453);
    noise *= sin(time * 10.0 + grid.x + grid.y) * 0.5 + 0.5;
    return noise;
}

// Chromatic aberration effect
vec3 chromaticAberration(vec2 uv, float intensity, float time) {
    float aberration = intensity * 0.01;
    
    vec2 redOffset = vec2(aberration, 0.0);
    vec2 greenOffset = vec2(0.0, 0.0);
    vec2 blueOffset = vec2(-aberration, 0.0);
    
    // Sample the interference pattern with offsets
    float redChannel = multipleInterference(uv + redOffset, time);
    float greenChannel = multipleInterference(uv + greenOffset, time);
    float blueChannel = multipleInterference(uv + blueOffset, time);
    
    return vec3(redChannel, greenChannel, blueChannel);
}

// Holographic projection effect
float hologramProjection(vec2 uv, float time) {
    // Simulated 3D object being projected
    vec2 center = vec2(0.5, 0.5);
    float dist = distance(uv, center);
    
    // Rotating object
    float angle = time * 0.5;
    vec2 rotatedUV = mat2(cos(angle), -sin(angle), sin(angle), cos(angle)) * (uv - center) + center;
    
    // Simple 3D shape (torus)
    float torus = sin(dist * 30.0 + time * 3.0) * cos(rotatedUV.x * 20.0) * sin(rotatedUV.y * 20.0);
    torus *= exp(-dist * 2.0); // Fade from center
    
    return torus;
}

// Laser pointer interaction
float laserPointer(vec2 uv, vec2 laserPos, float intensity) {
    if (length(laserPos) == 0.0) return 0.0;
    
    float dist = distance(uv, laserPos);
    float laser = exp(-dist * 100.0) * intensity;
    
    // Laser beam effect
    float beam = exp(-abs(uv.y - laserPos.y) * 50.0) * 
                 smoothstep(min(uv.x, laserPos.x), max(uv.x, laserPos.x), laserPos.x);
    
    return laser + beam * 0.3;
}

// Holographic color palette
vec3 holographicColor(float intensity, float time) {
    // Shift through holographic colors
    float hue = intensity * 3.0 + time * 0.5;
    
    vec3 color;
    color.r = sin(hue) * 0.5 + 0.5;
    color.g = sin(hue + 2.094) * 0.5 + 0.5; // 2π/3
    color.b = sin(hue + 4.189) * 0.5 + 0.5; // 4π/3
    
    // Enhance cyan and magenta for holographic look
    color = mix(color, vec3(0.0, 1.0, 1.0), smoothstep(0.3, 0.7, color.g * color.b));
    color = mix(color, vec3(1.0, 0.0, 1.0), smoothstep(0.3, 0.7, color.r * color.b));
    
    return color;
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction creates laser pointer effect
    vec2 mouse = mousePos / resolution;
    float mouseIntensity = 0.0;
    if (length(mousePos) > 0.0) {
        mouseIntensity = power;
    }
    
    // Base holographic interference
    float baseInterference = multipleInterference(uv, time);
    
    // Add chromatic aberration
    vec3 chromaticEffect = chromaticAberration(uv, 2.0 + mouseIntensity * 3.0, time);
    
    // Combine interference patterns
    float totalInterference = baseInterference + 
                             (chromaticEffect.r + chromaticEffect.g + chromaticEffect.b) * 0.33;
    
    // Add holographic projection
    float projection = hologramProjection(uv, time);
    totalInterference += projection * 0.5;
    
    // Add scan lines
    float scanLineEffect = scanLines(uv, time);
    totalInterference *= scanLineEffect * 0.8 + 0.2;
    
    // Add digital noise
    float noise = digitalNoise(uv, time);
    totalInterference += noise * 0.1;
    
    // Laser pointer interaction
    float laser = laserPointer(uv, mouse, mouseIntensity);
    totalInterference += laser;
    
    // Convert to holographic colors
    vec3 color = holographicColor(totalInterference, time);
    
    // Add intensity-based brightness
    float brightness = abs(totalInterference) * 0.8 + 0.2;
    color *= brightness;
    
    // Holographic transparency effect
    float alpha = smoothstep(-0.5, 1.0, totalInterference);
    color *= alpha;
    
    // Background (dark space)
    vec3 background = vec3(0.02, 0.05, 0.1);
    color = mix(background, color, alpha);
    
    // Add holographic grid overlay
    float gridX = sin(uv.x * 50.0) * 0.5 + 0.5;
    float gridY = sin(uv.y * 50.0) * 0.5 + 0.5;
    float grid = gridX * gridY;
    grid *= sin(time * 2.0) * 0.1 + 0.9; // Subtle animation
    color *= grid * 0.1 + 0.9;
    
    // Flickering effect for hologram instability
    float flicker = sin(time * 15.0 + uv.x * 100.0) * sin(time * 12.0 + uv.y * 80.0) * 0.05 + 0.95;
    color *= flicker;
    
    // Edge glow for holographic boundaries
    vec2 center = vec2(0.5, 0.5);
    float distFromCenter = distance(uv, center);
    float edgeGlow = smoothstep(0.4, 0.7, distFromCenter) * smoothstep(0.8, 0.7, distFromCenter);
    color += edgeGlow * vec3(0.0, 1.0, 1.0) * 0.3;
    
    // Mouse creates additional interference
    if (mouseIntensity > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseInterference = sin(mouseDist * 50.0 - time * 10.0) * exp(-mouseDist * 3.0);
        color += mouseInterference * vec3(1.0, 0.5, 0.0) * mouseIntensity * 0.5;
    }
    
    // Final holographic enhancement
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.3; // Brightness boost
    
    // Ensure holographic look with high contrast
    color = smoothstep(0.0, 1.0, color);
    
    fragColor = vec4(color, 1.0);
}
