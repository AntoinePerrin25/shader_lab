#include "raylib.h"
#include <sys/stat.h> // Pour stat()
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h> // Pour fminf, fmaxf
#include <stdlib.h>
// Removed pthread dependency - using synchronous processing
#include <dirent.h> // Pour parcourir les répertoires
#include <unistd.h> // Pour access()
#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN // Exclure les en-têtes inutiles de Windows
#define NOUSER // Exclure les en-têtes inutiles de Windows
#define NOGDI // Exclure les en-têtes inutiles de Windows
#include <windows.h> // Pour Sleep
#undef LoadImage // Éviter le conflit avec la fonction WinAPI
#endif
#define UNUSED (void)

#define ENABLE_LOGGING 1 // Activer temporairement le logging pour déboguer

// Structure pour le logging avec gestion des patterns
#define MAX_LOG_BUFFER 64
#define MAX_MESSAGE_LENGTH 256

typedef struct {
    char messages[MAX_LOG_BUFFER][MAX_MESSAGE_LENGTH];
    int messageCount;
    int writeIndex;
    FILE* logFile;
    char lastMessage[MAX_MESSAGE_LENGTH];
    int consecutiveCount;
} LogManager;

static LogManager gLogManager = {0};

void InitLogger(void) {
    #if !ENABLE_LOGGING
    return;
    #endif
    gLogManager.logFile = fopen("debug.log", "w");
    if (gLogManager.logFile) {
        fprintf(gLogManager.logFile, "=== LOG START ===\n");
        fflush(gLogManager.logFile);
    }
    gLogManager.messageCount = 0;
    gLogManager.writeIndex = 0;
    gLogManager.consecutiveCount = 0;
    gLogManager.lastMessage[0] = '\0';
}

void FlushConsecutiveMessages(void) {
    if (gLogManager.consecutiveCount > 1) {
        fprintf(gLogManager.logFile, "%s x%d\n", gLogManager.lastMessage, gLogManager.consecutiveCount);
        fflush(gLogManager.logFile);
    } else if (gLogManager.consecutiveCount == 1) {
        fprintf(gLogManager.logFile, "%s\n", gLogManager.lastMessage);
        fflush(gLogManager.logFile);
    }
    gLogManager.consecutiveCount = 0;
}

void LogMessage(const char* message) {
    #if !ENABLE_LOGGING
    return;
    #endif
    if (!gLogManager.logFile) return;
    
    // Si c'est le même message que le précédent, juste incrémenter le compteur
    if (gLogManager.consecutiveCount > 0 && strcmp(gLogManager.lastMessage, message) == 0) {
        gLogManager.consecutiveCount++;
        return;
    }
    
    // Flush les messages consécutifs précédents
    FlushConsecutiveMessages();
    
    // Commencer un nouveau message
    strcpy(gLogManager.lastMessage, message);
    gLogManager.consecutiveCount = 1;
    
    // Ajouter au buffer circulaire pour détection de patterns plus complexes
    strcpy(gLogManager.messages[gLogManager.writeIndex], message);
    gLogManager.writeIndex = (gLogManager.writeIndex + 1) % MAX_LOG_BUFFER;
    if (gLogManager.messageCount < MAX_LOG_BUFFER) {
        gLogManager.messageCount++;
    }
}

void CloseLogger(void) {
    if (gLogManager.logFile) {
        FlushConsecutiveMessages();
        fprintf(gLogManager.logFile, "=== LOG END ===\n");
        fclose(gLogManager.logFile);
    }
}

// Structure pour gérer les erreurs de shader
typedef struct {
    bool hasError;
    char errorMessage[512];
    bool isDefaultShader;
} ShaderState;

// Structure pour gérer les shaders disponibles
#define MAX_SHADERS 32
#define MAX_SHADER_NAME 64
typedef struct {
    char names[MAX_SHADERS][MAX_SHADER_NAME];
    char paths[MAX_SHADERS][320];
    int count;
    int selectedIndex;
    bool dropdownActive;
} ShaderManager;

static ShaderManager gShaderManager = {0};

// Shader par défaut simple qui ne fait rien
const char* defaultFragmentShader = 
"#version 460\n"
"in vec2 fragTexCoord;\n"
"out vec4 fragColor;\n"
"uniform sampler2D texture0;\n"
"void main() {\n"
"    fragColor = texture(texture0, fragTexCoord);\n"
"}\n";

// Fonction pour charger un shader de manière sécurisée
Shader LoadShaderSafe(const char* vsFileName, const char* fsFileName, ShaderState* state) {
    Shader shader = {0};
    
    // Réinitialiser l'état
    state->hasError = false;
    state->errorMessage[0] = '\0';
    state->isDefaultShader = false;
    
    // Essayer de charger le shader
    shader = LoadShader(vsFileName, fsFileName);
    
    // Vérifier si le shader est valide
    if (shader.id == 0) {
        // Échec du chargement, utiliser le shader par défaut
        printf("ERROR: Failed to load shader '%s'\n", fsFileName);
        LogMessage("LOG Shader loading failed");
        
        // Charger le shader par défaut
        shader = LoadShaderFromMemory(NULL, defaultFragmentShader);
        
        if (shader.id == 0) {
            // Même le shader par défaut a échoué
            state->hasError = true;
            snprintf(state->errorMessage, sizeof(state->errorMessage), 
                    "ERREUR CRITIQUE: Impossible de charger le shader par défaut");
            printf("CRITICAL ERROR: Cannot load default shader\n");
        } else {
            state->isDefaultShader = true;
            snprintf(state->errorMessage, sizeof(state->errorMessage), 
                    "ERREUR SHADER: Utilisation du shader par défaut. Vérifiez '%s'", fsFileName);
            printf("Using default shader due to error in '%s'\n", fsFileName);
        }
    } else {
        printf("Shader loaded successfully: %s\n", fsFileName);
        LogMessage("LOG Shader loaded successfully");
    }
    
    return shader;
}

// Fonction pour découvrir les shaders disponibles
void DiscoverShaders(void) {
    printf("=== DISCOVERING SHADERS ===\n");
    
    gShaderManager.count = 0;
    gShaderManager.selectedIndex = 0;
    gShaderManager.dropdownActive = false;
    
    // Chercher dans le dossier racine
    DIR* dir = opendir(".");
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL && gShaderManager.count < MAX_SHADERS) {
            if (strstr(entry->d_name, ".glsl") && entry->d_name[0] != '.') {
                // Extraire le nom sans extension
                char nameWithoutExt[MAX_SHADER_NAME];
                int copyLen = snprintf(nameWithoutExt, sizeof(nameWithoutExt), "%s", entry->d_name);
                if (copyLen >= MAX_SHADER_NAME) {
                    printf("Warning: shader name too long, truncated: %s\n", entry->d_name);
                    continue; // Ignorer ce shader si le nom est trop long
                }
                
                char* dotPos = strrchr(nameWithoutExt, '.');
                if (dotPos) *dotPos = '\0';
                
                // Ajouter au manager
                snprintf(gShaderManager.names[gShaderManager.count], MAX_SHADER_NAME, "%s", nameWithoutExt);
                snprintf(gShaderManager.paths[gShaderManager.count], 320, "%s", entry->d_name);
                
                printf("Found shader: %s -> %s\n", nameWithoutExt, entry->d_name);
                gShaderManager.count++;
            }
        }
        closedir(dir);
    }
    
    // Chercher dans un dossier "shaders" s'il existe
    DIR* shaderDir = opendir("shaders");
    if (shaderDir) {
        struct dirent* entry;
        while ((entry = readdir(shaderDir)) != NULL && gShaderManager.count < MAX_SHADERS) {
            if (strstr(entry->d_name, ".glsl") && entry->d_name[0] != '.') {
                // Extraire le nom sans extension
                char nameWithoutExt[MAX_SHADER_NAME];
                int copyLen = snprintf(nameWithoutExt, sizeof(nameWithoutExt), "%s", entry->d_name);
                if (copyLen >= MAX_SHADER_NAME) {
                    printf("Warning: shader name too long, truncated: %s\n", entry->d_name);
                    continue; // Ignorer ce shader si le nom est trop long
                }
                
                char* dotPos = strrchr(nameWithoutExt, '.');
                if (dotPos) *dotPos = '\0';
                
                // Ajouter au manager avec le chemin du dossier
                snprintf(gShaderManager.names[gShaderManager.count], MAX_SHADER_NAME, "%s", nameWithoutExt);
                
                // Construire le chemin avec vérification de la longueur
                int pathLen = snprintf(gShaderManager.paths[gShaderManager.count], 256, "shaders/%s", entry->d_name);
                if (pathLen >= 256) {
                    printf("Warning: shader path too long, truncated: %s\n", entry->d_name);
                    continue; // Ignorer ce shader si le chemin est trop long
                }
                
                printf("Found shader: %s -> %s\n", nameWithoutExt, gShaderManager.paths[gShaderManager.count]);
                gShaderManager.count++;
            }
        }
        closedir(shaderDir);
    }
    
    printf("Total shaders found: %d\n", gShaderManager.count);
    
    // Si aucun shader trouvé, ajouter un shader par défaut
    if (gShaderManager.count == 0) {
        snprintf(gShaderManager.names[0], MAX_SHADER_NAME, "Default");
        snprintf(gShaderManager.paths[0], 256, "effect.glsl");
        gShaderManager.count = 1;
        printf("No shaders found, using default\n");
    }
}

// Fonction pour obtenir le chemin du shader sélectionné
const char* GetSelectedShaderPath(void) {
    if (gShaderManager.selectedIndex >= 0 && gShaderManager.selectedIndex < gShaderManager.count) {
        return gShaderManager.paths[gShaderManager.selectedIndex];
    }
    return "effect.glsl"; // Fallback
}
time_t My_GetFileModTime(const char *path)
{
    struct stat attrib;
    if (stat(path, &attrib) == 0)
        return attrib.st_mtime;
    else
        return 0;
}

// Structure pour le traitement vidéo synchrone
typedef struct {
    char inputPath[256];
    char outputDir[256];
    int frameCount;
    float fps;
    bool isCompleted;
    bool hasError;
    char errorMessage[256];
} VideoProcessor;

static VideoProcessor gVideoProcessor = {0};

// Structure pour gérer un buffer de textures
typedef struct {
    Texture2D* textures;
    int count;
    int capacity;
    bool isAllocated;
} TextureBuffer;

// Déclarations forward des fonctions
bool LoadExtractedFrames(Image** sequence, TextureBuffer* textureBuffer, int* frameCount, float* fps);
int CheckAndLoadNewFrames(Image** sequence, TextureBuffer* textureBuffer, int currentMaxFrames);
void InitTextureBuffer(TextureBuffer* buffer, int capacity);
void FreeTextureBuffer(TextureBuffer* buffer);
bool LoadTextureToBuffer(TextureBuffer* buffer, const Image* image, int index);
Texture2D* GetTextureFromBuffer(TextureBuffer* buffer, int index);

// Fonction pour initialiser le processeur vidéo
void InitVideoProcessor(void) {
    memset(&gVideoProcessor, 0, sizeof(VideoProcessor));
    snprintf(gVideoProcessor.outputDir, sizeof(gVideoProcessor.outputDir), "./temp_frames/");
    printf("Video processor initialized (synchronous mode)\n");
}

// Fonction pour nettoyer le répertoire temporaire
void CleanupTempFrames(void) {
    DIR* dir = opendir(gVideoProcessor.outputDir);
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL) {
            if (strstr(entry->d_name, "frame_") && strstr(entry->d_name, ".png")) {
                char fullPath[1024];
                snprintf(fullPath, sizeof(fullPath), "%s%s", gVideoProcessor.outputDir, entry->d_name);
                remove(fullPath);
            }
        }
        closedir(dir);
    }
    rmdir(gVideoProcessor.outputDir);
}

// Fonction synchrone pour traiter la vidéo avec FFmpeg
bool ProcessVideoSynchronous(const char* videoPath) {
    printf("=== PROCESSING VIDEO SYNCHRONOUSLY ===\n");
    
    // Copier le chemin de la vidéo
    strcpy(gVideoProcessor.inputPath, videoPath);
    gVideoProcessor.isCompleted = false;
    gVideoProcessor.hasError = false;
    gVideoProcessor.frameCount = 0;
    gVideoProcessor.fps = 0.0f;
    
    // Créer le répertoire temporaire
    printf("Creating temp directory...\n");
    #ifdef _WIN32
    int mkdirResult = system("if not exist temp_frames mkdir temp_frames");
    #else
    int mkdirResult = mkdir(gVideoProcessor.outputDir, 0755);
    #endif
    
    if (mkdirResult != 0) {
        printf("ERROR: Failed to create temp directory\n");
        gVideoProcessor.hasError = true;
        strcpy(gVideoProcessor.errorMessage, "Impossible de créer le répertoire temporaire");
        return false;
    }
    
    // Commande FFmpeg pour extraire les frames et obtenir les informations
    char ffmpegCmd[1024];
    char ffprobeCmd[1024];
    
    printf("Getting video info with ffprobe...\n");
    // D'abord, obtenir les informations sur la vidéo (FPS, durée, etc.)
    snprintf(ffprobeCmd, sizeof(ffprobeCmd), "ffprobe -v quiet -select_streams v:0 -show_entries stream=r_frame_rate -of csv=p=0 \"%s\" > temp_fps.txt", 
            videoPath);
    
    int result = system(ffprobeCmd);
    if (result == 0) {
        // Lire le FPS de la vidéo
        FILE* fpsFile = fopen("temp_fps.txt", "r");
        if (fpsFile) {
            char fpsString[64];
            if (fgets(fpsString, sizeof(fpsString), fpsFile)) {
                // Retirer le saut de ligne
                fpsString[strcspn(fpsString, "\n")] = 0;
                
                // Gérer les fractions comme "30/1"
                float num, den;
                if (sscanf(fpsString, "%f/%f", &num, &den) == 2 && den > 0) {
                    gVideoProcessor.fps = num / den;
                } else {
                    gVideoProcessor.fps = atof(fpsString);
                }
                printf("Detected FPS: %.2f\n", gVideoProcessor.fps);
            }
            fclose(fpsFile);
        }
        remove("temp_fps.txt");
    } else {
        printf("WARNING: Failed to get video FPS, using default\n");
    }
    
    // Si on n'a pas pu obtenir le FPS, utiliser une valeur par défaut
    if (gVideoProcessor.fps <= 0) {
        gVideoProcessor.fps = 30.0f;
        printf("Using default FPS: %.2f\n", gVideoProcessor.fps);
    }
    
    // Extraire les frames avec FFmpeg
    printf("Starting FFmpeg frame extraction...\n");
    snprintf(ffmpegCmd, sizeof(ffmpegCmd), "ffmpeg -i \"%s\" \"%sframe_%%06d.png\" -y", 
            videoPath, gVideoProcessor.outputDir);
    
    printf("Executing FFmpeg command: %s\n", ffmpegCmd);
    result = system(ffmpegCmd);
    printf("FFmpeg command result: %d\n", result);
    
    if (result != 0) {
        printf("ERROR: FFmpeg failed with error code: %d\n", result);
        gVideoProcessor.hasError = true;
        snprintf(gVideoProcessor.errorMessage, sizeof(gVideoProcessor.errorMessage), 
                "Erreur FFmpeg (code: %d)", result);
        return false;
    }
    
    // Attendre que FFmpeg termine et compter les frames une seule fois
    printf("FFmpeg extraction successful, waiting for completion...\n");
    
    #ifdef _WIN32
    Sleep(2000); // Attendre 2 secondes pour que FFmpeg termine
    #else
    sleep(2);
    #endif
    
    // Compter le nombre de frames générées (une seule fois)
    DIR* dir = opendir(gVideoProcessor.outputDir);
    int finalFrameCount = 0;
    if (dir) {
        struct dirent* entry;
        while ((entry = readdir(dir)) != NULL) {
            if (strstr(entry->d_name, "frame_") && strstr(entry->d_name, ".png")) {
                // Vérifier que le fichier n'est pas vide
                char fullPath[1024];
                snprintf(fullPath, sizeof(fullPath), "%s%s", gVideoProcessor.outputDir, entry->d_name);
                FILE* testFile = fopen(fullPath, "rb");
                if (testFile) {
                    fseek(testFile, 0, SEEK_END);
                    long size = ftell(testFile);
                    fclose(testFile);
                    if (size > 100) { // Fichier doit avoir au moins 100 bytes
                        finalFrameCount++;
                    }
                }
            }
        }
        closedir(dir);
    }
    
    printf("Frame extraction completed: %d frames found\n", finalFrameCount);
    
    // Finaliser le traitement
    gVideoProcessor.frameCount = finalFrameCount;
    printf("=== FINAL FRAME COUNT: %d ===\n", finalFrameCount);
    
    if (finalFrameCount > 0) {
        gVideoProcessor.isCompleted = true;
        gVideoProcessor.hasError = false;
        printf("*** VIDEO PROCESSING COMPLETED SUCCESSFULLY ***\n");
        printf("*** %d frames extracted at %.2f FPS ***\n", finalFrameCount, gVideoProcessor.fps);
        return true;
    } else {
        gVideoProcessor.hasError = true;
        strcpy(gVideoProcessor.errorMessage, "Aucune frame extraite");
        printf("ERROR: No frames extracted\n");
        return false;
    }
}

// Fonction pour démarrer le traitement vidéo synchrone
bool StartVideoProcessing(const char* videoPath) {
    printf("=== STARTING VIDEO PROCESSING (SYNCHRONOUS) ===\n");
    printf("Video file: %s\n", videoPath);
    
    // Nettoyer les frames précédentes
    CleanupTempFrames();
    
    // Traiter la vidéo immédiatement
    return ProcessVideoSynchronous(videoPath);
}

// Fonction pour vérifier si une frame spécifique est disponible
bool IsFrameAvailable(int frameIndex) {
    if (frameIndex < 0) return false;
    
    char framePath[1024];
    snprintf(framePath, sizeof(framePath), "%sframe_%06d.png", gVideoProcessor.outputDir, frameIndex + 1);
    
    if (access(framePath, F_OK) != 0) {
        return false;
    }
    
    // Vérifier la taille du fichier
    FILE* testFile = fopen(framePath, "rb");
    if (testFile) {
        fseek(testFile, 0, SEEK_END);
        long fileSize = ftell(testFile);
        fclose(testFile);
        return fileSize > 100; // Fichier doit avoir au moins 100 bytes
    }
    
    return false;
}

// Fonction pour charger une frame spécifique à la demande
bool LoadSpecificFrame(int frameIndex, Image* frameImage) {
    if (frameIndex < 0) return false;
    
    char framePath[1024];
    snprintf(framePath, sizeof(framePath), "%sframe_%06d.png", gVideoProcessor.outputDir, frameIndex + 1);
    
    if (!IsFrameAvailable(frameIndex)) {
        return false;
    }
    
    *frameImage = LoadImage(framePath);
    return frameImage->data != NULL;
}

// Fonction pour charger les frames extraites (version synchrone)
bool LoadExtractedFrames(Image** sequence, TextureBuffer* textureBuffer, int* frameCount, float* fps) {
    LogMessage("LOG LoadExtractedFrames called");
    printf("=== LOADING EXTRACTED FRAMES ===\n");
    
    printf("LoadExtractedFrames: checking state - completed=%d, hasError=%d, frameCount=%d\n", 
           gVideoProcessor.isCompleted, gVideoProcessor.hasError, gVideoProcessor.frameCount);
    
    // Vérifier que le traitement est terminé
    if (gVideoProcessor.hasError) {
        printf("LoadExtractedFrames failed: hasError=%d, error='%s'\n", 
               gVideoProcessor.hasError, gVideoProcessor.errorMessage);
        LogMessage("LOG LoadExtractedFrames failed - has error");
        return false;
    }
    
    *fps = gVideoProcessor.fps > 0 ? gVideoProcessor.fps : 30.0f;
    int totalExpectedFrames = gVideoProcessor.frameCount;
    
    // Compter les frames actuellement disponibles
    int availableFrames = 0;
    for (int i = 0; i < 15000; i++) {
        if (IsFrameAvailable(i)) {
            availableFrames = i + 1;
        } else {
            break;
        }
    }
    
    printf("Found %d available frames (expected: %d)\n", availableFrames, totalExpectedFrames);
    LogMessage("LOG Counted available frames");
    
    if (availableFrames <= 0) {
        LogMessage("LOG No frames available to load");
        return false;
    }
    
    // Allouer la mémoire pour les frames
    *sequence = (Image*)calloc(15000, sizeof(Image));
    if (*sequence == NULL) {
        LogMessage("LOG Failed to allocate memory for frames");
        return false;
    }
    
    // Initialiser le buffer de textures
    InitTextureBuffer(textureBuffer, 15000);
    
    LogMessage("LOG Memory allocated for frames and texture buffer");
    
    // Charger les frames disponibles et leurs textures
    int loadedFrames = 0;
    int failedTextures = 0;
    
    for (int i = 0; i < availableFrames; i++) {
        if (LoadSpecificFrame(i, &(*sequence)[i])) {
            // Charger la texture dans le buffer
            if (LoadTextureToBuffer(textureBuffer, &(*sequence)[i], i)) {
                loadedFrames++;
                if (loadedFrames == 1) {
                    LogMessage("LOG First frame loaded successfully");
                    printf("*** FIRST FRAME LOADED - READY FOR DISPLAY ***\n");
                }
                // Afficher le progrès tous les 50 frames
                if (loadedFrames % 50 == 0) {
                    printf("Progress: %d/%d frames loaded (%.1f%%)\n", 
                           loadedFrames, availableFrames, 
                           (float)loadedFrames / availableFrames * 100.0f);
                }
            } else {
                printf("Failed to load texture for frame %d\n", i);
                failedTextures++;
                UnloadImage((*sequence)[i]);
                
                if (failedTextures > 10) {
                    printf("Too many texture failures (%d), stopping load\n", failedTextures);
                    break;
                }
            }
        } else {
            printf("Failed to load frame %d\n", i);
            LogMessage("LOG Failed to load specific frame");
            break;
        }
    }
    
    *frameCount = loadedFrames;
    printf("Total frames loaded: %d/%d (%.1f%%) - Texture failures: %d\n", 
           loadedFrames, availableFrames, 
           (float)loadedFrames / availableFrames * 100.0f, failedTextures);
    LogMessage("LOG Frame loading completed");
    
    if (loadedFrames == 0) {
        free(*sequence);
        *sequence = NULL;
        FreeTextureBuffer(textureBuffer);
        LogMessage("LOG No frames loaded - cleaning up");
        return false;
    }
    
    LogMessage("LOG LoadExtractedFrames returning true");
    return true;
}

// Fonction pour vérifier et charger de nouvelles frames pendant la lecture
int CheckAndLoadNewFrames(Image** sequence, TextureBuffer* textureBuffer, int currentMaxFrames) {
    if (!sequence || !*sequence || !textureBuffer) return currentMaxFrames;
    
    int newMaxFrames = currentMaxFrames;
    int newFramesLoaded = 0;
    
    // Vérifier plus de frames à la fois pour un chargement plus rapide
    for (int i = currentMaxFrames; i < currentMaxFrames + 50; i++) {
        if (IsFrameAvailable(i)) {
            if (LoadSpecificFrame(i, &(*sequence)[i])) {
                // Charger la texture dans le buffer
                if (LoadTextureToBuffer(textureBuffer, &(*sequence)[i], i)) {
                    newMaxFrames = i + 1;
                    newFramesLoaded++;
                } else {
                    printf("Failed to load texture for new frame %d\n", i + 1);
                    UnloadImage((*sequence)[i]); // Décharger l'image si la texture a échoué
                    break;
                }
            } else {
                break;
            }
        } else {
            break;
        }
    }
    
    if (newFramesLoaded > 0) {
        printf("Loaded %d new frames: total now %d\n", newFramesLoaded, newMaxFrames);
    }
    
    return newMaxFrames;
}

// Fonction pour obtenir le statut du traitement vidéo (version synchrone)
void GetVideoProcessingStatus(bool* isProcessing, bool* isCompleted, bool* hasError, char* errorMsg) {
    *isProcessing = false; // En mode synchrone, le traitement n'est jamais en cours
    *isCompleted = gVideoProcessor.isCompleted;
    *hasError = gVideoProcessor.hasError;
    
    if (errorMsg && gVideoProcessor.hasError) {
        strcpy(errorMsg, gVideoProcessor.errorMessage);
    }
}

// Fonction pour nettoyer le processeur vidéo (version synchrone)
void CleanupVideoProcessor(void) {
    printf("Cleaning up video processor...\n");
    
    CleanupTempFrames();
    printf("Video processor cleanup completed\n");
}

// Fonction pour initialiser le buffer de textures
void InitTextureBuffer(TextureBuffer* buffer, int capacity) {
    buffer->textures = (Texture2D*)calloc(capacity, sizeof(Texture2D));
    buffer->count = 0;
    buffer->capacity = capacity;
    buffer->isAllocated = true;
    printf("Texture buffer initialized with capacity: %d\n", capacity);
}

// Fonction pour libérer le buffer de textures
void FreeTextureBuffer(TextureBuffer* buffer) {
    if (buffer->isAllocated && buffer->textures) {
        for (int i = 0; i < buffer->count; i++) {
            if (buffer->textures[i].id > 0) {
                UnloadTexture(buffer->textures[i]);
            }
        }
        free(buffer->textures);
        buffer->textures = NULL;
        buffer->count = 0;
        buffer->capacity = 0;
        buffer->isAllocated = false;
        printf("Texture buffer freed\n");
    }
}

// Fonction pour charger une texture dans le buffer
bool LoadTextureToBuffer(TextureBuffer* buffer, const Image* image, int index) {
    if (!buffer->isAllocated || !buffer->textures || index >= buffer->capacity) {
        printf("ERROR: Invalid texture buffer or index\n");
        return false;
    }
    
    // Si il y a déjà une texture à cet index, la décharger
    if (index < buffer->count && buffer->textures[index].id > 0) {
        UnloadTexture(buffer->textures[index]);
    }
    
    // Charger la nouvelle texture
    buffer->textures[index] = LoadTextureFromImage(*image);
    
    // Mettre à jour le count si nécessaire
    if (index >= buffer->count) {
        buffer->count = index + 1;
    }
    
    printf("Texture loaded to buffer at index %d (ID: %d)\n", index, buffer->textures[index].id);
    return buffer->textures[index].id > 0;
}

// Fonction pour obtenir une texture du buffer
Texture2D* GetTextureFromBuffer(TextureBuffer* buffer, int index) {
    if (!buffer->isAllocated || !buffer->textures || index >= buffer->count || index < 0) {
        return NULL;
    }
    
    if (buffer->textures[index].id > 0) {
        return &buffer->textures[index];
    }
    
    return NULL;
}

// Fonction pour forcer le chargement de toutes les frames disponibles
void LoadAllAvailableFrames(Image** sequence, TextureBuffer* textureBuffer, int* totalFrames) {
    if (!sequence || !*sequence || !textureBuffer) return;
    
    printf("=== LOADING ALL AVAILABLE FRAMES ===\n");
    
    int currentMax = *totalFrames;
    int maxAvailable = 0;
    
    // Compter toutes les frames disponibles
    for (int i = 0; i < 15000; i++) {
        if (IsFrameAvailable(i)) {
            maxAvailable = i + 1;
        } else {
            break;
        }
    }
    
    printf("Found %d total available frames, currently loaded: %d\n", maxAvailable, currentMax);
    
    if (maxAvailable <= currentMax) {
        printf("All frames already loaded\n");
        return;
    }
    
    int loadedFrames = 0;
    int failedFrames = 0;
    
    // Charger toutes les frames manquantes
    for (int i = currentMax; i < maxAvailable; i++) {
        if (LoadSpecificFrame(i, &(*sequence)[i])) {
            if (LoadTextureToBuffer(textureBuffer, &(*sequence)[i], i)) {
                loadedFrames++;
                if (loadedFrames % 100 == 0) {
                    printf("Batch loading progress: %d/%d frames\n", 
                           currentMax + loadedFrames, maxAvailable);
                }
            } else {
                failedFrames++;
                UnloadImage((*sequence)[i]);
                if (failedFrames > 20) {
                    printf("Too many texture failures, stopping batch load\n");
                    break;
                }
            }
        } else {
            failedFrames++;
            if (failedFrames > 20) {
                printf("Too many load failures, stopping batch load\n");
                break;
            }
        }
    }
    
    *totalFrames = currentMax + loadedFrames;
    printf("Batch load completed: %d new frames loaded (total: %d)\n", 
           loadedFrames, *totalFrames);
}


int main(void)
{
    InitLogger();
    LogMessage("LOG Program Start");
    
    // Initialiser le processeur vidéo
    InitVideoProcessor();
    LogMessage("LOG Video processor initialized");
    
    const int screenWidth = 1080;
    const int screenHeight = 720;
    InitWindow(screenWidth, screenHeight, "Drag & Drop + Shader Zone");
    LogMessage("LOG Window Initialized");

    // Découvrir les shaders disponibles
    DiscoverShaders();
    LogMessage("LOG Shaders discovered");

    const char *shaderPath = GetSelectedShaderPath();
    ShaderState shaderState = {0};
    Shader shader = LoadShaderSafe(0, shaderPath, &shaderState);
    LogMessage("LOG Shader loaded with error checking");

    time_t lastModTime = My_GetFileModTime(shaderPath);
    LogMessage("LOG File mod time retrieved");

    Texture2D originalImageTex = {0}; // Image originale non modifiée
    LogMessage("LOG Image textures initialized");
    
    // Variables pour la gestion de l'image redimensionnée
    Rectangle imageRect = {0}; // Rectangle de destination pour l'image
    Rectangle sourceRect = {0}; // Rectangle source de l'image
    Vector2 imageScale = {1.0f, 1.0f}; // Facteur d'échelle de l'image

    // Variables pour la gestion des séquences/vidéos
    bool isSequence = false;
    bool isPlaying = false;
    int currentFrame = 0;
    int totalFrames = 0;
    float frameTime = 0.0f;
    float frameRate = 30.0f; // FPS par défaut
    Image* frameSequence = NULL;
    TextureBuffer videoTextureBuffer = {0}; // Buffer pour les textures vidéo
    char loadedFilePath[512] = {0};
    
    // Variables pour le traitement vidéo (simplifiées pour le mode synchrone)
    // char videoErrorMessage[256] = {0}; // Removed - unused variable
    
    // Variables pour les contrôles UI
    Rectangle playPauseButton = {10, 550, 80, 30};
    Rectangle prevButton = {100, 550, 40, 30};
    Rectangle nextButton = {150, 550, 40, 30};
    Rectangle frameSlider = {10, 590, 180, 20};
    Rectangle reloadButton = {10, 620, 80, 30};
    Rectangle loadAllButton = {100, 620, 90, 30};
    float sliderValue = 0.0f;
    
    // Variables pour le dropdown des shaders
    Rectangle shaderDropdown = {10, 50, 180, 25};
    Rectangle shaderReloadButton = {10, 80, 100, 25};
    Rectangle shaderDropdownList = {10, 75, 180, 200}; // Position de la liste déroulante

    // Variables pour le système de verrouillage de la souris
    bool mouseLocked = false;
    Vector2 lockedMouseInImage = {0, 0};
    bool wasSpacePressed = false;
    float radius = 50.0f;
    float power  = 1.0f;
    unsigned int powerIndex = 5;
    UNUSED powerIndex;
    SetTargetFPS(60);
    LogMessage("LOG FPS set to 60");

    while (!WindowShouldClose())
    {
        
        // Vérifier modification toutes les 60 frames
        static int frameCounter = 0;
        
        // Gestion drag & drop
        if (IsFileDropped())
        {
            LogMessage("LOG File dropped");
            FilePathList files = LoadDroppedFiles();
            if (files.count > 0)
            {
                LogMessage("LOG Processing dropped files");
                // Vérifier si le fichier est une image/vidéo supportée
                if (IsFileExtension(files.paths[0], ".png") || 
                    IsFileExtension(files.paths[0], ".jpg") || 
                    IsFileExtension(files.paths[0], ".jpeg") || 
                    IsFileExtension(files.paths[0], ".bmp") || 
                    IsFileExtension(files.paths[0], ".tga") || 
                    IsFileExtension(files.paths[0], ".gif") || 
                    IsFileExtension(files.paths[0], ".hdr") || 
                    IsFileExtension(files.paths[0], ".pic") || 
                    IsFileExtension(files.paths[0], ".psd"))
                {
                    LogMessage("LOG Image file detected");
                    
                    // Nettoyer les données précédentes
                    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
                    if (frameSequence != NULL) {
                        for (int i = 0; i < totalFrames; i++) {
                            UnloadImage(frameSequence[i]);
                        }
                        free(frameSequence);
                        frameSequence = NULL;
                    }
                    // Nettoyer le buffer de textures vidéo
                    FreeTextureBuffer(&videoTextureBuffer);
                    
                    // Charger la nouvelle image
                    Image img = LoadImage(files.paths[0]);
                    originalImageTex = LoadTextureFromImage(img);
                    UnloadImage(img);
                    
                    // Réinitialiser les variables de séquence
                    isSequence = false;
                    isPlaying = false;
                    currentFrame = 0;
                    totalFrames = 1;
                    frameTime = 0.0f;
                    sliderValue = 0.0f;
                    
                    strcpy(loadedFilePath, files.paths[0]);
                    
                    frameCounter = -1;
                    LogMessage("LOG Image loaded and texture updated");
                    
                    // Calculer les dimensions pour adapter l'image à la fenêtre
                    float availableWidth = screenWidth - 200; // Largeur disponible (écran - panel)
                    float availableHeight = screenHeight;
                    
                    float scaleX = availableWidth / originalImageTex.width;
                    float scaleY = availableHeight / originalImageTex.height;
                    float scale = fminf(scaleX, scaleY); // Garder les proportions
                    
                    imageScale.x = scale;
                    imageScale.y = scale;
                    
                    // Centrer l'image dans la zone disponible
                    float scaledWidth = originalImageTex.width * scale;
                    float scaledHeight = originalImageTex.height * scale;
                    
                    imageRect.x = 200 + (availableWidth - scaledWidth) / 2;
                    imageRect.y = (availableHeight - scaledHeight) / 2;
                    imageRect.width = scaledWidth;
                    imageRect.height = scaledHeight;
                    
                    sourceRect.x = 0;
                    sourceRect.y = 0;
                    sourceRect.width = originalImageTex.width;
                    sourceRect.height = originalImageTex.height;
                    
                    LogMessage("LOG Image dimensions calculated");
                }
                else if (IsFileExtension(files.paths[0], ".mp4") || 
                         IsFileExtension(files.paths[0], ".mov") || 
                         IsFileExtension(files.paths[0], ".avi") || 
                         IsFileExtension(files.paths[0], ".mkv") || 
                         IsFileExtension(files.paths[0], ".webm"))
                {
                    LogMessage("LOG Video file detected");
                    
                    // Nettoyer les données précédentes
                    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
                    if (frameSequence != NULL) {
                        for (int i = 0; i < totalFrames; i++) {
                            UnloadImage(frameSequence[i]);
                        }
                        free(frameSequence);
                        frameSequence = NULL;
                    }
                    // Nettoyer le buffer de textures vidéo
                    FreeTextureBuffer(&videoTextureBuffer);
                    
                    // Démarrer le traitement vidéo synchrone
                    if (StartVideoProcessing(files.paths[0])) {
                        strcpy(loadedFilePath, files.paths[0]);
                        
                        // En mode synchrone, charger immédiatement les frames
                        if (LoadExtractedFrames(&frameSequence, &videoTextureBuffer, &totalFrames, &frameRate)) {
                            LogMessage("LOG Video frames loaded successfully");
                            printf("*** VIDEO FRAMES LOADED: %d frames at %.2f FPS ***\n", totalFrames, frameRate);
                            
                            isSequence = true;
                            currentFrame = 0;
                            frameTime = 0.0f;
                            sliderValue = 0.0f;
                            
                            // Charger la première frame
                            if (frameSequence != NULL && totalFrames > 0) {
                                Texture2D* firstTexture = GetTextureFromBuffer(&videoTextureBuffer, 0);
                                if (firstTexture != NULL) {
                                    originalImageTex = *firstTexture;
                                } else {
                                    originalImageTex = LoadTextureFromImage(frameSequence[0]);
                                }
                                
                                // Calculer les dimensions pour adapter l'image à la fenêtre
                                float availableWidth = screenWidth - 200;
                                float availableHeight = screenHeight;
                                
                                float scaleX = availableWidth / originalImageTex.width;
                                float scaleY = availableHeight / originalImageTex.height;
                                float scale = fminf(scaleX, scaleY);
                                
                                imageScale.x = scale;
                                imageScale.y = scale;
                                
                                float scaledWidth = originalImageTex.width * scale;
                                float scaledHeight = originalImageTex.height * scale;
                                
                                imageRect.x = 200 + (availableWidth - scaledWidth) / 2;
                                imageRect.y = (availableHeight - scaledHeight) / 2;
                                imageRect.width = scaledWidth;
                                imageRect.height = scaledHeight;
                                
                                sourceRect.x = 0;
                                sourceRect.y = 0;
                                sourceRect.width = originalImageTex.width;
                                sourceRect.height = originalImageTex.height;
                                
                                printf("*** VIDEO READY FOR PLAYBACK ***\n");
                                LogMessage("LOG Video ready for playback");
                            }
                        } else {
                            LogMessage("LOG Failed to load video frames");
                            printf("ERROR: Failed to load video frames\n");
                        }
                    } else {
                        LogMessage("LOG Failed to start video processing");
                        printf("ERROR: Failed to process video\n");
                    }
                }
                else
                {
                    LogMessage("LOG Non-media file dropped");
                }
            }
            UnloadDroppedFiles(files);
        }
        
        frameCounter++;
        if (frameCounter >= 60)
        {
            frameCounter = 0;
            const char* currentShaderPath = GetSelectedShaderPath();
            time_t modTime = My_GetFileModTime(currentShaderPath);
            if (modTime != lastModTime)
            {
                lastModTime = modTime;
                UnloadShader(shader);
                shader = LoadShaderSafe(0, currentShaderPath, &shaderState);
                LogMessage("LOG Shader reloaded due to file modification");
                TraceLog(LOG_INFO, "Shader reloaded due to file modification.");
            }
        }

        // Gestion du verrouillage de la souris avec la touche espace
        bool spacePressed = IsKeyPressed(KEY_SPACE);
        if (spacePressed && !wasSpacePressed) {
            mouseLocked = !mouseLocked;
            if (mouseLocked) {
                // Bloquer la position actuelle de la souris
                Vector2 mouse = GetMousePosition();
                if (originalImageTex.id > 0 && CheckCollisionPointRec(mouse, imageRect)) {
                    lockedMouseInImage.x = (mouse.x - imageRect.x) / imageScale.x;
                    lockedMouseInImage.y = (mouse.y - imageRect.y) / imageScale.y;
                    LogMessage("LOG Mouse position locked");
                } else {
                    mouseLocked = false; // Ne pas verrouiller si la souris n'est pas sur l'image
                }
            } else {
                LogMessage("LOG Mouse position unlocked");
            }
        }
        wasSpacePressed = spacePressed;

        // En mode synchrone, pas besoin de vérifier le statut du traitement vidéo
        // car tout est fait de manière immédiate

        // Mise à jour des séquences/animations avec gestion d'erreur
        if (isSequence && isPlaying) {
            frameTime += GetFrameTime();
            if (frameTime >= 1.0f / frameRate) {
                frameTime = 0.0f;
                int nextFrame = currentFrame + 1;
                
                // Vérifier si la frame suivante est disponible
                if (nextFrame < totalFrames && frameSequence[nextFrame].data != NULL) {
                    currentFrame = nextFrame;
                    
                    // Mettre à jour la texture avec la frame actuelle depuis le buffer
                    Texture2D* nextTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                    if (nextTexture != NULL) {
                        originalImageTex = *nextTexture;
                        LogMessage("LOG Frame updated from texture buffer");
                    } else {
                        // Fallback: créer la texture normalement
                        UnloadTexture(originalImageTex);
                        originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        LogMessage("LOG Frame updated (fallback - texture creation)");
                    }
                    
                    // Mettre à jour le slider
                    sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                } else {
                    // Frame suivante pas disponible
                    if (nextFrame >= totalFrames) {
                        // Fin de séquence, recommencer au début
                        currentFrame = 0;
                        if (frameSequence != NULL && frameSequence[0].data != NULL) {
                            Texture2D* firstTexture = GetTextureFromBuffer(&videoTextureBuffer, 0);
                            if (firstTexture != NULL) {
                                originalImageTex = *firstTexture;
                            } else {
                                UnloadTexture(originalImageTex);
                                originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                            }
                        }
                        sliderValue = 0.0f;
                    } else {
                        // Frame suivante pas encore chargée, arrêter la lecture
                        isPlaying = false;
                        printf("Playback paused: frame %d not ready\n", nextFrame);
                        LogMessage("LOG Playback paused - next frame not ready");
                        
                        // Essayer de charger la frame manquante
                        if (LoadSpecificFrame(nextFrame, &frameSequence[nextFrame])) {
                            // Charger la texture dans le buffer
                            if (LoadTextureToBuffer(&videoTextureBuffer, &frameSequence[nextFrame], nextFrame)) {
                                printf("Late frame %d loaded, resuming playback\n", nextFrame);
                                isPlaying = true;
                            }
                        }
                    }
                }
            }
        }

        // Panel gauche (étendu pour inclure les boutons)
        Rectangle panel = {0, 0, 200, screenHeight};

        // Gestion clic + shader (maintenir le clic pour appliquer continuellement)
        Vector2 mouse = GetMousePosition();
        Vector2 effectiveMouseInImage = mouseLocked ? lockedMouseInImage : (Vector2){0, 0};
        
        bool applyShader = false;
        
        // Vérifier si la souris est sur un bouton UI
        bool mouseOnUI = false;
        // Toujours vérifier les boutons de shader
        bool mouseOnShaderUI = CheckCollisionPointRec(mouse, shaderDropdown) ||
                              CheckCollisionPointRec(mouse, shaderReloadButton) ||
                              (gShaderManager.dropdownActive && CheckCollisionPointRec(mouse, shaderDropdownList));
        
        if (isSequence) {
            mouseOnUI = CheckCollisionPointRec(mouse, playPauseButton) ||
                       CheckCollisionPointRec(mouse, prevButton) ||
                       CheckCollisionPointRec(mouse, nextButton) ||
                       CheckCollisionPointRec(mouse, frameSlider) ||
                       CheckCollisionPointRec(mouse, reloadButton) ||
                       CheckCollisionPointRec(mouse, loadAllButton);
        }
        
        // Combiner tous les UI
        mouseOnUI = mouseOnUI || mouseOnShaderUI;
        
        if (mouseLocked) {
            // Si la souris est verrouillée, appliquer le shader en permanence à la position verrouillée
            applyShader = originalImageTex.id > 0;
        } else {
            // Comportement normal : clic gauche pour appliquer le shader
            applyShader = (IsMouseButtonDown(MOUSE_LEFT_BUTTON) || IsMouseButtonPressed(MOUSE_LEFT_BUTTON)) && 
                          !CheckCollisionPointRec(mouse, panel) && 
                          !mouseOnUI &&
                          originalImageTex.id > 0 && 
                          CheckCollisionPointRec(mouse, imageRect);
        }
        
        if (applyShader) {
            if (mouseLocked) {
                LogMessage("LOG Shader applied - mouse locked");
            } else {
                LogMessage("LOG Mouse click - applying shader");
            }
        }

        BeginDrawing();
            ClearBackground(RAYWHITE);
            unsigned int textHeight = 10; 
            // Draw panel
            DrawRectangleRec(panel, LIGHTGRAY);
            
            // Sélecteur de shader en haut du panel
            Vector2 mousePos = GetMousePosition();
            DrawText("Shader:", 10, 15, 14, BLACK);
            
            // Dropdown des shaders
            Color dropdownColor = DARKGRAY;
            if (CheckCollisionPointRec(mousePos, shaderDropdown)) {
                dropdownColor = ColorBrightness(dropdownColor, 0.3f);
            }
            DrawRectangleRec(shaderDropdown, dropdownColor);
            DrawRectangleLinesEx(shaderDropdown, 2, BLACK);
            
            // Texte du shader sélectionné
            const char* selectedShaderName = "Default";
            if (gShaderManager.selectedIndex >= 0 && gShaderManager.selectedIndex < gShaderManager.count) {
                selectedShaderName = gShaderManager.names[gShaderManager.selectedIndex];
            }
            DrawText(selectedShaderName, shaderDropdown.x + 5, shaderDropdown.y + 5, 12, BLACK);
            DrawText("v", shaderDropdown.x + shaderDropdown.width - 15, shaderDropdown.y + 5, 12, BLACK);
            
            // Bouton reload des shaders
            Color reloadShaderColor = SKYBLUE;
            if (CheckCollisionPointRec(mousePos, shaderReloadButton)) {
                reloadShaderColor = ColorBrightness(reloadShaderColor, 0.2f);
            }
            DrawRectangleRec(shaderReloadButton, reloadShaderColor);
            DrawRectangleLinesEx(shaderReloadButton, 2, BLACK);
            DrawText("Reload Shaders", shaderReloadButton.x + 5, shaderReloadButton.y + 5, 10, BLACK);
            
            // Liste déroulante des shaders si active
            if (gShaderManager.dropdownActive) {
                float itemHeight = 20;
                float maxHeight = fminf(gShaderManager.count * itemHeight, shaderDropdownList.height);
                Rectangle listRect = {shaderDropdownList.x, shaderDropdownList.y, shaderDropdownList.width, maxHeight};
                
                DrawRectangleRec(listRect, LIGHTGRAY);
                DrawRectangleLinesEx(listRect, 2, BLACK);
                
                for (int i = 0; i < gShaderManager.count; i++) {
                    Rectangle itemRect = {listRect.x, listRect.y + i * itemHeight, listRect.width, itemHeight};
                    
                    Color itemColor = (i == gShaderManager.selectedIndex) ? BLUE : LIGHTGRAY;
                    if (CheckCollisionPointRec(mousePos, itemRect)) {
                        itemColor = ColorBrightness(itemColor, 0.2f);
                    }
                    
                    DrawRectangleRec(itemRect, itemColor);
                    DrawRectangleLinesEx(itemRect, 1, DARKGRAY);
                    DrawText(gShaderManager.names[i], itemRect.x + 5, itemRect.y + 2, 12, BLACK);
                }
            }
            textHeight += 100;
            //unsigned int textHeight = 110; // Décaler le texte après les contrôles shader
            DrawText("Contrôles:", 10, textHeight+=25, 16, BLACK);
            DrawText("Rayon : Haut/Bas", 10, textHeight+=25, 14, BLACK);
            DrawText(TextFormat("Rayon: %.0f", radius), 10, textHeight+=25, 14, BLACK);
            DrawText("Puissance : +/-", 10, textHeight+=25, 14, BLACK);
            DrawText(TextFormat("Puissance: %.0f", power), 10, textHeight+=25, 14, BLACK);
            DrawText("Espace: Bloquer souris", 10, textHeight+=25, 12, BLACK);
            
            if (mouseLocked) {
                DrawText("SOURIS BLOQUÉE", 10, textHeight+=25, 12, RED);
                DrawText(TextFormat("Pos: %.0f,%.0f", lockedMouseInImage.x, lockedMouseInImage.y), 10, textHeight+=15, 10, RED);
            }
            
            // Affichage des erreurs de shader
            if (shaderState.hasError) {
                DrawText("ERREUR SHADER:", 10, textHeight+=25, 12, RED);
                DrawText("CRITIQUE - Arrêt", 10, textHeight+=15, 10, RED);
                DrawText(shaderState.errorMessage, 10, textHeight+=15, 8, RED);
            } else if (shaderState.isDefaultShader) {
                DrawText("SHADER PAR DÉFAUT:", 10, textHeight+=25, 12, ORANGE);
                DrawText("Corrigez effect.fs", 10, textHeight+=15, 10, ORANGE);
                DrawText("et sauvegardez", 10, textHeight+=15, 10, ORANGE);
            }
            
            // Affichage du statut de traitement vidéo (simplifié)
            if (gVideoProcessor.hasError) {
                DrawText("ERREUR VIDÉO:", 10, textHeight+=25, 12, RED);
                DrawText(gVideoProcessor.errorMessage, 10, textHeight+=15, 10, RED);
            }
            
            // Contrôles de lecture pour les séquences
            if (isSequence) {
                textHeight += 20;
                DrawText("Lecture:", 10, textHeight+=20, 14, BLACK);
                DrawText("P: Play/Pause", 10, textHeight+=15, 10, DARKGRAY);
                DrawText("←→: Frame prec/suiv", 10, textHeight+=15, 10, DARKGRAY);
                
                // Affichage des informations de frame
                DrawText(TextFormat("Frame: %d/%d", currentFrame + 1, totalFrames), 10, textHeight+=20, 12, BLACK);
                DrawText(TextFormat("FPS: %.1f", frameRate), 10, textHeight+=15, 12, BLACK);
                
                // Afficher le statut de chargement (simplifié)
                DrawText("Chargement terminé", 10, textHeight+=15, 10, GREEN);
            }
            
            if (originalImageTex.id > 0) {
                textHeight += 20;
                DrawText("Image chargée:", 10, textHeight+=25, 14, BLACK);
                DrawText(TextFormat("Taille: %dx%d", originalImageTex.width, originalImageTex.height), 10, textHeight+=25, 12, BLACK);
                DrawText(TextFormat("Échelle: %.2f", imageScale.x), 10, textHeight+=25, 12, BLACK);
                DrawText("Maintenir clic gauche", 10, textHeight+=25, 12, BLACK);
                DrawText("pour appliquer shader", 10, textHeight+=25, 12, BLACK);

                // Afficher les coordonnées de la souris si dans l'image
                if (!mouseLocked && CheckCollisionPointRec(mouse, imageRect)) {
                    Vector2 mouseInImage;
                    mouseInImage.x = (mouse.x - imageRect.x) / imageScale.x;
                    mouseInImage.y = (mouse.y - imageRect.y) / imageScale.y;
                    DrawText(TextFormat("Souris: %.0f,%.0f", mouseInImage.x, mouseInImage.y), 10, textHeight+=25, 12, DARKBLUE);
                }
            } else {
                DrawText("Glissez une image/vidéo", 10, textHeight+=25, 14, BLACK);
                DrawText("(PNG, JPG, BMP, GIF,", 10, textHeight+=25, 14, BLACK);
                DrawText("MP4, MOV, AVI, etc.)", 10, textHeight+=25, 14, BLACK);
                DrawText("dans la zone de droite", 10, textHeight+=25, 14, BLACK);
            }

            // Dessiner les boutons UI en bas du panel (après tout le texte)
            if (isSequence) {
                Vector2 mousePos = GetMousePosition();
                
                // Bouton Play/Pause
                Color playButtonColor = isPlaying ? GREEN : RED;
                if (CheckCollisionPointRec(mousePos, playPauseButton)) {
                    playButtonColor = ColorBrightness(playButtonColor, 0.2f); // Éclaircir au survol
                }
                DrawRectangleRec(playPauseButton, playButtonColor);
                DrawRectangleLinesEx(playPauseButton, 2, BLACK);
                DrawText(isPlaying ? "Pause" : "Play", playPauseButton.x + 10, playPauseButton.y + 8, 14, BLACK);
                
                // Boutons Previous/Next
                Color prevButtonColor = SKYBLUE;
                if (CheckCollisionPointRec(mousePos, prevButton)) {
                    prevButtonColor = ColorBrightness(prevButtonColor, 0.2f);
                }
                DrawRectangleRec(prevButton, prevButtonColor);
                DrawRectangleLinesEx(prevButton, 2, BLACK);
                DrawText("<", prevButton.x + 15, prevButton.y + 8, 14, BLACK);
                
                Color nextButtonColor = SKYBLUE;
                if (CheckCollisionPointRec(mousePos, nextButton)) {
                    nextButtonColor = ColorBrightness(nextButtonColor, 0.2f);
                }
                DrawRectangleRec(nextButton, nextButtonColor);
                DrawRectangleLinesEx(nextButton, 2, BLACK);
                DrawText(">", nextButton.x + 15, nextButton.y + 8, 14, BLACK);
                
                // Slider de progression
                Color sliderColor = DARKGRAY;
                if (CheckCollisionPointRec(mousePos, frameSlider)) {
                    sliderColor = ColorBrightness(sliderColor, 0.3f);
                }
                DrawRectangleRec(frameSlider, sliderColor);
                DrawRectangleLinesEx(frameSlider, 2, BLACK);
                float sliderPos = frameSlider.x + (sliderValue * frameSlider.width);
                DrawRectangle(sliderPos - 5, frameSlider.y - 2, 10, frameSlider.height + 4, BLUE);
                
                // Bouton Reload
                Color reloadButtonColor = ORANGE;
                if (CheckCollisionPointRec(mousePos, reloadButton)) {
                    reloadButtonColor = ColorBrightness(reloadButtonColor, 0.2f);
                }
                DrawRectangleRec(reloadButton, reloadButtonColor);
                DrawRectangleLinesEx(reloadButton, 2, BLACK);
                DrawText("Reload", reloadButton.x + 10, reloadButton.y + 8, 14, BLACK);
                
                // Bouton Load All
                Color loadAllButtonColor = YELLOW;
                if (CheckCollisionPointRec(mousePos, loadAllButton)) {
                    loadAllButtonColor = ColorBrightness(loadAllButtonColor, 0.2f);
                }
                DrawRectangleRec(loadAllButton, loadAllButtonColor);
                DrawRectangleLinesEx(loadAllButton, 2, BLACK);
                DrawText("Load All", loadAllButton.x + 5, loadAllButton.y + 8, 14, BLACK);
            }
            
            // Modifier rayon
            if (IsKeyDown(KEY_UP)) {
                radius += 1.0f;
                LogMessage("LOG Radius increased");
            }
            if (IsKeyDown(KEY_DOWN)) {
                radius -= 1.0f;
                LogMessage("LOG Radius decreased");
            }
            // Modifier puissance
            if (IsKeyDown(KEY_KP_ADD)) {
                power += 1.0f;
                LogMessage("LOG Power increased");
            }
            if (IsKeyDown(KEY_KP_SUBTRACT)) {
                power -= 1.0f;
                LogMessage("LOG Power decreased");
            }
            
            // Contrôles clavier pour les séquences
            if (isSequence) {
                if (IsKeyPressed(KEY_P)) {
                    // Vérifier si on peut lire la frame suivante
                    if (!isPlaying) {
                        int nextFrame = (currentFrame + 1) % totalFrames;
                        if (frameSequence[nextFrame].data != NULL) {
                            isPlaying = true;
                            LogMessage("LOG Playback started (keyboard)");
                        } else {
                            printf("Cannot start playback: next frame not ready\n");
                        }
                    } else {
                        isPlaying = false;
                        LogMessage("LOG Playback paused (keyboard)");
                    }
                }
                if (IsKeyPressed(KEY_LEFT)) {
                    int prevFrame = (currentFrame - 1 + totalFrames) % totalFrames;
                    if (frameSequence != NULL && frameSequence[prevFrame].data != NULL) {
                        currentFrame = prevFrame;
                        Texture2D* prevTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (prevTexture != NULL) {
                            originalImageTex = *prevTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Previous frame (keyboard)");
                    }
                }
                if (IsKeyPressed(KEY_RIGHT)) {
                    int nextFrame = (currentFrame + 1) % totalFrames;
                    if (frameSequence != NULL && frameSequence[nextFrame].data != NULL) {
                        currentFrame = nextFrame;
                        Texture2D* nextTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (nextTexture != NULL) {
                            originalImageTex = *nextTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Next frame (keyboard)");
                    } else {
                        printf("Next frame not available yet\n");
                    }
                }
            }

            // Gestion des clics sur les boutons UI
            if (isSequence) {
                Vector2 mousePos = GetMousePosition();
                
                // Clic sur le bouton Play/Pause
                if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, playPauseButton)) {
                    if (!isPlaying) {
                        int nextFrame = (currentFrame + 1) % totalFrames;
                        if (frameSequence[nextFrame].data != NULL) {
                            isPlaying = true;
                            LogMessage("LOG Playback started (button)");
                        } else {
                            printf("Cannot start playback: next frame not ready\n");
                        }
                    } else {
                        isPlaying = false;
                        LogMessage("LOG Playback paused (button)");
                    }
                }
                
                // Clic sur le bouton Previous
                if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, prevButton)) {
                    int prevFrame = (currentFrame - 1 + totalFrames) % totalFrames;
                    if (frameSequence != NULL && frameSequence[prevFrame].data != NULL) {
                        currentFrame = prevFrame;
                        Texture2D* prevTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (prevTexture != NULL) {
                            originalImageTex = *prevTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Previous frame (button)");
                    }
                }
                
                // Clic sur le bouton Next
                if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, nextButton)) {
                    int nextFrame = (currentFrame + 1) % totalFrames;
                    if (frameSequence != NULL && frameSequence[nextFrame].data != NULL) {
                        currentFrame = nextFrame;
                        Texture2D* nextTexture = GetTextureFromBuffer(&videoTextureBuffer, currentFrame);
                        if (nextTexture != NULL) {
                            originalImageTex = *nextTexture;
                        } else {
                            UnloadTexture(originalImageTex);
                            originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                        }
                        sliderValue = totalFrames > 1 ? (float)currentFrame / (float)(totalFrames - 1) : 0.0f;
                        LogMessage("LOG Next frame (button)");
                    } else {
                        printf("Next frame not available yet\n");
                    }
                }
                
                // Clic sur le bouton Reload
                if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, reloadButton)) {
                    printf("Reloading video...\n");
                    currentFrame = 0;
                    totalFrames = 0;
                    isPlaying = false;
                    
                    // Libérer les textures existantes
                    FreeTextureBuffer(&videoTextureBuffer);
                    
                    // Réinitialiser et recharger
                    InitTextureBuffer(&videoTextureBuffer, 15000);
                    LoadExtractedFrames(&frameSequence, &videoTextureBuffer, &totalFrames, &frameRate);
                    
                    printf("Video reloaded successfully with %d frames\n", totalFrames);
                    LogMessage("LOG Video reloaded");
                }
                
                // Clic sur le bouton Load All
                if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, loadAllButton)) {
                    printf("Force loading all available frames...\n");
                    LoadAllAvailableFrames(&frameSequence, &videoTextureBuffer, &totalFrames);
                    LogMessage("LOG All frames loaded");
                }
            }

            // Gestion des clics sur le dropdown des shaders
            if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON)) {
                Vector2 mousePos = GetMousePosition();
                
                // Clic sur le dropdown principal
                if (CheckCollisionPointRec(mousePos, shaderDropdown)) {
                    gShaderManager.dropdownActive = !gShaderManager.dropdownActive;
                    LogMessage("LOG Shader dropdown toggled");
                }
                // Clic sur le bouton reload des shaders
                else if (CheckCollisionPointRec(mousePos, shaderReloadButton)) {
                    printf("Reloading shader list...\n");
                    DiscoverShaders();
                    
                    // Recharger le shader actuel
                    const char* currentShaderPath = GetSelectedShaderPath();
                    UnloadShader(shader);
                    shader = LoadShaderSafe(0, currentShaderPath, &shaderState);
                    lastModTime = My_GetFileModTime(currentShaderPath);
                    
                    printf("Shader list reloaded, found %d shaders\n", gShaderManager.count);
                    LogMessage("LOG Shader list reloaded");
                }
                // Clic sur un item de la liste déroulante
                else if (gShaderManager.dropdownActive && CheckCollisionPointRec(mousePos, shaderDropdownList)) {
                    float itemHeight = 20;
                    int clickedIndex = (int)((mousePos.y - shaderDropdownList.y) / itemHeight);
                    
                    if (clickedIndex >= 0 && clickedIndex < gShaderManager.count) {
                        gShaderManager.selectedIndex = clickedIndex;
                        gShaderManager.dropdownActive = false;
                        
                        // Charger le nouveau shader
                        const char* newShaderPath = GetSelectedShaderPath();
                        printf("Switching to shader: %s\n", newShaderPath);
                        
                        UnloadShader(shader);
                        shader = LoadShaderSafe(0, newShaderPath, &shaderState);
                        lastModTime = My_GetFileModTime(newShaderPath);
                        
                        LogMessage("LOG Shader switched");
                    }
                }
                // Clic ailleurs ferme la liste déroulante
                else if (gShaderManager.dropdownActive) {
                    gShaderManager.dropdownActive = false;
                    LogMessage("LOG Shader dropdown closed");
                }
            }

            // Draw image with shader if needed
            if (originalImageTex.id > 0)
            {
                LogMessage("LOG Drawing imagef");
                
                // Vérifier si on peut appliquer le shader
                if (applyShader && !shaderState.hasError && shader.id > 0)
                {
                    // Convertir les coordonnées de la souris vers les coordonnées de l'image
                    Vector2 mouseInImage;
                    if (mouseLocked) {
                        mouseInImage = effectiveMouseInImage;
                    } else {
                        mouseInImage.x = (mouse.x - imageRect.x) / imageScale.x;
                        mouseInImage.y = (mouse.y - imageRect.y) / imageScale.y;
                    }
                    
                    // S'assurer que les coordonnées sont dans les limites de l'image
                    mouseInImage.x = fmaxf(0, fminf(mouseInImage.x, originalImageTex.width));
                    mouseInImage.y = fmaxf(0, fminf(mouseInImage.y, originalImageTex.height));

                    // Obtenir le temps en secondes depuis le lancement du programme
                    float timeSeconds = (float)GetTime();
                    
                    // Debug: afficher le temps toutes les 120 frames (2 secondes)
                    static int timeDebugCounter = 0;
                    timeDebugCounter++;
                    if (timeDebugCounter % 120 == 0) {
                        printf("Shader time: %.2f seconds\n", timeSeconds);
                    }
                    
                    // Appliquer le shader directement lors de l'affichage
                    BeginShaderMode(shader);
                        // Envoyer toutes les variables uniform au shader
                        int timeLocation = GetShaderLocation(shader, "time");
                        if (timeLocation != -1) {
                            SetShaderValue(shader, timeLocation, &timeSeconds, SHADER_UNIFORM_FLOAT);
                        } else {
                            if (timeDebugCounter % 300 == 0) { // Toutes les 5 secondes
                                printf("WARNING: Shader uniform 'time' not found\n");
                            }
                        }
                        SetShaderValue(shader, GetShaderLocation(shader, "mousePos"), 
                                     (float[2]){mouseInImage.x, mouseInImage.y}, SHADER_UNIFORM_VEC2);
                        SetShaderValue(shader, GetShaderLocation(shader, "radius"), &radius, SHADER_UNIFORM_FLOAT);
                        SetShaderValue(shader, GetShaderLocation(shader, "power"), &power, SHADER_UNIFORM_FLOAT);
                        SetShaderValue(shader, GetShaderLocation(shader, "resolution"), 
                                     (float[2]){(float)originalImageTex.width, (float)originalImageTex.height}, SHADER_UNIFORM_VEC2);

                        DrawTexturePro(originalImageTex, sourceRect, imageRect, (Vector2){0, 0}, 0.0f, WHITE);
                    EndShaderMode();
                    LogMessage("LOG Shader applied to image");
                }
                else
                {
                    // Afficher l'image originale sans shader
                    DrawTexturePro(originalImageTex, sourceRect, imageRect, (Vector2){0, 0}, 0.0f, WHITE);
                    
                    // Si le shader a une erreur, afficher un message sur l'image
                    if (shaderState.hasError) {
                        DrawText("SHADER INVALIDE", imageRect.x + 10, imageRect.y + 10, 20, RED);
                        DrawText("Corrigez effect.fs", imageRect.x + 10, imageRect.y + 35, 16, RED);
                    } else if (shaderState.isDefaultShader) {
                        DrawText("SHADER PAR DÉFAUT", imageRect.x + 10, imageRect.y + 10, 16, ORANGE);
                    }
                }
                
                // Dessiner un contour autour de l'image pour debug
                DrawRectangleLinesEx(imageRect, 2, DARKGRAY);
            }
            else
            {
                // Afficher un message si aucune image n'est chargée
                DrawText("Glissez une image/vidéo ici", 250, 300, 20, DARKGRAY);
                DrawText("Formats supportés:", 250, 330, 16, GRAY);
                DrawText("Images: PNG, JPG, BMP, TGA, GIF, HDR, PIC, PSD", 250, 350, 14, GRAY);
                DrawText("Vidéos: MP4, MOV, AVI, MKV, WEBM", 250, 370, 14, GRAY);
                DrawText("(Nécessite FFmpeg installé)", 250, 390, 12, GRAY);
                DrawText("Zone d'affichage:", 210, 50, 16, GRAY);
                Rectangle dropZone = {200, 0, screenWidth - 200, screenHeight};
                DrawRectangleLinesEx(dropZone, 2, LIGHTGRAY);
            }

        EndDrawing();
    }

    LogMessage("LOG Program ending - cleaning up");
    
    // Nettoyer le processeur vidéo
    CleanupVideoProcessor();
    LogMessage("LOG Video processor cleaned up");
    
    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
    
    // Nettoyer les séquences d'images
    if (frameSequence != NULL) {
        for (int i = 0; i < totalFrames; i++) {
            UnloadImage(frameSequence[i]);
        }
        free(frameSequence);
    }
    
    // Nettoyer le buffer de textures
    FreeTextureBuffer(&videoTextureBuffer);
    
    UnloadShader(shader);
    CloseWindow();
    LogMessage("LOG Program ended");

    CloseLogger();
    return 0;
}
