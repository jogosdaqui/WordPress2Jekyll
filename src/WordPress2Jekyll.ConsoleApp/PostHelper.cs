﻿using System;
namespace WordPress2Jekyll.ConsoleApp
{
    public static class PostHelper
    {
        public static string GetPostType(dynamic post)
        {
            var type = post.Type;

            if (type == null)
                return null;

            var name = post.Name;

            if (name.StartsWith("promocao"))
                return "Promo";

            if (name.StartsWith("entrevista"))
                return "Interview";

            if (name.StartsWith("previa") || name.StartsWith("preview") || type.Equals("20"))
                return "Preview";

            if (type.Equals("10") || type.Equals("24"))
                return "News";

            if (type.Equals("19"))
                return "Game";

            if (type.Equals("22") || type.Equals("23"))
                return "Promo";

            if (type.Equals("25") || name.StartsWith("evento") || name.StartsWith("spjam"))
                return "Event";

            return null;
        }
    }
}
