using Dapper;
using DapperExtensions;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using WhatWhereWhen.Data.Sql.Mappings;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql
{
    public class QuestionDataSql : IQuestionData
    {
        private readonly string _connectionString;

        public QuestionDataSql(bool withMapping = false)
        {
            if (withMapping)
            {
                DapperExtensions.DapperExtensions.SetMappingAssemblies(new[] { typeof(TournamentMapper).Assembly });
            }
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultSql"].ConnectionString;
        }

        public QuestionDataSql(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<QuestionItem> GetRandomQuestion(string conversationId, QuestionComplexity? complexity = null, bool markAsRead = true)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT TOP 1 Q.* FROM [cgk].[Question] Q 
                                LEFT JOIN [cgk].[QuestionConversation] QC ON Q.Id = QC.QuestionId AND QC.ConversationId = @conversationId
                                WHERE QC.ConversationId IS NULL AND (ABS(CAST((BINARY_CHECKSUM(Id) * RAND()) as int)) % 100) < 10";

                if (complexity > 0)
                {
                    sql += $" AND Q.Complexity = " + (byte)complexity;
                }

                var result = await connection.QueryFirstOrDefaultAsync<QuestionItem>(sql, new { conversationId });

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

        public int InsertTour(Tour tour)
        {
            string state = "";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tour #{tour.Id} ({tour.ChildrenNum})");
                    if (!Exist(connection, tour))
                    {
                        tour.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tour);
                        state = "Inserted";
                    }
                    else
                    {
                        Trace.TraceInformation($"Tour #{tour.Id} already exists");
                        state = "Skipped";
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception when inserting tour #{tour.Id}", ex);
                    state = "Error";
                }
            }

            Trace.TraceInformation($"Tour #{tour.Id} has been handled. State: {state}.");

            return 1;
        }

        public int InsertTournament(Tournament tournament)
        {
            string state = "";
            short inserted = 0;
            short skipped = 0;
            short error = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tounament #{tournament.Id} ({tournament.QuestionsNum})");
                    if (!Exist(connection, tournament))
                    {
                        Trace.TraceInformation($"Inserting tounament #{tournament.Id}");
                        tournament.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tournament);
                        state = "Inserted";

                        foreach (var question in tournament.Questions)
                        {
                            try
                            {
                                //Trace.TraceInformation($"Checking question #{question.Id}");
                                if (!Exist(connection, question))
                                {
                                    //Trace.TraceInformation($"Inserting question #{question.Id}");

                                    question.TournamentId = tournament.Id;
                                    question.TournamentTitle = tournament.Title;
                                    question.ImportedAt = DateTime.UtcNow;

                                    connection.Insert(question);
                                    inserted++;
                                }
                                else
                                {
                                    //Trace.TraceInformation($"Question #{question.Id} already exists");
                                    skipped++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError($"Exception when inserting question #{question.Id}", ex);
                                error++;
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceInformation($"Tounament #{tournament.Id} already exists");
                        state = "Skipped";
                    }


                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception when inserting tournament #{tournament.Id}", ex);
                    state = "Error";
                }
            }

            Trace.TraceInformation($"Tournament #{tournament.Id} has been handled. State: {state}." +
                $" {inserted} questions have need inserted; {skipped} questions have been skipped; {error} have been missed(there was an exception).");

            return tournament.Questions.Count;
        }

        private bool Exist<T>(SqlConnection connection, T model) where T : BaseEntity
        {
            return connection.Get<T>(model.Id) != null;
        }
    }
}
