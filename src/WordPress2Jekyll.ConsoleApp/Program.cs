using System;

namespace WordPress2Jekyll.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando a conversão...");
            string postName = null;
            bool writeSourceContent = false;
            int postCount = 0;

            using (var reader = new WordPressReader(postName))
            {
                var writer = new JekyllWriter(reader, writeSourceContent);

                foreach (var p in reader.GetPosts())
                {
                    postCount++;
                    Console.WriteLine($"#{postCount} {p.Title}");
                    writer.WritePost(p);
                }
            }

            Console.WriteLine("Conversão finalizada.");
            Console.ReadKey();
        }
    }
}
