using DemoRepo.Models;
using DemoRepo.Repositories;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace TestProjectDemo
{
    public class AdoNetManagerRepositoryTests : IAsyncLifetime
    {
        // _container est un conteneur SQL Server pour les tests. Il est créé par Testcontainers et démarré avant chaque test.
        private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

        // _repo est l’instance de AdoNetManagerRepository utilisée pour les tests. Elle est initialisée dans InitializeAsync.
        private IManagerRepository _repo = null!;

        // xUnit crée une nouvelle instance de la classe pour CHAQUE test, puis appelle InitializeAsync.
        // Chaque test a donc son propre SQL Server, avec une table vide et des Id qui repartent de 1.
        public async Task InitializeAsync()
        {
            // On démarre le conteneur SQL Server pour les tests. Cela peut prendre quelques secondes.
            await _container.StartAsync();

            // On crée la base de données SGBD2026 dans le conteneur SQL Server. Cela peut prendre quelques secondes.
            await using (var master = new SqlConnection(_container.GetConnectionString()))
            {
                // On ouvre la connexion à la base de données master pour créer la nouvelle base de données SGBD2026.
                await master.OpenAsync();
                // On exécute la commande SQL pour créer la base de données SGBD2026.
                await new SqlCommand("CREATE DATABASE SGBD2026", master).ExecuteNonQueryAsync();
            }

            // On crée la chaîne de connexion pour se connecter à la base de données SGBD2026 dans le conteneur SQL Server.
            string cs = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "SGBD2026"
            }.ConnectionString;

            // On ouvre une connexion à la base de données SGBD2026 pour créer la table Manager et l’index unique.
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // Même structure que ta vraie table.
            await new SqlCommand(
                """
                CREATE TABLE dbo.Manager (
                    MNG_Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Manager_Id PRIMARY KEY,
                    MNG_FirstName NVARCHAR(50) NULL,
                    MNG_LastName  NVARCHAR(50) NOT NULL
                )
                """, conn).ExecuteNonQueryAsync();

            // On crée un index unique sur le couple (MNG_FirstName, MNG_LastName) pour simuler la contrainte d’unicité.
            await new SqlCommand(
                "CREATE UNIQUE NONCLUSTERED INDEX IX_Manager_FN_LN_Unique ON dbo.Manager (MNG_FirstName, MNG_LastName)",
                conn).ExecuteNonQueryAsync();

            // CREATE PROCEDURE doit être seule dans son lot : une commande à part, sans GO.
            await new SqlCommand(
                """
                CREATE PROCEDURE dbo.Manager_Update
                    @MNG_Id        INT,
                    @MNG_FirstName NVARCHAR(50),
                    @MNG_LastName  NVARCHAR(50)
                AS
                BEGIN
                    UPDATE dbo.Manager
                    SET MNG_FirstName = @MNG_FirstName,
                        MNG_LastName  = @MNG_LastName
                    WHERE MNG_Id = @MNG_Id;
                END
                """, conn).ExecuteNonQueryAsync();

            // On crée l’instance de AdoNetManagerRepository avec la chaîne de connexion vers la base de données SGBD2026.
            _repo = ManagerRepoFactory.CreateManagerRepository(RepositoryType.AdoNet, cs);
        }


        // xUnit appelle DisposeAsync après chaque test pour nettoyer les ressources. On arrête le conteneur SQL Server.
        public Task DisposeAsync() => _container.DisposeAsync().AsTask();


        // Test 1 : Vérifie que l’ajout d’un manager fonctionne et que l’Id est auto-incrémenté.
        [Fact]
        public void AddManager_WithNullFirstName_ThenGetById_ReturnsNullFirstName()
        {
            // On ajoute un manager avec un prénom null et un nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = _repo.AddManager(new Manager { MNG_FirstName = null, MNG_LastName = "Dupont" });

            // On récupère le manager ajouté par son Id pour vérifier que le prénom est bien null et que le nom est correct.
            var stored = _repo.GetManagerById(id);

            // On vérifie que le manager récupéré n’est pas null, que le prénom est null et que le nom est "Dupont".
            Assert.NotNull(stored);
            Assert.Null(stored.MNG_FirstName);
            Assert.Equal("Dupont", stored.MNG_LastName);
        }


        // Test 2 : Vérifie que l’ajout d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException.
        [Fact]
        public void AddManager_DuplicateCouple_ThrowsUniqueIndexViolation()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". Cela fonctionne car c’est le premier ajout.
            _repo.AddManager(new Manager { MNG_FirstName = "Jean", MNG_LastName = "Dupont" });

            // On tente d’ajouter un autre manager avec le même couple prénom "Jean" et nom "Dupont". Cela doit lancer une exception SqlException car l’index unique est violé.
            var ex = Assert.Throws<SqlException>(
                () => _repo.AddManager(new Manager { MNG_FirstName = "Jean", MNG_LastName = "Dupont" }));

            // On vérifie que le numéro de l’erreur est 2601 (violation d’index unique) et que le message contient le nom de l’index unique.
            Assert.Equal(2601, ex.Number);                       // 2601 = index unique (2627 = contrainte unique)
            Assert.Contains("IX_Manager_FN_LN_Unique", ex.Message);
        }


        // Test 3 : Vérifie que si un appel échoue à cause d’une violation d’index unique, la connexion est refermée et le prochain appel fonctionne.
        [Fact]
        public void AddManager_AfterAFailure_ConnectionIsClosedAndNextCallWorks()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". Cela fonctionne car c’est le premier ajout.
            _repo.AddManager(new Manager { MNG_FirstName = "Jean", MNG_LastName = "Dupont" });
            Assert.Throws<SqlException>(
                () => _repo.AddManager(new Manager { MNG_FirstName = "Jean", MNG_LastName = "Dupont" }));

            // Prouve que le try/finally a refermé la connexion : sinon "connexion déjà ouverte".
            int id = _repo.AddManager(new Manager { MNG_FirstName = "Marie", MNG_LastName = "Dupont" });

            // On vérifie que l’Id retourné est supérieur à 0, ce qui prouve que le manager a été ajouté avec succès après l’échec précédent.
            Assert.True(id > 0);
        }


        // Méthode utilitaire pour ajouter un manager avec un prénom et un nom, et retourner l’Id attribué.
        private int Add(string? firstName, string lastName) =>
        _repo.AddManager(new Manager { MNG_FirstName = firstName, MNG_LastName = lastName });

        // ---------- AddManager ----------


        // Test 4 : Vérifie que l’ajout d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException, même si le prénom est null.
        [Fact]
        public void AddManager_DuplicateWithNullFirstName_ThrowsUniqueIndexViolation()
        {
            // On ajoute un manager avec un prénom null et un nom "Dupont". Cela fonctionne car c’est le premier ajout.
            Add(null, "Dupont");

            // On tente d’ajouter un autre manager avec un prénom null et le même nom "Dupont".
            // Cela doit lancer une exception SqlException car l’index unique est violé (NULL == NULL pour l’index unique).
            var ex = Assert.Throws<SqlException>(() => Add(null, "Dupont"));   // NULL == NULL pour l'index unique

            // On vérifie que le numéro de l’erreur est 2601 (violation d’index unique).
            Assert.Equal(2601, ex.Number);
        }


        // Test 5 : Vérifie que l’ajout d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException, même si la casse est différente.
        [Fact]
        public void AddManager_DuplicateWithDifferentCase_ThrowsUniqueIndexViolation()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". Cela fonctionne car c’est le premier ajout.
            Add("Jean", "Dupont");

            // On tente d’ajouter un autre manager avec le couple prénom "JEAN" et nom "dupont".
            // Cela doit lancer une exception SqlException car l’index unique est violé (la comparaison est insensible à la casse).
            var ex = Assert.Throws<SqlException>(() => Add("JEAN", "dupont"));

            // On vérifie que le numéro de l’erreur est 2601 (violation d’index unique).
            Assert.Equal(2601, ex.Number);
        }


        // Test 6 : Vérifie que l’ajout de plusieurs managers avec le même nom mais des prénoms différents ou null fonctionne.
        [Fact]
        public void AddManager_SameLastNameWithDifferentOrNullFirstName_AddSuccess()
        {
            // On ajoute trois managers avec le même nom "Dupont" mais des prénoms différents ou null. Cela doit fonctionner car l’index unique est sur le couple (prénom, nom).
            Add("Jean", "Dupont");
            Add("Marie", "Dupont");
            Add(null, "Dupont");

            // On vérifie que le nombre de managers dans la base est bien 3, ce qui prouve que les trois ajouts ont réussi.
            Assert.Equal(3, _repo.GetAllManagers().Count());
        }


        // ---------- Tests supplémentaires pour AddManager, UpdateManager, GetAllManagers, GetManagerById, DeleteManager ----------

        // Test 7 : Vérifie que l’ajout d’un manager avec un nom vide ou composé uniquement d’espaces lance une exception ArgumentException.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddManager_InvalidLastName_ThrowsArgumentException(string? lastName)
        {
            // On tente d’ajouter un manager avec un nom invalide (null, vide ou composé uniquement d’espaces). Cela doit lancer une exception ArgumentException.
            var ex = Assert.Throws<ArgumentException>(
                () => _repo.AddManager(new Manager { MNG_FirstName = "Jean", MNG_LastName = lastName! }));

            // On vérifie que le nom du paramètre dans l’exception est bien "manager", ce qui indique que c’est le manager passé en paramètre qui est invalide.
            Assert.Equal("manager", ex.ParamName);
        }


        // Test 8 : Vérifie que l’ajout d’un manager null lance une exception ArgumentNullException.
        [Fact]
        public void AddManager_NullManager_ThrowsArgumentNullException()
        {
            // On tente d’ajouter un manager null. Cela doit lancer une exception ArgumentNullException.
            Assert.Throws<ArgumentNullException>(() => _repo.AddManager(null!));
        }

        // ---------- GetAllManagers / GetManagerById ----------


        // Test 9 : Vérifie que GetAllManagers retourne une collection vide lorsque la table est vide.
        [Fact]
        public void GetAllManagers_EmptyTable_ReturnsEmptyCollection()
        {
            // On vérifie que GetAllManagers retourne une collection vide lorsque la table est vide.
            Assert.Empty(_repo.GetAllManagers());
        }


        // Test 10 : Vérifie que GetAllManagers retourne tous les managers dans l’ordre croissant de leur Id, même si certains ont un prénom null.
        [Fact]
        public void GetAllManagers_WithNullFirstName_ReturnsAllInIdOrder()
        {
            // On vérifie que GetAllManagers retourne tous les managers dans l’ordre croissant de leur Id, même si certains ont un prénom null.
            int id1 = Add(null, "Beta");
            int id2 = Add("Jean", "Alpha");

            // On récupère tous les managers et on les convertit en liste pour pouvoir accéder aux éléments par index.
            var managers = _repo.GetAllManagers().ToList();

            // On vérifie que le nombre de managers retournés est bien 2, que les Id sont corrects et que le prénom du premier manager est null.
            Assert.Equal(2, managers.Count);
            Assert.Equal(id1, managers[0].MNG_Id);
            Assert.Null(managers[0].MNG_FirstName);       // teste le IsDBNull de MapManager
            Assert.Equal(id2, managers[1].MNG_Id);
            Assert.Equal("Jean", managers[1].MNG_FirstName);
        }


        // Test 11 : Vérifie que GetManagerById retourne null lorsque l’Id n’existe pas dans la table.
        [Fact]
        public void GetManagerById_UnknownId_ReturnsNull()
        {
            // On vérifie que GetManagerById retourne null lorsque l’Id n’existe pas dans la table.
            Assert.Null(_repo.GetManagerById(9999));
        }

        // ---------- UpdateManager (procédure stockée Manager_Update) ----------


        // Test 12 : Vérifie que la mise à jour d’un manager existant fonctionne et que les modifications sont bien enregistrées.
        [Fact]
        public void UpdateManager_ChangeLastName_UpdateSuccess()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = Add("Jean", "Dupont");
            var manager = _repo.GetManagerById(id)!;
            manager.MNG_LastName = "Martin";

            // On met à jour le manager avec le nouveau nom "Martin". La méthode UpdateManager retourne true si la mise à jour a réussi.
            Assert.True(_repo.UpdateManager(manager));

            // On récupère le manager mis à jour par son Id pour vérifier que le nom a bien été modifié et que le prénom est resté le même.
            var updated = _repo.GetManagerById(id)!;
            Assert.Equal("Martin", updated.MNG_LastName);
            Assert.Equal("Jean", updated.MNG_FirstName);   // le prénom n'a pas bougé
        }


        // Test 13 : Vérifie que la mise à jour d’un manager existant avec un prénom null fonctionne et que les modifications sont bien enregistrées.
        [Fact]
        public void UpdateManager_FirstNameToNull_UpdateSuccess()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = Add("Jean", "Dupont");
            var manager = _repo.GetManagerById(id)!;
            manager.MNG_FirstName = null;

            // On met à jour le manager avec le prénom null. La méthode UpdateManager retourne true si la mise à jour a réussi.
            Assert.True(_repo.UpdateManager(manager));

            // On récupère le manager mis à jour par son Id pour vérifier que le prénom est bien null et que le nom est resté le même.
            Assert.Null(_repo.GetManagerById(id)!.MNG_FirstName);   // teste le DBNull.Value envoyé à la procédure
        }


        // Test 14 : Vérifie que la mise à jour d’un manager avec un Id inconnu retourne false et ne modifie pas la table.
        [Fact]
        public void UpdateManager_UnknownId_ReturnsFalse()
        {
            // On crée un manager avec un Id inconnu (9999) et un couple prénom/nom quelconque. La méthode UpdateManager doit retourner false car l’Id n’existe pas dans la table.
            var unknown = new Manager { MNG_Id = 9999, MNG_FirstName = "X", MNG_LastName = "Y" };

            // On vérifie que la méthode UpdateManager retourne false pour un Id inconnu, ce qui indique que la mise à jour a échoué.
            Assert.False(_repo.UpdateManager(unknown));
        }


        // Test 15 : Vérifie que la mise à jour d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException.
        [Fact]
        public void UpdateManager_ToExistingCouple_ThrowsUniqueIndexViolation()
        {
            // On ajoute deux managers avec des couples prénom/nom différents. L’Id est auto-incrémenté par SQL Server.
            Add("Jean", "Dupont");
            int id2 = Add("Marie", "Martin");
            var manager = _repo.GetManagerById(id2)!;
            manager.MNG_FirstName = "Jean";
            manager.MNG_LastName = "Dupont";

            // On tente de mettre à jour le deuxième manager avec le couple prénom/nom du premier manager. Cela doit lancer une exception SqlException car l’index unique est violé.
            var ex = Assert.Throws<SqlException>(() => _repo.UpdateManager(manager));

            // On vérifie que le numéro de l’erreur est 2601 (violation d’index unique).
            Assert.Equal(2601, ex.Number);
        }


        // Test 16 : Vérifie que la mise à jour d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException, même si le prénom est null.
        [Fact]
        public void UpdateManager_SaveWithNoChanges_ReturnsTrue()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = Add("Jean", "Dupont");
            var manager = _repo.GetManagerById(id)!;

            // On met à jour le manager sans changer ses valeurs. La méthode UpdateManager retourne true si la mise à jour a réussi, même si aucune modification n’a été effectuée.
            Assert.True(_repo.UpdateManager(manager));
        }


        // Test 17 : Vérifie que la mise à jour d’un manager avec un couple prénom/nom déjà existant lance une exception SqlException, même si la casse est différente.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateManager_InvalidLastName_ThrowsArgumentException(string? lastName)
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = Add("Jean", "Dupont");
            var manager = _repo.GetManagerById(id)!;
            manager.MNG_LastName = lastName!;

            //  On tente de mettre à jour le manager avec un nom invalide (null, vide ou composé uniquement d’espaces). Cela doit lancer une exception ArgumentException.
            var ex = Assert.Throws<ArgumentException>(() => _repo.UpdateManager(manager));

            // On vérifie que le nom du paramètre dans l’exception est bien "manager", ce qui indique que c’est le manager passé en paramètre qui est invalide.
            Assert.Equal("manager", ex.ParamName);
        }


        // Test 18 : Vérifie que la mise à jour d’un manager null lance une exception ArgumentNullException.
        [Fact]
        public void UpdateManager_NullManager_ThrowsArgumentNullException()
        {
            // On tente de mettre à jour un manager null. Cela doit lancer une exception ArgumentNullException.
            Assert.Throws<ArgumentNullException>(() => _repo.UpdateManager(null!));
        }

        // ---------- DeleteManager ----------

        // Test 19 : Vérifie que la suppression d’un manager existant fonctionne et que le manager est bien supprimé de la table.
        [Fact]
        public void DeleteManager_ExistingManager_ReturnsTrueAndRemovesIt()
        {
            // On ajoute un manager avec le couple prénom "Jean" et nom "Dupont". L’Id est auto-incrémenté par SQL Server.
            int id = Add("Jean", "Dupont");

            // On supprime le manager ajouté par son Id. La méthode DeleteManager retourne true si la suppression a réussi.
            Assert.True(_repo.DeleteManager(id));
            Assert.Null(_repo.GetManagerById(id));
        }


        // Test 20 : Vérifie que la suppression d’un manager inconnu retourne false.
        [Fact]
        public void DeleteManager_UnknownId_ReturnsFalse()
        {
            // On tente de supprimer un manager avec un Id inconnu (9999). La méthode DeleteManager doit retourner false car l’Id n’existe pas dans la table.
            Assert.False(_repo.DeleteManager(9999));
        }


        //--------- Test transposé de Fake -----------//

        [Fact]
        public void AddManager_IgnoresIdProvidedByCaller()
        {
            int id = _repo.AddManager(new Manager { MNG_Id = 2, MNG_FirstName = "Jean", MNG_LastName = "Dupont" });

            Assert.Equal(1, id);                    // base neuve : le premier Id généré est 1
            Assert.Null(_repo.GetManagerById(2));   // l'Id fourni par l'appelant n'a pas été utilisé
        }

        [Fact]
        public void AddManager_AddDeleteAdd_IdIsNotReused()
        {
            int id1 = Add("Temp", "Manager");
            Assert.True(_repo.DeleteManager(id1));

            int id2 = Add("Temp", "Manager");

            Assert.True(id2 > id1);
        }

        [Fact]
        public void UpdateManager_UnknownIdWithExistingCouple_ReturnsFalse()
        {
            Add("Jean", "Dupont");
            var unknown = new Manager { MNG_Id = 9999, MNG_FirstName = "Jean", MNG_LastName = "Dupont" };

            Assert.False(_repo.UpdateManager(unknown));
        }

        [Fact]
        public void UpdateManager_DoesNotChangeCountOrOrder()
        {
            int id1 = Add("Jean", "Alpha");
            int id2 = Add("Marie", "Beta");
            var manager = _repo.GetManagerById(id1)!;
            manager.MNG_LastName = "Zulu";
            _repo.UpdateManager(manager);

            var ids = _repo.GetAllManagers().Select(m => m.MNG_Id).ToList();

            Assert.Equal(new[] { id1, id2 }, ids);
        }
    }
}