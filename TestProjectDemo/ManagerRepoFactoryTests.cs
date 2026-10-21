using DemoRepo.Repositories;

namespace TestProjectDemo
{
    public class ManagerRepoFactoryTests
    {
        /** 7
         * ManagerRepoFactoryTests.cs teste la factory pour s’assurer qu’elle crée bien les bons types de repository selon le type demandé.
         * On teste aussi les cas d’erreur pour s’assurer que la factory se comporte correctement.
         */


        // Test 1 : Vérifie que la factory crée bien un FakeManagerRepository lorsque le type est RepositoryType.Fake.
        [Fact]
        public void CreateManagerRepository_Fake_ReturnsFakeManagerRepository()
        {
            // La factory doit créer un FakeManagerRepository lorsque le type est RepositoryType.Fake.
            var repo = ManagerRepoFactory.CreateManagerRepository(RepositoryType.Fake);

            // On vérifie que le type de l’objet retourné est bien FakeManagerRepository.
            Assert.IsType<FakeManagerRepository>(repo);
        }


        // Test 2 : Vérifie que la factory crée bien un AdoNetManagerRepository lorsque le type est RepositoryType.AdoNet et qu’une chaîne de connexion est fournie.
        [Fact]
        public void CreateManagerRepository_AdoNetWithConnectionString_ReturnsAdoNetManagerRepository()
        {
            // La connexion n'est pas ouverte à la création : pas besoin d'un vrai serveur.
            var repo = ManagerRepoFactory.CreateManagerRepository(
                RepositoryType.AdoNet, "Server=localhost;Database=Fake;Integrated Security=True;");

            // On vérifie que le type de l’objet retourné est bien AdoNetManagerRepository.
            Assert.IsType<AdoNetManagerRepository>(repo);
        }


        // Test 3 : Vérifie que la factory lance une exception lorsque le type est RepositoryType.AdoNet mais qu’aucune chaîne de connexion n’est fournie.
        [Fact]
        public void CreateManagerRepository_AdoNetWithoutConnectionString_ThrowsArgumentException()
        {
            // On s’attend à ce que la factory lance une exception ArgumentException lorsque la chaîne de connexion est null.
            var exception = Assert.Throws<ArgumentException>(
                () => ManagerRepoFactory.CreateManagerRepository(RepositoryType.AdoNet, null));

            // On vérifie que le nom du paramètre dans l’exception est bien "connectionString".
            Assert.Equal("connectionString", exception.ParamName);
        }


        // Test 4 : Vérifie que la factory lance une exception lorsque le type de repository est invalide.
        [Fact]
        public void CreateManagerRepository_InvalidType_ThrowsArgumentException()
        {
            // On s’attend à ce que la factory lance une exception ArgumentException lorsque le type de repository est invalide.
            var exception = Assert.Throws<ArgumentException>(
                () => ManagerRepoFactory.CreateManagerRepository((RepositoryType)999));

            // On vérifie que le nom du paramètre dans l’exception est bien "repositoryType".
            Assert.Equal("repositoryType", exception.ParamName);
        }
    }
}