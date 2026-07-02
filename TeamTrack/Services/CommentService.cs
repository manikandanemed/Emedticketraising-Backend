using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class CommentService : ICommentService
    {
        private readonly IRepository<Comment> _commentRepo;
        private readonly IRepository<WorkItem> _workItemRepo;

        public CommentService(IRepository<Comment> commentRepo, IRepository<WorkItem> workItemRepo)
        {
            _commentRepo = commentRepo;
            _workItemRepo = workItemRepo;
        }

        public async Task<CommentResponseDto> AddCommentAsync(CreateCommentRequestDto request, int userId)
        {
            var comment = new Comment
            {
                WorkItemId = request.WorkItemId,
                UserId = userId,
                Message = request.Message,
                IsInternal = request.IsInternal,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepo.AddAsync(comment);
            await _commentRepo.SaveAsync();

            var created = await _commentRepo.Query()
                .Include(c => c.User)
                .Include(c => c.WorkItem)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            return MapToDto(created!);
        }

        public async Task<List<CommentResponseDto>> GetCommentsByWorkItemAsync(int workItemId, bool isProductManager)
        {
            var query = _commentRepo.Query()
                .Include(c => c.User)
                .Include(c => c.WorkItem)
                .Where(c => c.WorkItemId == workItemId);

            // Employee — internal comments பார்க்க முடியாது
            if (!isProductManager)
                query = query.Where(c => !c.IsInternal);

            var comments = await query
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapToDto).ToList();
        }

        private static CommentResponseDto MapToDto(Comment c)
        {
            return new CommentResponseDto
            {
                Id = c.Id,
                Message = c.Message,
                IsInternal = c.IsInternal,
                PostedBy = c.User?.Name ?? string.Empty,
                WorkItemTitle = c.WorkItem?.Title ?? string.Empty,
                CreatedAt = c.CreatedAt
            };
        }
    }
}