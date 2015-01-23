using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib
{
    public static class Settings
    {
        public const int SCREEN_WIDTH = 1024;
        public const int SCREEN_HEIGHT = 768;
        public const int SERVER_PORT = 15356;
        public const int SOUND_CHANNELS = 32;
        public const int SERVER_GRACE_TIME = 1000; //Time before closing a dieing connection
        public const int SERVER_LOWEST_CLIENT_VERSION = 1; //Lowest client version accepted by server
        public const int CLIENT_LOWEST_SERVER_VERSION = 1; //Lowest server version accepted by client
        public const ushort CLIENT_VERSION = 1;
        public const ushort SERVER_VERSION = 1;

        public const float PLAYER_WALK_SPEED = 2.5f; //Tiles per second

        public const float SERVER_MESSAGE_COMPACTION_TOLERANCE = 0.1f; //The maximum time a client is allowed to validate actions that are overlapping (two actions arrive with too little time in between)

        public const string ALLOWED_CHARACTER_NAMES = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
    }
}
