using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Infrastructure
{
    public class BaseRepo
    {
        public string LoadFileFromAssembly(string fileName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            var manifestResourceNames = assembly.GetManifestResourceNames();

            string? resourceName = manifestResourceNames
                .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                throw new FileNotFoundException(
                    $"Ressource '{fileName}' introuvable. Vérifie que le fichier est en 'Ressource incorporée'.");
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new FileNotFoundException($"Impossible d'ouvrir la ressource '{resourceName}'.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
