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

// Complex number operations
vec2 complexSquare(vec2 z) {
    return vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y);
}

// Mandelbrot set
float mandelbrot(vec2 c, int maxIter) {
    vec2 z = vec2(0.0);
    for (int i = 0; i < maxIter; i++) {
        if (dot(z, z) > 4.0) return float(i) / float(maxIter);
        z = complexSquare(z) + c;
    }
    return 0.0;
}

// Julia set
float julia(vec2 z, vec2 c, int maxIter) {
    for (int i = 0; i < maxIter; i++) {
        if (dot(z, z) > 4.0) return float(i) / float(maxIter);
        z = complexSquare(z) + c;
    }
    return 0.0;
}

// Color palette
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
    
    // Transform coordinates for fractal space
    vec2 c = (uv - 0.5) * 3.0;
    
    // Mouse interaction for Julia set parameter
    vec2 mouse = mousePos / resolution;
    vec2 juliaC = vec2(
        -0.7 + (mouse.x - 0.5) * 0.5,
        0.27015 + (mouse.y - 0.5) * 0.3
    );
    
    // Animated zoom and rotation
    float zoom = 1.0 + 0.3 * sin(time * 0.2);
    float rotation = time * 0.1;
    mat2 rot = mat2(cos(rotation), -sin(rotation), sin(rotation), cos(rotation));
    vec2 transformedC = rot * (c / zoom);
    
    // Calculate fractals
    float mandel = mandelbrot(transformedC * 0.8, 80);
    float juliaVal = julia(transformedC, juliaC, 60);
    
    // Mix between Mandelbrot and Julia based on time
    float mixFactor = (sin(time * 0.3) + 1.0) * 0.5;
    float fractal = mix(mandel, juliaVal, mixFactor);
    
    // Mouse influence
    float mouseDist = distance(uv, mouse);
    float mouseInfluence = exp(-mouseDist * radius * 0.2) * power * 0.3;
    fractal += mouseInfluence;
    
    // Color mapping
    vec3 color1 = palette(fractal + time * 0.1);
    vec3 color2 = palette(fractal * 2.0 + 0.5);
    
    vec3 finalColor = mix(color1, color2, smoothstep(0.2, 0.8, fractal));
    
    // Add glow
    float glow = pow(fractal, 0.7) * 0.4;
    finalColor += glow * vec3(0.2, 0.4, 0.9);
    
    // Contrast enhancement
    finalColor = pow(finalColor, vec3(0.9));
    
    fragColor = vec4(finalColor, 1.0);
}
