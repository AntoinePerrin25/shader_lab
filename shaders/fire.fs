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

// Turbulent noise for fire
float turbulence(vec2 p) {
    float t = 0.0;
    float scale = 1.0;
    
    for (int i = 0; i < 5; i++) {
        t += abs(fbm(p * scale) - 0.5) / scale;
        scale *= 2.0;
    }
    return t;
}

// Fire shape function
float fireShape(vec2 uv, float time) {
    // Base flame shape
    float flame = 1.0 - uv.y;
    flame *= 1.0 - abs(uv.x - 0.5) * 2.0;
    
    // Add turbulence
    float turb = turbulence(uv * 4.0 + vec2(0.0, time * 2.0));
    flame += turb * 0.5;
    
    // Flickering effect
    flame *= 1.0 + sin(time * 8.0 + uv.x * 10.0) * 0.1;
    
    return clamp(flame, 0.0, 1.0);
}

// Fire color mapping
vec3 fireColor(float intensity) {
    vec3 black = vec3(0.0);
    vec3 red = vec3(1.0, 0.1, 0.0);
    vec3 orange = vec3(1.0, 0.5, 0.0);
    vec3 yellow = vec3(1.0, 1.0, 0.2);
    vec3 white = vec3(1.0, 1.0, 1.0);
    
    if (intensity < 0.2) {
        return mix(black, red, intensity * 5.0);
    } else if (intensity < 0.5) {
        return mix(red, orange, (intensity - 0.2) * 3.33);
    } else if (intensity < 0.8) {
        return mix(orange, yellow, (intensity - 0.5) * 3.33);
    } else {
        return mix(yellow, white, (intensity - 0.8) * 5.0);
    }
}

// Smoke generation
float smoke(vec2 uv, float time) {
    vec2 smokeUV = uv + vec2(sin(time * 0.5) * 0.1, 0.0);
    float s = fbm(smokeUV * 3.0 + vec2(0.0, time * 0.8));
    
    // Smoke rises and dissipates
    float rise = 1.0 - uv.y;
    s *= rise * rise;
    
    return s;
}

// Spark generation
float sparks(vec2 uv, float time) {
    vec2 sparkUV = uv * 50.0 + vec2(0.0, time * 10.0);
    float spark = noise(sparkUV);
    
    // Only bright sparks
    if (spark > 0.95) {
        float brightness = (spark - 0.95) * 20.0;
        return brightness * (1.0 - uv.y); // Sparks fade with height
    }
    return 0.0;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Adjust UV for flame shape
    uv.y = 1.0 - uv.y; // Flip Y so fire rises from bottom
    
    // Mouse interaction - wind effect
    vec2 windOffset = vec2(0.0);
    if (length(mousePos) > 0.0) {
        vec2 mouseDir = mouse - vec2(0.5, 0.0);
        windOffset = mouseDir * power * 0.1;
    }
    
    vec2 fireUV = uv + windOffset;
    
    // Generate fire intensity
    float fireIntensity = fireShape(fireUV, time);
    
    // Add noise distortion
    float distortion = fbm(fireUV * 8.0 + time * 1.5) * 0.1;
    fireIntensity += distortion;
    
    // Create fire color
    vec3 color = fireColor(fireIntensity);
    
    // Add inner flame (blue core)
    float coreIntensity = fireShape(fireUV * 1.2 - vec2(0.1, 0.1), time * 1.2);
    coreIntensity = smoothstep(0.3, 0.7, coreIntensity);
    vec3 blueCore = vec3(0.1, 0.3, 1.0) * coreIntensity;
    color = mix(color, color + blueCore, coreIntensity * 0.3);
    
    // Add smoke
    float smokeAmount = smoke(uv, time);
    vec3 smokeColor = vec3(0.2, 0.15, 0.1);
    
    // Smoke appears above the flame
    if (uv.y > 0.6) {
        color = mix(color, smokeColor, smokeAmount * 0.5);
    }
    
    // Add sparks
    float sparkAmount = sparks(uv, time);
    color += sparkAmount * vec3(1.0, 0.8, 0.2);
    
    // Mouse interaction - intensify fire
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(fireUV, vec2(mouse.x, 0.1));
        float mouseEffect = exp(-mouseDist * radius * 0.5) * power;
        
        // Increase fire intensity
        color += mouseEffect * vec3(1.0, 0.6, 0.2);
        
        // Add explosion effect
        float explosion = exp(-mouseDist * 5.0) * power;
        color += explosion * vec3(1.0, 1.0, 0.5);
    }
    
    // Add heat distortion effect
    float heatWave = sin(uv.y * 20.0 + time * 10.0) * fireIntensity * 0.02;
    fireUV.x += heatWave;
    
    // Enhance glow
    float glow = fireIntensity * 0.5;
    color += glow * vec3(1.0, 0.3, 0.1);
    
    // Add atmospheric scattering
    float atmosphere = (1.0 - uv.y) * fireIntensity;
    color += atmosphere * vec3(0.2, 0.1, 0.05);
    
    // Final intensity adjustment
    color *= 1.5;
    
    fragColor = vec4(color, 1.0);
}
