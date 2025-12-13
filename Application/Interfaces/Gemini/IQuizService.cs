using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DataTransferModels.QuizViewModels;
using Application.DataTransferModels.ResponseModel;

namespace Application.Interfaces.Gemini
{
    public interface IQuizService
    {
        ResponseVM GenerateQuiz(QuizVM model);
        ResponseVM GetAllQuizQuestions(long quizId);
        //Task<ResponseVM> GenerateQuizAsync(QuizVM model);
        //Task<ResponseVM> GetAllQuizQuestionsAsync(long quizId);

        Task<ResponseVM> GetQuizQuestionsByNumberAsync(long quizId, long questionId);
        ResponseVM ResultSubmitted(ResultSubmittedVM model);
        //Task<ResponseVM> ResultSubmittedAsync(ResultSubmittedVM model);

        ResponseVM GetQuizHistory();
        //Task<ResponseVM> GetQuizHistoryAsync();
        //Task<ResponseVM> GetQuizDetailsAsync(long quizId);

        ResponseVM GetQuizDetails(long quizId);
        ResponseVM GenerateQuizForUser(QuizVM model, long userId);


        ResponseVM ResultSubmittedForUser(ResultSubmittedVM model, long userId);
    }
}
