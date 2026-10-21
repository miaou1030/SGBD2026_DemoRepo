using System;
using System.Collections.Generic;
using System.Text;
using DemoRepo.Models;

namespace DemoRepo.Repositories
{
    /** 2
     * IManagerRepository.cs est le contrat que les deux implémentations (fake et ADO.NET) devront respecter. 
     * Le code appelant (console, tests) ne dépendra que de cette interface et pourra changer d’implémentation sans modification.
     */



    public interface IManagerRepository
    {
        /// <summary>GetAllManagers() retourne tous les managers. On utilise IEnumerable<Manager> car on veut retourner une collection de managers.</summary>
        IEnumerable<Manager> GetAllManagers();


        /// <summary>Récupère le manager correspondant à l'Id spécifié.</summary>
        /// <param name="managerId">Id du manager à rechercher.</param>
        /// <returns>Le manager correspondant à l’Id spécifié, ou null s’il est introuvable.</returns>
        Manager? GetManagerById(int managerId);
        
        
        /// <summary>Ajoute un nouveau manager.</summary>
        /// <param name="manager">Manager à ajouter.</param>
        /// <returns>Le resultat de l'ajout du manager.</returns>
        int AddManager(Manager manager);

        
        /// <summary>Met à jour le manager spécifié.</summary>
        /// <param name="manager">Manager à mettre à jour.</param>
        /// <returns>true si la mise à jour réussit ; sinon, false.</returns>
        bool UpdateManager(Manager manager);

        
        /// <summary>Supprime le manager correspondant à l’Id spécifié.</summary>
        /// <param name="managerId">Id du manager à supprimer.</param>
        /// <returns>true si le manager a été supprimé ; sinon, false.</returns>
        bool DeleteManager(int managerId);
    }
}