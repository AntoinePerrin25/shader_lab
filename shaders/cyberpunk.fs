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

// Noise function
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

// Rain drops
float rainDrop(vec2 uv, vec2 pos, float speed, float size) {
    vec2 p = uv - pos;
    p.y += time * speed;
    p.y = mod(p.y, 1.0);
    
    float drop = 1.0 - smoothstep(0.0, size, length(p));
    
    // Add vertical streak
    float streak = exp(-abs(p.x) * 50.0) * smoothstep(0.0, 0.3, p.y) * smoothstep(0.8, 0.0, p.y);
    
    return max(drop, streak * 0.3);
}

// Neon glow effect
vec3 neonGlow(vec3 color, float intensity) {
    vec3 glow = color * intensity;
    glow += color * color * intensity * 0.5;
    return glow;
}

// Cyberpunk building silhouettes
float building(vec2 uv) {
    float buildings = 0.0;
    
    for (int i = 0; i < 8; i++) {
        float x = float(i) * 0.15 - 0.5;
        float width = 0.05 + hash(vec2(float(i))) * 0.08;
        float height = 0.3 + hash(vec2(float(i), 1.0)) * 0.4;
        
        if (abs(uv.x - x) < width && uv.y < height - 0.5) {
            buildings = 1.0;
            
            // Add windows
            vec2 windowUV = (uv - vec2(x, 0.0)) / vec2(width, height) + 0.5;
            windowUV.y = fract(windowUV.y * 8.0);
            windowUV.x = fract(windowUV.x * 4.0);
            
            if (windowUV.x > 0.2 && windowUV.x < 0.8 && windowUV.y > 0.3 && windowUV.y < 0.7) {
                if (hash(vec2(float(i), floor(windowUV.y * 8.0))) > 0.7) {
                    buildings = 2.0; // Lit window
                }
            }
        }
    }
    
    return buildings;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 originalUV = uv;
    
    // Mouse interaction - affects wind direction
    vec2 mouse = mousePos / resolution;
    vec2 wind = vec2(0.0);
    if (length(mousePos) > 0.0) {
        wind = (mouse - 0.5) * power * 0.1;
    }
    
    // Base cyberpunk atmosphere
    vec3 skyColor = vec3(0.05, 0.02, 0.15); // Dark purple sky
    vec3 color = skyColor;
    
    // Add atmospheric gradient
    float gradient = 1.0 - uv.y;
    color += vec3(0.2, 0.05, 0.3) * gradient * 0.5;
    
    // Generate multiple rain layers
    float rain = 0.0;
    vec3 rainColor = vec3(0.0);
    
    for (int i = 0; i < 50; i++) {
        vec2 rainPos = vec2(
            hash(vec2(float(i))) * 1.2 - 0.1,
            hash(vec2(float(i), 1.0))
        );
        
        float speed = 2.0 + hash(vec2(float(i), 2.0)) * 3.0;
        float size = 0.002 + hash(vec2(float(i), 3.0)) * 0.005;
        
        // Apply wind to rain position
        rainPos.x += wind.x * (1.0 - uv.y) * 2.0;
        
        float drop = rainDrop(uv, rainPos, speed, size);
        rain += drop;
        
        // Neon colored rain
        if (drop > 0.0) {
            vec3 dropColor = vec3(0.0, 0.8, 1.0); // Cyan
            if (hash(vec2(float(i), 4.0)) > 0.7) {
                dropColor = vec3(1.0, 0.2, 0.8); // Magenta
            } else if (hash(vec2(float(i), 5.0)) > 0.8) {
                dropColor = vec3(0.2, 1.0, 0.3); // Green
            }
            
            rainColor += dropColor * drop * neonGlow(dropColor, 2.0);
        }
    }
    
    // Add buildings
    float buildings = building(uv);
    if (buildings > 0.5) {
        if (buildings > 1.5) {
            // Lit windows
            color = vec3(1.0, 0.8, 0.2) * 0.8;
            color = neonGlow(color, 1.5);
        } else {
            // Building silhouette
            color = vec3(0.1, 0.05, 0.2);
        }
    }
    
    // Add neon signs on buildings
    if (buildings > 0.5 && buildings < 1.5) {
        float signNoise = noise(uv * 20.0 + time * 2.0);
        if (signNoise > 0.85) {
            vec3 neonColors[3];
            neonColors[0] = vec3(0.0, 1.0, 1.0); // Cyan
            neonColors[1] = vec3(1.0, 0.0, 1.0); // Magenta
            neonColors[2] = vec3(0.0, 1.0, 0.0); // Green
            
            int colorIndex = int(hash(uv * 10.0) * 3.0);
            vec3 neonColor = neonColors[colorIndex];
            
            // Flickering effect
            float flicker = sin(time * 10.0 + hash(uv * 5.0) * 20.0) * 0.3 + 0.7;
            color += neonGlow(neonColor, 3.0) * flicker;
        }
    }
    
    // Add rain color to scene
    color = mix(color, color + rainColor, min(rain, 1.0));
    
    // Street lighting reflections in puddles
    if (uv.y < 0.15) {
        float puddleReflection = noise(uv * 50.0) * 0.5 + 0.5;
        puddleReflection *= smoothstep(0.0, 0.15, 0.15 - uv.y);
        
        // Reflect neon colors
        vec3 reflectionColor = color * 0.3;
        color = mix(color, reflectionColor, puddleReflection * 0.7);
        
        // Add ripples from rain
        if (rain > 0.0) {
            float ripples = sin(length(uv - originalUV) * 100.0 - time * 10.0) * 0.5 + 0.5;
            color += ripples * rainColor * 0.2;
        }
    }
    
    // Add volumetric fog
    float fog = noise(uv * 2.0 + time * 0.1) * 0.3;
    fog *= (1.0 - uv.y) * 0.5;
    color = mix(color, skyColor + vec3(0.1, 0.05, 0.2), fog);
    
    // Lightning flash effect
    float lightning = 0.0;
    if (sin(time * 0.1) > 0.99) {
        lightning = sin(time * 50.0) * 0.5 + 0.5;
        lightning *= noise(uv * 3.0) * 2.0;
    }
    color += lightning * vec3(0.8, 0.9, 1.0);
    
    // Mouse interaction glow
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseGlow = exp(-mouseDist * radius * 2.0) * power;
        color += mouseGlow * vec3(0.0, 1.0, 1.0) * 2.0;
    }
    
    // Final post-processing
    color = pow(color, vec3(0.8)); // Gamma correction
    color *= 1.1; // Brightness boost
    
    // Add scan lines for CRT effect
    float scanlines = sin(uv.y * resolution.y * PI) * 0.05 + 0.95;
    color *= scanlines;
    
    fragColor = vec4(color, 1.0);
}
