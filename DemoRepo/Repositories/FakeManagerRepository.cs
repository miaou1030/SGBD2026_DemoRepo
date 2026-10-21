using DemoRepo.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DemoRepo.Repositories
{
    public class FakeManagerRepository : IManagerRepository
    {
        /** 3
         * FakeManagerRepository.cs permet de tester la logique et la console sans base de données, et il sert de comportement de référence. 
         * Il doit donc imiter les règles de SQL Server, sinon un test qui passe avec le fake pourrait échouer avec la vraie base.
         */



        /// <summary>
        ///     On initialise la liste avec 3 managers pour tester la console et les tests unitaires.
        ///     private readonly car on ne veut pas que la liste soit modifiée par l’extérieur de la classe.
        ///     _managers est une convention de nommage pour les champs privés.
        /// </summary>
        private readonly List<Manager> _managers = new()
        {
            new Manager { MNG_Id = 1, MNG_FirstName = "ManagerFN1", MNG_LastName = "ManagerLN1" },
            new Manager { MNG_Id = 2, MNG_FirstName = "ManagerFN2", MNG_LastName = "ManagerLN2" },
            new Manager { MNG_Id = 3, MNG_FirstName = null, MNG_LastName = "ManagerLN3" }
        };



        /** 
         * On initialise à 4 car on a déjà 3 managers dans la liste. Cela permet de simuler l’auto-incrémentation de l’Id dans SQL Server.
         * Chaque fois qu’on ajoute un manager, on incrémente _nextId pour simuler la génération d’un nouvel Id unique.
         */
        private int _nextId = 4; 


        /// <summary>Ajoute un manager et lui attribue un Id.</summary>
        /// <param name="manager">Manager à ajouter.</param>
        /// <returns>Id attribué au manager.</returns>
        public int AddManager(Manager manager)
        {
            // On vérifie que le manager n’est pas null pour éviter une exception plus tard.
            if (manager is null)
            {
                throw new ArgumentNullException(nameof(manager), "Le manager ne peut pas être null.");
            }

            /** 
             * On vérifie que le nom du manager n’est pas vide ou composé uniquement d’espaces.
             * On ne vérifie pas MNG_FirstName car il peut être null ou vide selon les règles du domaine.
             */
            if (string.IsNullOrWhiteSpace(manager.MNG_LastName))
            {
                throw new ArgumentException("Le nom du manager ne peut pas être vide.", nameof(manager));
            }
                         
            /**
             * On vérifie qu'aucun autre manager n'a le même couple de prénom et de nom.
             * On utilise Any() pour vérifier si au moins un manager dans la liste _managers a le même couple de prénom et de nom que le manager passé en paramètre.
             * On utilise StringComparison.OrdinalIgnoreCase pour ignorer la casse lors de la comparaison.
             */
            if (_managers.Any(m =>
                    string.Equals(m.MNG_FirstName, manager.MNG_FirstName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.MNG_LastName, manager.MNG_LastName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Un autre manager a déjà ce couple de prénom et de nom.");
            }


            // cloneManager est une copie du manager passé en paramètre pour éviter que le code appelant ne modifie directement la liste interne.
            var cloneManager = Clone(manager);

            // On incrémente _nextId pour simuler l’auto-incrémentation de l’Id dans SQL Server et on attribue le nouvel Id au manager cloné.
            cloneManager.MNG_Id = _nextId++;

            // On ajoute le manager cloné à la liste interne des managers. On utilise Add pour ajouter un élément à la liste puis on retourne l’Id du manager.
            _managers.Add(cloneManager);
            return cloneManager.MNG_Id;
        }


        /// <summary>Supprime le manager associé à l’Id spécifié.</summary>
        /// <param name="managerId">Id du manager à supprimer.</param>
        /// <returns>true si au moins un manager a été supprimé ; sinon, false.</returns>
        public bool DeleteManager(int managerId)
        {
            // RemoveAll retourne le nombre d’éléments supprimés, donc on vérifie si c’est supérieur à 0 pour savoir si un manager a été supprimé.
            return _managers.RemoveAll(m => m.MNG_Id == managerId) > 0;
        }


        /// <summary>Obtient tous les managers.</summary>
        /// <returns>Une collection contenant des copies de tous les managers.</returns>
        public IEnumerable<Manager> GetAllManagers()
        {
            // On retourne une copie de la liste des managers pour éviter que le code appelant ne modifie directement la liste interne.
            return _managers.Select(Clone).ToList();
        }


        /// <summary>
        /// Récupère un manager à partir de son Id.
        /// </summary>
        /// <param name="managerId">Id du manager à récupérer.</param>
        /// <returns>Le manager correspondant, ou `null` si aucun gestionnaire ne possède cet Id.</returns>
        public Manager? GetManagerById(int managerId)
        {
            // On cherche le manager dans la liste par son Id. Si aucun manager n’est trouvé, on retourne null.
            var findManager = _managers.Find(m => m.MNG_Id == managerId);
            return findManager is null ? null : Clone(findManager);
        }
               

        /// <summary>Màj un manager existant.</summary>
        /// <param name="manager">Manager contenant les nouvelles valeurs.</param>
        /// <returns>retourne true si le manager a été màj ; sinon, false.</returns>
        public bool UpdateManager(Manager manager)
        {
            // On vérifie que le manager n’est pas null pour éviter une exception plus tard.
            if (manager is null)
            {
                throw new ArgumentNullException(nameof(manager), "Le manager ne peut pas être null.");
            }

            // On cherche le manager existant dans la liste par son Id.
            var existingManager = _managers.Find(m => m.MNG_Id == manager.MNG_Id);

            // Si le manager n’existe pas, on retourne false.
            if (existingManager is null)
            {
                return false;
            }

            /** 
             * On vérifie que le nom du manager n’est pas vide ou composé uniquement d’espaces.
             * On ne vérifie pas MNG_FirstName car il peut être null ou vide selon les règles du domaine.
             */
            if (string.IsNullOrWhiteSpace(manager.MNG_LastName))
            {
                throw new ArgumentException("Le nom du manager ne peut pas être vide.", nameof(manager));
            }

            // On vérifie qu'aucun autre manager n'a le même couple de prénom et de nom.
            /**
             * On utilise Any pour vérifier si un manager existe déjà avec le même couple de prénom et de nom.
             * On exclut le manager en cours de mise à jour (m.MNG_Id != manager.MNG_Id) pour permettre la mise à jour d'un manager existant.
             * On utilise StringComparison.OrdinalIgnoreCase pour ignorer la casse lors de la comparaison des chaînes.
             */

            if (_managers.Any(m => m.MNG_Id != manager.MNG_Id && string.Equals(m.MNG_FirstName, manager.MNG_FirstName, StringComparison.OrdinalIgnoreCase) && string.Equals(m.MNG_LastName, manager.MNG_LastName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Un autre manager a déjà ce couple de prénom et de nom.");
            }
                       
            // On met à jour les propriétés du manager existant avec les nouvelles valeurs.
            existingManager.MNG_FirstName = manager.MNG_FirstName;
            existingManager.MNG_LastName = manager.MNG_LastName;
            return true;
        }


        /// <summary>Crée une copie d’un manager pour usage interne car les objets peuvent être modifiés après leur création.</summary>
        /// <param name="m">Manager à cloner.</param>
        /// <returns>Nouvelle instance de <see cref="Manager"/> contenant les mêmes valeurs que <paramref name="m"/>.</returns>
        private static Manager Clone(Manager m) => new()
        {
            MNG_Id = m.MNG_Id,
            MNG_FirstName = m.MNG_FirstName,
            MNG_LastName = m.MNG_LastName
        };
    }
}
