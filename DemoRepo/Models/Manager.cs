using System;
using System.Collections.Generic;
using System.Text;

namespace DemoRepo.Models
{
    public class Manager
    {
        /** 1
         * Manager.cs est important car tout le reste (interface, fake, SQL, tests) manipule cet objet. 
         * Il doit refléter fidèlement la table SQL, donc on fixe d’abord les règles du domaine :
         * 
         *  Champ           TypeC#      Pourquoi?
         *  MNG_Id          int         Clé primaire IDENTITY, générée par SQL Server, comme pour Product.
         *  MNG_FirstName   string?     Le prénom peut être NULL. Le ? dit au compilateur que le NULL est autorisé.
         *  MNG_LastName    string      Le nom est obligatoire (NOT NULL).
         */

        

        /// <summary>Id du manager.</summary>
        public int MNG_Id { get; set; }


        /// <summary>string? dit que que la variable peut être null car on ne veut pas forcer l'utilisateur à entrer un prénom.</summary>
        public string? MNG_FirstName { get; set; }


        /// <summary>string.Empty dit que la variable est initialisée à une chaîne vide car on ne veut pas qu'elle soit null</summary>
        public string MNG_LastName { get; set; } = string.Empty;


        /// <summary>MNG_FirstName ?? "N/A" dit que si MNG_FirstName est null, on retourne "N/A" car on ne veut pas afficher null dans la console</summary>
        public override string ToString()
        {
            return $"Manager Id: {MNG_Id}, FirstName: {MNG_FirstName ?? "N/A"}, LastName: {MNG_LastName}";
        }
    }
}
