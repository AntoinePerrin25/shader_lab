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

// Digital matrix noise functions
float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

float hash2D(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

// Character generation for digital rain
float digitalChar(vec2 uv, float seed, float time) {
    // Create digital character cells
    vec2 cell = floor(uv * 20.0);
    vec2 cellUV = fract(uv * 20.0);
    
    // Character selection based on seed and time
    float charSeed = hash2D(cell + seed);
    float charTime = time * (2.0 + charSeed * 3.0);
    float charIndex = floor(charTime + charSeed * 10.0);
    
    // Character brightness flicker
    float brightness = hash(charIndex + charSeed) * (sin(charTime * 8.0) * 0.3 + 0.7);
    
    // Create character pattern (simplified matrix-style)
    float char = 0.0;
    
    // Vertical lines for character structure
    if (abs(cellUV.x - 0.2) < 0.1 || abs(cellUV.x - 0.8) < 0.1) {
        char += 0.8;
    }
    
    // Horizontal lines
    if (abs(cellUV.y - 0.3) < 0.1 || abs(cellUV.y - 0.7) < 0.1) {
        char += 0.6;
    }
    
    // Random pixels for variation
    if (hash2D(cellUV + charIndex) > 0.7) {
        char += 1.0;
    }
    
    return char * brightness;
}

// Digital rain columns
float digitalRain(vec2 uv, float time) {
    float rain = 0.0;
    
    // Multiple rain columns
    for (int i = 0; i < 15; i++) {
        float columnX = float(i) / 15.0 + hash(float(i)) * 0.1;
        float columnWidth = 0.02 + hash(float(i) * 2.0) * 0.03;
        
        // Column position
        if (abs(uv.x - columnX) < columnWidth) {
            // Rain speed variation
            float speed = 0.5 + hash(float(i) * 3.0) * 1.5;
            float rainY = uv.y + time * speed;
            
            // Create falling character trail
            float trail = fract(rainY);
            float intensity = exp(-trail * 3.0); // Fade as characters fall
            
            // Add characters along the trail
            float chars = digitalChar(vec2(uv.x, rainY), float(i), time);
            rain += chars * intensity;
            
            // Leading bright character
            float leader = exp(-abs(trail - 0.95) * 50.0);
            rain += leader * 2.0;
        }
    }
    
    return rain;
}

// Neural network visualization
float neuralNetwork(vec2 uv, float time) {
    float network = 0.0;
    
    // Network nodes
    for (int i = 0; i < 25; i++) {
        vec2 nodePos = vec2(
            hash(float(i)),
            hash(float(i) * 1.7)
        );
        
        float dist = distance(uv, nodePos);
        
        // Node activation
        float activation = sin(time * 3.0 + float(i)) * 0.5 + 0.5;
        network += exp(-dist * 15.0) * activation;
        
        // Network connections
        for (int j = i + 1; j < 25; j++) {
            vec2 nodePos2 = vec2(
                hash(float(j)),
                hash(float(j) * 1.7)
            );
            
            // Connection line
            vec2 line = nodePos2 - nodePos;
            vec2 toPoint = uv - nodePos;
            float projLength = clamp(dot(toPoint, line) / dot(line, line), 0.0, 1.0);
            vec2 projection = nodePos + line * projLength;
            
            float lineDist = distance(uv, projection);
            float connection = exp(-lineDist * 100.0);
            
            // Signal propagation
            float signal = sin(time * 5.0 + float(i + j) - projLength * 10.0) * 0.5 + 0.5;
            network += connection * signal * 0.3;
        }
    }
    
    return network;
}

// Data stream visualization
float dataStreams(vec2 uv, float time) {
    float streams = 0.0;
    
    // Horizontal data streams
    for (int i = 0; i < 10; i++) {
        float streamY = float(i) / 10.0 + sin(time * 0.5 + float(i)) * 0.1;
        float streamHeight = 0.02;
        
        if (abs(uv.y - streamY) < streamHeight) {
            // Data packets moving horizontally
            float packetX = fract(uv.x + time * (1.0 + float(i) * 0.3));
            
            // Packet visualization
            for (int j = 0; j < 5; j++) {
                float packetPos = float(j) * 0.2;
                float packet = exp(-abs(packetX - packetPos) * 20.0);
                streams += packet;
            }
        }
    }
    
    return streams;
}

// Glitch effects
vec2 glitchDistortion(vec2 uv, float time) {
    // Digital glitch displacement
    float glitchTime = floor(time * 10.0) / 10.0;
    
    // Random glitch lines
    for (int i = 0; i < 5; i++) {
        float glitchY = hash(glitchTime + float(i));
        float glitchHeight = 0.05;
        
        if (abs(uv.y - glitchY) < glitchHeight) {
            // Horizontal displacement
            float displacement = (hash(glitchTime + float(i) * 2.0) - 0.5) * 0.1;
            uv.x += displacement * sin(time * 20.0);
        }
    }
    
    return uv;
}

// Code compilation visualization
float codeCompilation(vec2 uv, float time) {
    float code = 0.0;
    
    // Scanning lines for compilation progress
    float scanLine = fract(time * 0.3);
    float scan = exp(-abs(uv.y - scanLine) * 30.0);
    code += scan * 0.5;
    
    // Syntax highlighting simulation
    vec2 cell = floor(uv * vec2(40.0, 20.0));
    float syntax = hash2D(cell + floor(time));
    
    if (syntax > 0.8) {
        code += 0.3; // Keywords
    } else if (syntax > 0.6) {
        code += 0.2; // Functions
    } else if (syntax > 0.4) {
        code += 0.1; // Variables
    }
    
    return code;
}

// Digital matrix particles
float matrixParticles(vec2 uv, float time) {
    float particles = 0.0;
    
    // Floating digital particles
    for (int i = 0; i < 30; i++) {
        float seed = float(i) * 13.7;
        vec2 particlePos = vec2(
            fract(hash(seed) + time * 0.1),
            fract(hash(seed * 2.0) + time * 0.2)
        );
        
        float dist = distance(uv, particlePos);
        float size = 0.01 + hash(seed * 3.0) * 0.02;
        
        // Particle glow
        particles += exp(-dist * 30.0 / size);
        
        // Particle trail
        vec2 trailOffset = vec2(0.0, -0.1) * hash(seed * 4.0);
        float trailDist = distance(uv, particlePos + trailOffset);
        particles += exp(-trailDist * 20.0 / size) * 0.5;
    }
    
    return particles;
}

// Cyber grid background
float cyberGrid(vec2 uv, float time) {
    vec2 grid = abs(fract(uv * 20.0) - 0.5);
    float gridPattern = 1.0 - smoothstep(0.0, 0.05, min(grid.x, grid.y));
    
    // Animated grid intensity
    gridPattern *= sin(time * 2.0 + uv.x * 10.0 + uv.y * 8.0) * 0.3 + 0.7;
    
    return gridPattern * 0.1;
}

// Holographic interference
float holographicEffect(vec2 uv, float time) {
    // Hologram scan lines
    float scanlines = sin(uv.y * 300.0 + time * 10.0) * 0.1 + 0.1;
    
    // Holographic noise
    float holoNoise = hash2D(uv * 100.0 + time * 5.0) * 0.2;
    
    return scanlines + holoNoise;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 mouse = mousePos / resolution;
    
    // Apply glitch distortion
    vec2 glitchedUV = glitchDistortion(uv, time);
    
    // Dark digital void background
    vec3 color = vec3(0.0, 0.05, 0.0);
    
    // Add cyber grid
    float grid = cyberGrid(glitchedUV, time);
    color += grid * vec3(0.0, 0.3, 0.1);
    
    // Digital rain effect
    float rain = digitalRain(glitchedUV, time);
    color += rain * vec3(0.1, 1.0, 0.3);
    
    // Neural network visualization
    float network = neuralNetwork(uv, time);
    color += network * 0.3 * vec3(0.0, 0.8, 0.4);
    
    // Data streams
    float streams = dataStreams(uv, time);
    color += streams * 0.4 * vec3(0.2, 0.9, 0.5);
    
    // Code compilation
    float code = codeCompilation(uv, time);
    color += code * vec3(0.0, 0.6, 0.2);
    
    // Matrix particles
    float particles = matrixParticles(uv, time);
    color += particles * 0.5 * vec3(0.3, 1.0, 0.6);
    
    // Holographic interference
    float holo = holographicEffect(uv, time);
    color += holo * vec3(0.0, 0.4, 0.2);
    
    // Add depth with distance fog
    float centerDist = distance(uv, vec2(0.5));
    float fog = exp(-centerDist * 1.5);
    color *= fog;
    
    // Mouse interaction - system breach
    if (length(mousePos) > 0.0) {
        float mouseDist = distance(uv, mouse);
        float breach = exp(-mouseDist * radius * 3.0) * power;
        
        // Security breach visualization
        float securityBreach = sin(mouseDist * 25.0 - time * 12.0) * breach;
        color += securityBreach * vec3(1.0, 0.2, 0.2);
        
        // Data corruption
        float corruption = breach * hash2D(uv * 50.0 + time * 8.0);
        color += corruption * vec3(1.0, 0.5, 0.0);
        
        // System access
        if (breach > 0.3) {
            // Access granted effect
            float access = exp(-mouseDist * 8.0) * breach;
            color += access * vec3(0.0, 1.0, 1.0);
            
            // Data extraction
            for (int i = 0; i < 8; i++) {
                float angle = float(i) * PI / 4.0 + time * 2.0;
                vec2 dataStream = mouse + vec2(cos(angle), sin(angle)) * mouseDist;
                float streamDist = distance(uv, dataStream);
                color += exp(-streamDist * 40.0) * breach * vec3(0.5, 1.0, 0.8);
            }
        }
        
        // Firewall activation
        float firewall = sin(time * 15.0) * breach;
        if (firewall > 0.5) {
            color += firewall * vec3(1.0, 0.0, 0.0);
        }
        
        // Matrix code download
        float download = breach * digitalChar(uv * 2.0, time, time * 5.0);
        color += download * vec3(0.8, 1.0, 0.2);
    }
    
    // Add terminal cursor
    float cursorX = 0.1;
    float cursorY = 0.9 - time * 0.1;
    cursorY = fract(cursorY);
    
    float cursor = exp(-distance(uv, vec2(cursorX, cursorY)) * 200.0);
    cursor *= sin(time * 5.0) * 0.5 + 0.5; // Blinking
    color += cursor * vec3(0.0, 1.0, 0.5);
    
    // Digital noise overlay
    float digitalNoise = hash2D(uv * 200.0 + time * 10.0);
    color += digitalNoise * 0.05 * vec3(0.0, 0.8, 0.3);
    
    // Enhance matrix glow
    color = pow(color, vec3(0.9));
    color *= 1.1;
    
    // Add chromatic aberration for digital distortion
    float aberration = rain * 0.01;
    color.r += digitalRain(glitchedUV + vec2(aberration, 0.0), time) * 0.1;
    color.b += digitalRain(glitchedUV - vec2(aberration, 0.0), time) * 0.1;
    
    fragColor = vec4(color, 1.0);
}
