using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerPro.Models
{
    [Table("TaskItems")]
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime Deadline { get; set; } = DateTime.Now;

        public int Progress { get; set; }

        public string AssignedTo { get; set; }
    }
}