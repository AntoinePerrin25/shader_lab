#include "raylib.h"
#include <sys/stat.h> // Pour stat()
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <math.h> // Pour fminf, fmaxf

#define UNUSED (void)

#define ENABLE_LOGGING 0 // Activer ou désactiver le logging

// Structure pour le logging avec gestion des patterns
#define MAX_PATTERN_SIZE 32
#define MAX_MESSAGE_LENGTH 256

typedef struct {
    char messages[MAX_PATTERN_SIZE][MAX_MESSAGE_LENGTH];
    int patternSize;
    int patternCount;
    int currentIndex;
    FILE* logFile;
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
    gLogManager.patternSize = 0;
    gLogManager.patternCount = 0;
    gLogManager.currentIndex = 0;
}

void FlushPattern(void) {
    if (!gLogManager.logFile || gLogManager.patternSize == 0) return;
    
    if (gLogManager.patternCount > 1) {
        // Afficher le pattern avec compteur
        if (gLogManager.patternSize == 1) {
            fprintf(gLogManager.logFile, "%s x%d\n", gLogManager.messages[0], gLogManager.patternCount);
        } else {
            fprintf(gLogManager.logFile, "(");
            for (int i = 0; i < gLogManager.patternSize; i++) {
                fprintf(gLogManager.logFile, "%s", gLogManager.messages[i]);
                if (i < gLogManager.patternSize - 1) {
                    fprintf(gLogManager.logFile, "\n");
                }
            }
            fprintf(gLogManager.logFile, ") x%d\n", gLogManager.patternCount);
        }
    } else {
        // Afficher le pattern une seule fois
        for (int i = 0; i < gLogManager.patternSize; i++) {
            fprintf(gLogManager.logFile, "%s\n", gLogManager.messages[i]);
        }
    }
    fflush(gLogManager.logFile);
}

bool IsPatternMatch(const char* message) {
    if (gLogManager.patternSize == 0) return false;
    
    int expectedIndex = gLogManager.currentIndex % gLogManager.patternSize;
    return strcmp(gLogManager.messages[expectedIndex], message) == 0;
}

void LogMessage(const char* message) {
    #if !ENABLE_LOGGING
    return;
    #endif
    if (!gLogManager.logFile) return;
    
    if (IsPatternMatch(message)) {
        // Continue le pattern existant
        gLogManager.currentIndex++;
        
        // Si on a complété un cycle complet du pattern
        if (gLogManager.currentIndex % gLogManager.patternSize == 0) {
            gLogManager.patternCount++;
        }
    } else {
        // Flush le pattern précédent s'il existe
        FlushPattern();
        
        // Tenter de détecter un nouveau pattern
        bool foundPattern = false;
        
        // Chercher si ce message fait partie d'un pattern existant
        for (int i = 0; i < gLogManager.patternSize; i++) {
            if (strcmp(gLogManager.messages[i], message) == 0) {
                // Nouveau pattern détecté, redémarrer à partir de ce message
                gLogManager.currentIndex = i + 1;
                gLogManager.patternCount = 1;
                foundPattern = true;
                break;
            }
        }
        
        if (!foundPattern) {
            // Nouveau message, étendre le pattern ou en commencer un nouveau
            if (gLogManager.patternSize < MAX_PATTERN_SIZE) {
                strcpy(gLogManager.messages[gLogManager.patternSize], message);
                gLogManager.patternSize++;
                gLogManager.currentIndex = 1;
                gLogManager.patternCount = 1;
            } else {
                // Pattern trop long, traiter comme message unique
                fprintf(gLogManager.logFile, "%s\n", message);
                fflush(gLogManager.logFile);
                gLogManager.patternSize = 0;
                gLogManager.patternCount = 0;
                gLogManager.currentIndex = 0;
            }
        }
    }
}

void CloseLogger(void) {
    if (gLogManager.logFile) {
        FlushPattern();
        fprintf(gLogManager.logFile, "=== LOG END ===\n");
        fclose(gLogManager.logFile);
    }
}
time_t My_GetFileModTime(const char *path)
{
    struct stat attrib;
    if (stat(path, &attrib) == 0)
        return attrib.st_mtime;
    else
        return 0;
}

// Fonction pour charger une séquence d'images (pour GIF animé par exemple)
bool LoadImageSequence(const char* filePath, Image** sequence, int* frameCount) {
    // Pour l'instant, on simule avec une seule image
    // Dans une vraie implémentation, on utiliserait une bibliothèque comme stb_image
    // pour extraire toutes les frames d'un GIF
    
    *frameCount = 1;
    *sequence = (Image*)malloc(sizeof(Image) * (*frameCount));
    
    (*sequence)[0] = LoadImage(filePath);
    
    if ((*sequence)[0].data == NULL) {
        free(*sequence);
        *sequence = NULL;
        *frameCount = 0;
        return false;
    }
    
    return true;
}


int main(void)
{
    InitLogger();
    LogMessage("LOG Program Start");
    
    const int screenWidth = 1280;
    const int screenHeight = 720;
    InitWindow(screenWidth, screenHeight, "Drag & Drop + Shader Zone");
    LogMessage("LOG Window Initialized");

    const char *shaderPath = "effect.fs";
    Shader shader = LoadShader(0, shaderPath);
    LogMessage("LOG Shader Loaded");

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
    char loadedFilePath[512] = {0};
    
    // Variables pour les contrôles UI
    Rectangle playPauseButton = {10, 400, 80, 30};
    Rectangle prevButton = {100, 400, 40, 30};
    Rectangle nextButton = {150, 400, 40, 30};
    Rectangle frameSlider = {10, 450, 180, 20};
    float sliderValue = 0.0f;

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
        LogMessage("LOG Update Frame");
        
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
                    
                    // Note: Raylib ne supporte pas nativement les vidéos
                    // On pourrait utiliser une bibliothèque externe comme FFmpeg
                    // Pour l'instant, on affiche juste un message d'erreur
                    LogMessage("LOG Video files not yet supported");
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
            LogMessage("LOG Update Shader");
            frameCounter = 0;
            time_t modTime = My_GetFileModTime(shaderPath);
            if (modTime != lastModTime)
            {
                lastModTime = modTime;
                UnloadShader(shader);
                shader = LoadShader(0, shaderPath);
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

        // Mise à jour des séquences/animations
        if (isSequence && isPlaying) {
            frameTime += GetFrameTime();
            if (frameTime >= 1.0f / frameRate) {
                frameTime = 0.0f;
                currentFrame++;
                if (currentFrame >= totalFrames) {
                    currentFrame = 0; // Boucle
                }
                
                // Mettre à jour la texture avec la frame actuelle
                if (frameSequence != NULL && currentFrame < totalFrames) {
                    UnloadTexture(originalImageTex);
                    originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                }
                
                // Mettre à jour le slider
                sliderValue = (float)currentFrame / (float)(totalFrames - 1);
            }
        }

        // Gestion des contrôles UI pour les séquences
        Vector2 mousePos = GetMousePosition();
        if (isSequence) {
            // Bouton Play/Pause
            if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, playPauseButton)) {
                isPlaying = !isPlaying;
                LogMessage(isPlaying ? "LOG Playback started" : "LOG Playback paused");
            }
            
            // Bouton Previous
            if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, prevButton)) {
                currentFrame = (currentFrame - 1 + totalFrames) % totalFrames;
                if (frameSequence != NULL) {
                    UnloadTexture(originalImageTex);
                    originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                }
                sliderValue = (float)currentFrame / (float)(totalFrames - 1);
                LogMessage("LOG Previous frame");
            }
            
            // Bouton Next
            if (IsMouseButtonPressed(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, nextButton)) {
                currentFrame = (currentFrame + 1) % totalFrames;
                if (frameSequence != NULL) {
                    UnloadTexture(originalImageTex);
                    originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                }
                sliderValue = (float)currentFrame / (float)(totalFrames - 1);
                LogMessage("LOG Next frame");
            }
            
            // Slider de progression
            if (IsMouseButtonDown(MOUSE_LEFT_BUTTON) && CheckCollisionPointRec(mousePos, frameSlider)) {
                float relativeX = (mousePos.x - frameSlider.x) / frameSlider.width;
                sliderValue = fmaxf(0.0f, fminf(1.0f, relativeX));
                currentFrame = (int)(sliderValue * (totalFrames - 1));
                if (frameSequence != NULL) {
                    UnloadTexture(originalImageTex);
                    originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                }
                LogMessage("LOG Frame selected via slider");
            }
        }

        // Panel gauche
        Rectangle panel = {0, 0, 200, screenHeight};

        // Gestion clic + shader (maintenir le clic pour appliquer continuellement)
        Vector2 mouse = GetMousePosition();
        Vector2 effectiveMouseInImage = mouseLocked ? lockedMouseInImage : (Vector2){0, 0};
        
        bool applyShader = false;
        
        if (mouseLocked) {
            // Si la souris est verrouillée, appliquer le shader en permanence à la position verrouillée
            applyShader = originalImageTex.id > 0;
        } else {
            // Comportement normal : clic gauche pour appliquer le shader
            applyShader = (IsMouseButtonDown(MOUSE_LEFT_BUTTON) || IsMouseButtonPressed(MOUSE_LEFT_BUTTON)) && 
                          !CheckCollisionPointRec(mouse, panel) && 
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
            
            // Contrôles de lecture pour les séquences
            if (isSequence) {
                textHeight += 20;
                DrawText("Lecture:", 10, textHeight+=20, 14, BLACK);
                DrawText("P: Play/Pause", 10, textHeight+=15, 10, DARKGRAY);
                DrawText("←→: Frame prec/suiv", 10, textHeight+=15, 10, DARKGRAY);
                
                // Bouton Play/Pause
                DrawRectangleRec(playPauseButton, isPlaying ? GREEN : RED);
                DrawRectangleLinesEx(playPauseButton, 2, BLACK);
                DrawText(isPlaying ? "Pause" : "Play", playPauseButton.x + 10, playPauseButton.y + 8, 14, BLACK);
                
                // Boutons Previous/Next
                DrawRectangleRec(prevButton, SKYBLUE);
                DrawRectangleLinesEx(prevButton, 2, BLACK);
                DrawText("<", prevButton.x + 15, prevButton.y + 8, 14, BLACK);
                
                DrawRectangleRec(nextButton, SKYBLUE);
                DrawRectangleLinesEx(nextButton, 2, BLACK);
                DrawText(">", nextButton.x + 15, nextButton.y + 8, 14, BLACK);
                
                // Slider de progression
                DrawRectangleRec(frameSlider, DARKGRAY);
                DrawRectangleLinesEx(frameSlider, 2, BLACK);
                float sliderPos = frameSlider.x + (sliderValue * frameSlider.width);
                DrawRectangle(sliderPos - 5, frameSlider.y - 2, 10, frameSlider.height + 4, BLUE);
                
                // Affichage des informations de frame
                DrawText(TextFormat("Frame: %d/%d", currentFrame + 1, totalFrames), 10, textHeight+=30, 12, BLACK);
                DrawText(TextFormat("FPS: %.1f", frameRate), 10, textHeight+=20, 12, BLACK);
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
                    isPlaying = !isPlaying;
                    LogMessage(isPlaying ? "LOG Playback started (keyboard)" : "LOG Playback paused (keyboard)");
                }
                if (IsKeyPressed(KEY_LEFT)) {
                    currentFrame = (currentFrame - 1 + totalFrames) % totalFrames;
                    if (frameSequence != NULL) {
                        UnloadTexture(originalImageTex);
                        originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                    }
                    sliderValue = (float)currentFrame / (float)(totalFrames - 1);
                    LogMessage("LOG Previous frame (keyboard)");
                }
                if (IsKeyPressed(KEY_RIGHT)) {
                    currentFrame = (currentFrame + 1) % totalFrames;
                    if (frameSequence != NULL) {
                        UnloadTexture(originalImageTex);
                        originalImageTex = LoadTextureFromImage(frameSequence[currentFrame]);
                    }
                    sliderValue = (float)currentFrame / (float)(totalFrames - 1);
                    LogMessage("LOG Next frame (keyboard)");
                }
            }

            // Draw image with shader if needed
            if (originalImageTex.id > 0)
            {
                LogMessage("LOG Drawing image");
                
                if (applyShader)
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

                    // Obtenir le temps en millisecondes depuis le lancement du programme
                    float timeMs = (float)GetTime() * 1000.0f;
                    SetShaderValue(shader, GetShaderLocation(shader, "timeMs"), &timeMs, SHADER_UNIFORM_FLOAT);

                    // Appliquer le shader directement lors de l'affichage
                    BeginShaderMode(shader);
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
                DrawText("Vidéos: MP4, MOV, AVI, MKV, WEBM (bientôt)", 250, 370, 14, GRAY);
                DrawText("Zone d'affichage:", 210, 50, 16, GRAY);
                Rectangle dropZone = {200, 0, screenWidth - 200, screenHeight};
                DrawRectangleLinesEx(dropZone, 2, LIGHTGRAY);
            }

        EndDrawing();
    }

    LogMessage("LOG Program ending - cleaning up");
    if (originalImageTex.id > 0) UnloadTexture(originalImageTex);
    
    // Nettoyer les séquences d'images
    if (frameSequence != NULL) {
        for (int i = 0; i < totalFrames; i++) {
            UnloadImage(frameSequence[i]);
        }
        free(frameSequence);
    }
    
    UnloadShader(shader);
    CloseWindow();
    LogMessage("LOG Program ended");

    CloseLogger();
    return 0;
}
