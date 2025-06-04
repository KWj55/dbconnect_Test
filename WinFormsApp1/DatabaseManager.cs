using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace WinFormsApp1
{
    public class DatabaseManager : IDisposable
    {
        private SqlConnection connection;
        private string connectionString;
        private static DatabaseManager instance;
        private string currentDatabase = "MADANG";  // 현재 연결된 데이터베이스

        // 싱글톤 패턴 구현
        public static DatabaseManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DatabaseManager();
                return instance;
            }
        }

        private DatabaseManager()
        {
            BuildConnectionString();
        }

        private void BuildConnectionString()
        {
            connectionString = $@"Server=localhost\SQLEXPRESS;Database={currentDatabase};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;MultipleActiveResultSets=True";
        }

        public bool ConnectToDatabase(string databaseName)
        {
            try
            {
                // 기존 연결이 있으면 해제
                Disconnect();

                // 새로운 데이터베이스로 연결 문자열 업데이트
                currentDatabase = databaseName;
                BuildConnectionString();

                // 새 연결 시도
                connection = new SqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch (SqlException ex)
            {
                string errorMsg = $"SQL 오류 번호: {ex.Number}\n" +
                                $"오류 메시지: {ex.Message}\n" +
                                $"서버: {ex.Server}";
                MessageBox.Show(errorMsg, "데이터베이스 연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError("db_error_log.txt", errorMsg + $"\nStackTrace: {ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = $"일반 오류 발생:\n{ex.GetType().Name}\n{ex.Message}";
                MessageBox.Show(errorMsg, "데이터베이스 연결 일반 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError("general_error_log.txt", errorMsg + $"\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public bool Connect()
        {
            return ConnectToDatabase(currentDatabase);
        }

        private void LogError(string logPath, string errorMsg)
        {
            string logMessage = $"[{DateTime.Now}]\n{errorMsg}\n\n";
            File.AppendAllText(logPath, logMessage);
        }

        public void Disconnect()
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
                connection.Dispose();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        // 데이터베이스 쿼리 실행을 위한 메서드
        public DataTable ExecuteQuery(string query)
        {
            try
            {
                // Connect() 메서드가 false를 반환할 수 있으므로, 연결 성공 여부 확인
                if (connection?.State != ConnectionState.Open)
                {
                    if (!Connect()) return null; // 연결 실패 시 null 반환
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // 데이터 수정/삭제/삽입을 위한 메서드
        public int ExecuteNonQuery(string query)
        {
            try
            {
                // Connect() 메서드가 false를 반환할 수 있으므로, 연결 성공 여부 확인
                if (connection?.State != ConnectionState.Open)
                {
                    if (!Connect()) return -1; // 연결 실패 시 -1 반환
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        // 데이터 수정/삭제/삽입을 위한 메서드 (파라미터 사용)
        public int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            try
            {
                if (connection?.State != ConnectionState.Open)
                {
                    if (!Connect()) return -1; // 연결 실패 시
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        // 지정된 테이블의 모든 데이터를 가져오는 메서드
        public DataTable GetTableData(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                MessageBox.Show("테이블 이름이 유효하지 않습니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            // SQL Injection 방지를 위해 tableName 검증이 필요할 수 있습니다. (예: 화이트리스트)
            string query = $"SELECT * FROM [{tableName}]";
            return ExecuteQuery(query);
        }

        // 지정된 테이블에서 특정 ID의 항목을 삭제하는 일반화된 메서드
        public bool DeleteDataEntry(string tableName, string idColumnName, object idValue)
        {
            // SQL Injection 방지를 위해 tableName 및 idColumnName 검증이 필요할 수 있습니다.
            string query = $"DELETE FROM [{tableName}] WHERE [{idColumnName}] = @idValue";
            SqlDbType idDbType = (idValue is int) ? SqlDbType.Int : SqlDbType.NVarChar; // ID 타입에 따라 조정
            SqlParameter idParam = new SqlParameter("@idValue", idDbType) { Value = idValue };
            int rowsAffected = ExecuteNonQuery(query, idParam.Value == DBNull.Value ? null : idParam); // DBNull.Value 처리
            return rowsAffected > 0; // 0보다 크면 최소 한 행이 삭제되었음을 의미
        }

        // 지정된 테이블에 새 항목을 추가하는 일반화된 메서드
        public bool AddDataEntry(string tableName, System.Collections.Generic.Dictionary<string, object> data)
        {
            if (string.IsNullOrWhiteSpace(tableName) || data == null || data.Count == 0)
            {
                MessageBox.Show("테이블 이름 또는 데이터가 유효하지 않습니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // SQL Injection 방지를 위해 테이블 이름 검증 (예: 허용된 테이블 목록과 비교)
            // 이 예제에서는 tableName이 안전하다고 가정합니다. 실제 애플리케이션에서는 보안 검토가 필요합니다.
            // 예: if (!IsValidTableName(tableName)) return false;

            var columns = new System.Text.StringBuilder();
            var values = new System.Text.StringBuilder();
            var parameters = new System.Collections.Generic.List<SqlParameter>();

            foreach (var entry in data)
            {
                if (columns.Length > 0) columns.Append(", ");
                columns.Append($"[{entry.Key}]"); // 컬럼 이름에 대괄호 추가 (특수 문자 포함 가능성 대비)

                if (values.Length > 0) values.Append(", ");
                values.Append($"@{entry.Key}");

                // 데이터 타입 추론 (간단한 예시, 실제로는 더 정교한 타입 매핑 필요)
                SqlDbType dbType = SqlDbType.NVarChar; // 기본값
                if (entry.Value is int) dbType = SqlDbType.Int;
                else if (entry.Value is decimal) dbType = SqlDbType.Decimal;
                else if (entry.Value is DateTime) dbType = SqlDbType.DateTime;
                else if (entry.Value is bool) dbType = SqlDbType.Bit;

                parameters.Add(new SqlParameter($"@{entry.Key}", dbType) { Value = entry.Value ?? DBNull.Value });
            }

            string query = $"INSERT INTO [{tableName}] ({columns}) VALUES ({values})";

            int rowsAffected = ExecuteNonQuery(query, parameters.ToArray());
            return rowsAffected > 0;
        }

        // 지정된 테이블의 특정 항목을 업데이트하는 일반화된 메서드
        public bool UpdateDataEntry(string tableName, string idColumnName, System.Collections.Generic.Dictionary<string, object> data)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(idColumnName) || data == null || !data.ContainsKey(idColumnName))
            {
                MessageBox.Show("테이블 이름, ID 컬럼 이름 또는 데이터가 유효하지 않거나 ID 값이 누락되었습니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // SQL Injection 방지를 위해 테이블 및 컬럼 이름 검증
            // 예: if (!IsValidTableName(tableName) || !IsValidColumnName(idColumnName)) return false;

            var setClauses = new System.Text.StringBuilder();
            var parameters = new System.Collections.Generic.List<SqlParameter>();
            object idValue = data[idColumnName];

            foreach (var entry in data)
            {
                // ID 컬럼은 SET 절에 포함시키지 않음 (일반적으로 ID는 변경하지 않음)
                if (entry.Key.Equals(idColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (setClauses.Length > 0) setClauses.Append(", ");
                setClauses.Append($"[{entry.Key}] = @{entry.Key}");

                SqlDbType dbType = SqlDbType.NVarChar;
                if (entry.Value is int) dbType = SqlDbType.Int;
                else if (entry.Value is decimal) dbType = SqlDbType.Decimal;
                else if (entry.Value is DateTime) dbType = SqlDbType.DateTime;
                else if (entry.Value is bool) dbType = SqlDbType.Bit;

                parameters.Add(new SqlParameter($"@{entry.Key}", dbType) { Value = entry.Value ?? DBNull.Value });
            }

            if (setClauses.Length == 0)
            {
                MessageBox.Show("업데이트할 데이터가 없습니다 (ID 컬럼 제외).", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false; // 아무것도 업데이트하지 않음
            }

            // WHERE 절에 사용될 ID 파라미터 추가
            SqlDbType idDbType = (idValue is int) ? SqlDbType.Int : SqlDbType.NVarChar; // ID 타입에 따라 조정
            parameters.Add(new SqlParameter($"@{idColumnName}", idDbType) { Value = idValue });

            string query = $"UPDATE [{tableName}] SET {setClauses} WHERE [{idColumnName}] = @{idColumnName}";

            int rowsAffected = ExecuteNonQuery(query, parameters.ToArray());
            return rowsAffected > 0;
        }


        // 지정된 테이블의 ID 컬럼에서 최대 값을 구하는 메서드
        public int GetMaxId(string tableName, string idColumnName)
        {
            try
            {
                string query = $"SELECT ISNULL(MAX([{idColumnName}]), 0) FROM [{tableName}]"; 
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ID 조회 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        // 현재 데이터베이스의 테이블 목록을 반환
        public List<string> GetTableNames()
        {
            var tables = new List<string>();
            try
            {
                if (connection?.State != ConnectionState.Open)
                {
                    if (!Connect()) return tables;
                }

                DataTable schema = connection.GetSchema("Tables");
                foreach (DataRow row in schema.Rows)
                {
                    if (row["TABLE_TYPE"].ToString() == "BASE TABLE")
                        tables.Add(row["TABLE_NAME"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"테이블 목록 조회 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return tables;
        }

        // 지정된 테이블의 컬럼 이름과 데이터 타입 정보를 조회
        public Dictionary<string, string> GetColumnTypes(string tableName)
        {
            var result = new Dictionary<string, string>();
            try
            {
                if (connection?.State != ConnectionState.Open)
                {
                    if (!Connect()) return result;
                }

                DataTable schema = connection.GetSchema("Columns", new string[] { null, null, tableName, null });
                foreach (DataRow row in schema.Rows)
                {
                    string column = row["COLUMN_NAME"].ToString();
                    string type = row["DATA_TYPE"].ToString();
                    result[column] = type;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"컬럼 정보 조회 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }
    }
} 