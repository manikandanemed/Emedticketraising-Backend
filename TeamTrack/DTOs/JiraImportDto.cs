namespace TeamTrack.DTOs
{
    public class JiraImportResponseDto
    {
        public int ProjectsImported { get; set; }
        public int UsersImported { get; set; }
        public int WorkItemsImported { get; set; }
        public int BugsImported { get; set; }
        public List<string> Warnings { get; set; } = [];
        public List<string> Errors { get; set; } = [];
    }
}
