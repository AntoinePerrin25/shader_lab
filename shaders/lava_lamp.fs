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

// Hash function for randomness
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Smooth noise
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

// Metaball calculation
float metaball(vec2 uv, vec2 center, float radius) {
    float dist = distance(uv, center);
    return radius / (dist * dist + 0.001);
}

// Animated blob
float blob(vec2 uv, vec2 basePos, float time, float id) {
    vec2 movement = vec2(
        sin(time * 0.5 + id * 2.0) * 0.15,
        sin(time * 0.3 + id * 3.0) * 0.2 + cos(time * 0.4 + id) * 0.1
    );
    
    vec2 pos = basePos + movement;
    float size = 0.05 + sin(time * 0.7 + id * 4.0) * 0.02;
    
    return metaball(uv, pos, size);
}

// Heat distortion effect
vec2 heatDistortion(vec2 uv, float intensity) {
    float distortX = sin(uv.y * 20.0 + time * 2.0) * intensity;
    float distortY = sin(uv.x * 15.0 + time * 1.5) * intensity;
    return uv + vec2(distortX, distortY) * 0.01;
}

// Lava lamp gradient colors
vec3 lavaColors(float t) {
    // Hot to cool color transition
    vec3 hot = vec3(1.0, 0.3, 0.0);      // Bright orange
    vec3 medium = vec3(1.0, 0.6, 0.0);    // Orange-yellow
    vec3 cool = vec3(0.8, 0.1, 0.1);      // Dark red
    vec3 background = vec3(0.1, 0.05, 0.2); // Dark purple
    
    if (t > 0.8) {
        return mix(medium, hot, (t - 0.8) * 5.0);
    } else if (t > 0.4) {
        return mix(cool, medium, (t - 0.4) * 2.5);
    } else if (t > 0.1) {
        return mix(background, cool, (t - 0.1) * 3.33);
    } else {
        return background;
    }
}

void main() {
    vec2 uv = fragTexCoord;
    
    // Mouse interaction - affects blob movement and heat
    vec2 mouse = mousePos / resolution;
    float mouseHeat = 0.0;
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        mouseHeat = exp(-mouseDist * radius * 0.5) * power;
    }
    
    // Apply heat distortion
    vec2 distortedUV = heatDistortion(uv, 0.5 + mouseHeat);
    
    // Generate multiple blobs
    float totalField = 0.0;
    
    // Large primary blobs
    for (int i = 0; i < 8; i++) {
        vec2 basePos = vec2(
            0.2 + float(i) * 0.1 + hash(vec2(float(i))) * 0.3,
            0.3 + hash(vec2(float(i), 1.0)) * 0.4
        );
        
        totalField += blob(distortedUV, basePos, time, float(i));
    }
    
    // Smaller secondary blobs
    for (int i = 8; i < 20; i++) {
        vec2 basePos = vec2(
            hash(vec2(float(i))) * 1.0,
            0.2 + hash(vec2(float(i), 1.0)) * 0.6
        );
        
        totalField += blob(distortedUV, basePos, time * 1.3, float(i)) * 0.5;
    }
    
    // Add mouse-influenced blob
    if (mouseHeat > 0.0) {
        totalField += metaball(distortedUV, mouse, 0.1 * mouseHeat);
    }
    
    // Bubble rising effect
    float bubbleField = 0.0;
    for (int i = 0; i < 6; i++) {
        float bubbleTime = time * 0.8 + float(i) * 2.0;
        vec2 bubblePos = vec2(
            0.3 + float(i) * 0.1 + sin(bubbleTime * 0.3) * 0.05,
            mod(bubbleTime * 0.2, 1.2) - 0.1
        );
        
        float bubbleSize = 0.02 + sin(bubbleTime) * 0.01;
        bubbleField += metaball(distortedUV, bubblePos, bubbleSize) * 0.3;
    }
    
    totalField += bubbleField;
    
    // Add heat convection currents
    float convection = noise(distortedUV * 5.0 + vec2(0.0, -time * 0.5)) * 0.2;
    totalField += convection;
    
    // Apply threshold for blob definition
    float blobMask = smoothstep(0.3, 0.6, totalField);
    
    // Calculate colors based on heat intensity
    float heatIntensity = totalField + mouseHeat * 2.0;
    heatIntensity += sin(time + totalField * 5.0) * 0.1; // Heat fluctuation
    
    vec3 color = lavaColors(heatIntensity);
    
    // Add glow around hot areas
    float glow = smoothstep(0.2, 0.8, totalField);
    color += glow * vec3(1.0, 0.5, 0.0) * 0.3;
    
    // Lamp glass effect
    float glassGradient = 1.0 - abs(uv.x - 0.5) * 2.0;
    glassGradient *= smoothstep(0.0, 0.1, uv.y) * smoothstep(1.0, 0.9, uv.y);
    
    // Background lamp color
    vec3 lampBackground = mix(vec3(0.05, 0.02, 0.1), vec3(0.1, 0.05, 0.2), glassGradient);
    
    // Blend blob colors with background
    color = mix(lampBackground, color, blobMask);
    
    // Add reflection highlights on glass
    float reflection = pow(glassGradient, 3.0) * sin(uv.y * PI) * 0.3;
    color += reflection * vec3(1.0, 1.0, 1.0);
    
    // Heat shimmer effect
    float shimmer = sin(distortedUV.x * 50.0 + time * 5.0) * sin(distortedUV.y * 60.0 + time * 7.0);
    shimmer *= smoothstep(0.2, 0.6, totalField) * 0.05;
    color += shimmer * vec3(1.0, 0.8, 0.0);
    
    // Mouse interaction glow
    if (mouseHeat > 0.0) {
        color += mouseHeat * vec3(1.0, 0.8, 0.2) * 0.8;
    }
    
    // Add subtle film grain
    float grain = hash(uv + time) * 0.02;
    color += grain;
    
    // Vignette for lamp container effect
    float vignette = 1.0 - smoothstep(0.3, 0.8, distance(uv, vec2(0.5)));
    color *= vignette * 0.7 + 0.3;
    
    // Final color adjustments
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.2; // Brightness boost
    
    fragColor = vec4(color, 1.0);
}
