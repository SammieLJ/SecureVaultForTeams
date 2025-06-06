using Xunit;
using BCrypt.Net;
using SecureVaultForTeams.Services;
using System.IO;

namespace SecureVaultForTeams.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void PasswordHashing_And_Verification_Works()
        {
            string password = "TestPassword123!";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
            Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword", hash));
        }

        [Fact]
        public void Export_And_Import_MigrationData_Works()
        {
            var dbService = new DatabaseService("Filename=:memory:");
            string path = "test_data.json";
            dbService.ExportToJson(path);
            Assert.True(File.Exists(path));
            dbService.ImportFromJson(path); // Should not throw
            File.Delete(path);
        }
    }
}
