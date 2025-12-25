namespace Webgiasu.Models
{
    public enum ProblemType
    {
        Toan,
        Ly,
        Hoa,
        Sinh,
        Van,
        Anh,
        Su,
        Dia
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        options
    }

    public enum ProblemStatus
    {
        WaitingForTutor,
        InProgress,
        Solved,
        Cancelled
    }

    public class Problem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProblemType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime Deadline { get; set; }
        public ProblemStatus Status { get; set; }
        public int? AssignedTutorId { get; set; }
        public decimal Price { get; set; }

        public ICollection<ProblemGroup>? Groups { get; set; }
    }
}
