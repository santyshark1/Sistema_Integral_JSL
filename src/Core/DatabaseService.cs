using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Servicio de base de datos SQLite para JSL SentinelPro.
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _connection;
        private readonly object _lock = new object();

        public DatabaseService(string dbPath)
        {
            string directory = Path.GetDirectoryName(dbPath)!;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _connectionString = $"Data Source={dbPath};Foreign Keys=True;";
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }

        /// <summary>
        /// Inicializa la base de datos creando todas las tablas necesarias.
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    AccountType TEXT DEFAULT 'Usuario estandar' CHECK(AccountType IN ('Usuario estandar', 'Administrador')),
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastLogin DATETIME,
                    IsActive INTEGER DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS PasswordResets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Email TEXT NOT NULL,
                    Token TEXT NOT NULL,
                    ExpiresAt DATETIME NOT NULL,
                    IsUsed INTEGER DEFAULT 0,
                    TempPassword TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS HardwareScans (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScanDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CpuUsage REAL,
                    RamUsedBytes INTEGER,
                    RamTotalBytes INTEGER,
                    DiskUsedBytes INTEGER,
                    DiskTotalBytes INTEGER,
                    MaxTemperature REAL,
                    Status TEXT
                );

                CREATE TABLE IF NOT EXISTS ThreatDetections (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DetectionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ThreatName TEXT,
                    FilePath TEXT,
                    ActionTaken TEXT CHECK(ActionTaken IN ('Eliminado', 'Cuarentena', 'Ignorado')),
                    Severity TEXT CHECK(Severity IN ('Critica', 'Alta', 'Media', 'Baja'))
                );

                CREATE TABLE IF NOT EXISTS MaintenanceLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ActionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ActionType TEXT CHECK(ActionType IN ('Limpieza', 'Optimizacion')),
                    SpaceFreedBytes INTEGER,
                    Details TEXT
                );

                CREATE TABLE IF NOT EXISTS SystemSnapshots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CpuUsage REAL,
                    RamUsedPercent REAL,
                    DiskUsedPercent REAL,
                    MaxTemp REAL,
                    NetworkSpeedMbps REAL
                );

                CREATE TABLE IF NOT EXISTS CompanyPartners (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Specialty TEXT,
                    City TEXT,
                    Address TEXT,
                    Rating REAL DEFAULT 0,
                    IsAvailable INTEGER DEFAULT 1,
                    Phone TEXT,
                    Email TEXT,
                    ImagePath TEXT,
                    HasWarranty INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS PartnerAppointments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CompanyName TEXT NOT NULL,
                    City TEXT,
                    Specialty TEXT,
                    Contact TEXT,
                    Status TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
                CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
                CREATE INDEX IF NOT EXISTS idx_password_resets_token ON PasswordResets(Token);
                CREATE INDEX IF NOT EXISTS idx_snapshots_timestamp ON SystemSnapshots(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_threats_date ON ThreatDetections(DetectionDate);
            ";

            using var cmd = new SqliteCommand(sql, _connection);
            await cmd.ExecuteNonQueryAsync();
            await MigrateDatabaseAsync();
            await SeedDefaultDataAsync();
        }

        private async Task MigrateDatabaseAsync()
        {
            await MigrateUsersAsync();
            await MigrateMaintenanceLogsAsync();
            await MigrateThreatDetectionsAsync();
        }

        private async Task MigrateUsersAsync()
        {
            using var schemaCmd = new SqliteCommand("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Users'", _connection);
            var schema = (await schemaCmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            if (schema.Contains("Usuario estandar"))
                return;

            using var tx = _connection.BeginTransaction();
            try
            {
                var migrationSql = @"
                    CREATE TABLE IF NOT EXISTS Users_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName TEXT NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        AccountType TEXT DEFAULT 'Usuario estandar' CHECK(AccountType IN ('Usuario estandar', 'Administrador')),
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LastLogin DATETIME,
                        IsActive INTEGER DEFAULT 1
                    );

                    INSERT INTO Users_new (Id, FullName, Email, Username, PasswordHash, AccountType, CreatedAt, LastLogin, IsActive)
                    SELECT Id,
                           FullName,
                           Email,
                           Username,
                           PasswordHash,
                           CASE
                               WHEN AccountType LIKE 'Usuario est%' THEN 'Usuario estandar'
                               ELSE AccountType
                           END,
                           CreatedAt,
                           LastLogin,
                           IsActive
                    FROM Users;

                    DROP TABLE Users;
                    ALTER TABLE Users_new RENAME TO Users;
                    CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
                    CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
                ";

                using var cmd = new SqliteCommand(migrationSql, _connection, tx);
                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private async Task MigrateMaintenanceLogsAsync()
        {
            using var schemaCmd = new SqliteCommand("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'MaintenanceLogs'", _connection);
            var schema = (await schemaCmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            if (schema.Contains("Optimizacion"))
                return;

            using var tx = _connection.BeginTransaction();
            try
            {
                var migrationSql = @"
                    CREATE TABLE IF NOT EXISTS MaintenanceLogs_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ActionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ActionType TEXT CHECK(ActionType IN ('Limpieza', 'Optimizacion')),
                        SpaceFreedBytes INTEGER,
                        Details TEXT
                    );

                    INSERT INTO MaintenanceLogs_new (Id, ActionDate, ActionType, SpaceFreedBytes, Details)
                    SELECT Id,
                           ActionDate,
                           CASE
                               WHEN ActionType LIKE 'Optimizaci%' THEN 'Optimizacion'
                               ELSE ActionType
                           END,
                           SpaceFreedBytes,
                           Details
                    FROM MaintenanceLogs;

                    DROP TABLE MaintenanceLogs;
                    ALTER TABLE MaintenanceLogs_new RENAME TO MaintenanceLogs;
                ";

                using var cmd = new SqliteCommand(migrationSql, _connection, tx);
                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private async Task MigrateThreatDetectionsAsync()
        {
            using var schemaCmd = new SqliteCommand("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'ThreatDetections'", _connection);
            var schema = (await schemaCmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            if (schema.Contains("Critica"))
                return;

            using var tx = _connection.BeginTransaction();
            try
            {
                var migrationSql = @"
                    CREATE TABLE IF NOT EXISTS ThreatDetections_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DetectionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ThreatName TEXT,
                        FilePath TEXT,
                        ActionTaken TEXT CHECK(ActionTaken IN ('Eliminado', 'Cuarentena', 'Ignorado')),
                        Severity TEXT CHECK(Severity IN ('Critica', 'Alta', 'Media', 'Baja'))
                    );

                    INSERT INTO ThreatDetections_new (Id, DetectionDate, ThreatName, FilePath, ActionTaken, Severity)
                    SELECT Id,
                           DetectionDate,
                           ThreatName,
                           FilePath,
                           ActionTaken,
                           CASE
                               WHEN Severity LIKE 'Cr%tica' THEN 'Critica'
                               ELSE Severity
                           END
                    FROM ThreatDetections;

                    DROP TABLE ThreatDetections;
                    ALTER TABLE ThreatDetections_new RENAME TO ThreatDetections;
                    CREATE INDEX IF NOT EXISTS idx_threats_date ON ThreatDetections(DetectionDate);
                ";

                using var cmd = new SqliteCommand(migrationSql, _connection, tx);
                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private async Task SeedDefaultDataAsync()
        {
            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM Users WHERE Username = 'admin'", _connection);
            var count = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L);
            if (count == 0)
            {
                var hash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                using var cmd = new SqliteCommand(@"
                    INSERT INTO Users (FullName, Email, Username, PasswordHash, AccountType, CreatedAt, IsActive)
                    VALUES ('Administrador del Sistema', 'admin@sentinelpro.com', 'admin', @hash, 'Administrador', CURRENT_TIMESTAMP, 1)
                ", _connection);
                cmd.Parameters.AddWithValue("@hash", hash);
                await cmd.ExecuteNonQueryAsync();
            }

            using var partnerCheck = new SqliteCommand("SELECT COUNT(*) FROM CompanyPartners", _connection);
            var pCount = (long)(await partnerCheck.ExecuteScalarAsync() ?? 0L);
            if (pCount == 0)
            {
                var partners = new[]
                {
                    ("TechCore Solutions", "Reparacion de hardware", "Bogota", "Calle 123 #45-67", 4.8, "+57 1 2345678", "techcore@ejemplo.com", 1),
                    ("DataSafe Pro", "Recuperacion de datos", "Medellin", "Carrera 45 #12-34", 4.6, "+57 4 8765432", "datasafe@ejemplo.com", 1),
                    ("SecureIT Centro", "Seguridad informatica", "Cali", "Av. Circunvalar #98-76", 4.9, "+57 2 3456789", "secureit@ejemplo.com", 1),
                    ("PC Master Tecnica", "Mantenimiento preventivo", "Barranquilla", "Calle 72 #43-21", 4.5, "+57 5 2345678", "pcmaster@ejemplo.com", 0),
                    ("RedSpeed Networks", "Redes y conectividad", "Cartagena", "Av. San Martin #11-22", 4.7, "+57 5 8761234", "redspeed@ejemplo.com", 1),
                    ("CyberShield Lab", "Auditoria de seguridad", "Bucaramanga", "Calle 33 #55-44", 4.8, "+57 7 6543210", "cybershield@ejemplo.com", 1)
                };

                foreach (var (name, specialty, city, address, rating, phone, email, warranty) in partners)
                {
                    using var cmd = new SqliteCommand(@"
                        INSERT INTO CompanyPartners (Name, Specialty, City, Address, Rating, IsAvailable, Phone, Email, HasWarranty)
                        VALUES (@name, @specialty, @city, @address, @rating, 1, @phone, @email, @warranty)
                    ", _connection);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@specialty", specialty);
                    cmd.Parameters.AddWithValue("@city", city);
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.Parameters.AddWithValue("@rating", rating);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@warranty", warranty);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await SeedExpandedPartnersAsync();
        }

        private async Task SeedExpandedPartnersAsync()
        {
            var partners = new[]
            {
                ("SSD Express Bogota", "Actualizacion SSD/RAM", "Bogota", "Cra 15 #88-21", 4.9, "+57 1 6011122", "ssdexpress.bogota@ejemplo.com", 1),
                ("CoolerLab Capital", "Refrigeracion y temperaturas", "Bogota", "Calle 80 #20-10", 4.8, "+57 1 6011133", "coolerlab.bogota@ejemplo.com", 1),
                ("RAM Pro Center", "Actualizacion SSD/RAM", "Bogota", "Av Suba #102-44", 4.7, "+57 1 6011144", "rampro.bogota@ejemplo.com", 1),
                ("Guardian PC Bogota", "Seguridad informatica", "Bogota", "Calle 72 #11-35", 4.8, "+57 1 6011155", "guardian.bogota@ejemplo.com", 1),
                ("Mantenimiento Elite BG", "Mantenimiento preventivo", "Bogota", "Cra 7 #45-18", 4.6, "+57 1 6011166", "elite.bogota@ejemplo.com", 0),
                ("Medellin SSD Lab", "Actualizacion SSD/RAM", "Medellin", "Calle 10 #43A-31", 4.9, "+57 4 6042211", "ssd.medellin@ejemplo.com", 1),
                ("Paisa Cooling Tech", "Refrigeracion y temperaturas", "Medellin", "Av El Poblado #18-22", 4.8, "+57 4 6042222", "cooling.medellin@ejemplo.com", 1),
                ("RAM Andes Medellin", "Actualizacion SSD/RAM", "Medellin", "Cra 70 #44B-12", 4.7, "+57 4 6042233", "ram.medellin@ejemplo.com", 1),
                ("CyberShield Medellin", "Seguridad informatica", "Medellin", "Calle 50 #49-40", 4.8, "+57 4 6042244", "cyber.medellin@ejemplo.com", 1),
                ("PC Vital Medellin", "Mantenimiento preventivo", "Medellin", "Cra 80 #35-60", 4.6, "+57 4 6042255", "vital.medellin@ejemplo.com", 0),
                ("Cali Upgrade Center", "Actualizacion SSD/RAM", "Cali", "Av 6N #23-45", 4.9, "+57 2 6023311", "upgrade.cali@ejemplo.com", 1),
                ("FrioPC Cali", "Refrigeracion y temperaturas", "Cali", "Calle 5 #38-20", 4.8, "+57 2 6023322", "friopc.cali@ejemplo.com", 1),
                ("RAM Pacifico", "Actualizacion SSD/RAM", "Cali", "Cra 66 #9-14", 4.7, "+57 2 6023333", "ram.pacifico@ejemplo.com", 1),
                ("Seguro Digital Cali", "Seguridad informatica", "Cali", "Calle 13 #65-30", 4.8, "+57 2 6023344", "seguro.cali@ejemplo.com", 1),
                ("Cali PC Care", "Mantenimiento preventivo", "Cali", "Av Roosevelt #29-55", 4.6, "+57 2 6023355", "care.cali@ejemplo.com", 0),
                ("Barranquilla SSD Pro", "Actualizacion SSD/RAM", "Barranquilla", "Cra 53 #79-112", 4.9, "+57 5 6054411", "ssd.barranquilla@ejemplo.com", 1),
                ("Caribe Cooling", "Refrigeracion y temperaturas", "Barranquilla", "Calle 84 #46-25", 4.8, "+57 5 6054422", "cooling.barranquilla@ejemplo.com", 1),
                ("RAM Caribe Express", "Actualizacion SSD/RAM", "Barranquilla", "Cra 43 #72-88", 4.7, "+57 5 6054433", "ram.caribe@ejemplo.com", 1),
                ("Secure Norte", "Seguridad informatica", "Barranquilla", "Calle 76 #54-11", 4.8, "+57 5 6054444", "secure.norte@ejemplo.com", 1),
                ("PC Salud Caribe", "Mantenimiento preventivo", "Barranquilla", "Cra 51B #82-90", 4.6, "+57 5 6054455", "salud.caribe@ejemplo.com", 0),
                ("Cartagena Upgrade Lab", "Actualizacion SSD/RAM", "Cartagena", "Bocagrande Cra 3 #8-44", 4.9, "+57 5 6055511", "upgrade.cartagena@ejemplo.com", 1),
                ("CoolTech Cartagena", "Refrigeracion y temperaturas", "Cartagena", "Manga Calle 29 #22-18", 4.8, "+57 5 6055522", "cool.cartagena@ejemplo.com", 1),
                ("RAM Heroica", "Actualizacion SSD/RAM", "Cartagena", "Av Pedro de Heredia #45-19", 4.7, "+57 5 6055533", "ram.heroica@ejemplo.com", 1),
                ("Fortaleza Digital", "Seguridad informatica", "Cartagena", "Centro Calle 36 #7-21", 4.8, "+57 5 6055544", "fortaleza.cartagena@ejemplo.com", 1),
                ("PC Care Cartagena", "Mantenimiento preventivo", "Cartagena", "Crespo Calle 70 #4-12", 4.6, "+57 5 6055555", "care.cartagena@ejemplo.com", 0),
                ("Bucaramanga SSD Max", "Actualizacion SSD/RAM", "Bucaramanga", "Cra 33 #48-12", 4.9, "+57 7 6076611", "ssd.bucaramanga@ejemplo.com", 1),
                ("Santander Cooling", "Refrigeracion y temperaturas", "Bucaramanga", "Calle 56 #31-40", 4.8, "+57 7 6076622", "cooling.santander@ejemplo.com", 1),
                ("RAM Oriente", "Actualizacion SSD/RAM", "Bucaramanga", "Cra 27 #36-25", 4.7, "+57 7 6076633", "ram.oriente@ejemplo.com", 1),
                ("Bucara Secure Lab", "Seguridad informatica", "Bucaramanga", "Calle 45 #28-55", 4.8, "+57 7 6076644", "secure.bucara@ejemplo.com", 1),
                ("PC Vital Santander", "Mantenimiento preventivo", "Bucaramanga", "Cra 35 #52-70", 4.6, "+57 7 6076655", "vital.santander@ejemplo.com", 0)
            };

            foreach (var (name, specialty, city, address, rating, phone, email, warranty) in partners)
            {
                using var exists = new SqliteCommand("SELECT COUNT(*) FROM CompanyPartners WHERE Name = @name", _connection);
                exists.Parameters.AddWithValue("@name", name);
                var count = (long)(await exists.ExecuteScalarAsync() ?? 0L);
                if (count > 0)
                    continue;

                using var cmd = new SqliteCommand(@"
                    INSERT INTO CompanyPartners (Name, Specialty, City, Address, Rating, IsAvailable, Phone, Email, HasWarranty)
                    VALUES (@name, @specialty, @city, @address, @rating, 1, @phone, @email, @warranty)
                ", _connection);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@specialty", specialty);
                cmd.Parameters.AddWithValue("@city", city);
                cmd.Parameters.AddWithValue("@address", address);
                cmd.Parameters.AddWithValue("@rating", rating);
                cmd.Parameters.AddWithValue("@phone", phone);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@warranty", warranty);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> RegisterUserAsync(User user, string password)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            using var cmd = new SqliteCommand(@"
                INSERT INTO Users (FullName, Email, Username, PasswordHash, AccountType, CreatedAt, IsActive)
                VALUES (@fullName, @email, @username, @hash, @accountType, CURRENT_TIMESTAMP, 1);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@fullName", user.FullName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@accountType", user.AccountType);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            using var cmd = new SqliteCommand("SELECT * FROM Users WHERE Username = @username AND IsActive = 1", _connection);
            cmd.Parameters.AddWithValue("@username", username);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var hash = reader.GetString(reader.GetOrdinal("PasswordHash"));
            if (!BCrypt.Net.BCrypt.Verify(password, hash)) return null;

            var user = MapUser(reader);
            using var updateCmd = new SqliteCommand("UPDATE Users SET LastLogin = CURRENT_TIMESTAMP WHERE Id = @id", _connection);
            updateCmd.Parameters.AddWithValue("@id", user.Id);
            await updateCmd.ExecuteNonQueryAsync();
            return user;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using var cmd = new SqliteCommand("SELECT * FROM Users WHERE Email = @email AND IsActive = 1", _connection);
            cmd.Parameters.AddWithValue("@email", email);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            using var cmd = new SqliteCommand("SELECT * FROM Users WHERE Username = @username AND IsActive = 1", _connection);
            cmd.Parameters.AddWithValue("@username", username);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUser(reader) : null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var cmd = new SqliteCommand("SELECT * FROM Users WHERE IsActive = 1 ORDER BY CreatedAt DESC", _connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(MapUser(reader));
            return users;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var cmd = new SqliteCommand(@"
                UPDATE Users SET FullName = @fullName, Email = @email, AccountType = @accountType
                WHERE Id = @id
            ", _connection);
            cmd.Parameters.AddWithValue("@fullName", user.FullName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@accountType", user.AccountType);
            cmd.Parameters.AddWithValue("@id", user.Id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            using var cmd = new SqliteCommand("UPDATE Users SET IsActive = 0 WHERE Id = @id", _connection);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            using var cmd = new SqliteCommand("UPDATE Users SET PasswordHash = @hash WHERE Id = @id", _connection);
            cmd.Parameters.AddWithValue("@hash", newPasswordHash);
            cmd.Parameters.AddWithValue("@id", userId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> CreatePasswordResetAsync(string email, string token, string tempPassword)
        {
            var tempHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            await ExpirePreviousPasswordResetsAsync(email);
            using var cmd = new SqliteCommand(@"
                INSERT INTO PasswordResets (Email, Token, ExpiresAt, IsUsed, TempPassword)
                VALUES (@email, @token, datetime('now', '+1 hour'), 0, @tempPassword);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@tempPassword", tempHash);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task ExpirePreviousPasswordResetsAsync(string email)
        {
            using var cmd = new SqliteCommand(@"
                UPDATE PasswordResets
                SET IsUsed = 1
                WHERE lower(Email) = lower(@email) AND IsUsed = 0
            ", _connection);
            cmd.Parameters.AddWithValue("@email", email);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<PasswordResetToken?> ValidatePasswordResetTokenAsync(string email, string token)
        {
            using var cmd = new SqliteCommand(@"
                SELECT * FROM PasswordResets
                WHERE lower(Email) = lower(@email)
                  AND Token = @token
                  AND IsUsed = 0
                  AND ExpiresAt > datetime('now')
                ORDER BY CreatedAt DESC
                LIMIT 1
            ", _connection);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@token", token);
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapPasswordReset(reader);
        }

        public async Task<bool> MarkPasswordResetUsedAsync(int id)
        {
            using var cmd = new SqliteCommand("UPDATE PasswordResets SET IsUsed = 1 WHERE Id = @id", _connection);
            cmd.Parameters.AddWithValue("@id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int> SaveHardwareScanAsync(HardwareScan scan)
        {
            if (scan.ScanDate == DateTime.MinValue)
                scan.ScanDate = DateTime.Now;

            using var cmd = new SqliteCommand(@"
                INSERT INTO HardwareScans (ScanDate, CpuUsage, RamUsedBytes, RamTotalBytes, DiskUsedBytes, DiskTotalBytes, MaxTemperature, Status)
                VALUES (@scanDate, @cpu, @ramUsed, @ramTotal, @diskUsed, @diskTotal, @temp, @status);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@scanDate", scan.ScanDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@cpu", scan.CpuUsage);
            cmd.Parameters.AddWithValue("@ramUsed", (long)scan.RamUsedBytes);
            cmd.Parameters.AddWithValue("@ramTotal", (long)scan.RamTotalBytes);
            cmd.Parameters.AddWithValue("@diskUsed", (long)scan.DiskUsedBytes);
            cmd.Parameters.AddWithValue("@diskTotal", (long)scan.DiskTotalBytes);
            cmd.Parameters.AddWithValue("@temp", scan.MaxTemperature);
            cmd.Parameters.AddWithValue("@status", scan.Status);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<HardwareScan>> GetHardwareScansAsync(DateTime from, DateTime to)
        {
            var scans = new List<HardwareScan>();
            using var cmd = new SqliteCommand(@"
                SELECT * FROM HardwareScans WHERE ScanDate BETWEEN @from AND @to ORDER BY ScanDate DESC
            ", _connection);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                scans.Add(MapHardwareScan(reader));
            return scans;
        }

        public async Task<int> SaveThreatDetectionAsync(ThreatScanResult threat)
        {
            if (threat.DetectionDate == DateTime.MinValue)
                threat.DetectionDate = DateTime.Now;

            using var cmd = new SqliteCommand(@"
                INSERT INTO ThreatDetections (DetectionDate, ThreatName, FilePath, ActionTaken, Severity)
                VALUES (@date, @name, @path, @action, @severity);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@date", threat.DetectionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@name", threat.ThreatName);
            cmd.Parameters.AddWithValue("@path", threat.FilePath);
            cmd.Parameters.AddWithValue("@action", threat.ActionTaken);
            cmd.Parameters.AddWithValue("@severity", threat.Severity);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<ThreatScanResult>> GetThreatDetectionsAsync(DateTime from, DateTime to)
        {
            var threats = new List<ThreatScanResult>();
            using var cmd = new SqliteCommand(@"
                SELECT * FROM ThreatDetections WHERE DetectionDate BETWEEN @from AND @to ORDER BY DetectionDate DESC
            ", _connection);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                threats.Add(MapThreat(reader));
            return threats;
        }

        public async Task<int> SaveMaintenanceLogAsync(MaintenanceLog log)
        {
            if (log.ActionDate == DateTime.MinValue)
                log.ActionDate = DateTime.Now;

            log.ActionType = NormalizeMaintenanceActionType(log.ActionType);
            using var cmd = new SqliteCommand(@"
                INSERT INTO MaintenanceLogs (ActionDate, ActionType, SpaceFreedBytes, Details)
                VALUES (@date, @type, @space, @details);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@date", log.ActionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@type", log.ActionType);
            cmd.Parameters.AddWithValue("@space", log.SpaceFreedBytes);
            cmd.Parameters.AddWithValue("@details", log.Details);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private static string NormalizeMaintenanceActionType(string actionType)
        {
            return actionType.StartsWith("Optimizaci", StringComparison.OrdinalIgnoreCase)
                ? "Optimizacion"
                : "Limpieza";
        }

        public async Task<List<MaintenanceLog>> GetMaintenanceLogsAsync(int limit = 100)
        {
            var logs = new List<MaintenanceLog>();
            using var cmd = new SqliteCommand("SELECT * FROM MaintenanceLogs ORDER BY ActionDate DESC LIMIT @limit", _connection);
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                logs.Add(MapMaintenanceLog(reader));
            return logs;
        }

        public async Task<int> SaveSystemSnapshotAsync(SystemSnapshot snapshot)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO SystemSnapshots (Timestamp, CpuUsage, RamUsedPercent, DiskUsedPercent, MaxTemp, NetworkSpeedMbps)
                VALUES (CURRENT_TIMESTAMP, @cpu, @ram, @disk, @temp, @network);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@cpu", snapshot.CpuUsage);
            cmd.Parameters.AddWithValue("@ram", snapshot.RamUsedPercent);
            cmd.Parameters.AddWithValue("@disk", snapshot.DiskUsedPercent);
            cmd.Parameters.AddWithValue("@temp", snapshot.MaxTemp);
            cmd.Parameters.AddWithValue("@network", snapshot.NetworkSpeedMbps);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<SystemSnapshot>> GetSystemHistoryAsync(DateTime from, DateTime to)
        {
            var snapshots = new List<SystemSnapshot>();
            using var cmd = new SqliteCommand(@"
                SELECT * FROM SystemSnapshots WHERE Timestamp BETWEEN @from AND @to ORDER BY Timestamp ASC
            ", _connection);
            cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                snapshots.Add(MapSnapshot(reader));
            return snapshots;
        }

        public async Task<List<CompanyPartner>> GetCompanyPartnersAsync(string? city = null, string? specialty = null)
        {
            var partners = new List<CompanyPartner>();
            var sql = "SELECT * FROM CompanyPartners WHERE 1=1";
            if (!string.IsNullOrEmpty(city)) sql += " AND City = @city";
            if (!string.IsNullOrEmpty(specialty)) sql += " AND Specialty = @specialty";
            sql += " ORDER BY Rating DESC";

            using var cmd = new SqliteCommand(sql, _connection);
            if (!string.IsNullOrEmpty(city)) cmd.Parameters.AddWithValue("@city", city);
            if (!string.IsNullOrEmpty(specialty)) cmd.Parameters.AddWithValue("@specialty", specialty);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                partners.Add(MapCompanyPartner(reader));
            return partners;
        }

        public async Task<int> SavePartnerAppointmentAsync(PartnerAppointment appointment)
        {
            if (appointment.RequestedAt == DateTime.MinValue)
                appointment.RequestedAt = DateTime.Now;

            using var cmd = new SqliteCommand(@"
                INSERT INTO PartnerAppointments (RequestedAt, CompanyName, City, Specialty, Contact, Status)
                VALUES (@date, @company, @city, @specialty, @contact, @status);
                SELECT last_insert_rowid();
            ", _connection);
            cmd.Parameters.AddWithValue("@date", appointment.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@company", appointment.CompanyName);
            cmd.Parameters.AddWithValue("@city", appointment.City);
            cmd.Parameters.AddWithValue("@specialty", appointment.Specialty);
            cmd.Parameters.AddWithValue("@contact", appointment.Contact);
            cmd.Parameters.AddWithValue("@status", appointment.Status);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<List<PartnerAppointment>> GetPartnerAppointmentsAsync(int limit = 50)
        {
            var appointments = new List<PartnerAppointment>();
            using var cmd = new SqliteCommand("SELECT * FROM PartnerAppointments ORDER BY RequestedAt DESC LIMIT @limit", _connection);
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appointments.Add(new PartnerAppointment
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    RequestedAt = reader.GetDateTime(reader.GetOrdinal("RequestedAt")),
                    CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                    City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
                    Specialty = reader.IsDBNull(reader.GetOrdinal("Specialty")) ? string.Empty : reader.GetString(reader.GetOrdinal("Specialty")),
                    Contact = reader.IsDBNull(reader.GetOrdinal("Contact")) ? string.Empty : reader.GetString(reader.GetOrdinal("Contact")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Solicitada" : reader.GetString(reader.GetOrdinal("Status"))
                });
            }
            return appointments;
        }

        private User MapUser(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                AccountType = reader.GetString(reader.GetOrdinal("AccountType")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLogin")),
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1
            };
        }

        private PasswordResetToken MapPasswordReset(SqliteDataReader reader)
        {
            return new PasswordResetToken
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Token = reader.GetString(reader.GetOrdinal("Token")),
                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                IsUsed = reader.GetInt32(reader.GetOrdinal("IsUsed")) == 1,
                TempPassword = reader.IsDBNull(reader.GetOrdinal("TempPassword")) ? string.Empty : reader.GetString(reader.GetOrdinal("TempPassword")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        private HardwareScan MapHardwareScan(SqliteDataReader reader)
        {
            return new HardwareScan
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ScanDate = reader.GetDateTime(reader.GetOrdinal("ScanDate")),
                CpuUsage = reader.GetDouble(reader.GetOrdinal("CpuUsage")),
                RamUsedBytes = (ulong)reader.GetInt64(reader.GetOrdinal("RamUsedBytes")),
                RamTotalBytes = (ulong)reader.GetInt64(reader.GetOrdinal("RamTotalBytes")),
                DiskUsedBytes = (ulong)reader.GetInt64(reader.GetOrdinal("DiskUsedBytes")),
                DiskTotalBytes = (ulong)reader.GetInt64(reader.GetOrdinal("DiskTotalBytes")),
                MaxTemperature = reader.GetDouble(reader.GetOrdinal("MaxTemperature")),
                Status = reader.GetString(reader.GetOrdinal("Status"))
            };
        }

        private ThreatScanResult MapThreat(SqliteDataReader reader)
        {
            return new ThreatScanResult
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                DetectionDate = reader.GetDateTime(reader.GetOrdinal("DetectionDate")),
                ThreatName = reader.GetString(reader.GetOrdinal("ThreatName")),
                ThreatType = "Desconocido",
                FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? string.Empty : reader.GetString(reader.GetOrdinal("FilePath")),
                ActionTaken = reader.IsDBNull(reader.GetOrdinal("ActionTaken")) ? "Pendiente" : reader.GetString(reader.GetOrdinal("ActionTaken")),
                Severity = reader.IsDBNull(reader.GetOrdinal("Severity")) ? "Media" : reader.GetString(reader.GetOrdinal("Severity")),
                Status = reader.IsDBNull(reader.GetOrdinal("ActionTaken")) ? "Pendiente" : reader.GetString(reader.GetOrdinal("ActionTaken"))
            };
        }

        private MaintenanceLog MapMaintenanceLog(SqliteDataReader reader)
        {
            return new MaintenanceLog
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ActionDate = reader.GetDateTime(reader.GetOrdinal("ActionDate")),
                ActionType = reader.GetString(reader.GetOrdinal("ActionType")),
                SpaceFreedBytes = reader.GetInt64(reader.GetOrdinal("SpaceFreedBytes")),
                Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? string.Empty : reader.GetString(reader.GetOrdinal("Details"))
            };
        }

        private SystemSnapshot MapSnapshot(SqliteDataReader reader)
        {
            return new SystemSnapshot
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                CpuUsage = reader.GetDouble(reader.GetOrdinal("CpuUsage")),
                RamUsedPercent = reader.GetDouble(reader.GetOrdinal("RamUsedPercent")),
                DiskUsedPercent = reader.GetDouble(reader.GetOrdinal("DiskUsedPercent")),
                MaxTemp = reader.GetDouble(reader.GetOrdinal("MaxTemp")),
                NetworkSpeedMbps = reader.GetDouble(reader.GetOrdinal("NetworkSpeedMbps"))
            };
        }

        private CompanyPartner MapCompanyPartner(SqliteDataReader reader)
        {
            return new CompanyPartner
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Specialty = reader.IsDBNull(reader.GetOrdinal("Specialty")) ? string.Empty : reader.GetString(reader.GetOrdinal("Specialty")),
                City = reader.IsDBNull(reader.GetOrdinal("City")) ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? string.Empty : reader.GetString(reader.GetOrdinal("Address")),
                Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? 0 : reader.GetDouble(reader.GetOrdinal("Rating")),
                IsAvailable = reader.GetInt32(reader.GetOrdinal("IsAvailable")) == 1,
                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? string.Empty : reader.GetString(reader.GetOrdinal("Phone")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) ? string.Empty : reader.GetString(reader.GetOrdinal("ImagePath")),
                HasWarranty = reader.GetInt32(reader.GetOrdinal("HasWarranty")) == 1
            };
        }

        public void Dispose()
        {
            try { _connection?.Close(); } catch { }
            try { _connection?.Dispose(); } catch { }
        }
    }
}
