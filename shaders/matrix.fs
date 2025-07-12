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

// Character set for matrix effect
float character(vec2 uv, int charIndex) {
    // Simple character patterns using mathematical functions
    vec2 p = fract(uv) - 0.5;
    
    float c = 0.0;
    
    // Different character patterns
    if (charIndex == 0) { // 0
        c = smoothstep(0.3, 0.25, length(p)) - smoothstep(0.15, 0.1, length(p));
    } else if (charIndex == 1) { // 1
        c = smoothstep(0.1, 0.05, abs(p.x)) * step(-0.4, p.y) * step(p.y, 0.4);
    } else if (charIndex == 2) { // Japanese-like character
        c = smoothstep(0.15, 0.1, abs(p.x - 0.1)) + smoothstep(0.15, 0.1, abs(p.y));
    } else if (charIndex == 3) { // Another pattern
        c = smoothstep(0.2, 0.15, abs(p.x + p.y)) + smoothstep(0.2, 0.15, abs(p.x - p.y));
    } else if (charIndex == 4) { // Cross pattern
        c = smoothstep(0.1, 0.05, abs(p.x)) + smoothstep(0.1, 0.05, abs(p.y));
    } else { // Default pattern
        c = smoothstep(0.3, 0.2, length(p - vec2(0.0, 0.1))) + 
            smoothstep(0.3, 0.2, length(p + vec2(0.0, 0.1)));
    }
    
    return clamp(c, 0.0, 1.0);
}

// Noise function for randomness
float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Matrix rain column
float matrixColumn(vec2 uv, float time, float speed, float density) {
    float col = floor(uv.x);
    float cellY = floor(uv.y * 20.0);
    
    // Random character selection
    float rand = noise(vec2(col, cellY + floor(time * speed)));
    int charIndex = int(rand * 6.0);
    
    // Character appearance
    float charMask = character(uv * vec2(1.0, 20.0), charIndex);
    
    // Rain effect - characters fall down
    float rainOffset = fract(time * speed + noise(vec2(col, 0.0)) * 10.0);
    float rainY = fract(uv.y + rainOffset);
    
    // Fade characters as they fall
    float fade = 1.0 - rainY;
    fade = pow(fade, 2.0);
    
    // Random column activation
    float columnActive = step(0.5, noise(vec2(col, floor(time * 0.1))));
    
    return charMask * fade * columnActive * density;
}

// Leading character (bright)
float leadingChar(vec2 uv, float time, float speed) {
    float col = floor(uv.x);
    
    // Position of leading character
    float leadPos = fract(time * speed + noise(vec2(col, 0.0)) * 10.0);
    float dist = abs(fract(uv.y) - leadPos);
    
    float lead = smoothstep(0.1, 0.05, dist);
    
    // Random character for leader
    float rand = noise(vec2(col, floor(time * speed * 10.0)));
    int charIndex = int(rand * 6.0);
    float charMask = character(uv * vec2(1.0, 20.0), charIndex);
    
    return lead * charMask;
}

// Background digital pattern
float digitalNoise(vec2 uv, float time) {
    vec2 gridUV = uv * 50.0;
    vec2 grid = floor(gridUV);
    
    float n = noise(grid + time * 0.5);
    n = step(0.9, n);
    
    return n * 0.1;
}

// Glitch effect
vec3 glitchEffect(vec2 uv, float time) {
    vec3 glitch = vec3(0.0);
    
    // Random glitch lines
    float glitchLine = step(0.99, noise(vec2(0.0, floor(uv.y * 100.0) + time * 10.0)));
    
    if (glitchLine > 0.0) {
        // Color channel displacement
        vec2 offset = vec2(noise(vec2(time * 100.0)) - 0.5, 0.0) * 0.02;
        glitch.r = matrixColumn(uv + offset, time, 2.0, 1.0);
        glitch.g = matrixColumn(uv, time, 2.0, 1.0);
        glitch.b = matrixColumn(uv - offset, time, 2.0, 1.0);
    }
    
    return glitch;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Scale UV for character grid
    vec2 matrixUV = uv * vec2(30.0, 1.0);
    
    // Dark background
    vec3 color = vec3(0.0, 0.02, 0.0);
    
    // Background digital noise
    color += digitalNoise(uv, time) * vec3(0.0, 0.2, 0.0);
    
    // Multiple layers of matrix rain at different speeds
    float rain1 = matrixColumn(matrixUV, time, 1.5, 0.8);
    float rain2 = matrixColumn(matrixUV + vec2(0.5, 0.0), time, 2.2, 0.6);
    float rain3 = matrixColumn(matrixUV + vec2(0.25, 0.0), time, 0.8, 0.4);
    
    // Different shades of green for depth
    color += rain1 * vec3(0.0, 0.8, 0.1);
    color += rain2 * vec3(0.0, 0.6, 0.05);
    color += rain3 * vec3(0.0, 0.4, 0.02);
    
    // Leading bright characters
    float lead1 = leadingChar(matrixUV, time, 1.5);
    float lead2 = leadingChar(matrixUV + vec2(0.5, 0.0), time, 2.2);
    
    color += lead1 * vec3(0.5, 1.0, 0.5);
    color += lead2 * vec3(0.3, 1.0, 0.3);
    
    // Mouse interaction - code injection effect
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float mouseEffect = exp(-mouseDist * radius * 0.5) * power;
        
        // Bright code burst at mouse position
        vec2 localUV = (uv - mouse) * 20.0;
        float burst = 0.0;
        
        for (int i = 0; i < 5; i++) {
            float angle = float(i) * PI * 0.4 + time * 2.0;
            vec2 rayDir = vec2(cos(angle), sin(angle));
            float ray = smoothstep(0.1, 0.0, abs(dot(normalize(localUV), rayDir) - 1.0));
            burst += ray;
        }
        
        color += burst * mouseEffect * vec3(1.0, 1.0, 0.5);
        
        // Enhanced matrix density around mouse
        float enhancedRain = matrixColumn(matrixUV, time * 3.0, 5.0, mouseEffect);
        color += enhancedRain * vec3(0.5, 1.0, 0.2);
    }
    
    // Add glitch effects occasionally
    if (sin(time * 0.3) > 0.9) {
        vec3 glitch = glitchEffect(uv, time);
        color += glitch * 0.5;
    }
    
    // Scan lines
    float scanline = sin(uv.y * 800.0) * 0.05;
    color += scanline * vec3(0.0, 0.1, 0.0);
    
    // CRT curvature effect (subtle)
    vec2 curved = uv - 0.5;
    curved *= 1.0 + dot(curved, curved) * 0.1;
    
    // Vignette
    float vignette = 1.0 - length(curved) * 0.5;
    color *= vignette;
    
    // Enhance contrast
    color = pow(color, vec3(0.8));
    
    fragColor = vec4(color, 1.0);
}
