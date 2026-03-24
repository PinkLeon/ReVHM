// 初始化語言資源 - 必須最先執行，不相依於其他資源
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace ReCIM.Core.Model
{
    public class MySQLHelper
    {
        public string ConnectString;
        public string Message = "";
        public MySQLHelper(MysqlActor Data)
        {
            try
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
                builder.Server = Data.IP;
                builder.Database = Data.InitialCatalog;
                //builder.Port = uint.Parse(Data.Port);
                builder.UserID = Data.UserID;
                builder.Password = Data.Password;
                builder.ConnectionTimeout = 3000;
                builder.DefaultCommandTimeout = 30000;
                builder.CharacterSet = "utf8";
                ConnectString = builder.ToString();
                MySqlConnection conn = new MySqlConnection(ConnectString);
                conn.Open();
                Message = "Connection Success";

            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        /// <summary>
        /// 把 單引號 ' 變成兩個連續單引號 '', EX: ("Can't find robot") => 結果變成合法SQL語法：Can''t find robot
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeString(string value)
        {
            if (value == null)
                return null;

            // 將單引號 ' 轉成 ''（SQL語法正確）
            return value.Replace("'", "''");
        }

        internal string GetLotNumber()
        {
            string LotNumber = "";

            using (var conn = new MySqlConnection(ConnectString))
            {
                conn.Open();

                try
                {
                    string sql = "CALL GetLotNumber()";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read()) // 確保有讀取到資料
                    {
                        LotNumber = reader["Lot"].ToString();
                    }
                }
                catch (Exception)
                {
                }
            }
            return LotNumber;
        }

        public bool Execute(string sql)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(this.ConnectString))
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand(sql, connection);
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                //Global.WriteLog("DB", "Execute", ex.Message + ex.StackTrace);
                //Global.WriteLog("DB", "Execute", sql);
                return false;

            }

        }


        public IList<T> GetALL<T>(string sql) where T : new()
        {
            var data = new List<T>();
            var properties = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var fields = typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

            using (var connection = new MySqlConnection(this.ConnectString))
            using (var command = new MySqlCommand(sql, connection))
            {
                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new T();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);

                                // 決定要操作的是屬性還是欄位
                                if (properties.TryGetValue(columnName, out var property))
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        property.SetValue(item, null);
                                        continue;
                                    }

                                    var targetType = property.PropertyType;
                                    var rawValue = reader.GetValue(i);
                                    object safeValue;
                                    if (targetType.IsEnum)
                                    {
                                        safeValue = Enum.ToObject(targetType, rawValue);
                                    }
                                    else
                                    {
                                        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                                        safeValue = Convert.ChangeType(rawValue, underlyingType);
                                    }
                                    property.SetValue(item, safeValue);
                                }
                                else if (fields.TryGetValue(columnName, out var field))
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        field.SetValue(item, null);
                                        continue;
                                    }

                                    var targetType = field.FieldType;
                                    var rawValue = reader.GetValue(i);
                                    object safeValue;
                                    if (targetType.IsEnum)
                                    {
                                        safeValue = Enum.ToObject(targetType, rawValue);
                                    }
                                    else
                                    {
                                        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                                        safeValue = Convert.ChangeType(rawValue, underlyingType);
                                    }
                                    field.SetValue(item, safeValue);
                                }
                            }

                            data.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // log 或 rethrow
                    //Global.WriteLog("DB", "Execute", ex.Message + ex.StackTrace);
                    //Global.WriteLog("DB", "Execute", sql);
                }
            }
            return data;
        }


        public List<string> GetString(string sql)
        {
            var data = new List<string>();

            using (var connection = new MySqlConnection(this.ConnectString))
            using (var command = new MySqlCommand(sql, connection))
            {
                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    // 安全地將任何型別轉換為字串
                                    var value = reader.GetValue(i);
                                    data.Add(value?.ToString() ?? string.Empty);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 記錄錯誤訊息到日誌檔案
                    Console.WriteLine("Error: " + ex.Message);
                    // 或者重新拋出例外
                    throw;
                }
            }
            return data;
        }


    }
}

