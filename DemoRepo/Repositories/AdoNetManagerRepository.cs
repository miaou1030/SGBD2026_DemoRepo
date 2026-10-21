using DemoRepo.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DemoRepo.Repositories
{
    public class AdoNetManagerRepository : IManagerRepository
    {
        /** 5
         * AdoNetManagerRepository.cs est l’implémentation réelle de IManagerRepository qui interagit avec SQL Server via ADO.NET.
         * Elle doit respecter les mêmes règles que FakeManagerRepository pour que les tests unitaires soient cohérents.
         */



        // _connection sert à établir la connexion avec la base de données SQL Server. Il est initialisé dans le constructeur avec la chaîne de connexion fournie.
        private readonly SqlConnection _connection;


        /// <summary>Initialise la nouvelle instance de la classe <see cref="AdoNetManagerRepository"/>.</summary>
        /// <param name="connectionString">La chaîne de connexion utilisée pour se connecter à la base de données.</param>
        /// <exception cref="ArgumentException">Lancée lorsque <paramref name="connectionString"/> est <see langword="null"/> ou vide.</exception>
        public AdoNetManagerRepository(string? connectionString)
        {
            // On vérifie que la chaîne de connexion n’est pas null ou vide pour éviter des erreurs lors de l’ouverture de la connexion.
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            // On initialise la connexion SQL avec la chaîne de connexion fournie.
            _connection = new SqlConnection(connectionString);
        }


        /// <summary>Charge le contenu d’un fichier depuis les ressources incorporées de l’assembly.</summary>
        /// <param name="fileName">Nom du fichier à rechercher dans les ressources incorporées.</param>
        /// <returns>Contenu textuel de la ressource chargée.</returns>
        /// <exception cref="FileNotFoundException">La ressource est introuvable ou ne peut pas être ouverte.</exception>
        private static string LoadFileFromAssembly(string fileName)
        {
            // On récupère l'assembly courant pour accéder aux ressources embarquées.
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            // On cherche le nom complet de la ressource qui se termine par le nom du fichier fourni (case-insensitive).
            string? resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            // Si la ressource n’est pas trouvée, on lance une exception pour signaler l’erreur.
            if (resourceName is null)
            {
                throw new FileNotFoundException(
                    $"Ressource '{fileName}' introuvable. Vérifie que le fichier est en 'Ressource incorporée'.");
            }

            // On ouvre un flux de lecture sur la ressource trouvée. Si le flux est null, on lance une exception.
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new FileNotFoundException($"Impossible d'ouvrir la ressource '{resourceName}'.");
            }

            // On lit le contenu du flux et on le retourne sous forme de chaîne de caractères.
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


        /// <summary>
        /// Mêmes règles que FakeManagerRepository : manager non null, nom obligatoire (ni null, ni vide, ni espaces).
        /// SQL Server refuserait un nom NULL mais accepterait "" ou "   " : on les refuse donc ici.
        /// </summary>
        /// <param name="manager">Manager à valider.</param>
        /// <exception cref="ArgumentException">Levée lorsque le nom du manager est null, vide ou composé uniquement d'espaces.</exception>
        private static void ValidateManager(Manager manager)
        {
            // On vérifie que le manager n’est pas null pour éviter une exception plus tard.
            ArgumentNullException.ThrowIfNull(manager);

            // On vérifie que le nom du manager n’est pas vide ou composé uniquement d’espaces.
            if (string.IsNullOrWhiteSpace(manager.MNG_LastName))
            {
                throw new ArgumentException("Le nom du manager ne peut pas être vide.", nameof(manager));
            }
        }


        /// <summary>Transforme la ligne courante du reader en Manager. Un prénom NULL en base devient null en C#.</summary>
        /// <param name="reader">Lecteur contenant les données du responsable.</param>
        /// <returns>Instance de <see cref="Manager"/> créée à partir des données lues.</returns>
        private static Manager MapManager(SqlDataReader reader)
        {
            // On récupère l'index de la colonne MNG_FirstName pour vérifier si elle est NULL.
            int firstNameOrdinal = reader.GetOrdinal("MNG_FirstName");

            /**
             * On crée un nouvel objet Manager en mappant les colonnes du reader aux propriétés de l'objet.
             * MNG_FirstName est vérifié pour NULL : si c'est le cas, on retourne null ; sinon, on retourne la valeur de la colonne.
             */
            return new Manager
            {
                MNG_Id = reader.GetInt32(reader.GetOrdinal("MNG_Id")),
                MNG_FirstName = reader.IsDBNull(firstNameOrdinal) ? null : reader.GetString(firstNameOrdinal),
                MNG_LastName = reader.GetString(reader.GetOrdinal("MNG_LastName"))
            };
        }


        /// <summary>Ajoute un responsable.</summary>
        /// <param name="manager">Le responsable à ajouter.</param>
        /// <returns>L’identifiant généré du responsable ajouté.</returns>
        int IManagerRepository.AddManager(Manager manager)
        {
            // On valide le manager avant de l’ajouter pour s’assurer qu’il respecte les règles du domaine.
            ValidateManager(manager);

            // On charge le script SQL d’insertion depuis les ressources incorporées.
            string sql = LoadFileFromAssembly("AddManager.sql");

            // On crée une commande SQL avec le script chargé et la connexion à la base de données.
            using var command = new SqlCommand(sql, _connection);
            command.CommandType = CommandType.Text;

            // (object?) ... ?? DBNull.Value : un prénom null doit être envoyé comme NULL SQL,
            // sinon ADO.NET n'envoie pas le paramètre du tout.
            command.Parameters.AddWithValue("@MNG_FirstName", (object?)manager.MNG_FirstName ?? DBNull.Value);
            command.Parameters.AddWithValue("@MNG_LastName", manager.MNG_LastName);

            // On ouvre la connexion à la base de données pour exécuter la commande.
            _connection.Open();
            try
            {
                // OUTPUT INSERTED.MNG_Id : la requête renvoie l'Id généré par IDENTITY.
                return (int)command.ExecuteScalar()!;
            }
            finally
            {
                // On ferme la connexion à la base de données pour libérer les ressources.
                _connection.Close();
            }
        }


        /// <summary>Supprime le responsable correspondant à l’identifiant spécifié.</summary>
        /// <param name="managerId">Identifiant du responsable à supprimer.</param>
        /// <returns>true si un responsable a été supprimé ; sinon, false.</returns>
        bool IManagerRepository.DeleteManager(int managerId)
        {
            // On charge le script SQL de suppression depuis les ressources incorporées.
            string sql = LoadFileFromAssembly("DeleteManager.sql");

            // On crée une commande SQL avec le script chargé et la connexion à la base de données.
            using var command = new SqlCommand(sql, _connection);
            command.CommandType = CommandType.Text;
            command.Parameters.AddWithValue("@MNG_Id", managerId);

            // On ouvre la connexion à la base de données pour exécuter la commande.
            _connection.Open();
            try
            {
                // ExecuteNonQuery renvoie le nombre de lignes supprimées : 0 si l'Id n'existe pas.
                return command.ExecuteNonQuery() > 0;
            }
            finally
            {
                _connection.Close();
            }
        }


        // GetAllManagers() retourne tous les managers. On utilise IEnumerable<Manager> car on veut retourner une collection de managers.
        IEnumerable<Manager> IManagerRepository.GetAllManagers()
        {
            // On crée une liste pour stocker les managers récupérés depuis la base de données.
            var managers = new List<Manager>();

            // On charge le script SQL de sélection depuis les ressources incorporées.
            string sql = LoadFileFromAssembly("GetAllManagers.sql");

            // On crée une commande SQL avec le script chargé et la connexion à la base de données.
            using var command = new SqlCommand(sql, _connection);
            command.CommandType = CommandType.Text;

            // On ouvre la connexion à la base de données pour exécuter la commande.
            _connection.Open();
            try
            {
                // ExecuteReader permet de lire les résultats de la requête ligne par ligne.
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //MapManager(reader) transforme la ligne courante du reader en un objet Manager, qu’on ajoute à la liste.
                    managers.Add(MapManager(reader));
                }
            }
            finally
            {
                _connection.Close();
            }

            return managers;
        }

        // Manager? car on peut ne pas trouver de manager avec l’Id spécifié, donc on retourne null dans ce cas.
        /// <summary>Récupère un manager par son identifiant.</summary>
        /// <param name="managerId">Id du manager à récupérer.</param>
        /// <returns>Le manager associé à l'identifiant spécifié, ou <c>null</c> si aucun manager n'est trouvé.</returns>
        Manager? IManagerRepository.GetManagerById(int managerId)
        {
            // On charge le script SQL de sélection depuis les ressources incorporées.
            string sql = LoadFileFromAssembly("GetManagerById.sql");

            // On crée une commande SQL avec le script chargé et la connexion à la base de données.
            using var command = new SqlCommand(sql, _connection);
            command.CommandType = CommandType.Text;
            command.Parameters.AddWithValue("@MNG_Id", managerId);

            _connection.Open();
            try
            {
                using var reader = command.ExecuteReader();
                return reader.Read() ? MapManager(reader) : null;
            }
            finally
            {
                _connection.Close();
            }
        }


        /// <summary>Met à jour un manager.</summary>
        /// <remarks>Retourne false si l’identifiant du manager est inconnu.</remarks>
        /// <param name="manager">Manager à mettre à jour.</param>
        /// <returns>true si le manager a été mis à jour ; sinon, false.</returns>
        bool IManagerRepository.UpdateManager(Manager manager)
        {
            // On valide le manager avant de le ma mettre à jour pour s’assurer qu’il respecte les règles du domaine.
            ValidateManager(manager);

            // La mise à jour passe par la procédure stockée dbo.Manager_Update.
            using var command = new SqlCommand("Manager_Update", _connection);
            command.CommandType = CommandType.StoredProcedure;

            // On ajoute les paramètres de la procédure stockée avec les valeurs du manager à mettre à jour.
            command.Parameters.AddWithValue("@MNG_Id", manager.MNG_Id);
            // (object?) ... ?? DBNull.Value : un prénom null doit être envoyé comme NULL SQL,
            command.Parameters.AddWithValue("@MNG_FirstName", (object?)manager.MNG_FirstName ?? DBNull.Value);
            command.Parameters.AddWithValue("@MNG_LastName", manager.MNG_LastName);

            _connection.Open();
            try
            {
                // 0 ligne modifiée = Id inconnu, donc false (la procédure ne doit PAS contenir SET NOCOUNT ON).
                return command.ExecuteNonQuery() > 0;
            }
            finally
            {
                _connection.Close();
            }
        }
    }
}