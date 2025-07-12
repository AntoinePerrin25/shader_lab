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

// Complex number operations for fractals
vec2 complexMul(vec2 a, vec2 b) {
    return vec2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

vec2 complexSquare(vec2 z) {
    return vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y);
}

// Mandelbrot-like fractal
float mandelbrot(vec2 c, int maxIter) {
    vec2 z = vec2(0.0);
    for (int i = 0; i < maxIter; i++) {
        if (dot(z, z) > 4.0) return float(i) / float(maxIter);
        z = complexSquare(z) + c;
    }
    return 0.0;
}

// Julia set fractal
float julia(vec2 z, vec2 c, int maxIter) {
    for (int i = 0; i < maxIter; i++) {
        if (dot(z, z) > 4.0) return float(i) / float(maxIter);
        z = complexSquare(z) + c;
    }
    return 0.0;
}

// Plasma effect
float plasma(vec2 uv, float time) {
    float x = uv.x;
    float y = uv.y;
    float v = 0.0;
    
    v += sin((x * 10.0 + time));
    v += sin((y * 10.0 + time) / 2.0);
    v += sin((x * 10.0 + y * 10.0 + time) / 2.0);
    
    float cx = x + 0.5 * sin(time / 5.0);
    float cy = y + 0.5 * cos(time / 3.0);
    v += sin(sqrt(100.0 * (cx * cx + cy * cy) + 1.0) + time);
    
    return v / 4.0;
}

// Voronoi-like cells
vec3 voronoi(vec2 uv) {
    vec2 g = floor(uv);
    vec2 f = fract(uv);
    
    float minDist = 1.0;
    vec2 minPoint;
    
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 neighbor = vec2(float(x), float(y));
            vec2 point = neighbor + sin(time + 6.2831 * (g + neighbor)) * 0.5 + 0.5;
            vec2 diff = neighbor + point - f;
            float dist = length(diff);
            
            if (dist < minDist) {
                minDist = dist;
                minPoint = point;
            }
        }
    }
    
    return vec3(minDist, minPoint);
}

// Electric/lightning effect
float lightning(vec2 uv, float time) {
    float noise = 0.0;
    float scale = 1.0;
    
    for (int i = 0; i < 6; i++) {
        noise += abs(sin(uv.x * scale + time * 2.0 + sin(uv.y * scale * 0.5 + time))) / scale;
        scale *= 2.0;
    }
    
    return pow(1.0 / (1.0 + noise * 20.0), 3.0);
}

// Color palette function
vec3 palette(float t) {
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.0, 1.0, 1.0);
    vec3 d = vec3(0.263, 0.416, 0.557);
    
    return a + b * cos(6.28318 * (c * t + d));
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 center = vec2(0.5, 0.5);
    
    // Normalize coordinates to [-1, 1] for fractals
    vec2 c = (uv - 0.5) * 4.0;
    
    // Mouse interaction
    vec2 mouse = mousePos / resolution;
    vec2 mouseOffset = (mouse - 0.5) * 2.0;
    
    // Animated Julia set parameter
    vec2 juliaC = vec2(
        -0.7 + 0.3 * cos(time * 0.5) + mouseOffset.x * 0.5,
        0.27015 + 0.1 * sin(time * 0.7) + mouseOffset.y * 0.5
    );
    
    // Zoom and rotation animation
    float zoom = 1.0 + 0.5 * sin(time * 0.3);
    float rotation = time * 0.2;
    mat2 rot = mat2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
    vec2 zoomedC = rot * (c / zoom);
    
    // Calculate different effects
    float julia_val = julia(zoomedC, juliaC, 50);
    float mandel_val = mandelbrot(zoomedC * 0.5, 30);
    float plasma_val = plasma(uv * 3.0, time * 2.0);
    float lightning_val = lightning(uv * 10.0, time * 3.0);
    
    // Voronoi cells
    vec3 voronoi_val = voronoi(uv * 8.0 + time * 0.5);
    
    // Combine effects based on time and mouse position
    float mixFactor = (sin(time * 0.4) + 1.0) * 0.5;
    float mouseDist = distance(uv, mouse);
    float mouseInfluence = exp(-mouseDist * radius * 0.1) * power;
    
    // Create layered composition
    float fractal = mix(julia_val, mandel_val, mixFactor);
    fractal = mix(fractal, plasma_val * 0.5, 0.3);
    
    // Add lightning around mouse
    if (length(mousePos) > 0.0) {
        fractal += lightning_val * mouseInfluence;
    }
    
    // Add voronoi structure
    fractal += voronoi_val.x * 0.2;
    fractal = mix(fractal, 1.0 - voronoi_val.x, 0.1);
    
    // Enhance with distance field
    float distField = abs(sin(length(uv - center) * 20.0 - time * 5.0)) * 0.1;
    fractal += distField;
    
    // Color mapping
    vec3 color1 = palette(fractal + time * 0.1);
    vec3 color2 = palette(fractal * 2.0 + time * 0.2 + 0.5);
    vec3 color3 = palette(fractal * 0.5 + time * 0.05 + 1.0);
    
    // Mix colors based on fractal value and mouse interaction
    vec3 finalColor = mix(color1, color2, smoothstep(0.0, 1.0, fractal));
    finalColor = mix(finalColor, color3, mouseInfluence * 0.5);
    
    // Add glow effect
    float glow = pow(fractal, 0.5) * 0.5;
    finalColor += glow * vec3(0.1, 0.3, 0.8);
    
    // Vignette effect
    float vignette = 1.0 - smoothstep(0.3, 1.0, distance(uv, center));
    finalColor *= vignette;
    
    // Final brightness and contrast adjustment
    finalColor = pow(finalColor, vec3(0.8));
    finalColor *= 1.2;
    
    fragColor = vec4(finalColor, 1.0);
}
