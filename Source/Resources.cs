using UnityEngine;
using Verse;

namespace YouDoYou
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D AutoIcon;

        public static readonly Texture2D[] Icons;

        static Resources()
        {
            AutoIcon = ContentFinder<Texture2D>.Get("UI/Icons/Auto");
        }
    }
}