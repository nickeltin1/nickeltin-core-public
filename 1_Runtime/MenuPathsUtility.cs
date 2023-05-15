namespace nickeltin.Core.Runtime
{
    public static class MenuPathsUtility
    {
        public const string assets = "Assets/";
        public const string baseMenu = "nickeltin/";

        public const string internalMenu = baseMenu + "_internal/";

        public const string worldGraph = "WorldGraph/";
        public const string worldGraphMenu = baseMenu + worldGraph;

        public const string tweening = "Tweening/";
        public const string tweeningMenu = baseMenu + tweening;

        public const string mvvm = "MVVM/";
        public const string mvvmMenu = baseMenu + mvvm;

        public const string saving = "Saving/";
        public const string savingMenu = baseMenu + saving;
        
        public const string audio = "Audio/";
        public const string audioMenu = baseMenu + audio;

        public const string utils ="Utils/";
        public const string utilsMenu = baseMenu + utils;
        
        public const string embeddedPackages = "EmbeddedPackages/";
        public const string embeddedPackagesMenu = internalMenu + embeddedPackages;

        public const string other = "Other/";
        public const string otherMenu = baseMenu + other;
        
        public const string pooling = "Pooling/";
        public const string poolingMenu = baseMenu + pooling;

    }
}