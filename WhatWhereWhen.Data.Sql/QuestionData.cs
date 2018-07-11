using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;
using Dapper;
using System.Threading.Tasks;

namespace WhatWhereWhen.Data.Sql
{
    public class QuestionDataSql : IQuestionData
    {
        private readonly string _connectionString;

        public QuestionDataSql()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultSql"].ConnectionString;
        }

        public async Task<QuestionItem> GetRandomQuestion(string conversationId, QuestionComplexity? complexity = null, bool markAsRead = true)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT TOP 1 Q.* FROM [cgk].[Question] Q 
                                LEFT JOIN [cgk].[QuestionConversation] QC ON Q.Id = QC.QuestionId AND QC.ConversationId = @conversationId
                                WHERE QC.ConversationId IS NULL";

                if (complexity > 0)
                {
                    sql += $" AND Q.Complexity = " + (byte)complexity;
                }

                var result = await connection.QueryFirstOrDefaultAsync<QuestionItem>(sql + " ORDER BY newid()", new { conversationId });

                if (result != null && markAsRead)
                {
                    string markSql = $"INSERT [cgk].[QuestionConversation] VALUES(@qustionId, @conversationId);";
                    await connection.ExecuteScalarAsync(markSql, new { qustionId = result.Id, conversationId });
                }

                return result;
            }
        }

        public int InsertOrUpdateQuestion(QuestionItem question)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand("cgk.InsertOrUpdateQuestion", connection);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                //foreach (var property in question.GetType().GetProperties())
                //{
                //    sqlCmd.Parameters.AddWithValue("@" + property.Name, property.GetValue(question));
                //}
                sqlCmd.Parameters.AddWithValue("@Id", question.Id);
                sqlCmd.Parameters.AddWithValue("@ParentId", question.ParentId);
                sqlCmd.Parameters.AddWithValue("@Number", question.Number);
                sqlCmd.Parameters.AddWithValue("@Type", question.Type);
                sqlCmd.Parameters.AddWithValue("@TypeNum", question.TypeNum);
                sqlCmd.Parameters.AddWithValue("@Question", question.Question);
                sqlCmd.Parameters.AddWithValue("@Answer", question.Answer);
                sqlCmd.Parameters.AddWithValue("@PassCriteria", question.PassCriteria);
                sqlCmd.Parameters.AddWithValue("@Authors", question.Authors);
                sqlCmd.Parameters.AddWithValue("@Sources", question.Sources);
                sqlCmd.Parameters.AddWithValue("@Comments", question.Comments);
                sqlCmd.Parameters.AddWithValue("@Rating", question.Rating);
                sqlCmd.Parameters.AddWithValue("@RatingNumber", question.RatingNumber);
                sqlCmd.Parameters.AddWithValue("@Complexity", question.Complexity);
                sqlCmd.Parameters.AddWithValue("@TourId", question.TourId);
                sqlCmd.Parameters.AddWithValue("@TournamentId", question.TournamentId);
                sqlCmd.Parameters.AddWithValue("@TourTitle", question.TourTitle);
                sqlCmd.Parameters.AddWithValue("@TournamentTitle", question.TournamentTitle);
                sqlCmd.Parameters.AddWithValue("@TournamentType", question.TournamentType);
                sqlCmd.Parameters.AddWithValue("@TourType", question.TourType);
                sqlCmd.Parameters.AddWithValue("@TourPlayedAt", question.TourPlayedAt);
                sqlCmd.Parameters.AddWithValue("@TournamentPlayedAt", question.TournamentPlayedAt);
                sqlCmd.Parameters.AddWithValue("@Notices", question.Notices);
                sqlCmd.Parameters.AddWithValue("@Topic", question.Topic);

                connection.Open();
                int result = sqlCmd.ExecuteNonQuery();
                connection.Close();
                return result;
            }
        }
    }
}
