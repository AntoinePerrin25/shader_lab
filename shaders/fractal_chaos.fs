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

// Noise and utility functions
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    
    return mix(mix(hash(i + vec2(0.0, 0.0)), hash(i + vec2(1.0, 0.0)), f.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float fbm(vec2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < octaves; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// Fractal dimension visualization
float fractalDimension(vec2 uv, float time, float dimension) {
    float scale = 1.0;
    float value = 0.0;
    
    // Non-integer dimension creates fractal behavior
    for (int i = 0; i < 8; i++) {
        float freq = pow(2.0, float(i));
        float amp = pow(freq, -dimension);
        
        vec2 p = uv * freq + time * 0.1;
        value += amp * noise(p);
        
        // Rotation for each octave
        float angle = float(i) * 0.5 + time * 0.1;
        p = mat2(cos(angle), -sin(angle), sin(angle), cos(angle)) * p;
    }
    
    return value;
}

// Strange attractor (Lorenz-like system)
vec2 strangeAttractor(vec2 p, float time) {
    // Modified Lorenz equations for 2D
    float sigma = 10.0 + sin(time * 0.1) * 2.0;
    float rho = 28.0 + cos(time * 0.15) * 5.0;
    float beta = 8.0/3.0;
    
    float dt = 0.01;
    vec2 velocity = vec2(
        sigma * (p.y - p.x),
        p.x * (rho - p.x) - p.y
    ) * dt;
    
    return p + velocity;
}

// Attractor field visualization
float attractorField(vec2 uv, float time) {
    float field = 0.0;
    
    // Multiple attractor points
    for (int i = 0; i < 6; i++) {
        float phase = float(i) * PI / 3.0;
        
        // Evolving attractor position
        vec2 center = vec2(0.5, 0.5);
        vec2 pos = center;
        
        // Simulate attractor evolution
        for (int j = 0; j < 20; j++) {
            pos = strangeAttractor(pos - center, time + phase) + center;
        }
        
        float dist = distance(uv, pos);
        field += exp(-dist * 8.0) * (sin(time * 2.0 + phase) * 0.5 + 0.5);
    }
    
    return field;
}

// Mandelbrot-like iteration
float mandelbrotVariant(vec2 uv, float time) {
    vec2 c = (uv - 0.5) * 3.0 + vec2(sin(time * 0.1), cos(time * 0.1)) * 0.5;
    vec2 z = vec2(0.0);
    
    float iterations = 0.0;
    for (int i = 0; i < 50; i++) {
        if (dot(z, z) > 4.0) break;
        
        // Modified iteration with time evolution
        float angle = time * 0.2 + float(i) * 0.1;
        vec2 rotation = vec2(cos(angle), sin(angle));
        z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c + rotation * 0.1;
        
        iterations += 1.0;
    }
    
    return iterations / 50.0;
}

// Julia set variation
float juliaSet(vec2 uv, float time) {
    vec2 z = (uv - 0.5) * 3.0;
    vec2 c = vec2(
        sin(time * 0.3) * 0.7885,
        cos(time * 0.4) * 0.7885
    );
    
    float iterations = 0.0;
    for (int i = 0; i < 50; i++) {
        if (dot(z, z) > 4.0) break;
        
        z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
        iterations += 1.0;
    }
    
    return smoothstep(0.0, 1.0, iterations / 50.0);
}

// Self-similar patterns
float selfSimilar(vec2 uv, float time) {
    float pattern = 0.0;
    float scale = 1.0;
    vec2 p = uv;
    
    for (int i = 0; i < 6; i++) {
        // Transform for self-similarity
        p = abs(p - 0.5) * 2.0;
        if (p.x < p.y) p.xy = p.yx;
        
        // Add rotation
        float angle = time * 0.1 + float(i) * 0.3;
        p = mat2(cos(angle), -sin(angle), sin(angle), cos(angle)) * (p - 0.5) + 0.5;
        
        // Scale and contribute
        scale *= 0.5;
        pattern += noise(p * pow(2.0, float(i)) + time * 0.2) * scale;
    }
    
    return pattern;
}

// Chaos game (Sierpinski-like)
float chaosGame(vec2 uv, float time) {
    // Triangle vertices
    vec2 v1 = vec2(0.5, 0.1);
    vec2 v2 = vec2(0.1, 0.9);
    vec2 v3 = vec2(0.9, 0.9);
    
    vec2 point = uv;
    float accumulator = 0.0;
    
    // Chaos game iteration
    for (int i = 0; i < 10; i++) {
        float r = hash(point + float(i) + time);
        
        vec2 target;
        if (r < 0.33) target = v1;
        else if (r < 0.66) target = v2;
        else target = v3;
        
        point = (point + target) * 0.5;
        
        // Accumulate density
        float dist = distance(uv, point);
        accumulator += exp(-dist * 50.0);
    }
    
    return accumulator;
}

// Fractal branching patterns
float fractalBranches(vec2 uv, float time) {
    float branches = 0.0;
    vec2 center = vec2(0.5, 0.9);
    
    // Recursive branching simulation
    for (int gen = 0; gen < 5; gen++) {
        for (int branch = 0; branch < int(pow(2.0, float(gen))); branch++) {
            float angle = float(branch) * PI / pow(2.0, float(gen - 1)) + time * 0.1;
            float length = 0.3 / pow(1.5, float(gen));
            
            vec2 direction = vec2(sin(angle), -cos(angle));
            vec2 endpoint = center + direction * length;
            
            // Distance to branch line
            vec2 toPoint = uv - center;
            float projLength = clamp(dot(toPoint, direction) / length, 0.0, 1.0);
            vec2 projection = center + direction * length * projLength;
            
            float dist = distance(uv, projection);
            float thickness = 0.01 / pow(1.5, float(gen));
            
            branches += exp(-dist * 20.0 / thickness);
        }
    }
    
    return branches;
}

// Fractal coloring scheme
vec3 fractalColors(float mandel, float julia, float dimension, float attractor, float branches, vec2 uv, float time) {
    vec3 color = vec3(0.0);
    
    // Mandelbrot-based coloring
    color += mandel * vec3(
        sin(mandel * 10.0 + time) * 0.5 + 0.5,
        sin(mandel * 8.0 + time + 2.0) * 0.5 + 0.5,
        sin(mandel * 6.0 + time + 4.0) * 0.5 + 0.5
    );
    
    // Julia set overlay
    color += julia * vec3(0.8, 0.4, 1.0) * 0.5;
    
    // Fractal dimension visualization
    color += dimension * vec3(1.0, 0.7, 0.3) * 0.3;
    
    // Strange attractor field
    color += attractor * vec3(0.2, 1.0, 0.6) * 0.4;
    
    // Branching patterns
    color += branches * vec3(0.6, 0.9, 0.2) * 0.6;
    
    return color;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Dynamic fractal dimension based on time
    float dimension = 1.5 + sin(time * 0.2) * 0.3;
    
    // Calculate fractal components
    float mandel = mandelbrotVariant(uv, time);
    float julia = juliaSet(uv, time);
    float fractalDim = fractalDimension(uv, time, dimension);
    float attractor = attractorField(uv, time);
    float selfSim = selfSimilar(uv, time);
    float chaos = chaosGame(uv, time);
    float branches = fractalBranches(uv, time);
    
    // Base fractal color
    vec3 color = fractalColors(mandel, julia, fractalDim, attractor, branches, uv, time);
    
    // Add self-similar patterns
    color += selfSim * vec3(0.5, 0.8, 1.0) * 0.2;
    
    // Add chaos game result
    color += chaos * vec3(1.0, 0.5, 0.8) * 0.1;
    
    // Add fractal noise
    float fractalNoise = fbm(uv * 4.0 + time * 0.1, 6);
    color += fractalNoise * 0.1 * vec3(0.8, 0.9, 1.0);
    
    // Mouse interaction - fractal zoom and distortion
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float interaction = exp(-mouseDist * radius) * power;
        
        // Fractal zoom effect
        vec2 zoomedUV = mouse + (uv - mouse) / (1.0 + interaction * 2.0);
        float zoomedMandel = mandelbrotVariant(zoomedUV, time);
        color += zoomedMandel * interaction * vec3(1.0, 0.8, 0.2);
        
        // Fractal distortion
        vec2 distortion = normalize(uv - mouse) * interaction * 0.1;
        vec2 distortedUV = uv + distortion * sin(time * 5.0);
        float distortedPattern = fractalDimension(distortedUV, time, dimension + interaction);
        color += distortedPattern * interaction * vec3(0.2, 1.0, 0.8);
        
        // Energy burst
        float burst = sin(mouseDist * 20.0 - time * 8.0) * interaction;
        color += burst * vec3(1.0, 1.0, 0.5);
    }
    
    // Add depth through layering
    float depth = smoothstep(0.0, 1.0, length(color));
    color *= 1.0 + depth * 0.3;
    
    // Enhance fractal glow
    color = pow(color, vec3(0.8));
    color *= 1.2;
    
    fragColor = vec4(color, 1.0);
}
