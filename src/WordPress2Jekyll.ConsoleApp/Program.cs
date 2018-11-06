using System;

namespace WordPress2Jekyll.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando a conversão...");

            using (var reader = new WordPressReader())
            {
                var writer = new JekyllWriter(reader);

                foreach (var p in reader.GetPosts())
                {
                    Console.WriteLine(p.Title);
                    writer.WritePost(p);
                }
            }

            Console.WriteLine("Conversão finalizada.");
            Console.ReadKey();
        }
    }
}
