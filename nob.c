#define NOB_IMPLEMENTATION
#include "nob.h" // Make sure nob.h is in your project directory

#include <stdio.h> // For snprintf
#include <errno.h> // For strerror with nob_copy_file

int main(int argc, char **argv) {
    NOB_GO_REBUILD_URSELF(argc, argv);

    // Build command for modular shader lab
    Nob_Cmd cmd = {0};
    nob_cmd_append(&cmd, "gcc", "-Wall", "-Wextra");
    nob_cmd_append(&cmd, "-Iinclude", "-Llib");
    nob_cmd_append(&cmd, "-o", "main.exe");
    nob_cmd_append(&cmd, "-O2");
    // All source files
    nob_cmd_append(&cmd, "src/main.c");
    nob_cmd_append(&cmd, "src/logger.c");
    nob_cmd_append(&cmd, "src/shader_manager.c");
    nob_cmd_append(&cmd, "src/video_processor.c");
    nob_cmd_append(&cmd, "src/ui_renderer.c");
    nob_cmd_append(&cmd, "-lraylib", "-lopengl32", "-lgdi32", "-lwinmm");
    nob_cmd_append(&cmd, "-mwindows");
    if (!nob_cmd_run_sync(cmd)) return 1;
   
    return 0;
}
