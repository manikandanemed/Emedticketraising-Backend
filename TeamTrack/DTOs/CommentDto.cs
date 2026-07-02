namespace TeamTrack.DTOs
{
    public class CreateCommentRequestDto
    {
        public int WorkItemId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public string PostedBy { get; set; } = string.Empty;
        public string WorkItemTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}