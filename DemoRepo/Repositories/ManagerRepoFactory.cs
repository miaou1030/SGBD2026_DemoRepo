using System;
using System.Collections.Generic;
using System.Text;

namespace DemoRepo.Repositories
{
    public class ManagerRepoFactory
    {
        /** 6
         * Le reste du code (console, tests) ne doit pas savoir s’il parle au fake ou à SQL Server. 
         * Il demande à la factory « donne-moi un IManagerRepository » en indiquant un type, et c’est tout. 
         * Changer de mode, c’est changer une valeur dans appsettings.json, sans modifier le code.
         */


        /// <summary>Crée une instance d’un IManagerRepository selon le type spécifié.</summary>
        /// <param name="repositoryType">Le type de repository à créer.</param>
        /// <param name="connectionString">La chaîne de connexion à utiliser pour la connexion à la base de données (si applicable).</param>
        /// <returns>Une instance de <see cref="IManagerRepository"/>.</returns>
        public static IManagerRepository CreateManagerRepository(RepositoryType repositoryType, string? connectionString = null)
        {
            // On utilise un switch expression pour créer l’instance appropriée selon le type de repository demandé.
            return repositoryType switch
            {
                RepositoryType.Fake => new FakeManagerRepository(),
                RepositoryType.AdoNet => new AdoNetManagerRepository(connectionString),
                _ => throw new ArgumentException("Invalid repository type", nameof(repositoryType))
            };
        }
    }
}
