using System;

namespace WordPress2Jekyll.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando a conversão...");
            string postName = "entrevista-fernando-paulo-criador-de-treeker";
            bool writeSourceContent = true;

            using (var reader = new WordPressReader(postName))
            {
                var writer = new JekyllWriter(reader, writeSourceContent);

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
