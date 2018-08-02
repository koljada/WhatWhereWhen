using Dapper;
using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WhatWhereWhen.Data.Sql.Mappings;
using WhatWhereWhen.Domain.Interfaces;
using WhatWhereWhen.Domain.Models;

namespace WhatWhereWhen.Data.Sql
{
    public class QuestionDataSql : IQuestionData
    {
        private readonly string _connectionString;
        private const string SCHEMA = "www";

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
                var result = await connection.QueryFirstOrDefaultAsync<QuestionItem>($"[{SCHEMA}].[GetRandomQuestion]",
                    new { conversationId, complexity = (byte)complexity },
                    commandType: CommandType.StoredProcedure);

                if (result != null && markAsRead)
                {
                    string markSql = $"INSERT [{SCHEMA}].[QuestionConversation] VALUES(@qustionId, @conversationId, GETDATE());";
                    await connection.ExecuteScalarAsync(markSql, new { qustionId = result.Id, conversationId });
                }

                return result;
            }
        }

        public async Task<TourBase> GetTourById(int tourId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string sql = $"SELECT TOP (1) Id, Title, Type, ChildrenNum, QuestionsNum  FROM [{SCHEMA}].[Tour] WHERE Id = @tourId";
                var tour = await connection.QueryFirstOrDefaultAsync<TourBase>(sql, new { tourId });

                if (tour.Type != "Т")
                {
                    string sql2 = $"SELECT Id, Title, Type, ChildrenNum, QuestionsNum FROM [{SCHEMA}].[Tour] WHERE ParentId = @tourId";
                    var childrenTours = await connection.QueryAsync<TourBase>(sql2, new { tourId });
                    return new Tournament
                    {
                        Id = tour.Id,
                        Title = tour.Title,
                        ChildrenNum = tour.ChildrenNum,
                        QuestionsNum = tour.QuestionsNum,
                        Tours = childrenTours.AsList()
                    };
                }
                else
                {
                    string sql3 = $"SELECT * FROM [{SCHEMA}].[Question] WHERE ParentId = @tourId";
                    var questions = await connection.QueryAsync<QuestionItem>(sql3, new { tourId });

                    return new Tour
                    {
                        Id = tour.Id,
                        Title = tour.Title,
                        ChildrenNum = tour.ChildrenNum,
                        QuestionsNum = tour.QuestionsNum,
                        Questions = questions.AsList()
                    };
                }
            }
        }

        public async Task<IEnumerable<Tour>> GetTours(byte level = 1)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Tour>($"SELECT * FROM [{SCHEMA}].[Tour] WHERE ParentId = 0 AND Id<>0");
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

        public int InsertTour(Tour tour, Tournament tournament)
        {
            string state = "";
            short inserted = 0;
            short skipped = 0;
            short error = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tour #{tour.Id} ({tour.QuestionsNum})");
                    if (!Exist(connection, tour))
                    {
                        Trace.TraceInformation($"Inserting tour #{tour.Id}");
                        tour.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tour);
                        state = "Inserted";
                    }
                    else
                    {
                        Trace.TraceInformation($"Tounament #{tour.Id} already exists");
                        state = "Skipped";
                    }
                    int tourId = tour.Id;
                    int tournamentId = tournament?.Id ?? tour.ParentId;
                    
                    foreach (var question in tour.Questions)
                    {
                        try
                        {
                            //Trace.TraceInformation($"Checking question #{question.Id}");
                            if (!Exist(connection, question))
                            {
                                Trace.Write(".");
                                //Trace.TraceInformation($"Inserting question #{question.Id}");

                                question.ParentId = tourId;
                                question.TourId = tourId;
                                question.TournamentId = tournamentId;
                                question.ImportedAt = DateTime.UtcNow;

                                connection.Insert(question);
                                inserted++;
                            }
                            else
                            {
                                Trace.Write("-");
                                //Trace.TraceInformation($"Question #{question.Id} already exists");
                                skipped++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("");
                            Trace.TraceError($"Exception when inserting question #{question.Id}. {ex.Message}", ex);
                            error++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("");
                    Trace.TraceError($"Exception when inserting tour #{tour.Id}. {ex.Message}", ex);
                    state = "Error";
                }
            }

            Trace.WriteLine("");
            Trace.TraceInformation($"Tour #{tour.Id} handled. State: {state}. {inserted} + {skipped} + {error} = {inserted + skipped + error} = {tour.QuestionsNum} ");

            return tour.Questions.Count;
        }

        public int InsertTournament(Tournament tournament)
        {
            string state = "";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    Trace.TraceInformation($"Checking tournament #{tournament.Id} ({tournament.ChildrenNum})");
                    if (!Exist(connection, tournament))
                    {
                        tournament.ImportedAt = DateTime.UtcNow;
                        connection.Insert(tournament);
                        state = "Inserted";
                    }
                    else
                    {
                        Trace.TraceInformation($"Tournament #{tournament.Id} already exists");
                        state = "Skipped";
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception when inserting tournament #{tournament.Id}. {ex.Message}", ex);
                    state = "Error";
                }
            }

            Trace.TraceInformation($"Tournament #{tournament.Id}({tournament.Type}) has been handled. State: {state}.");

            return 1;
        }
       
        private bool Exist<T>(SqlConnection connection, T model) where T : BaseEntity
        {            
            return connection.Get<T>(model.Id) != null;
        }
    }
}
