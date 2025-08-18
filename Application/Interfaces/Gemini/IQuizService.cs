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
        Task<ResponseVM> GenerateQuizAsync(QuizVM model);
        Task<ResponseVM> GetAllQuizQuestionsAsync(long quizId);

        Task<ResponseVM> GetQuizQuestionsByNumberAsync(long quizId, int questionNumber);

        Task<ResponseVM> ResultSubmittedAsync(ResultSubmittedVM model);


    }
}
