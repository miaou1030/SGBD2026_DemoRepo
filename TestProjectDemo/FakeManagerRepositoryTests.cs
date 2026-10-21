using DemoRepo.Models;
using DemoRepo.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProjectDemo
{
    public class FakeManagerRepositoryTests
    {
        /** 4
         * FakeManagerRepositoryTests.cs est la classe de tests unitaires pour FakeManagerRepository.cs. 
         * Elle permet de vérifier que la logique de FakeManagerRepository fonctionne correctement.
         * On teste les méthodes AddManager, GetAllManagers, GetManagerById, UpdateManager et DeleteManager.
         * On utilise xUnit pour les tests unitaires.
         */



        // On utilise _managerRepository pour accéder aux méthodes de FakeManagerRepository.
        private readonly IManagerRepository _managerRepository;


        /**
         * On initialise _managerRepository avec une instance de FakeManagerRepository dans le constructeur de la classe de tests.
         * Cela permet de réutiliser la même instance de FakeManagerRepository pour tous les tests.
         */
        public FakeManagerRepositoryTests()
        {
            _managerRepository = new FakeManagerRepository();
        }


        /// <summary>Vérifie que tous les managers sont récupérés depuis les données initiales.</summary>
        [Fact]
        public void GetAllManagers_InitialData()
        {
            // On appelle la méthode GetAllManagers() pour récupérer tous les managers.
            var managers = _managerRepository.GetAllManagers();

            // On vérifie que la liste contient exactement 3 managers, comme initialisé dans FakeManagerRepository.
            Assert.Equal(3, managers.Count());
        }


        /// <summary>Vérifie que la modification d’un élément récupéré ne modifie pas les données retournées par une nouvelle récupération des managers.</summary>
        [Fact]
        public void GetAllManagers_EditingAReturnedElementWithoutUsingUpdateManager()
        {
            // On récupère tous les managers.
            var managers = _managerRepository.GetAllManagers();

            // On modifie le prénom du premier manager.
            var firstManager = managers.First();
            firstManager.MNG_FirstName = "UpdatedName";
            
            // On récupère à nouveau tous les managers.
            var updatedManagers = _managerRepository.GetAllManagers();
            
            // On vérifie que le prénom du premier manager n'a pas été modifié dans la liste originale.
            Assert.NotEqual("UpdatedName", updatedManagers.First().MNG_FirstName);
        }


        /// <summary>Vérifie que le manager correspondant à un identifiant existant est retourné avec les propriétés attendues.</summary>
        /// <param name="id">Identifiant du manager à rechercher.</param>
        /// <param name="expectedFirstName">Prénom attendu du manager.</param>
        /// <param name="expectedLastName">Nom attendu du manager.</param>
        [Theory]
        [InlineData(1, "ManagerFN1", "ManagerLN1")]
        [InlineData(2, "ManagerFN2", "ManagerLN2")]
        [InlineData(3, null, "ManagerLN3")]
        public void GetManagerById_ExistingId_ReturnsCorrectManager(int id, string? expectedFirstName, string expectedLastName)
        {
            // On appelle la méthode GetManagerById() avec un Id existant.
            var manager = _managerRepository.GetManagerById(id);
            
            // On vérifie que le manager retourné n'est pas null.
            Assert.NotNull(manager);
            
            // On vérifie que les propriétés du manager correspondent aux valeurs attendues.
            Assert.Equal(expectedFirstName, manager?.MNG_FirstName);
            Assert.Equal(expectedLastName, manager?.MNG_LastName);
        }


        /// <summary>Vérifie que la recherche d’un manager inexistant retourne null.</summary>
        [Fact]
        public void GetManagerById_NonExistingId_ReturnsNull()
        {
            // On appelle la méthode GetManagerById() avec un Id qui n'existe pas.
            var manager = _managerRepository.GetManagerById(999);

            // On vérifie que le manager retourné est null.
            Assert.Null(manager);
        }


        /// <summary>Vérifie qu’une exception <see cref="ArgumentException"/> est levée lorsque le nom du manager est null, vide ou composé uniquement d’espaces.</summary>
        /// <param name="firstName">Prénom du manager.</param>
        /// <param name="lastName">Nom du manager, qui doit être null, vide ou composé uniquement d’espaces.</param>
        [Theory]
        [InlineData("ManagerFN", null)]
        [InlineData("ManagerFN", "")]
        [InlineData("ManagerFN", "   ")]
        [InlineData(null, "")]
        public void AddManager_LastNameNullOrEmptyOrWhitespace_ThrowsArgumentException(string? firstName, string? lastName)
        {
            // On crée un nouveau manager avec le prénom et le nom spécifiés.
            var newManager = new Manager
            {
                MNG_FirstName = firstName,
                MNG_LastName = lastName!
            };

            // On vérifie que l'ajout d'un manager avec un nom null, vide ou composé uniquement d'espaces lance une exception ArgumentException.
            var exception = Assert.Throws<ArgumentException>(() => _managerRepository.AddManager(newManager));
            Assert.Equal("manager", exception.ParamName);
        }


        /// <summary>Vérifie que l’ajout d’un manager null lève une exception <see cref="ArgumentNullException"/>.</summary>
        [Fact]
        public void AddManager_ArgumentNullManager_ThrowsArgumentNullException()
        {
            // On vérifie que l'ajout d'un manager null lance une exception ArgumentNullException.
            var exception = Assert.Throws<ArgumentNullException>(() => _managerRepository.AddManager(null!));
            Assert.Equal("manager", exception.ParamName);
        }


        /// <summary>Vérifie que l’ajout d’un manager possédant un couple prénom-nom déjà existant lève une InvalidOperationException.</summary>
        [Fact]
        public void AddManager_CoupleExisting_FirstNameLastName_ThrowsInvalidOperationException()
        {
            // On crée un manager avec un couple prénom/nom déjà existant dans la liste initiale.
            var existingManager = new Manager
            {
                MNG_FirstName = "ManagerFN1",
                MNG_LastName = "ManagerLN1"
            };

            // On vérifie que l'ajout d'un manager avec un couple prénom/nom déjà existant lance une exception InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _managerRepository.AddManager(existingManager));
        }


        /// <summary>Ajoute un manager portant le même nom de famille et un prénom différent, puis vérifie que son identifiant est 4.</summary>
        [Fact]
        public void AddManager_SameLastNameDifferentFirstName_AddSuccess()
        {
            // On crée un manager avec le même nom mais un prénom différent de ceux existants.
            var newManager = new Manager
            {
                MNG_FirstName = "UniqueFirstName",
                MNG_LastName = "ManagerLN1" // Même nom que le premier manager existant.
            };
            
            // On ajoute le nouveau manager et on récupère l'Id retourné.
            var result = _managerRepository.AddManager(newManager);
            
            // On vérifie que l'Id retourné est 4, car il s'agit du quatrième manager ajouté à la liste initiale de 3 managers.
            Assert.Equal(4, result);

            // On vérifie que le manager ajouté peut être récupéré par son Id et qu'il n'est pas null.
            Assert.NotNull(_managerRepository.GetManagerById(result));
        }


        /// <summary>Vérifie l’ajout réussi d’un manager portant le même nom qu’un manager existant et dont le prénom est null.</summary>
        [Fact]
        public void AddManager_SameLastNameWithFirstNameNullWhenOtherManagerHasFirstName_AddSuccess()
        {
            // On crée un manager avec le même nom que le troisième manager existant, mais avec un prénom null.
            var newManager = new Manager
            {
                MNG_FirstName = null,
                MNG_LastName = "ManagerLN1" // le manager 1 a le prénom "ManagerFN1"
            };

            // On ajoute le nouveau manager et on récupère l'Id retourné.
            var result = _managerRepository.AddManager(newManager);

            // On vérifie que l'Id retourné est 4, car il s'agit du quatrième manager ajouté à la liste initiale de 3 managers.
            Assert.Equal(4, result);

            // On vérifie que le manager ajouté peut être récupéré par son Id et qu'il n'est pas null.
            Assert.NotNull(_managerRepository.GetManagerById(result));
        }


        /// <summary>Vérifie que l’ajout d’un manager en double avec un prénom null lève une InvalidOperationException.</summary>
        [Fact]
        public void AddManager_DuplicateWithNullFirstName_ThrowsInvalidOperationException()
        {
            // On crée un manager avec un prénom null et un nom déjà existant dans la liste initiale.
            var duplicate = new Manager
            {
                MNG_FirstName = null,
                MNG_LastName = "ManagerLN3"   // le manager 3 a déjà (null, "ManagerLN3")
            };

            // On vérifie que l'ajout d'un manager avec un couple prénom/nom déjà existant (null, "ManagerLN3") lance une exception InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _managerRepository.AddManager(duplicate));
        }


        /// <summary>Vérifie que deux managers peuvent être ajoutés successivement avec succès.</summary>
        [Fact]
        public void AddManager_TwoAddsInSuits_AddSuccess()
        {
            // On crée un premier manager avec un prénom et un nom uniques.
            var firstManager = new Manager
            {
                MNG_FirstName = "FirstManagerFN",
                MNG_LastName = "FirstManagerLN"
            };

            // On ajoute le premier manager et on récupère l'Id retourné.
            var firstResult = _managerRepository.AddManager(firstManager);
            Assert.Equal(4, firstResult); // Le premier ajout devrait donner l'Id 4.
            
            // On crée un deuxième manager avec un prénom et un nom uniques.
            var secondManager = new Manager
            {
                MNG_FirstName = "SecondManagerFN",
                MNG_LastName = "SecondManagerLN"
            };
            
            // On ajoute le deuxième manager et on récupère l'Id retourné.
            var secondResult = _managerRepository.AddManager(secondManager);
            Assert.Equal(5, secondResult); // Le deuxième ajout devrait donner l'Id 5.
        }


        /// <summary>Vérifie qu’un gestionnaire peut être ajouté, supprimé, puis ajouté de nouveau avec succès.</summary>
        [Fact]
        public void AddManager_AddDeleteAdd_AddSuccess()
        {
            // On crée un manager avec un prénom et un nom uniques.
            var newManager = new Manager
            {
                MNG_FirstName = "TempManagerFN",
                MNG_LastName = "TempManagerLN"
            };
            
            // On ajoute le manager et on récupère l'Id retourné.
            var addResult = _managerRepository.AddManager(newManager);
            Assert.Equal(4, addResult); // Le premier ajout devrait donner l'Id 4.
            
            // On supprime le manager ajouté.
            var deleteResult = _managerRepository.DeleteManager(addResult);
            Assert.True(deleteResult); // La suppression devrait réussir.
            
            // On ajoute à nouveau un manager avec le même prénom et nom.
            var newManager2 = new Manager
            {
                MNG_FirstName = "TempManagerFN",
                MNG_LastName = "TempManagerLN"
            };

            // On ajoute le manager et on récupère l'Id retourné.
            var addResult2 = _managerRepository.AddManager(newManager2);
            Assert.Equal(5, addResult2); // Le nouvel ajout devrait donner l'Id 5.
        }


        /// <summary>Le doublon est détecté même si la casse est différente (comme la collation SQL Server par défaut).</summary>
        [Fact]
        public void AddManager_DuplicateWithDifferentCase_ThrowsInvalidOperationException()
        {
            // On crée un manager avec le même couple prénom/nom qu'un manager existant, mais avec une casse différente.
            var duplicate = new Manager { MNG_FirstName = "managerfn1", MNG_LastName = "MANAGERLN1" };

            // On vérifie que l'ajout d'un manager avec un couple prénom/nom déjà existant (même si la casse est différente) lance une exception InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _managerRepository.AddManager(duplicate));
        }


        /// <summary>Le manager ajouté est bien stocké : on le relit par son Id, avec son prénom resté null.</summary>
        [Fact]
        public void AddManager_ThenGetById_ReturnsStoredManagerWithNullFirstName()
        {
            // On ajoute un manager avec un prénom null et un nom renseigné.
            int id = _managerRepository.AddManager(new Manager { MNG_FirstName = null, MNG_LastName = "NewLN" });

            // On récupère le manager ajouté par son Id.
            var stored = _managerRepository.GetManagerById(id);

            // On vérifie que le manager récupéré n'est pas null, que son Id correspond à celui retourné par AddManager, que son prénom est null et que son nom est correct.
            Assert.NotNull(stored);
            Assert.Equal(id, stored.MNG_Id);
            Assert.Null(stored.MNG_FirstName);
            Assert.Equal("NewLN", stored.MNG_LastName);
        }


        /// <summary>Le fake stocke une copie : modifier l'objet passé à AddManager après l'ajout ne change pas les données stockées.</summary>
        [Fact]
        public void AddManager_ModifyingOriginalAfterAdd_DoesNotChangeStoredManager()
        {
            // On crée un manager avec un prénom et un nom.
            var original = new Manager { MNG_FirstName = "Jean", MNG_LastName = "Dupont" };

            // On ajoute le manager et on récupère l'Id retourné.
            int id = _managerRepository.AddManager(original);

            // On modifie le nom du manager original après l'ajout.
            original.MNG_LastName = "Modifié";

            // On récupère le manager stocké par son Id.
            Assert.Equal("Dupont", _managerRepository.GetManagerById(id)!.MNG_LastName);
        }


        /// <summary>L'Id fourni par l'appelant est ignoré : c'est le repository qui attribue l'Id (comme IDENTITY).</summary>
        [Fact]
        public void AddManager_IgnoresIdProvidedByCaller()
        {
            // On ajoute un manager avec un Id fourni par l'appelant (2), mais le repository doit ignorer cet Id et attribuer le prochain Id disponible (4).
            int id = _managerRepository.AddManager(new Manager { MNG_Id = 2, MNG_FirstName = "Jean", MNG_LastName = "Dupont" });

            // On vérifie que l'Id retourné est 4, car le repository attribue le prochain Id disponible, et que le manager avec l'Id 2 n'a pas été modifié.
            Assert.Equal(4, id);
            Assert.Equal("ManagerLN2", _managerRepository.GetManagerById(2)!.MNG_LastName); // le manager 2 n'a pas été touché
        }


        /// <summary>Vérifie la mise à jour réussie d’un manager existant.</summary>
        [Fact]
        public void UpdateManager_ExistingManager_UpdateSuccess()
        {
            // On récupère un manager existant.
            var existingManager = _managerRepository.GetManagerById(1);
            Assert.NotNull(existingManager);
            
            // On modifie le prénom du manager.
            existingManager.MNG_FirstName = "UpdatedFirstName";
            
            // On met à jour le manager dans le repository.
            var updateResult = _managerRepository.UpdateManager(existingManager);
            Assert.True(updateResult); // La mise à jour devrait réussir.
            
            // On récupère à nouveau le manager pour vérifier la mise à jour.
            var updatedManager = _managerRepository.GetManagerById(1);
            Assert.Equal("UpdatedFirstName", updatedManager?.MNG_FirstName);
        }


        /// <summary>Vérifie que la mise à jour d’un manager réussit lorsque son prénom est défini à <c>null</c>.</summary>
        [Fact]
        public void UpdateManager_UpdateFirstNameToNull_UpdateSuccess()
        {
            // On récupère un manager existant.
            var existingManager = _managerRepository.GetManagerById(2);
            Assert.NotNull(existingManager);

            // On modifie le prénom du manager pour le passer à null.
            existingManager.MNG_FirstName = null;

            // On met à jour le manager dans le repository.
            var updateResult = _managerRepository.UpdateManager(existingManager);
            Assert.True(updateResult); // La mise à jour devrait réussir.

            // On récupère à nouveau le manager pour vérifier la mise à jour.
            var updatedManager = _managerRepository.GetManagerById(2);
            Assert.Null(updatedManager?.MNG_FirstName); // Le prénom devrait être null après la mise à jour.
        }


        /// <summary>Vérifie que la mise à jour d’un manager non existant retourne false.</summary>
        [Fact]
        public void UpdateManager_UpdateNonExistingManager_ReturnsFalse()
        {
            // On crée un manager avec un Id qui n'existe pas dans le repository.
            var nonExistingManager = new Manager
            {
                MNG_Id = 999,
                MNG_FirstName = "NonExisting",
                MNG_LastName = "Manager"
            };

            // On tente de mettre à jour le manager inexistant.
            var updateResult = _managerRepository.UpdateManager(nonExistingManager);
            
            // On vérifie que la mise à jour retourne false, car le manager n'existe pas.
            Assert.False(updateResult);
        }


        /// <summary>Vérifie que la mise à jour d’un manager avec un prénom et un nom déjà associés à un autre manager lève une exception InvalidOperationException.</summary>
        [Fact]
        public void UpdateManager_UpdateToExistingFirstNameLastName_ThrowsInvalidOperationException()
        {
            // On récupère un manager existant.
            var existingManager = _managerRepository.GetManagerById(1);
            Assert.NotNull(existingManager);
            
            // On modifie le prénom et le nom du manager pour correspondre à un autre manager existant.
            existingManager.MNG_FirstName = "ManagerFN2"; // Prénom du deuxième manager.
            existingManager.MNG_LastName = "ManagerLN2";   // Nom du deuxième manager.

            // On vérifie que la mise à jour d'un manager avec un couple prénom/nom déjà existant lance une exception InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _managerRepository.UpdateManager(existingManager));
        }


        /// <summary>Vérifie que l’enregistrement d’un manager existant sans modification retourne true.</summary>
        [Fact]
        public void UpdateManager_SaveWithNoChanges_ReturnsTrue()
        {
            // On récupère un manager existant.
            var existingManager = _managerRepository.GetManagerById(1);
            Assert.NotNull(existingManager);
            
            // On tente de mettre à jour le manager sans apporter de modifications.
            var updateResult = _managerRepository.UpdateManager(existingManager);
            
            // On vérifie que la mise à jour retourne true, car le manager existe et aucune modification n'a été apportée.
            Assert.True(updateResult);
        }


        /// <summary>
        ///     Vérifie qu’une exception <see cref="InvalidOperationException"/> est levée lors de la mise à jour d’un
        ///     manager dont l’identifiant est inconnu et dont le prénom et le nom correspondent à ceux d’un manager existant.
        /// </summary>
        [Fact]
        public void UpdateManager_IdUnknownWithExistingFirstNameLastName_ReturnsFalse()
        {
            // On crée un manager avec un Id inconnu mais avec un couple prénom/nom existant.
            var unknownManager = new Manager
            {
                MNG_Id = 999, // Id inconnu
                MNG_FirstName = "ManagerFN1", // Prénom du premier manager existant
                MNG_LastName = "ManagerLN1"   // Nom du premier manager existant
            };
            
            // On vérifie que la mise à jour d'un manager avec un Id inconnu mais un couple prénom/nom existant retourne false.
            var updateResult = _managerRepository.UpdateManager(unknownManager);
            Assert.False(updateResult);
        }


        /// <summary>Un nom null, vide ou composé d'espaces est refusé à la mise à jour.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateManager_InvalidLastName_ThrowsArgumentException(string? lastName)
        {
            // On récupère un manager existant et on modifie son nom pour le rendre invalide (null, vide ou composé uniquement d'espaces).
            var manager = _managerRepository.GetManagerById(1)!;
            manager.MNG_LastName = lastName!;

            // On vérifie que la mise à jour d'un manager avec un nom null, vide ou composé uniquement d'espaces lance une exception ArgumentException.
            var exception = Assert.Throws<ArgumentException>(() => _managerRepository.UpdateManager(manager));
            Assert.Equal("manager", exception.ParamName);
        }


        /// <summary>Un manager null est refusé.</summary>
        [Fact]
        public void UpdateManager_NullManager_ThrowsArgumentNullException()
        {
            // On vérifie que la mise à jour d'un manager null lance une exception ArgumentNullException.
            var exception = Assert.Throws<ArgumentNullException>(() => _managerRepository.UpdateManager(null!));
            Assert.Equal("manager", exception.ParamName);
        }


        /// <summary>Le nom modifié est bien relu après la mise à jour.</summary>
        [Fact]
        public void UpdateManager_ChangeLastName_UpdateSuccess()
        {
            // On récupère un manager existant et on modifie son nom.
            var manager = _managerRepository.GetManagerById(1)!;
            manager.MNG_LastName = "NewLastName";

            // On met à jour le manager dans le repository.
            Assert.True(_managerRepository.UpdateManager(manager));

            // On récupère à nouveau le manager pour vérifier que le nom a été mis à jour.
            var updated = _managerRepository.GetManagerById(1);
            Assert.Equal("NewLastName", updated!.MNG_LastName);
            Assert.Equal("ManagerFN1", updated.MNG_FirstName); // le prénom n'a pas changé
        }


        /// <summary>NULL == NULL aussi à la mise à jour : (null, "ManagerLN3") existe déjà (manager 3).</summary>
        [Fact]
        public void UpdateManager_ToExistingCoupleWithNullFirstName_ThrowsInvalidOperationException()
        {
            // On récupère le manager avec l'Id 1 et on modifie son prénom pour le passer à null et son nom pour correspondre à celui du manager 3.
            var manager = _managerRepository.GetManagerById(1)!;
            manager.MNG_FirstName = null;
            manager.MNG_LastName = "ManagerLN3";

            // On vérifie que la mise à jour d'un manager avec un couple prénom/nom déjà existant (null, "ManagerLN3") lance une exception InvalidOperationException.
            Assert.Throws<InvalidOperationException>(() => _managerRepository.UpdateManager(manager));
        }


        /// <summary>Une mise à jour ne change ni le nombre de managers ni leur ordre.</summary>
        [Fact]
        public void UpdateManager_DoesNotChangeCountOrOrder()
        {
            // On modifie le nom du manager avec l'Id 1.
            var manager = _managerRepository.GetManagerById(1)!;
            manager.MNG_LastName = "NewLastName";
            _managerRepository.UpdateManager(manager);

            // On récupère tous les managers après la mise à jour.
            var ids = _managerRepository.GetAllManagers().Select(m => m.MNG_Id).ToList();

            // On vérifie que le nombre de managers est toujours 3 et que l'ordre des Id est inchangé.
            Assert.Equal(new[] { 1, 2, 3 }, ids);
        }


        /// <summary>Supprime un manager existant et vérifie que sa suppression réussit.</summary>
        /// <remarks>Vérifie également que le manager supprimé n'est plus récupérable.</remarks>
        [Fact]
        public void DeleteManager_ExistingManager_ReturnsTrue()
        {
            // On supprime un manager existant.
            var deleteResult = _managerRepository.DeleteManager(1);

            // On vérifie que la suppression retourne true, car le manager existe.
            Assert.True(deleteResult);

            // On tente de récupérer le manager supprimé pour vérifier qu'il n'existe plus.
            var deletedManager = _managerRepository.GetManagerById(1);
            Assert.Null(deletedManager);
        }


        /// <summary>Supprime un manager qui n'existe pas et vérifie que sa suppression retourne false.</summary>
        [Fact]
        public void DeleteManager_NonExistingManager_ReturnsFalse()
        {
            // On tente de supprimer un manager qui n'existe pas.
            var deleteResult = _managerRepository.DeleteManager(999);
            
            // On vérifie que la suppression retourne false, car le manager n'existe pas.
            Assert.False(deleteResult);
        }

    }
}
