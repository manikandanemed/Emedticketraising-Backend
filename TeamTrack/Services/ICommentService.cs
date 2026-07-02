using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface ICommentService
    {
        Task<CommentResponseDto> AddCommentAsync(CreateCommentRequestDto request, int userId);
        Task<List<CommentResponseDto>> GetCommentsByWorkItemAsync(int workItemId, bool isProductManager);
    }
}