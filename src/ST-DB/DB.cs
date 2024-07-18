using System.ComponentModel;
using SurfTimer.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SurfTimer.Validators;
// https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html
using MySqlConnector; 

namespace SurfTimer;

// This will have functions for DB access and query sending
internal class TimerDatabase
{
    private readonly MySqlConnection? _db;
    private readonly string _connString = string.Empty;

    public TimerDatabase()
    {
        // Null'd
    }


    public TimerDatabase(string host, string database, string user, string password, int port, int timeout)
    {
        this._connString = $"server={host};user={user};password={password};database={database};port={port};connect timeout={timeout};";
        this._db = new MySqlConnection(this._connString);
        this._db.Open();
    }

    public void Close()
    {
        if (this._db != null)
            this._db!.Close();
    }

    public async Task<MySqlDataReader> Query(string query)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (this._db == null)
                {
                    throw new InvalidOperationException("Database connection is not open.");
                }

                MySqlCommand cmd = new(query, this._db);
                MySqlDataReader reader = await cmd.ExecuteReaderAsync();

                return reader;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<int> Write(string query)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (this._db == null)
                {
                    throw new InvalidOperationException("Database connection is not open.");
                }

                MySqlCommand cmd = new(query, this._db);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing write operation: {ex.Message}");
                throw;
            }
        });
    }

    public void InitDb()
    {
        List<Type> models = new List<Type>()
        {
            typeof(PlayerModel),
            typeof(PlayerSettingsModel),
            typeof(PlayerStatsModel),
            typeof(MapsModel),
            typeof(MapTimesModel),
            typeof(MapTimeInsightsModel),
            typeof(MapZonesModel),
            typeof(CheckpointsModel)
        };
        
        foreach (var model in models)
        {
            object modelClass = Activator.CreateInstance(model)!;
            var query = BuildCreateTable(modelClass);
            Console.WriteLine(query + ");");
        }
    }

    private string BuildCreateTable<T>(T model)
    {
        var tableName = model!.GetType().Name.Replace("Model", string.Empty);

        string createTableQuery = $"CREATE TABLE IF NOT EXISTS {tableName} (";
        List<string> foreignKeys = new List<string>();
        
        var type = model.GetType();
        for (var i = 0; i < type.GetProperties().Length; i++)
        {
            PropertyInfo property = type.GetProperties()[i];

            var propertyString = "";
            if (i > 0)
            {
                propertyString += ", ";
            }
            propertyString += $"{property.Name} ";

            var customTypeReference = property.GetCustomAttributes(typeof(CustomTypeValidator), false);
            if (customTypeReference.Length != 0)
            {
                var customType = customTypeReference.Cast<CustomTypeValidator>().Single();
                propertyString += $"{customType.CustomType} ";
            }
            else
            {
                TypeCode propertyTypeCode = Type.GetTypeCode(property.PropertyType);
                switch(propertyTypeCode)
                {
                    case TypeCode.String:
                        var maxLengthReference = property.GetCustomAttributes(typeof(StringLengthAttribute), false);
                        if (maxLengthReference.Length != 0)
                        {
                            var maxStrLength = maxLengthReference.Cast<StringLengthAttribute>().Single().MaximumLength;
                            propertyString += $"VARCHAR({maxStrLength}) ";
                        }
                        break;
                    case TypeCode.Byte:
                        propertyString += "TINYINT ";
                        break;
                    case TypeCode.Int16:
                        propertyString += "SMALLINT ";
                        break;
                    case TypeCode.Int32:
                        propertyString += "INT ";
                        break;
                    case TypeCode.Int64:
                        propertyString += "BIGINT ";
                        break;
                    case TypeCode.Boolean:
                        propertyString += "BOOLEAN ";
                        break;
                    case TypeCode.Decimal:
                        var decimalReference = property.GetCustomAttributes(typeof(DecimalValidator), false);
                        if (decimalReference.Length == 0)
                        {
                            Console.WriteLine($"ERROR: Decimal Validator is missing!");
                            throw new ValidationException();
                        }
                        var decimalValues = decimalReference.Cast<DecimalValidator>().Single();
                        propertyString += $"DECIMAL({decimalValues.MaxDigits}, {decimalValues.NumberOfDigits}) ";
                        break;
                }
            }


            bool isNotNull = property.GetCustomAttributes(typeof(RequiredAttribute), false).Length != 0;
            if (isNotNull)
            {
                propertyString += "NOT NULL ";
            }
            
            var defaultValueReference = property.GetCustomAttributes(typeof(DefaultValueAttribute), false);
            if (defaultValueReference.Length != 0)
            {
                var defaultValue = defaultValueReference.Cast<DefaultValueAttribute>().Single();
                propertyString += $"DEFAULT {defaultValue.Value} ";
            }

            bool isIncrement = property.GetCustomAttributes(typeof(IncrementValidator), false).Length != 0;
            if (isIncrement)
            {
                propertyString += "AUTO_INCREMENT ";
            }
            
            bool isUnique = property.GetCustomAttributes(typeof(UniqueValidator), false).Length != 0;
            if (isUnique)
            {
                propertyString += "UNIQUE ";
            }
            
            bool isPk = property.GetCustomAttributes(typeof(KeyAttribute), false).Length != 0;
            if (isPk)
            {
                propertyString += "PRIMARY KEY ";
            }
            
            var comments = property.GetCustomAttributes(typeof(CommentValidator), false);
            if (comments.Length != 0)
            {
                string comment = comments.Cast<CommentValidator>().Single().Comment;
                propertyString += $"COMMENT '{comment}' ";
            }
            
            var references = property.GetCustomAttributes(typeof(ReferenceValidator), false);
            if (references.Length != 0)
            {
                var foreignKey = references.Cast<ReferenceValidator>().Single();
                foreignKeys.Add(
                    $"FOREIGN KEY ({property.Name}) REFERENCES {foreignKey.TableName}({foreignKey.ForeignKey})");
            }

            createTableQuery += propertyString;
        }

        if (foreignKeys.Count != 0)
        {
            createTableQuery += ", " + String.Join(", ", foreignKeys);
        }

        return createTableQuery;
    }
}
