using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IJiraImportService
    {
        Task<JiraImportResponseDto> ImportJiraCsvAsync(Stream csvStream, int currentUserId);
    }
}
